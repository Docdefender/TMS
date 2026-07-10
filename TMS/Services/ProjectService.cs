using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class ProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public ProjectService(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns projects visible to the user.
    /// Admins/Managers see all. Regular users only see projects they own, manage, or are a member of.
    /// </summary>
    public async Task<List<Project>> GetAllProjectsAsync(
        List<string>? statuses = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        bool showAll = false)
    {
        var query = _context.Projects
            .Where(p => !p.IsDeleted)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Manager)
            .Include(p => p.Department)
            .Include(p => p.Category)
            .Include(p => p.Members)
            .AsQueryable();

        // Scope to user's projects if not admin/manager
        if (!showAll && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(p =>
                p.CreatedByUserId == userId ||
                p.ManagerUserId   == userId ||
                p.Members.Any(m => m.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        if (startDate.HasValue)
            query = query.Where(p => p.StartDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => !p.EndDate.HasValue || p.EndDate.Value <= endDate.Value);

        if (statuses != null && statuses.Count > 0)
        {
            var statusEnums = new List<ProjectStatus>();
            bool includeOverdue = false;

            foreach (var s in statuses)
            {
                if (s == "overdue")
                    includeOverdue = true;
                else if (Enum.TryParse<ProjectStatus>(s, true, out var ps))
                    statusEnums.Add(ps);
            }

            var today = DateTime.Today;
            if (statusEnums.Count > 0 && includeOverdue)
                query = query.Where(p => statusEnums.Contains(p.Status)
                    || (p.EndDate.HasValue && p.EndDate.Value < today
                        && p.Status != ProjectStatus.Completed
                        && p.Status != ProjectStatus.Cancelled));
            else if (statusEnums.Count > 0)
                query = query.Where(p => statusEnums.Contains(p.Status));
            else if (includeOverdue)
                query = query.Where(p => p.EndDate.HasValue && p.EndDate.Value < today
                    && p.Status != ProjectStatus.Completed
                    && p.Status != ProjectStatus.Cancelled);
        }

        return await query.OrderByDescending(p => p.StartDate).ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
                .ThenInclude(t => t.AssignedToUser)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Category)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Manager)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .Include(p => p.Department)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    /// <summary>
    /// Checks whether a user has access to a specific project.
    /// </summary>
    public async Task<bool> CanUserAccessProjectAsync(int projectId, string userId, bool isAdminOrManager)
    {
        if (isAdminOrManager) return true;

        return await _context.Projects
            .AnyAsync(p => p.Id == projectId && !p.IsDeleted && (
                p.CreatedByUserId == userId ||
                p.ManagerUserId   == userId ||
                p.Members.Any(m => m.UserId == userId)));
    }

    public async Task CreateProjectAsync(Project project, string? userId = null, List<string>? memberIds = null)
    {
        project.CreatedByUserId = userId;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        if (memberIds != null && memberIds.Count > 0)
        {
            foreach (var memberId in memberIds.Distinct())
            {
                if (memberId == project.ManagerUserId) continue;
                _context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = memberId });
            }
            await _context.SaveChangesAsync();
        }

        await _auditLogService.LogAsync("Created", "Project", project.Id, userId, $"Project '{project.Name}' created.");
    }

    public async Task UpdateProjectAsync(Project project, string? userId = null, List<string>? memberIds = null)
    {
        _context.Projects.Update(project);

        if (memberIds != null)
        {
            var existing = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id)
                .ToListAsync();
            _context.ProjectMembers.RemoveRange(existing);

            foreach (var memberId in memberIds.Distinct())
            {
                if (memberId == project.ManagerUserId) continue;
                _context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = memberId });
            }
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Updated", "Project", project.Id, userId, $"Project '{project.Name}' updated.");
    }

    public async Task DeleteProjectAsync(int id, string? userId = null)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is not null)
        {
            project.IsDeleted      = true;
            project.DeletedAt      = DateTime.UtcNow;
            project.DeletedByUserId = userId;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("SoftDelete", "Project", id, userId, $"Project '{project.Name}' soft deleted.");
        }
    }

    // ── Recycle Bin ──────────────────────────────────────────────────────

    public async Task<List<Project>> GetDeletedProjectsAsync()
    {
        return await _context.Projects
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Manager)
            .Include(p => p.Department)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync();
    }

    public async Task RestoreProjectAsync(int id, string? userId = null)
    {
        var project = await _context.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is not null)
        {
            project.IsDeleted       = false;
            project.DeletedAt       = null;
            project.DeletedByUserId = null;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("Restored", "Project", id, userId, $"Project '{project.Name}' restored.");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await _context.Users.OrderBy(u => u.FullName).ToListAsync();
    }
}
