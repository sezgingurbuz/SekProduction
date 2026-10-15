using System.ComponentModel.DataAnnotations;

namespace SekProduction.Web.Models;

public class UserListItemViewModel
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public string? JobTitle { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public string RoleDisplayName => AppRoles.DisplayName(Role);
}

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Ad soyad girin."), StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Görev")]
    public string? JobTitle { get; set; }

    [Required(ErrorMessage = "E-posta girin."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin."), StringLength(150)]
    [Display(Name = "E-posta (giriş adresi)")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Yetki")]
    public string Role { get; set; } = AppRoles.Employee;

    [Display(Name = "Hesap aktif")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password), StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    [Display(Name = "Şifre")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler aynı değil.")]
    [Display(Name = "Şifre (tekrar)")]
    public string? ConfirmPassword { get; set; }

    public bool IsNew => string.IsNullOrEmpty(Id);
}
