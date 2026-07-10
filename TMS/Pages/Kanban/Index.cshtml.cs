using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Kanban;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLogService;
    private readonly ProjectService _projectService;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        AuditLogService auditLogService, ProjectService projectService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _projectService = projectService;
    }

    public List<Project> NotStartedProjects { get; set; } = new();
    public List<Project> InProgressProjects { get; set; } = new();
    public List<Project> CompletedProjects  { get; set; } = new();
    public List<Project> OnHoldProjects     { get; set; } = new();
    public List<Project> CancelledProjects  { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId  = _userManager.GetUserId(User)!;
        var showAll = User.IsInRole("Admin") || User.IsInRole("Manager");

        var query = _context.Projects
            .Where(p => !p.IsDeleted)
            .Include(p => p.Manager)
            .Include(p => p.Department)
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .AsQueryable();

        if (!showAll)
        {
            query = query.Where(p =>
                p.CreatedByUserId == userId ||
                p.ManagerUserId   == userId ||
                p.Members.Any(m => m.UserId == userId));
        }

        var allProjects = await query.OrderByDescending(p => p.StartDate).ToListAsync();

        NotStartedProjects = allProjects.Where(p => p.Status == ProjectStatus.NotStarted).ToList();
        InProgressProjects = allProjects.Where(p => p.Status == ProjectStatus.InProgress).ToList();
        CompletedProjects  = allProjects.Where(p => p.Status == ProjectStatus.Completed).ToList();
        OnHoldProjects     = allProjects.Where(p => p.Status == ProjectStatus.OnHold).ToList();
        CancelledProjects  = allProjects.Where(p => p.Status == ProjectStatus.Cancelled).ToList();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateStatusRequest request)
    {
        if (request is null || !Enum.TryParse<ProjectStatus>(request.NewStatus, true, out var newStatus))
            return BadRequest();

        var project = await _projectService.GetProjectByIdAsync(request.ProjectId);
        if (project is null) return NotFound();

        // Only admin/manager or the project manager can change status via Kanban
        var userId = _userManager.GetUserId(User)!;
        var canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") || project.ManagerUserId == userId;
        if (!canEdit) return Forbid();

        var oldStatus = project.Status;
        project.Status = newStatus;
        await _projectService.UpdateProjectAsync(project, userId);

        await _auditLogService.LogAsync("StatusChanged", "Project", project.Id, userId,
            $"Proje '{project.Name}' durumu: {oldStatus} → {newStatus}.");

        return new JsonResult(new { success = true });
    }

    public class UpdateStatusRequest
    {
        public int ProjectId  { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
