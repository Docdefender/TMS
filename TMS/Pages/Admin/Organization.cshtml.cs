using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class OrganizationModel(ApplicationDbContext db, UserManager<ApplicationUser> users, AccessService access, AuditLogService audit) : PageModel
{
    public List<Department> Departments { get; set; } = [];
    public List<ApplicationUser> Managers { get; set; } = [];
    public List<ApplicationUser> Users { get; set; } = [];
    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        AccessService.Require((await access.ActorAsync()).IsAdmin);
        Departments = await db.Departments.Include(x => x.Managers).OrderBy(x => x.Name).ToListAsync();
        Managers = await access.AssignableUsersAsync(managersOnly: true);
        Users = await db.Users.OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task<IActionResult> OnPostManagersAsync(int departmentId, List<string> managerIds, string? defaultManagerId)
    {
        AccessService.Require((await access.ActorAsync()).IsAdmin);
        if (!await db.Departments.AnyAsync(x => x.Id == departmentId)) return NotFound();
        var selected = managerIds.Distinct().ToHashSet();
        var eligible = (await access.AssignableUsersAsync(managersOnly: true)).Select(x => x.Id).ToHashSet();
        if (selected.Except(eligible).Any() || selected.Count > 0 && (defaultManagerId is null || !selected.Contains(defaultManagerId)))
        { Message = "Sorumlu Manager'ları ve aralarından bir varsayılan Manager seçin."; return RedirectToPage(); }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var previous = await db.ManagerDepartments.Where(x => x.DepartmentId == departmentId).ToListAsync();
        // Clear old default first: SQL Server's filtered unique index applies per statement.
        foreach (var link in previous) link.IsDefault = false;
        await db.SaveChangesAsync();
        db.ManagerDepartments.RemoveRange(previous.Where(x => !selected.Contains(x.UserId)));
        foreach (var id in selected)
        {
            var link = previous.FirstOrDefault(x => x.UserId == id);
            if (link is null) { link = new ManagerDepartment { DepartmentId = departmentId, UserId = id }; db.ManagerDepartments.Add(link); }
            link.IsDefault = id == defaultManagerId;
        }
        await db.SaveChangesAsync();
        await audit.LogAsync("Updated", "Department", departmentId, (await access.ActorAsync()).UserId, "Departman Manager sorumlulukları güncellendi.");
        await tx.CommitAsync();
        Message = "Departman sorumluları güncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUserDepartmentAsync(string userId, int? departmentId)
    {
        AccessService.Require((await access.ActorAsync()).IsAdmin);
        var user = await users.FindByIdAsync(userId);
        if (user is null) return NotFound();
        if (departmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == departmentId)) return BadRequest();
        user.DepartmentId = departmentId;
        var result = await users.UpdateAsync(user);
        Message = result.Succeeded ? "Kullanıcının ana departmanı güncellendi. Manager sorumlulukları ayrıca yönetilir." : "Güncelleme yapılamadı.";
        if (result.Succeeded) await audit.LogAsync("Updated", "User", null, (await access.ActorAsync()).UserId, "Kullanıcı departmanı güncellendi.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeHistoryAsync(int taskId, string userId)
    {
        AccessService.Require((await access.ActorAsync()).IsAdmin);
        var grant = await db.TaskHistoryAccesses.FindAsync(taskId, userId);
        if (grant is null) { Message = "Bu görev ve kullanıcı için devirden kalan erişim bulunamadı."; return RedirectToPage(); }
        grant.RevokedAt = DateTime.UtcNow;
        grant.RevokedByUserId = (await access.ActorAsync()).UserId;
        await db.SaveChangesAsync();
        await audit.LogAsync("AccessRevoked", "TaskItem", taskId, grant.RevokedByUserId, "Devir geçmişinden gelen okuma erişimi kaldırıldı.");
        Message = "Geçmiş erişimi kaldırıldı. Aktif atama ve üyelikten gelen yetkiler korunur.";
        return RedirectToPage();
    }
}
