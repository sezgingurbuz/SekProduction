using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Çalışanlar şehirlerdeki otelleri ve fiyatları buradan görür.
// Sayfa bütün otelleri gönderir, şehir seçimi haritadan veya aramadan tarayıcıda yapılır.
public class HotelsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ProductionScope _scope;

    public HotelsController(ApplicationDbContext context, ProductionScope scope)
    {
        _context = context;
        _scope = scope;
    }

    // GET: /Hotels?city=Ankara
    public async Task<IActionResult> Index(string? city)
    {
        var hotels = await _context.Hotels
            .Where(h => h.IsActive)
            .ToListAsync();

        return View(new HotelListViewModel
        {
            CityGroups = hotels
                .GroupBy(h => h.City)
                .OrderBy(g => g.Key, CityDirectory.TurkishComparer)
                .ToList(),
            SelectedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            TotalCount = hotels.Count,
            UpcomingEventCountByCity = await UpcomingEventCountByCityAsync()
        });
    }

    private async Task<Dictionary<string, int>> UpcomingEventCountByCityAsync()
    {
        var query = _context.EventSchedules
            .Where(e => e.EventDate >= DateTime.Today && e.City != null && e.City != "");

        // Çalışan yalnızca görevli olduğu yapımların etkinliklerini sayar.
        var allowed = await _scope.AllowedProductionIdsAsync(User);
        if (allowed is not null)
        {
            query = query.Where(e => allowed.Contains(e.ProductionId));
        }

        var rows = await query
            .GroupBy(e => e.City!)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.City, r => r.Count, CityDirectory.TurkishComparer);
    }
}
