using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class NewsPost
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(170)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Özet")]
    public string? Summary { get; set; }

    [Display(Name = "İçerik")]
    public string? Content { get; set; }

    [Display(Name = "Kapak Görseli URL")]
    public string? CoverImageUrl { get; set; }

    [StringLength(100)]
    [Display(Name = "Yazar")]
    public string? AuthorName { get; set; }

    [Display(Name = "Yayın Tarihi")]
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Yayında")]
    public bool IsPublished { get; set; } = true;
}
