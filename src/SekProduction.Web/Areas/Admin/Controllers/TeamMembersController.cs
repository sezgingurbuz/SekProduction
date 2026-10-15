using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;
using SekProduction.Web.Services;

namespace SekProduction.Web.Areas.Admin.Controllers;

// Ekip kayıtları. Kimlik fotokopileri wwwroot dışında tutulur ve yalnızca bu
// controller üzerinden, yönetici yetkisiyle görüntülenir.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class TeamMembersController : Controller
{
    private const string DocumentFolder = "team-documents";

    private readonly ApplicationDbContext _context;
    private readonly PrivateFileStorage _storage;

    public TeamMembersController(ApplicationDbContext context, PrivateFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    // GET: Admin/TeamMembers
    public async Task<IActionResult> Index()
    {
        var members = await _context.TeamMembers.ToListAsync();
        return View(members
            .OrderBy(m => m.FullName, CityDirectory.TurkishComparer)
            .ToList());
    }

    // GET: Admin/TeamMembers/Create
    public IActionResult Create() => View(new TeamMemberFormViewModel());

    // POST: Admin/TeamMembers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(PrivateFileStorage.MaxBytes + 8192)]
    public async Task<IActionResult> Create(TeamMemberFormViewModel form)
    {
        ValidateDocument(form);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var member = new TeamMember();
        Apply(form, member);

        if (form.IdDocument is not null)
        {
            member.IdDocumentFileName = await _storage.SaveAsync(form.IdDocument, DocumentFolder);
            member.IdDocumentOriginalName = Path.GetFileName(form.IdDocument.FileName);
            member.IdDocumentUploadedAt = DateTime.UtcNow;
        }

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{member.FullName} ekip listesine eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/TeamMembers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);
        if (member is null)
        {
            return NotFound();
        }

        return View(new TeamMemberFormViewModel
        {
            Id = member.Id,
            FullName = member.FullName,
            Title = member.Title,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            HasIdDocument = member.HasIdDocument,
            IdDocumentOriginalName = member.IdDocumentOriginalName,
            IdDocumentUploadedAt = member.IdDocumentUploadedAt
        });
    }

    // POST: Admin/TeamMembers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(PrivateFileStorage.MaxBytes + 8192)]
    public async Task<IActionResult> Edit(int id, TeamMemberFormViewModel form)
    {
        var member = await _context.TeamMembers.FindAsync(id);
        if (member is null)
        {
            return NotFound();
        }

        ValidateDocument(form);
        form.Id = member.Id;
        if (!ModelState.IsValid)
        {
            form.HasIdDocument = member.HasIdDocument;
            form.IdDocumentOriginalName = member.IdDocumentOriginalName;
            form.IdDocumentUploadedAt = member.IdDocumentUploadedAt;
            return View(form);
        }

        Apply(form, member);
        member.UpdatedAt = DateTime.UtcNow;

        // Yeni dosya geldiyse veya silme işaretlendiyse eskisi diskten de kaldırılır.
        if (form.IdDocument is not null || form.RemoveIdDocument)
        {
            _storage.Delete(DocumentFolder, member.IdDocumentFileName);
            member.IdDocumentFileName = null;
            member.IdDocumentOriginalName = null;
            member.IdDocumentUploadedAt = null;
        }

        if (form.IdDocument is not null)
        {
            member.IdDocumentFileName = await _storage.SaveAsync(form.IdDocument, DocumentFolder);
            member.IdDocumentOriginalName = Path.GetFileName(form.IdDocument.FileName);
            member.IdDocumentUploadedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{member.FullName} güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/TeamMembers/Document/5 — kimlik fotokopisini yalnızca yöneticiye gösterir.
    public async Task<IActionResult> Document(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);
        if (member is null || !member.HasIdDocument)
        {
            return NotFound();
        }

        var stream = _storage.Open(DocumentFolder, member.IdDocumentFileName);
        if (stream is null)
        {
            return NotFound();
        }

        // Tarayıcının içeriği tahmin etmesini engelle; dosya kesinlikle önbelleğe alınmasın.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(stream, PrivateFileStorage.ContentTypeFor(member.IdDocumentFileName));
    }

    // POST: Admin/TeamMembers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var member = await _context.TeamMembers.FindAsync(id);
        if (member is null)
        {
            return NotFound();
        }

        _storage.Delete(DocumentFolder, member.IdDocumentFileName);
        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"{member.FullName} ekip listesinden silindi.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateDocument(TeamMemberFormViewModel form)
    {
        if (form.IdDocument is null)
        {
            return;
        }

        var error = _storage.Validate(form.IdDocument);
        if (error is not null)
        {
            ModelState.AddModelError(nameof(form.IdDocument), error);
        }
    }

    private static void Apply(TeamMemberFormViewModel form, TeamMember member)
    {
        member.FullName = form.FullName.Trim();
        member.Title = form.Title.Trim();
        member.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
        member.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
    }
}
