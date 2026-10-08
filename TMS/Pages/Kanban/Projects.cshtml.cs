using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Kanban;

[Authorize]
public class ProjectsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly AuditLogService _auditLog;

    public ProjectsModel(ApplicationDbContext context, AccessService access, AuditLogService auditLog)
    {
        _context = context;
        _access = access;
        _auditLog = auditLog;
    }

    public IReadOnlyList<Project> Projects { get; private set; } = [];
    public bool CanMoveProjects { get; private set; }

    public async Task OnGetAsync()
    {
        var actor = await _access.ActorAsync();
        CanMoveProjects = actor.IsAdmin;
        var visibleTaskIds = (await (await _access.TasksAsync()).Select(x => x.Id).ToArrayAsync()).ToHashSet();

        Projects = await (await _access.ProjectsAsync())
            .Include(x => x.Manager)
            .Include(x => x.Department)
            .Include(x => x.Tasks)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .AsSplitQuery()
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        foreach (var project in Projects)
            project.Tasks = project.Tasks.Where(x => !x.IsDeleted && visibleTaskIds.Contains(x.Id)).ToList();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateProjectStatusRequest request)
    {
        var actor = await _access.ActorAsync();
        if (!actor.IsAdmin) return Forbid();
        if (request is null || !Enum.TryParse<ProjectStatus>(request.NewStatus, true, out var newStatus)
            || !Enum.IsDefined(newStatus))
            return BadRequest(new { success = false, message = "Geçersiz proje durumu." });

        var project = await _context.Projects.FirstOrDefaultAsync(x => x.Id == request.ProjectId && !x.IsDeleted);
        if (project is null) return NotFound(new { success = false, message = "Proje bulunamadı." });

        var oldStatus = project.Status;
        if (oldStatus == newStatus) return new JsonResult(new { success = true });
        project.Status = newStatus;
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("StatusChanged", "Project", project.Id, actor.UserId,
            $"Proje '{project.Name}' durumu: {oldStatus} → {newStatus}.");

        return new JsonResult(new { success = true });
    }

    public sealed class UpdateProjectStatusRequest
    {
        public int ProjectId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
