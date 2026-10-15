using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Çalışanın bir etkinliğin bütün ayrıntılarını gördüğü sayfa.
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly TourCalendarService _calendar;

    public EventsController(ApplicationDbContext context, TourCalendarService calendar)
    {
        _context = context;
        _calendar = calendar;
    }

    // GET: /Events/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var current = await _context.EventSchedules
            .Include(e => e.Production).ThenInclude(p => p!.CastMembers)
            .FirstOrDefaultAsync(e => e.Id == id);

        // Şehri girilmemiş seanslar henüz taslaktır; ekibe açılmaz.
        if (current is null || !current.IsReady)
        {
            return NotFound();
        }

        var sameDay = await _context.EventSchedules
            .Include(e => e.Production)
            .Where(e => e.Id != current.Id
                && e.EventDate >= current.EventDate.Date
                && e.EventDate < current.EventDate.Date.AddDays(1)
                && e.City != null && e.City != "")
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return View(new EventDetailsViewModel
        {
            Event = current,
            SameDayEvents = sameDay,
            ProductionColors = await _calendar.ProductionColorsAsync()
        });
    }
}
