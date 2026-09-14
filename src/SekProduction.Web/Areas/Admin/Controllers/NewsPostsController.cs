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
    public class NewsPostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsPostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: NewsPosts
        public async Task<IActionResult> Index()
        {
            return View(await _context.NewsPosts.ToListAsync());
        }

        // GET: NewsPosts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsPost = await _context.NewsPosts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (newsPost == null)
            {
                return NotFound();
            }

            return View(newsPost);
        }

        // GET: NewsPosts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: NewsPosts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Slug,Summary,Content,CoverImageUrl,AuthorName,PublishedAt,IsPublished")] NewsPost newsPost)
        {
            if (ModelState.IsValid)
            {
                _context.Add(newsPost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(newsPost);
        }

        // GET: NewsPosts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsPost = await _context.NewsPosts.FindAsync(id);
            if (newsPost == null)
            {
                return NotFound();
            }
            return View(newsPost);
        }

        // POST: NewsPosts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Slug,Summary,Content,CoverImageUrl,AuthorName,PublishedAt,IsPublished")] NewsPost newsPost)
        {
            if (id != newsPost.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(newsPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsPostExists(newsPost.Id))
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
            return View(newsPost);
        }

        // GET: NewsPosts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var newsPost = await _context.NewsPosts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (newsPost == null)
            {
                return NotFound();
            }

            return View(newsPost);
        }

        // POST: NewsPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var newsPost = await _context.NewsPosts.FindAsync(id);
            if (newsPost != null)
            {
                _context.NewsPosts.Remove(newsPost);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NewsPostExists(int id)
        {
            return _context.NewsPosts.Any(e => e.Id == id);
        }
    }
}
