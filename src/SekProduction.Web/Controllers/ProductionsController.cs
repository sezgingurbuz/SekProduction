using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Controllers;

public class ProductionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Yapımlar artık menüdeki başlıklar altında listeleniyor; eski tüm-yapımlar adresi anasayfaya gider.
    [HttpGet("Yapimlar")]
    public IActionResult Index()
    {
        return RedirectToActionPermanent("Index", "Home");
    }

    [HttpGet("{categorySlug:regex(" + ProductionCategoryRoutes.SlugPattern + ")}")]
    public async Task<IActionResult> Category(string categorySlug)
    {
        var category = ProductionCategoryRoutes.FromSlug(categorySlug);
        if (category is null)
        {
            return NotFound();
        }

        var productions = await _context.Productions
            .Where(p => p.IsPublished && p.Category == category)
            .Include(p => p.EventSchedules)
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Year)
            .ToListAsync();

        ViewData["Category"] = category.Value;
        return View(productions);
    }

    [HttpGet("Yapimlar/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var production = await _context.Productions
            .Include(p => p.EventSchedules)
            .Include(p => p.CastMembers)
            .Include(p => p.Photos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (production is null)
        {
            return NotFound();
        }

        return View(production);
    }
}
