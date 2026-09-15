using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Controllers;

[Route("Yapimlar")]
public class ProductionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var productions = await _context.Productions
            .Where(p => p.IsPublished)
            .Include(p => p.EventSchedules)
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Year)
            .ToListAsync();

        return View(productions);
    }

    [HttpGet("{slug}")]
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
