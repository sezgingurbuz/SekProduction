namespace SekProduction.Web.Models;

public class HomeViewModel
{
    public List<Production> FeaturedProductions { get; set; } = new();
    public List<NewsPost> LatestNews { get; set; } = new();
    public List<ClientReference> ClientReferences { get; set; } = new();
}
