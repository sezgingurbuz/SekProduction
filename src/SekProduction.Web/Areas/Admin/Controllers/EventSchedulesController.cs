using System;
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
    public class EventSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventSchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EventSchedules?productionId=5
        public async Task<IActionResult> Index(int productionId)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            ViewData["Production"] = production;
            var schedules = await _context.EventSchedules
                .Where(e => e.ProductionId == productionId)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(schedules);
        }

        // GET: EventSchedules/Create?productionId=5
        public async Task<IActionResult> Create(int productionId)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            ViewData["Production"] = production;
            return View(new EventSchedule { ProductionId = productionId });
        }

        // POST: EventSchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductionId,EventDate,Venue,City,TicketUrl,DisplayOrder")] EventSchedule eventSchedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventSchedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { productionId = eventSchedule.ProductionId });
            }

            ViewData["Production"] = await _context.Productions.FindAsync(eventSchedule.ProductionId);
            return View(eventSchedule);
        }

        // GET: EventSchedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventSchedule = await _context.EventSchedules.FindAsync(id);
            if (eventSchedule == null)
            {
                return NotFound();
            }

            ViewData["Production"] = await _context.Productions.FindAsync(eventSchedule.ProductionId);
            return View(eventSchedule);
        }

        // POST: EventSchedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductionId,EventDate,Venue,City,TicketUrl,DisplayOrder")] EventSchedule eventSchedule)
        {
            if (id != eventSchedule.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventSchedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventScheduleExists(eventSchedule.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index), new { productionId = eventSchedule.ProductionId });
            }

            ViewData["Production"] = await _context.Productions.FindAsync(eventSchedule.ProductionId);
            return View(eventSchedule);
        }

        // GET: EventSchedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventSchedule = await _context.EventSchedules
                .Include(e => e.Production)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (eventSchedule == null)
            {
                return NotFound();
            }

            return View(eventSchedule);
        }

        // POST: EventSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventSchedule = await _context.EventSchedules.FindAsync(id);
            var productionId = eventSchedule?.ProductionId;
            if (eventSchedule != null)
            {
                _context.EventSchedules.Remove(eventSchedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { productionId });
        }

        private bool EventScheduleExists(int id)
        {
            return _context.EventSchedules.Any(e => e.Id == id);
        }
    }
}
