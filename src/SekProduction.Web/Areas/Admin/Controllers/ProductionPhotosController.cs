using System.Collections.Generic;
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
    public class ProductionPhotosController : Controller
    {
        private const string ImageFolder = "gallery";
        private const int MaxFilesPerUpload = 10;
        private const long MaxUploadBytes = MaxFilesPerUpload * ImageStorage.MaxBytes + 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly ImageStorage _images;

        public ProductionPhotosController(ApplicationDbContext context, ImageStorage images)
        {
            _context = context;
            _images = images;
        }

        // GET: ProductionPhotos?productionId=5
        public async Task<IActionResult> Index(int productionId)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            ViewData["Production"] = production;
            var photos = await _context.ProductionPhotos
                .Where(p => p.ProductionId == productionId)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .ToListAsync();

            return View(photos);
        }

        // POST: ProductionPhotos/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> Upload(int productionId, List<IFormFile> photos)
        {
            if (!await _context.Productions.AnyAsync(p => p.Id == productionId))
            {
                return NotFound();
            }

            if (photos.Count == 0)
            {
                TempData["ErrorMessage"] = "Yüklemek için en az bir fotoğraf seçin.";
                return RedirectToAction(nameof(Index), new { productionId });
            }
            if (photos.Count > MaxFilesPerUpload)
            {
                TempData["ErrorMessage"] = $"Tek seferde en fazla {MaxFilesPerUpload} fotoğraf yükleyebilirsiniz.";
                return RedirectToAction(nameof(Index), new { productionId });
            }

            var order = await _context.ProductionPhotos
                .Where(p => p.ProductionId == productionId)
                .Select(p => (int?)p.DisplayOrder)
                .MaxAsync() ?? 0;

            var rejected = new List<string>();
            var added = 0;
            foreach (var file in photos)
            {
                if (_images.Validate(file) is not null)
                {
                    rejected.Add(file.FileName);
                    continue;
                }

                _context.ProductionPhotos.Add(new ProductionPhoto
                {
                    ProductionId = productionId,
                    ImageUrl = await _images.SaveAsync(file, ImageFolder),
                    DisplayOrder = ++order
                });
                added++;
            }

            await _context.SaveChangesAsync();

            if (added > 0)
            {
                TempData["StatusMessage"] = $"{added} fotoğraf galeriye eklendi.";
            }
            if (rejected.Count > 0)
            {
                TempData["ErrorMessage"] = $"Şu dosyalar yüklenemedi (sadece JPG/PNG/WEBP, en fazla 5 MB): {string.Join(", ", rejected)}";
            }
            return RedirectToAction(nameof(Index), new { productionId });
        }

        // POST: ProductionPhotos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _context.ProductionPhotos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            _context.ProductionPhotos.Remove(photo);
            await _context.SaveChangesAsync();
            _images.Delete(photo.ImageUrl);
            TempData["StatusMessage"] = "Fotoğraf silindi.";
            return RedirectToAction(nameof(Index), new { productionId = photo.ProductionId });
        }
    }
}
