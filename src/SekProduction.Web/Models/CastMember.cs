using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class CastMember
{
    public int Id { get; set; }

    public int ProductionId { get; set; }
    public Production? Production { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Rol")]
    public string? RoleName { get; set; }

    [Display(Name = "Fotoğraf")]
    public string? PhotoUrl { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }
}
