namespace SekProduction.Web.Models;

public class HotelListViewModel
{
    public required List<IGrouping<string, Hotel>> CityGroups { get; init; }
    public required List<string> Cities { get; init; }
    public string? SelectedCity { get; init; }
    public required int TotalCount { get; init; }
}
