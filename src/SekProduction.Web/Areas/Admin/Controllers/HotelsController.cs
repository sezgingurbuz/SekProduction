using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers;

// Turne şehirlerindeki otel ve fiyat kataloğu.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class HotelsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CityDirectory _cities;

    public HotelsController(ApplicationDbContext context, CityDirectory cities)
    {
        _context = context;
        _cities = cities;
    }

    // GET: Admin/Hotels?city=Ankara
    public async Task<IActionResult> Index(string? city)
    {
        var query = _context.Hotels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(h => h.City == city);
        }

        var hotels = await query.ToListAsync();
        ViewData["Cities"] = CityDirectory.Sorted(await _context.Hotels.Select(h => h.City).Distinct().ToListAsync());
        ViewData["SelectedCity"] = city;
        ViewData["EventCitiesWithoutHotel"] = await EventCitiesWithoutHotelAsync();

        return View(Ordered(hotels));
    }

    // GET: Admin/Hotels/Create?city=Ankara
    public async Task<IActionResult> Create(string? city)
    {
        await LoadSuggestionsAsync();
        return View(new Hotel { City = city ?? string.Empty });
    }

    // POST: Admin/Hotels/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Hotel hotel)
    {
        if (!ModelState.IsValid)
        {
            await LoadSuggestionsAsync();
            return View(hotel);
        }

        Normalize(hotel);
        hotel.PriceUpdatedAt = hotel.HasPrice ? DateTime.UtcNow : null;
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{hotel.City} · {hotel.Name} eklendi.";
        return RedirectToAction(nameof(Index), new { city = hotel.City });
    }

    // GET: Admin/Hotels/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel is null)
        {
            return NotFound();
        }

        await LoadSuggestionsAsync();
        return View(hotel);
    }

    // POST: Admin/Hotels/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Hotel form)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadSuggestionsAsync();
            return View(form);
        }

        var priceChanged = hotel.SingleRoomPrice != form.SingleRoomPrice || hotel.DoubleRoomPrice != form.DoubleRoomPrice;

        hotel.City = form.City;
        hotel.Name = form.Name;
        hotel.Stars = form.Stars;
        hotel.Address = form.Address;
        hotel.Phone = form.Phone;
        hotel.ContactName = form.ContactName;
        hotel.WebsiteUrl = form.WebsiteUrl;
        hotel.SingleRoomPrice = form.SingleRoomPrice;
        hotel.DoubleRoomPrice = form.DoubleRoomPrice;
        hotel.BreakfastIncluded = form.BreakfastIncluded;
        hotel.Notes = form.Notes;
        hotel.IsPreferred = form.IsPreferred;
        hotel.IsActive = form.IsActive;
        hotel.UpdatedAt = DateTime.UtcNow;
        Normalize(hotel);

        if (priceChanged)
        {
            hotel.PriceUpdatedAt = hotel.HasPrice ? DateTime.UtcNow : null;
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{hotel.City} · {hotel.Name} güncellendi.";
        return RedirectToAction(nameof(Index), new { city = hotel.City });
    }

    // POST: Admin/Hotels/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel is null)
        {
            return NotFound();
        }

        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{hotel.City} · {hotel.Name} silindi.";
        return RedirectToAction(nameof(Index), new { city = hotel.City });
    }

    // Etkinlik olan ama henüz oteli kaydedilmemiş şehirler; yöneticiye eksikleri hatırlatır.
    private async Task<List<string>> EventCitiesWithoutHotelAsync()
    {
        var eventCities = await _cities.EventCitiesAsync();
        var hotelCities = await _context.Hotels.Select(h => h.City).Distinct().ToListAsync();
        return eventCities.Except(CityDirectory.Sorted(hotelCities), CityDirectory.TurkishComparer).ToList();
    }

    private async Task LoadSuggestionsAsync()
    {
        ViewData["CitySuggestions"] = await _cities.SuggestionsAsync();
    }

    private static List<Hotel> Ordered(List<Hotel> hotels) => hotels
        .OrderBy(h => h.City, CityDirectory.TurkishComparer)
        .ThenByDescending(h => h.IsPreferred)
        .ThenBy(h => h.Name, CityDirectory.TurkishComparer)
        .ToList();

    private static void Normalize(Hotel hotel)
    {
        hotel.City = hotel.City.Trim();
        hotel.Name = hotel.Name.Trim();
        hotel.Address = Blank(hotel.Address);
        hotel.Phone = Blank(hotel.Phone);
        hotel.ContactName = Blank(hotel.ContactName);
        hotel.WebsiteUrl = Blank(hotel.WebsiteUrl);
        hotel.Notes = Blank(hotel.Notes);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
