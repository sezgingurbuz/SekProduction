using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SekProduction.Web.Models;

// Portalı kullanan şirket çalışanı. Hesaplar yalnızca yönetim panelinden açılır.
public class AppUser : IdentityUser
{
    [Required, StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Görev")]
    public string? JobTitle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Pasife alınan hesap Identity'nin kilit mekanizmasıyla engellenir; ayrı bir bayrak tutulmaz.
    [NotMapped]
    public bool IsActive => LockoutEnd is null || LockoutEnd <= DateTimeOffset.UtcNow;

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? (UserName ?? string.Empty) : FullName;
}
