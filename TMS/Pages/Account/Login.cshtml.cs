using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLogService;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        AuditLogService auditLogService,
        IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _environment = environment;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public bool ShowDemoLogin => IsDemoLoginAllowed();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var account = await _userManager.FindByEmailAsync(Input.Email);
        if (account is null || !account.IsActive)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre geçersiz.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            await _auditLogService.LogAsync("Login", "User", null, user?.Id, $"User '{Input.Email}' logged in.");
            return RedirectToPage("/Index");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }

    public async Task<IActionResult> OnPostDemoAsync(string profile)
    {
        if (!IsDemoLoginAllowed())
        {
            return NotFound();
        }

        var demoAccount = profile switch
        {
            "admin" => (Email: "admin@tms.com", Role: "Admin"),
            "manager" => (Email: "deniz.manager@demo.dizge.test", Role: "Manager"),
            "member" => (Email: "ece.member@demo.dizge.test", Role: "Member"),
            _ => default
        };

        if (string.IsNullOrWhiteSpace(demoAccount.Email))
        {
            return BadRequest();
        }

        var account = await _userManager.FindByEmailAsync(demoAccount.Email);
        if (account is null || !account.IsActive || !await _userManager.IsInRoleAsync(account, demoAccount.Role))
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Seçilen demo hesabı kullanıma hazır değil.");
            return Page();
        }

        await _signInManager.SignInAsync(account, isPersistent: false);
        await _auditLogService.LogAsync(
            "Login",
            "User",
            null,
            account.Id,
            $"Local demo login used for '{demoAccount.Email}'.");

        return RedirectToPage("/Index");
    }

    private bool IsDemoLoginAllowed()
    {
        if (!_environment.IsDevelopment())
        {
            return false;
        }

        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        return remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);
    }
}
