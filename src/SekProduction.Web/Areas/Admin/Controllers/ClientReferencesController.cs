using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SeedData.AdminRole)]
    public class ClientReferencesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientReferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClientReferences
        public async Task<IActionResult> Index()
        {
            return View(await _context.ClientReferences.ToListAsync());
        }

        // GET: ClientReferences/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientReference = await _context.ClientReferences
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clientReference == null)
            {
                return NotFound();
            }

            return View(clientReference);
        }

        // GET: ClientReferences/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ClientReferences/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClientName,LogoUrl,WebsiteUrl,Description,DisplayOrder,IsPublished")] ClientReference clientReference)
        {
            if (ModelState.IsValid)
            {
                _context.Add(clientReference);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(clientReference);
        }

        // GET: ClientReferences/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientReference = await _context.ClientReferences.FindAsync(id);
            if (clientReference == null)
            {
                return NotFound();
            }
            return View(clientReference);
        }

        // POST: ClientReferences/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientName,LogoUrl,WebsiteUrl,Description,DisplayOrder,IsPublished")] ClientReference clientReference)
        {
            if (id != clientReference.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(clientReference);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientReferenceExists(clientReference.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(clientReference);
        }

        // GET: ClientReferences/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientReference = await _context.ClientReferences
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clientReference == null)
            {
                return NotFound();
            }

            return View(clientReference);
        }

        // POST: ClientReferences/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clientReference = await _context.ClientReferences.FindAsync(id);
            if (clientReference != null)
            {
                _context.ClientReferences.Remove(clientReference);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClientReferenceExists(int id)
        {
            return _context.ClientReferences.Any(e => e.Id == id);
        }
    }
}
