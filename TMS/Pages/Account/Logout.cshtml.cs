using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuditLogService _auditLogService;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, AuditLogService auditLogService)
    {
        _signInManager = signInManager;
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _signInManager.UserManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        await _auditLogService.LogAsync("Logout", "User", null, userId, "User logged out.");
        return RedirectToPage("/Account/Login");
    }
}
