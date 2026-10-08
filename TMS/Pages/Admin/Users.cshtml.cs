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
        if (NewUser.Role is not ("Admin" or "Manager" or "Member")) return BadRequest();
        if (NewUser.DepartmentId.HasValue && !await _context.Departments.AnyAsync(x => x.Id == NewUser.DepartmentId)) return BadRequest();
        await LoadAsync();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x)));
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewUser.FullName) ||
            string.IsNullOrWhiteSpace(NewUser.Email) ||
            string.IsNullOrWhiteSpace(NewUser.Password))
        {
            TempData["Error"] = "Ad Soyad, e-posta ve şifre zorunludur.";
            return Page();
        }

        var existing = await _userManager.FindByEmailAsync(NewUser.Email);
        if (existing is not null)
        {
            TempData["Error"] = "Bu e-posta adresi zaten kullanımda.";
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

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var result = await _userManager.CreateAsync(user, NewUser.Password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        var roleResult = await _userManager.AddToRoleAsync(user, NewUser.Role);
        if (!roleResult.Succeeded)
        {
            TempData["Error"] = "Kullanıcı rolü atanamadı. Kullanıcı oluşturulmadı; tekrar deneyin.";
            return Page();
        }
        await transaction.CommitAsync();

        TempData["Success"] = $"{NewUser.FullName} kullanıcısı başarıyla oluşturuldu.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string role)
    {
        if (role is not ("Admin" or "Manager" or "Member")) return BadRequest();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (role != "Manager" && (await _context.ManagerDepartments.AnyAsync(x => x.UserId == userId)
            || await _context.Projects.AnyAsync(x => x.ManagerUserId == userId)))
        {
            TempData["Error"] = "Rolü değiştirmeden önce departman ve proje sorumluluklarını başka bir Manager'a aktarın.";
            return RedirectToPage(new { Search });
        }
        if (await _userManager.IsInRoleAsync(user, "Admin") && role != "Admin" && (await _userManager.GetUsersInRoleAsync("Admin")).Count <= 1)
        { TempData["Error"] = "Son Admin'in rolü değiştirilemez."; return RedirectToPage(new { Search }); }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var removed = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removed.Succeeded) { TempData["Error"] = "Rol değiştirilemedi."; return RedirectToPage(new { Search }); }

        var added = await _userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded) { TempData["Error"] = "Rol değiştirilemedi."; return RedirectToPage(new { Search }); }
        var stamp = await _userManager.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded) { TempData["Error"] = "Rol değiştirilemedi."; return RedirectToPage(new { Search }); }
        await transaction.CommitAsync();

        TempData["Success"] = "Kullanıcı rolü güncellendi.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostDeactivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (user.Email == "admin@tms.com" || user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Varsayılan yönetici veya kendi hesabınız pasifleştirilemez.";
            return RedirectToPage(new { Search });
        }
        if (!user.IsActive) return RedirectToPage(new { Search });
        if (await _context.ManagerDepartments.AnyAsync(x => x.UserId == userId)
            || await _context.Projects.AnyAsync(x => x.ManagerUserId == userId
                && x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.Cancelled)
            || await _context.TaskItems.AnyAsync(x => x.AssignedToUserId == userId && x.Status != TMS.Models.TaskStatus.Done)
            || await _context.Tickets.AnyAsync(x => x.AssignedToUserId == userId && x.Status != TicketStatus.Closed))
        {
            TempData["Error"] = "Hesabı pasifleştirmeden önce yöneticilik ve açık iş sorumluluklarını devredin.";
            return RedirectToPage(new { Search });
        }
        user.IsActive = false;
        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Hesap pasifleştirilemedi: " + string.Join(" ", result.Errors.Select(x => x.Description));
            return RedirectToPage(new { Search });
        }
        TempData["Success"] = "Kullanıcı pasifleştirildi; geçmiş kayıtları korundu.";
        return RedirectToPage(new { Search });
    }

    public async Task<IActionResult> OnPostActivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (user.IsActive) return RedirectToPage(new { Search });
        user.IsActive = true;
        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Hesap yeniden etkinleştirilemedi: " + string.Join(" ", result.Errors.Select(x => x.Description));
            return RedirectToPage(new { Search });
        }
        TempData["Success"] = "Kullanıcı yeniden etkinleştirildi.";
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
                Roles          = roles.ToList(),
                IsActive       = u.IsActive
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
        public bool IsActive { get; set; }
    }

    public class CreateUserInputModel
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Şifre gereklidir.")]
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public int? DepartmentId { get; set; }
    }
}
