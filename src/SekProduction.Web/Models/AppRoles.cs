namespace SekProduction.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";

    public static readonly string[] All = { Admin, Employee };

    public static string DisplayName(string? role) => role switch
    {
        Admin => "Yönetici",
        Employee => "Çalışan",
        _ => role ?? "—"
    };
}
