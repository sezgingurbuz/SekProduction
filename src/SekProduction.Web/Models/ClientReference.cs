using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class ClientReference
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Müşteri Adı")]
    public string ClientName { get; set; } = string.Empty;

    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [Display(Name = "Web Sitesi")]
    public string? WebsiteUrl { get; set; }

    [StringLength(300)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Yayında")]
    public bool IsPublished { get; set; } = true;
}
