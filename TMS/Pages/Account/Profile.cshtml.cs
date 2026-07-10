using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLogService;

    public ProfileModel(UserManager<ApplicationUser> userManager, AuditLogService auditLogService)
    {
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public ApplicationUser CurrentUser { get; set; } = null!;
    public string UserRole { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    [BindProperty]
    public ChangePasswordInputModel PasswordInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user is null) return NotFound();

        CurrentUser = user;
        var roles = await _userManager.GetRolesAsync(user);
        UserRole = roles.FirstOrDefault() ?? "Kullanıcı";
        Input.FullName = user.FullName;
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        ModelState.Remove(nameof(PasswordInput.CurrentPassword));
        ModelState.Remove(nameof(PasswordInput.NewPassword));
        ModelState.Remove(nameof(PasswordInput.ConfirmPassword));

        var user = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            CurrentUser = user;
            var roles = await _userManager.GetRolesAsync(user);
            UserRole = roles.FirstOrDefault() ?? "Kullanıcı";
            return Page();
        }

        user.FullName = Input.FullName;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _auditLogService.LogAsync("Updated", "UserProfile", null, user.Id,
                $"Ad güncellendi: {user.FullName}");
            StatusMessage = "Profiliniz başarıyla güncellendi.";
            return RedirectToPage();
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        CurrentUser = user;
        var r = await _userManager.GetRolesAsync(user);
        UserRole = r.FirstOrDefault() ?? "Kullanıcı";
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        ModelState.Remove(nameof(Input.FullName));

        var user = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            CurrentUser = user;
            var roles = await _userManager.GetRolesAsync(user);
            UserRole = roles.FirstOrDefault() ?? "Kullanıcı";
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            PasswordInput.CurrentPassword,
            PasswordInput.NewPassword);

        if (result.Succeeded)
        {
            await _auditLogService.LogAsync("ChangePassword", "UserProfile", null, user.Id,
                "Şifre değiştirildi.");
            StatusMessage = "Şifreniz başarıyla değiştirildi.";
            return RedirectToPage();
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        CurrentUser = user;
        var r = await _userManager.GetRolesAsync(user);
        UserRole = r.FirstOrDefault() ?? "Kullanıcı";
        return Page();
    }
}

public class ProfileInputModel
{
    [Required(ErrorMessage = "Ad Soyad gereklidir")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;
}

public class ChangePasswordInputModel
{
    [Required(ErrorMessage = "Mevcut şifre gereklidir")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre gereklidir")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
