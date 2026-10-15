using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Services;

// Şehir adları takvim, otel kataloğu ve filtrelerde aynı sırada ve aynı yazımla görünsün diye tek yerden gelir.
public class CityDirectory
{
    public static readonly StringComparer TurkishComparer = StringComparer.Create(new CultureInfo("tr-TR"), true);

    private readonly ApplicationDbContext _context;

    public CityDirectory(ApplicationDbContext context)
    {
        _context = context;
    }

    // Etkinlik girilmiş şehirler (takvim ve otel sayfasındaki filtreler bunu kullanır).
    // allowedProductionIds verilirse yalnızca o yapımların şehirleri döner.
    public async Task<List<string>> EventCitiesAsync(IReadOnlySet<int>? allowedProductionIds = null)
    {
        var query = _context.EventSchedules.Where(e => e.City != null && e.City != "");
        if (allowedProductionIds is not null)
        {
            query = query.Where(e => allowedProductionIds.Contains(e.ProductionId));
        }

        var cities = await query.Select(e => e.City!).Distinct().ToListAsync();
        return Sorted(cities);
    }

    public async Task<List<string>> HotelCitiesAsync()
    {
        var cities = await _context.Hotels
            .Where(h => h.IsActive)
            .Select(h => h.City)
            .Distinct()
            .ToListAsync();

        return Sorted(cities);
    }

    // Form önerileri: etkinliği veya oteli olan şehirler + Türkiye il listesi.
    public async Task<List<string>> SuggestionsAsync()
    {
        var cities = await EventCitiesAsync();
        var hotelCities = await HotelCitiesAsync();
        return Sorted(cities.Concat(hotelCities).Concat(TurkeyProvinces.Names));
    }

    public static List<string> Sorted(IEnumerable<string> cities) => cities
        .Select(c => c.Trim())
        .Where(c => c.Length > 0)
        .Distinct(TurkishComparer)
        .OrderBy(c => c, TurkishComparer)
        .ToList();
}
