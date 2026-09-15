using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class TourFilter
{
    public ProductionCategory? Category { get; set; }
    public int? ProductionId { get; set; }
    public string? City { get; set; }
    public string? View { get; set; }

    public bool IsListView => View == "list";
    public bool IsActive => Category is not null || ProductionId is not null || !string.IsNullOrEmpty(City);
}

public class TourSessionInput
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Yapım seçin.")]
    public int? ProductionId { get; set; }

    [Required(ErrorMessage = "Tarih girin.")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Saat girin.")]
    public TimeSpan? Time { get; set; }

    [Required(ErrorMessage = "Şehir girin.")]
    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(150)]
    public string? Venue { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Bilet linki http:// veya https:// ile başlayan geçerli bir adres olmalı.")]
    public string? TicketUrl { get; set; }
}

public record TourStop(string City, DateTime From, DateTime To, List<EventSchedule> Sessions);

public class TourCalendarViewModel
{
    public required DateTime Month { get; init; }
    public required DateTime GridStart { get; init; }
    public required int Weeks { get; init; }
    public required TourFilter Filter { get; init; }
    public required List<EventSchedule> Sessions { get; init; }
    public required List<Production> Productions { get; init; }
    public required Dictionary<int, string> ProductionColors { get; init; }
    public required List<string> Cities { get; init; }
    public required List<string> Venues { get; init; }

    public DateTime PreviousMonth => Month.AddMonths(-1);
    public DateTime NextMonth => Month.AddMonths(1);

    public List<EventSchedule> MonthSessions => Sessions
        .Where(s => s.EventDate.Year == Month.Year && s.EventDate.Month == Month.Month)
        .ToList();

    public IEnumerable<EventSchedule> SessionsOn(DateTime day) => Sessions.Where(s => s.EventDate.Date == day.Date);

    public string ColorFor(int productionId) => ProductionColors.GetValueOrDefault(productionId, "#6c757d");

    // Aynı gün farklı şehirlerde seans varsa ekip iki yerde birden olamaz; takvimde uyarı gösterilir.
    public HashSet<DateTime> ConflictDays => Sessions
        .Where(s => !string.IsNullOrWhiteSpace(s.City))
        .GroupBy(s => s.EventDate.Date)
        .Where(g => g.Select(s => s.City!.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"))).Distinct().Count() > 1)
        .Select(g => g.Key)
        .ToHashSet();

    // Ay içindeki seansları art arda aynı şehirde kalınan duraklara böler: Ankara → İstanbul → Ankara ...
    public List<TourStop> Route
    {
        get
        {
            var stops = new List<TourStop>();
            foreach (var session in MonthSessions)
            {
                var city = string.IsNullOrWhiteSpace(session.City) ? "Şehir belirtilmemiş" : session.City.Trim();
                var last = stops.LastOrDefault();
                if (last is not null && string.Equals(last.City, city, StringComparison.CurrentCultureIgnoreCase))
                {
                    last.Sessions.Add(session);
                    stops[^1] = last with { To = session.EventDate };
                }
                else
                {
                    stops.Add(new TourStop(city, session.EventDate, session.EventDate, new List<EventSchedule> { session }));
                }
            }

            return stops;
        }
    }
}
