using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;

namespace SekProduction.Web.Controllers;

[Route("Haberler")]
public class NewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var posts = await _context.NewsPosts
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.PublishedAt)
            .ToListAsync();

        return View(posts);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _context.NewsPosts
            .FirstOrDefaultAsync(n => n.Slug == slug && n.IsPublished);

        if (post is null)
        {
            return NotFound();
        }

        return View(post);
    }
}
