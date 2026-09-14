using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel
        {
            FeaturedProductions = await _context.Productions
                .Where(p => p.IsPublished && p.IsFeatured)
                .Include(p => p.EventSchedules)
                .OrderBy(p => p.DisplayOrder)
                .Take(6)
                .ToListAsync(),
            LatestNews = await _context.NewsPosts
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedAt)
                .Take(3)
                .ToListAsync(),
            ClientReferences = await _context.ClientReferences
                .Where(c => c.IsPublished)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
