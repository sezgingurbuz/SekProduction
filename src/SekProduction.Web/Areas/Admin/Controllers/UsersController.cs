using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SekProduction.Web.Data;
using SekProduction.Web.Models;

namespace SekProduction.Web.Areas.Admin.Controllers;

// Çalışan hesapları yalnızca buradan açılır; kendi kendine kayıt kapalıdır.
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: Admin/Users
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ThenBy(u => u.Email).ToListAsync();
        var items = new List<UserListItemViewModel>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToListItemAsync(user));
        }

        return View(items);
    }

    // GET: Admin/Users/Create
    public IActionResult Create() => View(new UserFormViewModel());

    // POST: Admin/Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError(nameof(form.Password), "Yeni hesap için şifre belirleyin.");
        }
        if (!AppRoles.All.Contains(form.Role))
        {
            ModelState.AddModelError(nameof(form.Role), "Geçersiz yetki.");
        }
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var email = form.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = form.FullName.Trim(),
            JobTitle = string.IsNullOrWhiteSpace(form.JobTitle) ? null : form.JobTitle.Trim()
        };

        var created = await _userManager.CreateAsync(user, form.Password!);
        if (!created.Succeeded)
        {
            AddErrors(created);
            return View(form);
        }

        await _userManager.AddToRoleAsync(user, form.Role);
        await SetActiveAsync(user, form.IsActive);

        TempData["StatusMessage"] = $"{user.FullName} ({AppRoles.DisplayName(form.Role)}) hesabı oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Users/Edit/<id>
    public async Task<IActionResult> Edit(string? id)
    {
        var user = id is null ? null : await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            JobTitle = user.JobTitle,
            Email = user.Email ?? string.Empty,
            Role = await GetRoleAsync(user),
            IsActive = user.IsActive
        });
    }

    // POST: Admin/Users/Edit/<id>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel form)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!AppRoles.All.Contains(form.Role))
        {
            ModelState.AddModelError(nameof(form.Role), "Geçersiz yetki.");
        }

        var currentRole = await GetRoleAsync(user);
        if (currentRole == AppRoles.Admin && form.Role != AppRoles.Admin && !await HasAnotherAdminAsync(user.Id))
        {
            ModelState.AddModelError(nameof(form.Role), "Sistemde en az bir yönetici kalmalı.");
        }
        if (IsSelf(user) && !form.IsActive)
        {
            ModelState.AddModelError(nameof(form.IsActive), "Kendi hesabınızı pasife alamazsınız.");
        }

        form.Id = user.Id;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var email = form.Email.Trim();
        user.FullName = form.FullName.Trim();
        user.JobTitle = string.IsNullOrWhiteSpace(form.JobTitle) ? null : form.JobTitle.Trim();
        user.UserName = email;
        user.Email = email;

        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            AddErrors(updated);
            return View(form);
        }

        if (!string.IsNullOrWhiteSpace(form.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, form.Password);
            if (!reset.Succeeded)
            {
                AddErrors(reset);
                return View(form);
            }
        }

        if (currentRole != form.Role)
        {
            await _userManager.RemoveFromRolesAsync(user, AppRoles.All);
            await _userManager.AddToRoleAsync(user, form.Role);
        }

        await SetActiveAsync(user, form.IsActive);

        TempData["StatusMessage"] = $"{user.FullName} hesabı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Users/Delete/<id>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (IsSelf(user))
        {
            TempData["StatusError"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) && !await HasAnotherAdminAsync(user.Id))
        {
            TempData["StatusError"] = "Sistemdeki son yönetici hesabı silinemez.";
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _userManager.DeleteAsync(user);
        TempData[deleted.Succeeded ? "StatusMessage" : "StatusError"] = deleted.Succeeded
            ? $"{user.DisplayName} hesabı silindi."
            : string.Join(" ", deleted.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }

    private bool IsSelf(AppUser user) => user.Id == _userManager.GetUserId(User);

    private async Task<string> GetRoleAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault(AppRoles.All.Contains) ?? AppRoles.Employee;
    }

    private async Task<bool> HasAnotherAdminAsync(string excludedUserId)
    {
        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Any(a => a.Id != excludedUserId);
    }

    // Pasif hesap, süresi dolmayan bir kilitle engellenir; aktife alınınca kilit kaldırılır.
    private async Task SetActiveAsync(AppUser user, bool isActive)
    {
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, isActive ? null : DateTimeOffset.MaxValue);
    }

    private async Task<UserListItemViewModel> ToListItemAsync(AppUser user) => new()
    {
        Id = user.Id,
        FullName = user.DisplayName,
        JobTitle = user.JobTitle,
        Email = user.Email ?? string.Empty,
        Role = await GetRoleAsync(user),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
