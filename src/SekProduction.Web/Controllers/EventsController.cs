using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<AppUser> _userManager;
    private readonly ProductionScope _scope;

    public EventsController(
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

        // Çalışan görevli olmadığı yapımın etkinliğini açamaz.
        var allowed = await _scope.AllowedProductionIdsAsync(User);
        if (allowed is not null && !allowed.Contains(current.ProductionId))
        {
            return NotFound();
        }

        var sameDayQuery = _context.EventSchedules
            .Include(e => e.Production)
            .Where(e => e.Id != current.Id
                && e.EventDate >= current.EventDate.Date
                && e.EventDate < current.EventDate.Date.AddDays(1)
                && e.City != null && e.City != "");

        if (allowed is not null)
        {
            sameDayQuery = sameDayQuery.Where(e => allowed.Contains(e.ProductionId));
        }

        return View(new EventDetailsViewModel
        {
            Event = current,
            SameDayEvents = await sameDayQuery.OrderBy(e => e.EventDate).ToListAsync(),
            Hotels = await HotelsInCityAsync(current.City!),
            TeamNotes = await NotesAsync(current.Id, EventNoteScope.Team),
            MyNotes = await NotesAsync(current.Id, EventNoteScope.Private, _userManager.GetUserId(User)),
            ProductionColors = await _calendar.ProductionColorsAsync()
        });
    }

    private async Task<List<EventNote>> NotesAsync(int eventScheduleId, EventNoteScope scope, string? authorId = null)
    {
        var query = _context.EventNotes.Where(n => n.EventScheduleId == eventScheduleId && n.Scope == scope);
        if (authorId is not null)
        {
            query = query.Where(n => n.AuthorId == authorId);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    private async Task<List<Hotel>> HotelsInCityAsync(string city)
    {
        var hotels = await _context.Hotels.Where(h => h.IsActive).ToListAsync();
        return hotels
            .Where(h => CityDirectory.TurkishComparer.Equals(h.City, city))
            .OrderByDescending(h => h.IsPreferred)
            .ThenBy(h => h.Name, CityDirectory.TurkishComparer)
            .ToList();
    }
}
