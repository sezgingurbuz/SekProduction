using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    // Yapımın Etkinlik Günleri: Turne Takvimi'nin tek yapıma kilitli hali.
    // Ekleme, düzenleme, taşıma ve silme TourController üzerinden yapılır.
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class EventSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TourCalendarService _calendar;

        public EventSchedulesController(ApplicationDbContext context, TourCalendarService calendar)
        {
            _context = context;
            _calendar = calendar;
        }

        // GET: Admin/EventSchedules?productionId=5&year=2026&month=9
        public async Task<IActionResult> Index(int productionId, int? year, int? month, string? view)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            return View(await _calendar.BuildAsync(year, month, new TourFilter { View = view }, production));
        }
    }
}
