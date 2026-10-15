using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public enum EventNoteScope
{
    // Yalnızca yazan kişinin gördüğü not.
    [Display(Name = "Kişisel not")]
    Private = 0,

    // Bütün ekibin gördüğü not; sadece yöneticiler yazabilir.
    [Display(Name = "Ekip notu")]
    Team = 1
}

public class EventNote
{
    public int Id { get; set; }

    public int EventScheduleId { get; set; }
    public EventSchedule? EventSchedule { get; set; }

    // Hesap silinirse ekip notu kaybolmasın diye yazar bağlantısı boşa düşer, adı kayıtta kalır.
    public string? AuthorId { get; set; }
    public AppUser? Author { get; set; }

    [Required, StringLength(120)]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Not metni girin."), StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    [Display(Name = "Not")]
    public string Body { get; set; } = string.Empty;

    public EventNoteScope Scope { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
