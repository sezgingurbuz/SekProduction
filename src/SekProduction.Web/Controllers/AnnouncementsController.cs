using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Controllers;

// Çalışanın kendisine gönderilen duyuruları okuduğu gelen kutusu.
public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AnnouncementsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Announcements
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var rows = await _context.AnnouncementRecipients
            .Include(r => r.Announcement).ThenInclude(a => a!.EventSchedule).ThenInclude(e => e!.Production)
            .Where(r => r.UserId == userId)
            .ToListAsync();

        var items = rows
            .Where(r => r.Announcement is not null)
            .Select(r => new InboxItemViewModel { Announcement = r.Announcement!, ReadAt = r.ReadAt })
            .OrderBy(i => i.IsRead)
            .ThenByDescending(i => i.Announcement.IsPinned)
            .ThenByDescending(i => i.Announcement.CreatedAt)
            .ToList();

        return View(items);
    }

    // GET: /Announcements/Details/5 — açıldığında duyuru okundu olarak işaretlenir.
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        var row = await _context.AnnouncementRecipients
            .Include(r => r.Announcement).ThenInclude(a => a!.EventSchedule).ThenInclude(e => e!.Production)
            .FirstOrDefaultAsync(r => r.AnnouncementId == id && r.UserId == userId);

        if (row?.Announcement is null)
        {
            return NotFound();
        }

        if (row.ReadAt is null)
        {
            row.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return View(new InboxItemViewModel { Announcement = row.Announcement, ReadAt = row.ReadAt });
    }

    // POST: /Announcements/MarkAllRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User);
        var unread = await _context.AnnouncementRecipients
            .Where(r => r.UserId == userId && r.ReadAt == null)
            .ToListAsync();

        foreach (var row in unread)
        {
            row.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = unread.Count == 0
            ? "Okunmamış duyuru yoktu."
            : $"{unread.Count} duyuru okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }
}
