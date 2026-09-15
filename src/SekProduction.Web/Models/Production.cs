using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace SekProduction.Web.Models;

public enum ProductionCategory
{
    [Display(Name = "Chaplin Sanat", Description = "Yetişkinler için sahnelediğimiz tiyatro oyunları")]
    ChaplinSanat = 0,
    [Display(Name = "Chaplin Çocuk Sanat", Description = "Çocuklar ve aileler için tiyatro oyunları")]
    ChaplinCocukSanat = 1,
    [Display(Name = "Sek Production", Description = "Konserler ve diğer etkinlikler")]
    SekProduction = 2
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

    [StringLength(30)]
    [Display(Name = "Yaş Sınırı")]
    public string? AgeLimit { get; set; }

    [Range(1, 600)]
    [Display(Name = "Süre (dakika)")]
    public int? DurationMinutes { get; set; }

    [Range(1, 10)]
    [Display(Name = "Perde")]
    public int? ActCount { get; set; }

    [Display(Name = "Oyunun Kadrosu")]
    public string? Credits { get; set; }

    [StringLength(300)]
    [Url]
    [Display(Name = "Fragman (YouTube linki)")]
    public string? TrailerUrl { get; set; }

    [Display(Name = "Öne Çıkan")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Yayında")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EventSchedule> EventSchedules { get; set; } = new List<EventSchedule>();
    public ICollection<CastMember> CastMembers { get; set; } = new List<CastMember>();
    public ICollection<ProductionPhoto> Photos { get; set; } = new List<ProductionPhoto>();

    [NotMapped]
    public string? DurationText => DurationMinutes switch
    {
        null => null,
        < 60 => $"{DurationMinutes} dk",
        _ when DurationMinutes % 60 == 0 => $"{DurationMinutes / 60} sa",
        _ => $"{DurationMinutes / 60} sa {DurationMinutes % 60} dk"
    };

    [NotMapped]
    public string? TrailerYouTubeId => GetYouTubeId(TrailerUrl);

    public static string? GetYouTubeId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;

        if (host == "youtu.be" && segments.Length > 0)
        {
            id = segments[0];
        }
        else if (host.EndsWith("youtube.com", StringComparison.Ordinal))
        {
            if (segments.Length >= 2 && (segments[0] == "embed" || segments[0] == "shorts" || segments[0] == "live"))
            {
                id = segments[1];
            }
            else
            {
                var match = Regex.Match(uri.Query, @"[?&]v=([^&]+)");
                id = match.Success ? match.Groups[1].Value : null;
            }
        }

        return id is not null && Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$") ? id : null;
    }
}
