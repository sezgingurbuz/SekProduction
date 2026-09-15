using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        return GetDisplayAttribute(value)?.Name ?? value.ToString();
    }

    public static string? GetDescription(this Enum value)
    {
        return GetDisplayAttribute(value)?.Description;
    }

    private static DisplayAttribute? GetDisplayAttribute(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString())[0];
        return member.GetCustomAttributes(typeof(DisplayAttribute), false)
            .Cast<DisplayAttribute>()
            .FirstOrDefault();
    }
}
