using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Çalışan portalının giriş sayfası.
public class HomeController : Controller
{
    private const int UpcomingEventLimit = 15;
    private const int UnreadAnnouncementLimit = 5;

    private readonly ApplicationDbContext _context;
    private readonly TourCalendarService _calendar;
    private readonly UserManager<AppUser> _userManager;
    private readonly ProductionScope _scope;

    public HomeController(
        ApplicationDbContext context,
        TourCalendarService calendar,
        UserManager<AppUser> userManager,
        ProductionScope scope)
    {
        _context = context;
        _calendar = calendar;
        _userManager = userManager;
        _scope = scope;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        // Şehri girilmemiş seanslar henüz taslaktır; ekibe gösterilmez.
        var ready = _context.EventSchedules
            .Include(e => e.Production)
            .Where(e => e.City != null && e.City != "");

        // Çalışan yalnızca görevli olduğu yapımların etkinliklerini görür.
        var allowed = await _scope.AllowedProductionIdsAsync(User);
        if (allowed is not null)
        {
            ready = ready.Where(e => allowed.Contains(e.ProductionId));
        }

        return View(new PortalDashboardViewModel
        {
            TodayEvents = await ready
                .Where(e => e.EventDate >= today && e.EventDate < tomorrow)
                .OrderBy(e => e.EventDate)
                .ToListAsync(),
            UpcomingEvents = await ready
                .Where(e => e.EventDate >= tomorrow)
                .OrderBy(e => e.EventDate)
                .Take(UpcomingEventLimit)
                .ToListAsync(),
            MonthEventCount = await ready.CountAsync(e => e.EventDate >= monthStart && e.EventDate < monthEnd),
            UnreadAnnouncements = await UnreadAnnouncementsAsync(),
            AssignedProductionTitles = allowed is null
                ? new List<string>()
                : await _context.Productions.Where(p => allowed.Contains(p.Id)).OrderBy(p => p.Title).Select(p => p.Title).ToListAsync(),
            ProductionColors = await _calendar.ProductionColorsAsync()
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<List<InboxItemViewModel>> UnreadAnnouncementsAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return new List<InboxItemViewModel>();
        }

        var rows = await _context.AnnouncementRecipients
            .Include(r => r.Announcement)
            .Where(r => r.UserId == userId && r.ReadAt == null)
            .OrderByDescending(r => r.Announcement!.IsPinned)
            .ThenByDescending(r => r.Announcement!.CreatedAt)
            .Take(UnreadAnnouncementLimit)
            .ToListAsync();

        return rows
            .Where(r => r.Announcement is not null)
            .Select(r => new InboxItemViewModel { Announcement = r.Announcement!, ReadAt = r.ReadAt })
            .ToList();
    }
}
