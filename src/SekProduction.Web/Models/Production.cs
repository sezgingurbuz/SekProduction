using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public enum ProductionCategory
{
    [Display(Name = "Tiyatro Oyunu")]
    Theatre,
    [Display(Name = "Konser")]
    Concert,
    [Display(Name = "Müzik Videosu")]
    MusicVideo,
    [Display(Name = "Sanatçı Yönetimi")]
    ArtistManagement,
    [Display(Name = "Diğer")]
    Other
}

public class Production
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(170)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Kısa Açıklama")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Kapak Görseli URL")]
    public string? CoverImageUrl { get; set; }

    [Display(Name = "Kategori")]
    public ProductionCategory Category { get; set; }

    [Display(Name = "Yıl")]
    [Range(1900, 2100)]
    public int? Year { get; set; }

    [StringLength(150)]
    [Display(Name = "Yönetmen / Sanatçı")]
    public string? ClientName { get; set; }

    [Display(Name = "Öne Çıkan")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Yayında")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EventSchedule> EventSchedules { get; set; } = new List<EventSchedule>();
}
