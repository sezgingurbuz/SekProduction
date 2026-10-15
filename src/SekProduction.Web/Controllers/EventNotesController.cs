using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Etkinlik notları: çalışanlar kendilerine kişisel not tutar, yöneticiler ekibe not bırakır.
public class EventNotesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ProductionScope _scope;

    public EventNotesController(ApplicationDbContext context, UserManager<AppUser> userManager, ProductionScope scope)
    {
        _context = context;
        _userManager = userManager;
        _scope = scope;
    }

    // POST: /EventNotes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventScheduleId, string? body, EventNoteScope scope)
    {
        var schedule = await _context.EventSchedules.FindAsync(eventScheduleId);
        if (schedule is null || !schedule.IsReady)
        {
            return NotFound();
        }

        if (scope == EventNoteScope.Team && !User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        // Görevli olmadığı yapımın etkinliğine not bırakılamaz.
        var allowed = await _scope.AllowedProductionIdsAsync(User);
        if (allowed is not null && !allowed.Contains(schedule.ProductionId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["StatusError"] = "Not metni boş olamaz.";
            return BackToEvent(eventScheduleId);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Forbid();
        }

        _context.EventNotes.Add(new EventNote
        {
            EventScheduleId = eventScheduleId,
            AuthorId = user.Id,
            AuthorName = user.DisplayName,
            Body = body.Trim(),
            Scope = scope
        });
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = scope == EventNoteScope.Team ? "Ekip notu eklendi." : "Kişisel notunuz kaydedildi.";
        return BackToEvent(eventScheduleId);
    }

    // POST: /EventNotes/Update/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? body)
    {
        var note = await _context.EventNotes.FindAsync(id);
        if (note is null)
        {
            return NotFound();
        }
        if (!CanModify(note))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["StatusError"] = "Not metni boş olamaz.";
            return BackToEvent(note.EventScheduleId);
        }

        note.Body = body.Trim();
        note.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Not güncellendi.";
        return BackToEvent(note.EventScheduleId);
    }

    // POST: /EventNotes/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var note = await _context.EventNotes.FindAsync(id);
        if (note is null)
        {
            return NotFound();
        }
        if (!CanModify(note))
        {
            return Forbid();
        }

        _context.EventNotes.Remove(note);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Not silindi.";
        return BackToEvent(note.EventScheduleId);
    }

    // Kişisel notu yalnızca sahibi, ekip notunu yöneticiler düzenler.
    private bool CanModify(EventNote note)
    {
        if (note.Scope == EventNoteScope.Team)
        {
            return User.IsInRole(AppRoles.Admin);
        }

        return note.AuthorId == _userManager.GetUserId(User);
    }

    private IActionResult BackToEvent(int eventScheduleId) =>
        RedirectToAction("Details", "Events", new { id = eventScheduleId });
}
