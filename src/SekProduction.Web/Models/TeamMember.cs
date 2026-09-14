using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class TeamMember
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Pozisyon")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Biyografi")]
    public string? Bio { get; set; }

    [Display(Name = "Fotoğraf URL")]
    public string? PhotoUrl { get; set; }

    [StringLength(150), EmailAddress]
    [Display(Name = "E-posta")]
    public string? Email { get; set; }

    [Display(Name = "LinkedIn URL")]
    public string? LinkedInUrl { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Yayında")]
    public bool IsPublished { get; set; } = true;
}
