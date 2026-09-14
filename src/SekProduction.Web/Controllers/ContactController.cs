using Microsoft.AspNetCore.Mvc;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Controllers;

[Route("Iletisim")]
public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new ContactFormViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        _context.ContactMessages.Add(new ContactMessage
        {
            FullName = form.FullName,
            Email = form.Email,
            Phone = form.Phone,
            Subject = form.Subject,
            Message = form.Message,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["ContactSuccess"] = "Mesajınız için teşekkürler. En kısa sürede size dönüş yapacağız.";
        return RedirectToAction(nameof(Index));
    }
}
