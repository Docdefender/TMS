using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly DepartmentService _departmentService;

    public UsersModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context, DepartmentService departmentService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _departmentService = departmentService;
    }

    public List<UserViewModel> Users { get; set; } = new();
    public SelectList DepartmentList { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public CreateUserInputModel NewUser { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateUserAsync()
    {
        await LoadAsync();

        if (string.IsNullOrWhiteSpace(NewUser.FullName) ||
            string.IsNullOrWhiteSpace(NewUser.Email) ||
            string.IsNullOrWhiteSpace(NewUser.Password))
        {
            TempData["Error"] = "Ad Soyad, e-posta ve þifre zorunludur.";
            return Page();
        }

        var existing = await _userManager.FindByEmailAsync(NewUser.Email);
        if (existing is not null)
        {
            TempData["Error"] = "Bu e-posta adresi zaten kullanýmda.";
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName      = NewUser.Email,
            Email         = NewUser.Email,
            FullName      = NewUser.FullName,
            DepartmentId  = NewUser.DepartmentId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, NewUser.Password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        if (!string.IsNullOrEmpty(NewUser.Role))
            await _userManager.AddToRoleAsync(user, NewUser.Role);

        TempData["Success"] = $"{NewUser.FullName} kullanýcýsý baþarýyla oluþturuldu.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!string.IsNullOrEmpty(role))
            await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = "Kullanýcý rolü güncellendi.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (user.Email == "admin@tms.com")
        {
            TempData["Error"] = "Varsayýlan sistem yöneticisi silinemez.";
            return RedirectToPage(new { Search });
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "Kullanýcý silindi.";
        return RedirectToPage(new { Search });
    }

    private async Task LoadAsync()
    {
        var users = _userManager.Users.Include(u => u.Department).ToList();

        if (!string.IsNullOrWhiteSpace(Search))
            users = users.Where(u =>
                (u.FullName != null && u.FullName.Contains(Search, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email    != null && u.Email.Contains(Search, StringComparison.OrdinalIgnoreCase))).ToList();

        var result = new List<UserViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserViewModel
            {
                Id             = u.Id,
                FullName       = u.FullName,
                Email          = u.Email ?? string.Empty,
                DepartmentName = u.Department?.Name,
                Roles          = roles.ToList()
            });
        }

        Users = result.OrderBy(u => u.FullName).ToList();

        var departments = await _departmentService.GetAllAsync();
        DepartmentList = new SelectList(departments, nameof(Department.Id), nameof(Department.Name));
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class CreateUserInputModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public int? DepartmentId { get; set; }
    }
}
