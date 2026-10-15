using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthEnd = new DateTime(today.Year, today.Month, 1).AddMonths(1);

        ViewData["ProductionCount"] = await _context.Productions.CountAsync();
        ViewData["TeamMemberCount"] = await _context.TeamMembers.CountAsync();
        ViewData["UpcomingEventCount"] = await _context.EventSchedules.CountAsync(e => e.EventDate >= today);
        ViewData["MonthEventCount"] = await _context.EventSchedules.CountAsync(e => e.EventDate >= today && e.EventDate < monthEnd);
        // Takvime eklenip şehri henüz girilmemiş seanslar ekibe görünmez; yönetici bunları tamamlamalı.
        ViewData["DraftEventCount"] = await _context.EventSchedules.CountAsync(e => e.EventDate >= today && (e.City == null || e.City == ""));
        ViewData["HotelCount"] = await _context.Hotels.CountAsync(h => h.IsActive);
        ViewData["AnnouncementCount"] = await _context.Announcements.CountAsync();
        // Henüz okunmamış duyuru satırları (kişi başına sayılır).
        ViewData["UnreadAnnouncementCount"] = await _context.AnnouncementRecipients.CountAsync(r => r.ReadAt == null);
        ViewData["UserCount"] = await _context.Users.CountAsync();
        return View();
    }
}
