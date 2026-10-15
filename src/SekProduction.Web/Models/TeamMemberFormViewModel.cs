using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class TeamMemberFormViewModel
{
    public int? Id { get; set; }

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

    [Display(Name = "Kimlik Fotokopisi")]
    public IFormFile? IdDocument { get; set; }

    [Display(Name = "Yüklü kimlik fotokopisini sil")]
    public bool RemoveIdDocument { get; set; }

    // Formda mevcut dosyayı göstermek için.
    public bool HasIdDocument { get; set; }
    public string? IdDocumentOriginalName { get; set; }
    public DateTime? IdDocumentUploadedAt { get; set; }

    public bool IsNew => Id is null;
}
