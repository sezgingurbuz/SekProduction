using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class TourController : Controller
    {
        private const int MaxDaysPerRequest = 100;
        private const int MaxSessionsPerDelete = 500;

        private readonly ApplicationDbContext _context;
        private readonly TourCalendarService _calendar;

        public TourController(ApplicationDbContext context, TourCalendarService calendar)
        {
            _context = context;
            _calendar = calendar;
        }

        // GET: Admin/Tour?year=2026&month=9
        public async Task<IActionResult> Index(int? year, int? month, TourFilter filter)
        {
            return View(await _calendar.BuildAsync(year, month, filter));
        }

        // POST: Admin/Tour/AddDays — takvimde seçilen günlerin her birine seans ekler; şehir/salon sonra girilir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDays(int? productionId, List<DateTime> dates, TimeSpan? time, TourFilter filter, string? returnTo)
        {
            var days = dates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();
            var production = productionId is null ? null : await _context.Productions.FindAsync(productionId);

            if (production == null || days.Count == 0 || days.Count > MaxDaysPerRequest || time is null)
            {
                TempData["StatusError"] = production == null ? "Seans eklenecek yapımı seçin."
                    : days.Count == 0 ? "Takvimden en az bir gün seçin."
                    : time is null ? "Seans saatini girin."
                    : $"Tek seferde en fazla {MaxDaysPerRequest} gün eklenebilir.";
                return RedirectToCalendar(days.FirstOrDefault(DateTime.Today), filter, returnTo, productionId);
            }

            var from = days[0];
            var to = days[^1].AddDays(1);
            var existing = await _context.EventSchedules
                .Where(e => e.ProductionId == production.Id && e.EventDate >= from && e.EventDate < to)
                .Select(e => e.EventDate)
                .ToListAsync();

            var added = 0;
            foreach (var day in days)
            {
                var eventDate = day + time.Value;
                if (existing.Contains(eventDate))
                {
                    continue;
                }

                _context.EventSchedules.Add(new EventSchedule { ProductionId = production.Id, EventDate = eventDate });
                added++;
            }
            await _context.SaveChangesAsync();

            var skipped = days.Count - added;
            TempData["StatusMessage"] = $"{production.Title} için {added} seans eklendi"
                + (skipped > 0 ? $" ({skipped} gün aynı saatte zaten vardı)" : "")
                + ". Şehir, salon ve bilet bilgisi için takvimde seansa tıklayın.";
            return RedirectToCalendar(from, filter, returnTo, production.Id);
        }

        // POST: Admin/Tour/Save — takvimdeki pencereden seans ekler veya günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TourSessionInput session, TourFilter filter, string? returnTo)
        {
            if (session.ProductionId is not null && !await _context.Productions.AnyAsync(p => p.Id == session.ProductionId))
            {
                ModelState.AddModelError(nameof(session.ProductionId), "Seçilen yapım bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["StatusError"] = "Seans kaydedilemedi. " + string.Join(" ", errors);
                return RedirectToCalendar(session.Date ?? DateTime.Today, filter, returnTo, session.ProductionId);
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
            entity.City = string.IsNullOrWhiteSpace(session.City) ? null : session.City.Trim();
            entity.Venue = string.IsNullOrWhiteSpace(session.Venue) ? null : session.Venue.Trim();
            entity.TicketUrl = string.IsNullOrWhiteSpace(session.TicketUrl) ? null : session.TicketUrl.Trim();
            await _context.SaveChangesAsync();

            var title = await _context.Productions.Where(p => p.Id == entity.ProductionId).Select(p => p.Title).FirstAsync();
            TempData["StatusMessage"] = $"{title} · {entity.EventDate:dd MMMM yyyy, HH:mm} · {entity.City ?? "şehir girilmedi"} {(session.Id is null ? "eklendi" : "güncellendi")}.";
            return RedirectToCalendar(entity.EventDate, filter, returnTo, entity.ProductionId);
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
        public async Task<IActionResult> Delete(int id, TourFilter filter, string? returnTo)
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

            TempData["StatusMessage"] = $"{entity.Production?.Title} · {entity.EventDate:dd MMMM yyyy, HH:mm} · {entity.City ?? "şehir girilmedi"} silindi.";
            return RedirectToCalendar(entity.EventDate, filter, returnTo, entity.ProductionId);
        }

        // POST: Admin/Tour/DeleteMany — takvimde seçilen günlerde görünen seansları toplu siler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMany(List<int> ids, TourFilter filter, string? returnTo)
        {
            var idList = ids.Distinct().Take(MaxSessionsPerDelete).ToList();
            var query = _context.EventSchedules.Where(e => idList.Contains(e.Id));

            // Yapım sayfasından gelen istek yalnızca o yapımın seanslarını silebilir.
            if (returnTo == TourReturn.Production)
            {
                query = query.Where(e => e.ProductionId == filter.ProductionId);
            }

            var sessions = await query.OrderBy(e => e.EventDate).ToListAsync();
            if (sessions.Count == 0)
            {
                TempData["StatusError"] = "Seçili günlerde silinecek seans bulunamadı.";
                return RedirectToCalendar(DateTime.Today, filter, returnTo, filter.ProductionId);
            }

            _context.EventSchedules.RemoveRange(sessions);
            await _context.SaveChangesAsync();

            var days = sessions.Select(s => s.EventDate.Date).Distinct().Select(d => d.ToString("d MMM"));
            TempData["StatusMessage"] = $"{sessions.Count} seans silindi: {string.Join(", ", days)}.";
            return RedirectToCalendar(sessions[0].EventDate, filter, returnTo, filter.ProductionId);
        }

        private IActionResult RedirectToCalendar(DateTime date, TourFilter filter, string? returnTo, int? productionId)
        {
            if (returnTo == TourReturn.Production && productionId is not null)
            {
                return RedirectToAction(nameof(Index), "EventSchedules", new
                {
                    area = "Admin",
                    productionId,
                    year = date.Year,
                    month = date.Month,
                    view = filter.View
                });
            }

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
