namespace SekProduction.Web.Models;

public class EventDetailsViewModel
{
    public required EventSchedule Event { get; init; }

    // Aynı gün başka şehirde seans varsa ekip bunu görmeli.
    public required List<EventSchedule> SameDayEvents { get; init; }
    public required Dictionary<int, string> ProductionColors { get; init; }

    // Etkinliğin yapıldığı şehirdeki oteller ve fiyatları.
    public required List<Hotel> Hotels { get; init; }

    public string Color => ProductionColors.GetValueOrDefault(Event.ProductionId, "#6c757d");

    public string ColorFor(int productionId) => ProductionColors.GetValueOrDefault(productionId, "#6c757d");
}
