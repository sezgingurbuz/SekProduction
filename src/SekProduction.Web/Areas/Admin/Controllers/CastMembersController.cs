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
    public class CastMembersController : Controller
    {
        private const string ImageFolder = "cast";

        private readonly ApplicationDbContext _context;
        private readonly ImageStorage _images;

        public CastMembersController(ApplicationDbContext context, ImageStorage images)
        {
            _context = context;
            _images = images;
        }

        // GET: CastMembers?productionId=5
        public async Task<IActionResult> Index(int productionId)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            ViewData["Production"] = production;
            var cast = await _context.CastMembers
                .Where(c => c.ProductionId == productionId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Id)
                .ToListAsync();

            return View(cast);
        }

        // GET: CastMembers/Create?productionId=5
        public async Task<IActionResult> Create(int productionId)
        {
            var production = await _context.Productions.FindAsync(productionId);
            if (production == null)
            {
                return NotFound();
            }

            var nextOrder = await _context.CastMembers
                .Where(c => c.ProductionId == productionId)
                .Select(c => (int?)c.DisplayOrder)
                .MaxAsync() ?? 0;

            ViewData["Production"] = production;
            return View(new CastMember { ProductionId = productionId, DisplayOrder = nextOrder + 1 });
        }

        // POST: CastMembers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductionId,FullName,RoleName,DisplayOrder")] CastMember castMember, IFormFile? photo)
        {
            var production = await _context.Productions.FindAsync(castMember.ProductionId);
            if (production == null)
            {
                return NotFound();
            }

            ValidatePhoto(photo);
            if (!ModelState.IsValid)
            {
                ViewData["Production"] = production;
                return View(castMember);
            }

            if (photo != null)
            {
                castMember.PhotoUrl = await _images.SaveAsync(photo, ImageFolder);
            }

            _context.Add(castMember);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"{castMember.FullName} eklendi.";
            return RedirectToAction(nameof(Index), new { productionId = castMember.ProductionId });
        }

        // GET: CastMembers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var castMember = await _context.CastMembers.Include(c => c.Production).FirstOrDefaultAsync(c => c.Id == id);
            if (castMember == null)
            {
                return NotFound();
            }

            ViewData["Production"] = castMember.Production;
            return View(castMember);
        }

        // POST: CastMembers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FullName,RoleName,DisplayOrder")] CastMember input, IFormFile? photo, bool removePhoto)
        {
            var castMember = await _context.CastMembers.Include(c => c.Production).FirstOrDefaultAsync(c => c.Id == id);
            if (castMember == null)
            {
                return NotFound();
            }

            ValidatePhoto(photo);
            if (!ModelState.IsValid)
            {
                input.Id = castMember.Id;
                input.ProductionId = castMember.ProductionId;
                input.PhotoUrl = castMember.PhotoUrl;
                ViewData["Production"] = castMember.Production;
                return View(input);
            }

            castMember.FullName = input.FullName;
            castMember.RoleName = input.RoleName;
            castMember.DisplayOrder = input.DisplayOrder;

            string? photoToDelete = null;
            if (photo != null)
            {
                photoToDelete = castMember.PhotoUrl;
                castMember.PhotoUrl = await _images.SaveAsync(photo, ImageFolder);
            }
            else if (removePhoto)
            {
                photoToDelete = castMember.PhotoUrl;
                castMember.PhotoUrl = null;
            }

            await _context.SaveChangesAsync();
            _images.Delete(photoToDelete);
            TempData["StatusMessage"] = $"{castMember.FullName} güncellendi.";
            return RedirectToAction(nameof(Index), new { productionId = castMember.ProductionId });
        }

        // POST: CastMembers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var castMember = await _context.CastMembers.FindAsync(id);
            if (castMember == null)
            {
                return NotFound();
            }

            _context.CastMembers.Remove(castMember);
            await _context.SaveChangesAsync();
            _images.Delete(castMember.PhotoUrl);
            TempData["StatusMessage"] = $"{castMember.FullName} kadrodan çıkarıldı.";
            return RedirectToAction(nameof(Index), new { productionId = castMember.ProductionId });
        }

        private void ValidatePhoto(IFormFile? photo)
        {
            if (photo != null && _images.Validate(photo) is { } error)
            {
                ModelState.AddModelError("photo", error);
            }
        }
    }
}
