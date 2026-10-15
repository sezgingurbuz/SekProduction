using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.ViewComponents;

// Menüdeki okunmamış duyuru sayısı.
public class UnreadAnnouncementsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UnreadAnnouncementsViewComponent(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId is null)
        {
            return View(0);
        }

        var count = await _context.AnnouncementRecipients
            .CountAsync(r => r.UserId == userId && r.ReadAt == null);

        return View(count);
    }
}
