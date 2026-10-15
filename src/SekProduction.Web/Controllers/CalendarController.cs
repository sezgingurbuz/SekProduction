using Microsoft.AspNetCore.Mvc;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Controllers;

// Çalışanların hangi günde hangi etkinlik olduğunu gördüğü takvim (salt okunur).
public class CalendarController : Controller
{
    private readonly TourCalendarService _calendar;
    private readonly ProductionScope _scope;

    public CalendarController(TourCalendarService calendar, ProductionScope scope)
    {
        _calendar = calendar;
        _scope = scope;
    }

    // GET: /Calendar?year=2026&month=10&productionId=3&city=Ankara
    public async Task<IActionResult> Index(int? year, int? month, TourFilter filter)
    {
        var allowed = await _scope.AllowedProductionIdsAsync(User);
        return View(await _calendar.BuildAsync(year, month, filter, readyOnly: true, allowedProductionIds: allowed));
    }
}
