namespace SekProduction.Web.Models;

// Çalışanın hangi yapımlarda görevli olduğunu tutar. Çalışan yalnızca bağlı olduğu
// yapımların etkinliklerini görür; hiç bağ girilmemişse kısıt uygulanmaz.
public class UserProduction
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    public int ProductionId { get; set; }
    public Production? Production { get; set; }
}
