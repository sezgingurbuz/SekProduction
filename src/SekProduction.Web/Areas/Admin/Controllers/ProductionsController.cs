using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class ProductionsController : Controller
    {
        private const string UploadFolder = "uploads/productions";
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductionsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Productions
        public async Task<IActionResult> Index()
        {
            var productions = await _context.Productions
                .Include(p => p.EventSchedules)
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (production == null)
            {
                return NotFound();
            }

            return View(production);
        }

        // GET: Productions/Create
        public IActionResult Create()
        {
            return View(new Production());
        }

        // POST: Productions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Slug,ShortDescription,Description,Category,Year,ClientName,IsFeatured,IsPublished,DisplayOrder")] Production production,
            IFormFile? coverImage)
        {
            ValidateImage(coverImage);

            if (ModelState.IsValid)
            {
                if (coverImage != null)
                {
                    production.CoverImageUrl = await SaveImageAsync(coverImage);
                }

                production.CreatedAt = DateTime.UtcNow;
                _context.Add(production);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = $"\"{production.Title}\" eklendi.";
                return RedirectToAction(nameof(Index), new { area = "Admin" });
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
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Slug,ShortDescription,Description,Category,Year,ClientName,IsFeatured,IsPublished,DisplayOrder")] Production production,
            IFormFile? coverImage,
            bool removeCoverImage)
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

            ValidateImage(coverImage);

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
                production.CoverImageUrl = await SaveImageAsync(coverImage);
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

            DeleteUploadedImage(imageToDelete);
            TempData["StatusMessage"] = $"\"{production.Title}\" güncellendi.";
            return RedirectToAction(nameof(Index), new { area = "Admin" });
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
            var production = await _context.Productions.FindAsync(id);
            if (production != null)
            {
                _context.Productions.Remove(production);
                await _context.SaveChangesAsync();
                DeleteUploadedImage(production.CoverImageUrl);
                TempData["StatusMessage"] = $"\"{production.Title}\" silindi.";
            }

            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private bool ProductionExists(int id)
        {
            return _context.Productions.Any(e => e.Id == id);
        }

        private void ValidateImage(IFormFile? file)
        {
            if (file == null)
            {
                return;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("coverImage", "Sadece JPG, PNG veya WEBP formatında görsel yükleyebilirsiniz.");
            }
            else if (file.Length == 0 || file.Length > MaxImageBytes)
            {
                ModelState.AddModelError("coverImage", "Görsel boyutu en fazla 5 MB olabilir.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var folder = Path.Combine(_environment.WebRootPath, UploadFolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{UploadFolder}/{fileName}";
        }

        private void DeleteUploadedImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith($"/{UploadFolder}/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var folder = Path.GetFullPath(Path.Combine(_environment.WebRootPath, UploadFolder));
            var fullPath = Path.GetFullPath(Path.Combine(folder, Path.GetFileName(imageUrl)));
            if (fullPath.StartsWith(folder + Path.DirectorySeparatorChar) && System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
