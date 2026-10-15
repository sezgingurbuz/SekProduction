using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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

    private readonly ApplicationDbContext _context;
    private readonly TourCalendarService _calendar;

    public HomeController(ApplicationDbContext context, TourCalendarService calendar)
    {
        _context = context;
        _calendar = calendar;
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
            ProductionColors = await _calendar.ProductionColorsAsync()
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
