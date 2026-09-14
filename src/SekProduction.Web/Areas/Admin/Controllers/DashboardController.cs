using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;

namespace SekProduction.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SeedData.AdminRole)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ProductionCount"] = await _context.Productions.CountAsync();
        ViewData["TeamMemberCount"] = await _context.TeamMembers.CountAsync();
        ViewData["NewsPostCount"] = await _context.NewsPosts.CountAsync();
        ViewData["ClientReferenceCount"] = await _context.ClientReferences.CountAsync();
        ViewData["UnreadMessageCount"] = await _context.ContactMessages.CountAsync(m => !m.IsRead);
        return View();
    }
}
