namespace SekProduction.Web.Models;

public class ProductionPhoto
{
    public int Id { get; set; }

    public int ProductionId { get; set; }
    public Production? Production { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
