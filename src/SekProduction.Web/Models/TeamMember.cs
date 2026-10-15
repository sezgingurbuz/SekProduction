using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SekProduction.Web.Models;

// Şirketin ekip kaydı: portal hesabı olmayan kişiler de (oyuncu, teknik ekip) burada tutulur.
public class TeamMember
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad soyad girin."), StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pozisyon girin."), StringLength(120)]
    [Display(Name = "Pozisyon")]
    public string Title { get; set; } = string.Empty;

    [StringLength(150), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string? Email { get; set; }

    [StringLength(30)]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    // Kimlik fotokopisi App_Data altında saklanır; yalnızca yöneticiler görüntüleyebilir.
    [StringLength(80)]
    public string? IdDocumentFileName { get; set; }

    [StringLength(260)]
    public string? IdDocumentOriginalName { get; set; }

    public DateTime? IdDocumentUploadedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public bool HasIdDocument => !string.IsNullOrEmpty(IdDocumentFileName);
}
