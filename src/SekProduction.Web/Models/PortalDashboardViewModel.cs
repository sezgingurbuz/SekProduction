namespace SekProduction.Web.Models;

// Çalışanın giriş yaptığında gördüğü özet sayfa.
// Listeler zaten çalışanın görevli olduğu yapımlara göre süzülmüş gelir.
public class PortalDashboardViewModel
{
    // Bugünden itibaren bütün etkinlikler; haritanın "yaklaşan tümü" görünümü de bunu kullanır.
    public required List<EventSchedule> UpcomingEvents { get; init; }

    // İçinde bulunulan ayın etkinlikleri; haritanın ay görünümü.
    public required List<EventSchedule> MonthEvents { get; init; }

    // Okunmamış duyurular; panelde ilk sırada gösterilir.
    public required List<InboxItemViewModel> UnreadAnnouncements { get; init; }

    // Çalışanın görevli olduğu yapımlar. Boşsa kısıt yoktur, bütün etkinlikler görünür.
    public required List<string> AssignedProductionTitles { get; init; }

    public required Dictionary<int, string> ProductionColors { get; init; }

    public int MonthEventCount => MonthEvents.Count;

    public List<EventSchedule> TodayEvents =>
        UpcomingEvents.Where(e => e.EventDate.Date == DateTime.Today).ToList();

    public List<EventSchedule> NextEvents =>
        UpcomingEvents.Where(e => e.EventDate.Date > DateTime.Today).Take(15).ToList();

    public string ColorFor(int productionId) => ProductionColors.GetValueOrDefault(productionId, "#6c757d");
}
