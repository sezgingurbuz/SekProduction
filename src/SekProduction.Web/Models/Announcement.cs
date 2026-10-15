using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SekProduction.Web.Models;

// Yöneticinin çalışanlara gönderdiği duyuru. Tek yönlüdür; yanıt yerine etkinlik notları kullanılır.
public class Announcement
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık girin."), StringLength(150)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj girin."), StringLength(4000, ErrorMessage = "Mesaj en fazla 4000 karakter olabilir.")]
    [Display(Name = "Mesaj")]
    public string Body { get; set; } = string.Empty;

    // Hesap silinirse duyuru kalır; gönderenin adı kayıtta saklanır.
    public string? AuthorId { get; set; }
    public AppUser? Author { get; set; }

    [Required, StringLength(120)]
    public string AuthorName { get; set; } = string.Empty;

    [Display(Name = "Bütün ekibe gönder")]
    public bool AudienceAll { get; set; } = true;

    [Display(Name = "Önemli olarak işaretle")]
    public bool IsPinned { get; set; }

    [Display(Name = "İlgili etkinlik")]
    public int? EventScheduleId { get; set; }
    public EventSchedule? EventSchedule { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AnnouncementRecipient> Recipients { get; set; } = new List<AnnouncementRecipient>();

    [NotMapped]
    public int ReadCount => Recipients.Count(r => r.ReadAt is not null);

    [NotMapped]
    public string Preview => Body.Length <= 160 ? Body : Body[..160] + "…";
}

// Duyurunun bir çalışana ulaşması ve okuma durumu.
public class AnnouncementRecipient
{
    public int Id { get; set; }

    public int AnnouncementId { get; set; }
    public Announcement? Announcement { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    public DateTime? ReadAt { get; set; }

    [NotMapped]
    public bool IsRead => ReadAt is not null;
}
