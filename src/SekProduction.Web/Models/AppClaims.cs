using System.Security.Claims;

namespace SekProduction.Web.Models;

public static class AppClaims
{
    public const string FullName = "sek:full_name";

    // Her sayfada veritabanına gitmemek için ad soyad giriş sırasında talep olarak yazılır.
    public static string DisplayName(this ClaimsPrincipal user)
    {
        var fullName = user.FindFirst(FullName)?.Value;
        return string.IsNullOrWhiteSpace(fullName) ? user.Identity?.Name ?? string.Empty : fullName;
    }
}
