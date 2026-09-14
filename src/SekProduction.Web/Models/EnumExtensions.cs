using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString())[0];
        var display = member.GetCustomAttributes(typeof(DisplayAttribute), false)
            .Cast<DisplayAttribute>()
            .FirstOrDefault();

        return display?.Name ?? value.ToString();
    }
}
