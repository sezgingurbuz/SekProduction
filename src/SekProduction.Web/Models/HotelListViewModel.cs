namespace SekProduction.Web.Models;

public class HotelListViewModel
{
    public required List<IGrouping<string, Hotel>> CityGroups { get; init; }

    // Başlangıçta seçili şehir (?city=... ile gelen bağlantılar için).
    public string? SelectedCity { get; init; }
    public required int TotalCount { get; init; }

    // Şehirde planlanmış etkinlik sayısı; otel listesinde hangi şehirlerin önemli olduğunu gösterir.
    public required Dictionary<string, int> UpcomingEventCountByCity { get; init; }

    public int EventCountFor(string city) => UpcomingEventCountByCity.TryGetValue(city, out var count) ? count : 0;
}
