using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Services;

// Çalışanın hangi yapımları görebileceğini belirler.
// null dönerse kısıt yoktur: yöneticiler ve henüz hiçbir yapıma bağlanmamış çalışanlar her şeyi görür.
public class ProductionScope
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ProductionScope(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<HashSet<int>?> AllowedProductionIdsAsync(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(AppRoles.Admin))
        {
            return null;
        }

        var userId = _userManager.GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        var ids = await _context.UserProductions
            .Where(up => up.UserId == userId)
            .Select(up => up.ProductionId)
            .ToListAsync();

        return ids.Count == 0 ? null : ids.ToHashSet();
    }
}
