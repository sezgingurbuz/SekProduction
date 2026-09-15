using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class ProductionsController : Controller
    {
        private const string ImageFolder = "productions";
        private const string BindFields = "Title,Slug,ShortDescription,Description,Category,Year,ClientName,AgeLimit,DurationMinutes,ActCount,Credits,TrailerUrl,IsFeatured,IsPublished,DisplayOrder";

        private readonly ApplicationDbContext _context;
        private readonly ImageStorage _images;

        public ProductionsController(ApplicationDbContext context, ImageStorage images)
        {
            _context = context;
            _images = images;
        }

        // GET: Productions
        public async Task<IActionResult> Index(ProductionCategory? category)
        {
            if (category is not null && !Enum.IsDefined(category.Value))
            {
                category = null;
            }

            ViewData["SelectedCategory"] = category;
            ViewData["CategoryCounts"] = await _context.Productions
                .GroupBy(p => p.Category)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var productions = await _context.Productions
                .Where(p => category == null || p.Category == category)
                .Include(p => p.EventSchedules)
                .Include(p => p.CastMembers)
                .Include(p => p.Photos)
                .AsSplitQuery()
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(productions);
        }

        // GET: Productions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var production = await _context.Productions
                .Include(p => p.EventSchedules)
                .Include(p => p.CastMembers)
                .Include(p => p.Photos)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (production == null)
            {
                return NotFound();
            }

            return View(production);
        }

        // GET: Productions/Create
        public IActionResult Create(ProductionCategory? category)
        {
            // Önce yapımın hangi menü başlığı altında listeleneceği seçilir.
            if (category is null || !Enum.IsDefined(category.Value))
            {
                return View("ChooseCategory");
            }

            return View(new Production { Category = category.Value });
        }

        // POST: Productions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(BindFields)] Production production, IFormFile? coverImage)
        {
            ValidateInput(production, coverImage);

            if (ModelState.IsValid)
            {
                if (coverImage != null)
                {
                    production.CoverImageUrl = await _images.SaveAsync(coverImage, ImageFolder);
                }

                production.CreatedAt = DateTime.UtcNow;
                _context.Add(production);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = $"\"{production.Title}\" {production.Category.GetDisplayName()} menüsüne eklendi.";
                return RedirectToAction(nameof(Index), new { area = "Admin", category = production.Category });
            }

            return View(production);
        }

        // GET: Productions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var production = await _context.Productions.FindAsync(id);
            if (production == null)
            {
                return NotFound();
            }
            return View(production);
        }

        // POST: Productions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id," + BindFields)] Production production, IFormFile? coverImage, bool removeCoverImage)
        {
            if (id != production.Id)
            {
                return NotFound();
            }

            var existing = await _context.Productions
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new { p.CoverImageUrl, p.CreatedAt })
                .FirstOrDefaultAsync();
            if (existing == null)
            {
                return NotFound();
            }

            ValidateInput(production, coverImage);

            if (!ModelState.IsValid)
            {
                production.CoverImageUrl = existing.CoverImageUrl;
                return View(production);
            }

            production.CreatedAt = existing.CreatedAt;
            production.UpdatedAt = DateTime.UtcNow;
            production.CoverImageUrl = existing.CoverImageUrl;

            string? imageToDelete = null;
            if (coverImage != null)
            {
                production.CoverImageUrl = await _images.SaveAsync(coverImage, ImageFolder);
                imageToDelete = existing.CoverImageUrl;
            }
            else if (removeCoverImage)
            {
                production.CoverImageUrl = null;
                imageToDelete = existing.CoverImageUrl;
            }

            try
            {
                _context.Update(production);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductionExists(production.Id))
                {
                    return NotFound();
                }
                throw;
            }

            _images.Delete(imageToDelete);
            TempData["StatusMessage"] = $"\"{production.Title}\" güncellendi.";
            return RedirectToAction(nameof(Index), new { area = "Admin", category = production.Category });
        }

        // GET: Productions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var production = await _context.Productions
                .Include(p => p.EventSchedules)
                .Include(p => p.CastMembers)
                .Include(p => p.Photos)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (production == null)
            {
                return NotFound();
            }

            return View(production);
        }

        // POST: Productions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var production = await _context.Productions
                .Include(p => p.CastMembers)
                .Include(p => p.Photos)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (production != null)
            {
                var files = production.CastMembers.Select(c => c.PhotoUrl)
                    .Concat(production.Photos.Select(p => p.ImageUrl))
                    .Append(production.CoverImageUrl)
                    .ToList();

                _context.Productions.Remove(production);
                await _context.SaveChangesAsync();

                files.ForEach(_images.Delete);
                TempData["StatusMessage"] = $"\"{production.Title}\" silindi.";
            }

            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private bool ProductionExists(int id)
        {
            return _context.Productions.Any(e => e.Id == id);
        }

        private void ValidateInput(Production production, IFormFile? coverImage)
        {
            if (!Enum.IsDefined(production.Category))
            {
                ModelState.AddModelError(nameof(Production.Category), "Yapımın listeleneceği menüyü seçin.");
            }

            if (coverImage != null && _images.Validate(coverImage) is { } error)
            {
                ModelState.AddModelError("coverImage", error);
            }

            if (!string.IsNullOrWhiteSpace(production.TrailerUrl) && production.TrailerYouTubeId is null)
            {
                ModelState.AddModelError(nameof(Production.TrailerUrl), "Geçerli bir YouTube video linki girin.");
            }
        }
    }
}
