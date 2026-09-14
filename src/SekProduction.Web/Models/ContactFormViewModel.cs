using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class ContactFormViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(150)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Konu zorunludur.")]
    [StringLength(150)]
    [Display(Name = "Konu")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj zorunludur.")]
    [StringLength(2000)]
    [Display(Name = "Mesaj")]
    public string Message { get; set; } = string.Empty;
}
