using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class AnnouncementFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Başlık girin."), StringLength(150)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj girin."), StringLength(4000, ErrorMessage = "Mesaj en fazla 4000 karakter olabilir.")]
    [Display(Name = "Mesaj")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Bütün ekibe gönder")]
    public bool AudienceAll { get; set; } = true;

    [Display(Name = "Önemli olarak işaretle")]
    public bool IsPinned { get; set; }

    [Display(Name = "İlgili etkinlik")]
    public int? EventScheduleId { get; set; }

    // AudienceAll kapalıysa duyurunun gideceği kişiler.
    public List<string> RecipientIds { get; set; } = new();

    // Formu doldurmak için gereken seçenekler.
    public List<UserListItemViewModel> Users { get; set; } = new();
    public List<EventSchedule> UpcomingEvents { get; set; } = new();

    public bool IsNew => Id is null;
}

public class AnnouncementDetailsViewModel
{
    public required Announcement Announcement { get; init; }
    public required List<AnnouncementRecipient> Recipients { get; init; }

    public List<AnnouncementRecipient> ReadBy => Recipients.Where(r => r.IsRead).ToList();
    public List<AnnouncementRecipient> NotReadBy => Recipients.Where(r => !r.IsRead).ToList();
}

// Çalışanın gelen kutusundaki bir satır.
public class InboxItemViewModel
{
    public required Announcement Announcement { get; init; }
    public required DateTime? ReadAt { get; init; }

    public bool IsRead => ReadAt is not null;
}
