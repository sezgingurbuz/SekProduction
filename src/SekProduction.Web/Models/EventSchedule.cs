using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class EventSchedule
{
    public int Id { get; set; }

    [Display(Name = "Yapım")]
    public int ProductionId { get; set; }
    public Production? Production { get; set; }

    [Required]
    [Display(Name = "Etkinlik Tarihi")]
    [DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; }

    [StringLength(150)]
    [Display(Name = "Mekan")]
    public string? Venue { get; set; }

    [StringLength(100)]
    [Display(Name = "Şehir")]
    public string? City { get; set; }

    [StringLength(500)]
    [Display(Name = "Bilet Linki")]
    [Url]
    public string? TicketUrl { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }
}
