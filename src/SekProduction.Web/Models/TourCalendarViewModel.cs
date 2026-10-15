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

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(150)]
    public string? Venue { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Bilet linki http:// veya https:// ile başlayan geçerli bir adres olmalı.")]
    public string? TicketUrl { get; set; }
}

// Takvimde sürükleyip bırakılan ama henüz kaydedilmemiş seans taşımaları.
public class SessionMoveInput
{
    public int Id { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }
}

public static class TourReturn
{
    public const string Tour = "tour";
    public const string Production = "production";
}

public record TourStop(string City, DateTime From, DateTime To, List<EventSchedule> Sessions);

public class TourCalendarViewModel
{
    private static readonly StringComparer TurkishComparer = StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true);

    public required DateTime Month { get; init; }
    public required DateTime GridStart { get; init; }
    public required int Weeks { get; init; }
    public required TourFilter Filter { get; init; }

    // Dolu ise sayfa tek bir yapımın Etkinlik Günleri takvimidir (filtreler gizlenir, yapım sabittir).
    public Production? FixedProduction { get; init; }
    public required List<EventSchedule> Sessions { get; init; }
    public required List<EventSchedule> UpcomingSessions { get; init; }
    public required List<Production> Productions { get; init; }
    public required Dictionary<int, string> ProductionColors { get; init; }
    public required List<string> Cities { get; init; }
    public required List<string> CitySuggestions { get; init; }
    public required List<string> Venues { get; init; }

    public bool IsProductionMode => FixedProduction is not null;

    // Kaydetme sonrası kullanıcının döneceği sayfa (TourController bu değere göre yönlendirir).
    public string ReturnTo => IsProductionMode ? TourReturn.Production : TourReturn.Tour;

    public DateTime PreviousMonth => Month.AddMonths(-1);
    public DateTime NextMonth => Month.AddMonths(1);

    public List<EventSchedule> MonthSessions => Sessions
        .Where(s => s.EventDate.Year == Month.Year && s.EventDate.Month == Month.Month)
        .ToList();

    public IEnumerable<EventSchedule> SessionsOn(DateTime day) => Sessions.Where(s => s.EventDate.Date == day.Date);

    public string ColorFor(int productionId) => ProductionColors.GetValueOrDefault(productionId, "#6c757d");

    // Çakışma, fiziksel olarak mümkün olmayan durumdur. Kurallar:
    //  1) Aynı yapım aynı gün birden fazla şehirde olamaz (ekip iki yerde birden olamaz).
    //  2) Aynı yapımın aynı gün aynı saatte iki seansı olamaz.
    //  3) Aynı salonda aynı gün aynı saatte iki seans olamaz.
    // Farklı yapımların aynı gün farklı şehirlerde oynaması normaldir, çakışma sayılmaz.
    public Dictionary<DateTime, List<string>> ConflictsByDay
    {
        get
        {
            var result = new Dictionary<DateTime, List<string>>();

            void Add(DateTime day, string reason)
            {
                if (!result.TryGetValue(day, out var reasons))
                {
                    result[day] = reasons = new List<string>();
                }
                if (!reasons.Contains(reason))
                {
                    reasons.Add(reason);
                }
            }

            foreach (var day in Sessions.GroupBy(s => s.EventDate.Date))
            {
                foreach (var production in day.GroupBy(s => s.ProductionId))
                {
                    var title = production.First().Production?.Title ?? "Bu yapım";

                    var cities = production
                        .Where(s => !string.IsNullOrWhiteSpace(s.City))
                        .Select(s => s.City!.Trim())
                        .Distinct(TurkishComparer)
                        .ToList();
                    if (cities.Count > 1)
                    {
                        Add(day.Key, $"{title} aynı gün {cities.Count} şehirde: {string.Join(", ", cities)}");
                    }

                    foreach (var slot in production.GroupBy(s => s.EventDate.TimeOfDay).Where(g => g.Count() > 1))
                    {
                        Add(day.Key, $"{title} saat {slot.Key:hh\\:mm} için {slot.Count()} kez girilmiş");
                    }
                }

                foreach (var slot in day
                    .Where(s => !string.IsNullOrWhiteSpace(s.Venue))
                    .GroupBy(s => (Venue: Normalize(s.Venue), City: Normalize(s.City), s.EventDate.TimeOfDay))
                    .Where(g => g.Count() > 1))
                {
                    var sample = slot.First();
                    var place = string.IsNullOrWhiteSpace(sample.City) ? sample.Venue!.Trim() : $"{sample.Venue!.Trim()} ({sample.City!.Trim()})";
                    Add(day.Key, $"{place} salonunda saat {sample.EventDate:HH\\:mm} için {slot.Count()} seans var");
                }
            }

            return result;
        }
    }

    public HashSet<DateTime> ConflictDays => ConflictsByDay.Keys.ToHashSet();

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));

    // Ay içindeki seansları art arda aynı şehirde kalınan duraklara böler: Ankara → İstanbul → Ankara ...
    public List<TourStop> Route
    {
        get
        {
            var stops = new List<TourStop>();
            foreach (var session in MonthSessions.Where(s => s.IsReady))
            {
                var city = session.City!.Trim();
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
