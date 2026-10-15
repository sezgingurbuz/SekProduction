using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers;

// Yöneticinin çalışanlara duyuru gönderdiği bölüm.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AnnouncementsController : Controller
{
    private const int EventChoiceLimit = 100;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AnnouncementsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Admin/Announcements
    public async Task<IActionResult> Index()
    {
        var announcements = await _context.Announcements
            .Include(a => a.Recipients)
            .Include(a => a.EventSchedule).ThenInclude(e => e!.Production)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(announcements);
    }

    // GET: Admin/Announcements/Create
    public async Task<IActionResult> Create(int? eventScheduleId)
    {
        var form = new AnnouncementFormViewModel { EventScheduleId = eventScheduleId };
        await FillOptionsAsync(form);
        return View(form);
    }

    // POST: Admin/Announcements/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementFormViewModel form)
    {
        if (!form.AudienceAll && form.RecipientIds.Count == 0)
        {
            ModelState.AddModelError(nameof(form.RecipientIds), "En az bir kişi seçin veya duyuruyu bütün ekibe gönderin.");
        }
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        var author = await _userManager.GetUserAsync(User);
        var announcement = new Announcement
        {
            Title = form.Title.Trim(),
            Body = form.Body.Trim(),
            AudienceAll = form.AudienceAll,
            IsPinned = form.IsPinned,
            EventScheduleId = form.EventScheduleId,
            AuthorId = author?.Id,
            AuthorName = author?.DisplayName ?? "Yönetim"
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        var count = await SyncRecipientsAsync(announcement, form);
        TempData["StatusMessage"] = $"Duyuru {count} kişiye gönderildi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Announcements/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var announcement = await _context.Announcements
            .Include(a => a.EventSchedule).ThenInclude(e => e!.Production)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (announcement is null)
        {
            return NotFound();
        }

        var recipients = await _context.AnnouncementRecipients
            .Include(r => r.User)
            .Where(r => r.AnnouncementId == id)
            .ToListAsync();

        return View(new AnnouncementDetailsViewModel
        {
            Announcement = announcement,
            Recipients = recipients
                .OrderBy(r => r.User == null ? string.Empty : r.User.DisplayName, CityDirectory.TurkishComparer)
                .ToList()
        });
    }

    // GET: Admin/Announcements/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var announcement = await _context.Announcements
            .Include(a => a.Recipients)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (announcement is null)
        {
            return NotFound();
        }

        var form = new AnnouncementFormViewModel
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Body = announcement.Body,
            AudienceAll = announcement.AudienceAll,
            IsPinned = announcement.IsPinned,
            EventScheduleId = announcement.EventScheduleId,
            RecipientIds = announcement.Recipients.Select(r => r.UserId).ToList()
        };
        await FillOptionsAsync(form);
        return View(form);
    }

    // POST: Admin/Announcements/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnnouncementFormViewModel form)
    {
        var announcement = await _context.Announcements
            .Include(a => a.Recipients)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (announcement is null)
        {
            return NotFound();
        }

        if (!form.AudienceAll && form.RecipientIds.Count == 0)
        {
            ModelState.AddModelError(nameof(form.RecipientIds), "En az bir kişi seçin veya duyuruyu bütün ekibe gönderin.");
        }

        form.Id = announcement.Id;
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        announcement.Title = form.Title.Trim();
        announcement.Body = form.Body.Trim();
        announcement.AudienceAll = form.AudienceAll;
        announcement.IsPinned = form.IsPinned;
        announcement.EventScheduleId = form.EventScheduleId;
        announcement.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SyncRecipientsAsync(announcement, form);
        TempData["StatusMessage"] = "Duyuru güncellendi.";
        return RedirectToAction(nameof(Details), new { id = announcement.Id });
    }

    // POST: Admin/Announcements/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement is null)
        {
            return NotFound();
        }

        _context.Announcements.Remove(announcement);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Duyuru silindi.";
        return RedirectToAction(nameof(Index));
    }

    // Alıcı listesini forma göre yeniden kurar. Var olan satırlar korunur, böylece okundu bilgisi kaybolmaz.
    private async Task<int> SyncRecipientsAsync(Announcement announcement, AnnouncementFormViewModel form)
    {
        var targetIds = form.AudienceAll
            ? await _userManager.Users.Select(u => u.Id).ToListAsync()
            : await _userManager.Users.Where(u => form.RecipientIds.Contains(u.Id)).Select(u => u.Id).ToListAsync();

        var existing = await _context.AnnouncementRecipients
            .Where(r => r.AnnouncementId == announcement.Id)
            .ToListAsync();

        var removed = existing.Where(r => !targetIds.Contains(r.UserId)).ToList();
        if (removed.Count > 0)
        {
            _context.AnnouncementRecipients.RemoveRange(removed);
        }

        var existingIds = existing.Select(r => r.UserId).ToHashSet();
        foreach (var userId in targetIds.Where(id => !existingIds.Contains(id)))
        {
            _context.AnnouncementRecipients.Add(new AnnouncementRecipient
            {
                AnnouncementId = announcement.Id,
                UserId = userId
            });
        }

        await _context.SaveChangesAsync();
        return targetIds.Count;
    }

    private async Task FillOptionsAsync(AnnouncementFormViewModel form)
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        form.Users = new List<UserListItemViewModel>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            form.Users.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.DisplayName,
                JobTitle = user.JobTitle,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault(AppRoles.All.Contains) ?? AppRoles.Employee,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        form.UpcomingEvents = await _context.EventSchedules
            .Include(e => e.Production)
            .Where(e => e.EventDate >= DateTime.Today && e.City != null && e.City != "")
            .OrderBy(e => e.EventDate)
            .Take(EventChoiceLimit)
            .ToListAsync();
    }
}
