namespace SekProduction.Web.Models;

public static class ProductionCategoryRoutes
{
    // Route şablonunda kullanılır; GetSlug ile aynı değerleri içermeli.
    public const string SlugPattern = "^chaplin-sanat$|^chaplin-cocuk-sanat$|^sek-production$";

    public static string GetSlug(this ProductionCategory category) => category switch
    {
        ProductionCategory.ChaplinSanat => "chaplin-sanat",
        ProductionCategory.ChaplinCocukSanat => "chaplin-cocuk-sanat",
        ProductionCategory.SekProduction => "sek-production",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    public static ProductionCategory? FromSlug(string slug)
    {
        foreach (var category in Enum.GetValues<ProductionCategory>())
        {
            if (category.GetSlug() == slug)
            {
                return category;
            }
        }

        return null;
    }
}
