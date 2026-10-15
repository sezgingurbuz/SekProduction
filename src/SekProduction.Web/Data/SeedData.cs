using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Models;

namespace SekProduction.Web.Data;

public static class SeedData
{
    public const string AdminRole = AppRoles.Admin;

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@sekproduction.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe!2026";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Sistem Yöneticisi",
                JobTitle = "Yönetici"
            };
            await userManager.CreateAsync(adminUser, adminPassword);
        }
        else if (string.IsNullOrWhiteSpace(adminUser.FullName))
        {
            adminUser.FullName = "Sistem Yöneticisi";
            await userManager.UpdateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }

        if (!await context.Productions.AnyAsync())
        {
            var play = new Production
            {
                Title = "Örnek Tiyatro Oyunu",
                Slug = "ornek-tiyatro-oyunu",
                ShortDescription = "Bu sezon sahnelenen örnek tiyatro oyunu.",
                Description = "Oyunun künyesi, oyuncu kadrosu ve konu özeti buraya eklenecek.",
                Category = ProductionCategory.ChaplinSanat,
                Year = DateTime.UtcNow.Year,
                ClientName = "Yönetmen Adı",
                AgeLimit = "16+",
                DurationMinutes = 85,
                ActCount = 1,
                Credits = "Yazan: Yazar Adı\nYöneten: Yönetmen Adı\nDekor ve Kostüm Tasarımı: Tasarımcı Adı\nIşık Tasarımı: Tasarımcı Adı",
                IsFeatured = true,
                IsPublished = true,
                DisplayOrder = 1
            };

            play.EventSchedules.Add(new EventSchedule
            {
                EventDate = DateTime.UtcNow.Date.AddDays(14).AddHours(20),
                Venue = "Sek Sahne",
                City = "İstanbul",
                TicketUrl = "https://biletinial.com",
                DisplayOrder = 1
            });
            play.EventSchedules.Add(new EventSchedule
            {
                EventDate = DateTime.UtcNow.Date.AddDays(28).AddHours(20),
                Venue = "Kültür Merkezi",
                City = "Ankara",
                TicketUrl = "https://biletix.com",
                DisplayOrder = 2
            });

            context.Productions.Add(play);
            await context.SaveChangesAsync();
        }
    }
}
