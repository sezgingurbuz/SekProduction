using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class TourController : Controller
    {
        private static readonly string[] Palette =
        {
            "#2563eb", "#dc2626", "#059669", "#d97706", "#7c3aed",
            "#db2777", "#0891b2", "#65a30d", "#9333ea", "#ea580c"
        };

        private static readonly CultureInfo Turkish = new("tr-TR");

        private readonly ApplicationDbContext _context;

        public TourController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Tour?year=2026&month=9
        public async Task<IActionResult> Index(int? year, int? month, TourFilter filter)
        {
            var today = DateTime.Today;
            var first = year is >= 2000 and <= 2100 && month is >= 1 and <= 12
                ? new DateTime(year.Value, month.Value, 1)
                : new DateTime(today.Year, today.Month, 1);

            // Takvim pazartesiden başlar; ayın ilk gününden önceki günler önceki aydan doldurulur.
            var offset = ((int)first.DayOfWeek + 6) % 7;
            var gridStart = first.AddDays(-offset);
            var weeks = (int)Math.Ceiling((offset + DateTime.DaysInMonth(first.Year, first.Month)) / 7.0);
            var gridEnd = gridStart.AddDays(weeks * 7);

            var productions = await _context.Productions
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.Title)
                .ToListAsync();

            var query = _context.EventSchedules
                .Include(e => e.Production)
                .Where(e => e.EventDate >= gridStart && e.EventDate < gridEnd);

            if (filter.Category is not null)
            {
                query = query.Where(e => e.Production!.Category == filter.Category);
            }
            if (filter.ProductionId is not null)
            {
                query = query.Where(e => e.ProductionId == filter.ProductionId);
            }
            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(e => e.City == filter.City);
            }

            var sessions = await query.OrderBy(e => e.EventDate).ToListAsync();

            var cities = await _context.EventSchedules
                .Where(e => e.City != null && e.City != "")
                .Select(e => e.City!)
                .Distinct()
                .ToListAsync();
            var venues = await _context.EventSchedules
                .Where(e => e.Venue != null && e.Venue != "")
                .Select(e => e.Venue!)
                .Distinct()
                .ToListAsync();

            var model = new TourCalendarViewModel
            {
                Month = first,
                GridStart = gridStart,
                Weeks = weeks,
                Filter = filter,
                Sessions = sessions,
                Productions = productions,
                ProductionColors = productions
                    .OrderBy(p => p.Id)
                    .Select((p, i) => (p.Id, Color: Palette[i % Palette.Length]))
                    .ToDictionary(x => x.Id, x => x.Color),
                Cities = cities.OrderBy(c => c, StringComparer.Create(Turkish, true)).ToList(),
                Venues = venues.OrderBy(v => v, StringComparer.Create(Turkish, true)).ToList()
            };

            return View(model);
        }

        // POST: Admin/Tour/Save — takvimdeki pencereden seans ekler veya günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TourSessionInput session, TourFilter filter)
        {
            if (session.ProductionId is not null && !await _context.Productions.AnyAsync(p => p.Id == session.ProductionId))
            {
                ModelState.AddModelError(nameof(session.ProductionId), "Seçilen yapım bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["StatusError"] = "Seans kaydedilemedi. " + string.Join(" ", errors);
                return RedirectToCalendar(session.Date ?? DateTime.Today, filter);
            }

            EventSchedule entity;
            if (session.Id is int id)
            {
                var existing = await _context.EventSchedules.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }
                entity = existing;
            }
            else
            {
                entity = new EventSchedule();
                _context.EventSchedules.Add(entity);
            }

            entity.ProductionId = session.ProductionId!.Value;
            entity.EventDate = session.Date!.Value.Date + session.Time!.Value;
            entity.City = session.City!.Trim();
            entity.Venue = string.IsNullOrWhiteSpace(session.Venue) ? null : session.Venue.Trim();
            entity.TicketUrl = string.IsNullOrWhiteSpace(session.TicketUrl) ? null : session.TicketUrl.Trim();
            await _context.SaveChangesAsync();

            var title = await _context.Productions.Where(p => p.Id == entity.ProductionId).Select(p => p.Title).FirstAsync();
            TempData["StatusMessage"] = $"{title} · {entity.EventDate:dd MMMM yyyy, HH:mm} · {entity.City} {(session.Id is null ? "eklendi" : "güncellendi")}.";
            return RedirectToCalendar(entity.EventDate, filter);
        }

        // POST: Admin/Tour/Move — sürükle-bırak ile seansı başka güne taşır, saat aynı kalır
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, DateTime date)
        {
            var entity = await _context.EventSchedules.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            entity.EventDate = date.Date + entity.EventDate.TimeOfDay;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: Admin/Tour/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, TourFilter filter)
        {
            var entity = await _context.EventSchedules
                .Include(e => e.Production)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.EventSchedules.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"{entity.Production?.Title} · {entity.EventDate:dd MMMM yyyy, HH:mm} · {entity.City} silindi.";
            return RedirectToCalendar(entity.EventDate, filter);
        }

        private IActionResult RedirectToCalendar(DateTime date, TourFilter filter)
        {
            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                year = date.Year,
                month = date.Month,
                category = filter.Category,
                productionId = filter.ProductionId,
                city = filter.City,
                view = filter.View
            });
        }
    }
}
