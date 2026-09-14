using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Models;

namespace SekProduction.Web.Data;

public static class SeedData
{
    public const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@sekproduction.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe!2026";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, adminPassword);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            await userManager.AddToRoleAsync(adminUser, AdminRole);
        }

        if (!await context.Productions.AnyAsync())
        {
            var play = new Production
            {
                Title = "Örnek Tiyatro Oyunu",
                Slug = "ornek-tiyatro-oyunu",
                ShortDescription = "Bu sezon sahnelenen örnek tiyatro oyunu.",
                Description = "Oyunun künyesi, oyuncu kadrosu ve konu özeti buraya eklenecek.",
                Category = ProductionCategory.Theatre,
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
