using System.Text.RegularExpressions;

namespace SekProduction.Web.Services;

// Kimlik fotokopisi gibi herkese açık olmaması gereken dosyalar için.
// wwwroot altına değil, uygulama kökündeki App_Data klasörüne yazar; bu yüzden
// dosyalara doğrudan URL ile erişilemez, yalnızca yetkili bir action üzerinden indirilir.
public class PrivateFileStorage
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const string StorageRoot = "App_Data";

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
    private static readonly Regex StoredNamePattern = new("^[0-9a-f]{32}\\.(jpg|jpeg|png|webp|pdf)$", RegexOptions.Compiled);

    private readonly IWebHostEnvironment _environment;

    public PrivateFileStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string? Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return "Sadece JPG, PNG, WEBP veya PDF dosyası yükleyebilirsiniz.";
        }
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            return "Dosya boyutu en fazla 10 MB olabilir.";
        }

        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_environment.ContentRootPath, StorageRoot, folder));
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

        await using var stream = new FileStream(Path.Combine(directory.FullName, storedName), FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return storedName;
    }

    public FileStream? Open(string folder, string? storedName)
    {
        var path = ResolvePath(folder, storedName);
        return path is not null && File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public void Delete(string folder, string? storedName)
    {
        var path = ResolvePath(folder, storedName);
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static string ContentTypeFor(string? storedName) => Path.GetExtension(storedName ?? string.Empty).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "image/jpeg"
    };

    // Dosya adı yalnızca kendi ürettiğimiz biçimde olabilir; böylece ".." gibi yollarla
    // klasör dışına çıkılamaz.
    private string? ResolvePath(string folder, string? storedName)
    {
        if (string.IsNullOrEmpty(storedName) || !StoredNamePattern.IsMatch(storedName))
        {
            return null;
        }

        return Path.Combine(_environment.ContentRootPath, StorageRoot, folder, storedName);
    }
}
