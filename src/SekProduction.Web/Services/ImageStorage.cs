namespace SekProduction.Web.Services;

public class ImageStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private const string UploadRoot = "uploads";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IWebHostEnvironment _environment;

    public ImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string? Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "Sadece JPG, PNG veya WEBP formatında görsel yükleyebilirsiniz.";
        }
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            return "Görsel boyutu en fazla 5 MB olabilir.";
        }
        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var directory = Path.Combine(_environment.WebRootPath, UploadRoot, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using (var stream = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        return $"/{UploadRoot}/{folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith($"/{UploadRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var root = Path.GetFullPath(Path.Combine(_environment.WebRootPath, UploadRoot));
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, url.TrimStart('/')));
        if (fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
