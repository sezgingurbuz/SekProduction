using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Çalışanlar turne şehirlerindeki otelleri ve fiyatları buradan görür (salt okunur).
public class HotelsController : Controller
{
    private readonly ApplicationDbContext _context;

    public HotelsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Hotels?city=Ankara
    public async Task<IActionResult> Index(string? city)
    {
        var all = await _context.Hotels.Where(h => h.IsActive).ToListAsync();
        var shown = string.IsNullOrWhiteSpace(city)
            ? all
            : all.Where(h => CityDirectory.TurkishComparer.Equals(h.City, city)).ToList();

        return View(new HotelListViewModel
        {
            CityGroups = shown
                .GroupBy(h => h.City)
                .OrderBy(g => g.Key, CityDirectory.TurkishComparer)
                .ToList(),
            Cities = CityDirectory.Sorted(all.Select(h => h.City)),
            SelectedCity = city,
            TotalCount = shown.Count
        });
    }
}
