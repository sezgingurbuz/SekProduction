using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Konu")]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    [Display(Name = "Mesaj")]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Okundu")]
    public bool IsRead { get; set; }
}
