using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Hesaplar yönetim panelinden açıldığı için e-posta doğrulaması aranmaz.
builder.Services.AddDefaultIdentity<SekProduction.Web.Models.AppUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<SekProduction.Web.Services.AppUserClaimsPrincipalFactory>();

// Portal tamamen kapalıdır: [AllowAnonymous] taşımayan her sayfa giriş ister.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new SekProduction.Web.Services.DecimalModelBinderProvider());
});
builder.Services.AddSingleton<SekProduction.Web.Services.ImageStorage>();
builder.Services.AddScoped<SekProduction.Web.Services.CityDirectory>();
builder.Services.AddScoped<SekProduction.Web.Services.TourCalendarService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var turkish = new System.Globalization.CultureInfo("tr-TR");
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(turkish),
    SupportedCultures = new[] { turkish },
    SupportedUICultures = new[] { turkish }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// Hesaplar yalnızca yönetim panelinden açılır; Identity'nin kendi kayıt sayfaları kapalıdır.
string[] disabledIdentityPages =
{
    "/Identity/Account/Register",
    "/Identity/Account/RegisterConfirmation",
    "/Identity/Account/ResendEmailConfirmation",
    "/Identity/Account/ExternalLogin",
    "/Identity/Account/ForgotPassword",
    "/Identity/Account/ForgotPasswordConfirmation",
    "/Identity/Account/ResetPassword",
    "/Identity/Account/ResetPasswordConfirmation",
    // E-posta ve hesap silme yönetici yetkisindedir.
    "/Identity/Account/Manage/Email",
    "/Identity/Account/Manage/PersonalData",
    "/Identity/Account/Manage/DeletePersonalData",
    "/Identity/Account/Manage/ExternalLogins"
};
app.Use(async (context, next) =>
{
    if (disabledIdentityPages.Any(page => context.Request.Path.StartsWithSegments(page, StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.Redirect("/Identity/Account/Login");
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await SekProduction.Web.Data.SeedData.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();
