using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Services;

// Turne Takvimi ve yapımın Etkinlik Günleri sayfası aynı takvimi kullanır.
public class TourCalendarService
{
    private static readonly string[] Palette =
    {
        "#2563eb", "#dc2626", "#059669", "#d97706", "#7c3aed",
        "#db2777", "#0891b2", "#65a30d", "#9333ea", "#ea580c"
    };

    private static readonly StringComparer TurkishComparer = CityDirectory.TurkishComparer;

    private readonly ApplicationDbContext _context;
    private readonly CityDirectory _cities;

    public TourCalendarService(ApplicationDbContext context, CityDirectory cities)
    {
        _context = context;
        _cities = cities;
    }

    // readyOnly: şehri girilmemiş taslak seanslar ekibe gösterilmez.
    public async Task<TourCalendarViewModel> BuildAsync(int? year, int? month, TourFilter filter, Production? fixedProduction = null, bool readyOnly = false)
    {
        if (fixedProduction is not null)
        {
            filter = new TourFilter { ProductionId = fixedProduction.Id, View = filter.View };
        }

        var today = DateTime.Today;
        var first = year is >= 2000 and <= 2100 && month is >= 1 and <= 12
            ? new DateTime(year.Value, month.Value, 1)
            : new DateTime(today.Year, today.Month, 1);

        // Takvim pazartesiden başlar; ayın ilk gününden önceki günler önceki aydan doldurulur.
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var gridStart = first.AddDays(-offset);
        var weeks = (int)Math.Ceiling((offset + DateTime.DaysInMonth(first.Year, first.Month)) / 7.0);
        var gridEnd = gridStart.AddDays(weeks * 7);

        var productions = await _context.Productions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.DisplayOrder)
            .ThenBy(p => p.Title)
            .ToListAsync();

        var sessions = await Filtered(filter, readyOnly)
            .Where(e => e.EventDate >= gridStart && e.EventDate < gridEnd)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        // Haritanın "Yaklaşan tümü" görünümü ve yapım sayfasının yan paneli için
        var upcoming = await Filtered(filter, readyOnly)
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var cities = await _cities.EventCitiesAsync();
        var venues = await _context.EventSchedules
            .Where(e => e.Venue != null && e.Venue != "")
            .Select(e => e.Venue!)
            .Distinct()
            .ToListAsync();

        return new TourCalendarViewModel
        {
            Month = first,
            GridStart = gridStart,
            Weeks = weeks,
            Filter = filter,
            FixedProduction = fixedProduction,
            Sessions = sessions,
            UpcomingSessions = upcoming,
            Productions = productions,
            ProductionColors = ColorMap(productions.OrderBy(p => p.Id).Select(p => p.Id)),
            Cities = cities,
            CitySuggestions = await _cities.SuggestionsAsync(),
            Venues = venues.OrderBy(v => v, TurkishComparer).ToList()
        };
    }

    // Yapım renkleri takvim dışındaki listelerde de aynı olsun diye tek yerden üretilir.
    public async Task<Dictionary<int, string>> ProductionColorsAsync()
    {
        var ids = await _context.Productions.OrderBy(p => p.Id).Select(p => p.Id).ToListAsync();
        return ColorMap(ids);
    }

    private static Dictionary<int, string> ColorMap(IEnumerable<int> productionIdsByAge) => productionIdsByAge
        .Select((id, index) => (id, Color: Palette[index % Palette.Length]))
        .ToDictionary(x => x.id, x => x.Color);

    private IQueryable<EventSchedule> Filtered(TourFilter filter, bool readyOnly)
    {
        var query = _context.EventSchedules.Include(e => e.Production).AsQueryable();

        if (readyOnly)
        {
            query = query.Where(e => e.City != null && e.City != "");
        }

        if (filter.Category is not null)
        {
            query = query.Where(e => e.Production!.Category == filter.Category);
        }
        if (filter.ProductionId is not null)
        {
            query = query.Where(e => e.ProductionId == filter.ProductionId);
        }
        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            query = query.Where(e => e.City == filter.City);
        }

        return query;
    }
}
