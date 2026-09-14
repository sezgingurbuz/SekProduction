using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;

namespace SekProduction.Web.Controllers;

[Route("Ekip")]
public class TeamController : Controller
{
    private readonly ApplicationDbContext _context;

    public TeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var members = await _context.TeamMembers
            .Where(m => m.IsPublished)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        return View(members);
    }
}
