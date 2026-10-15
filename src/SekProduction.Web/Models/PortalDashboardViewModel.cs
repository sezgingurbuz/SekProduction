namespace SekProduction.Web.Models;

// Çalışanın giriş yaptığında gördüğü özet sayfa.
public class PortalDashboardViewModel
{
    public required List<EventSchedule> TodayEvents { get; init; }
    public required List<EventSchedule> UpcomingEvents { get; init; }
    public required Dictionary<int, string> ProductionColors { get; init; }
    public required int MonthEventCount { get; init; }

    public string ColorFor(int productionId) => ProductionColors.GetValueOrDefault(productionId, "#6c757d");
}
