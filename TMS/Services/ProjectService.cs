using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class ProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly AuditLogService _auditLogService;

    public ProjectService(ApplicationDbContext context, AccessService access, AuditLogService auditLogService)
    {
        _context = context;
        _access = access;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns projects visible to the user.
    /// Applies the current user's department, membership and explicit assignment access.
    /// </summary>
    public async Task<List<Project>> GetAllProjectsAsync(
        List<string>? statuses = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        bool showAll = false)
    {
        var query = (await _access.ProjectsAsync())
            .Where(p => !p.IsDeleted)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Manager)
            .Include(p => p.Department)
            .Include(p => p.Category)
            .Include(p => p.Members)
            .AsQueryable();


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
        if (!(await _access.ProjectAsync(id)).View) return null;
        var visibleTaskIds = await (await _access.TasksAsync()).Where(t => t.ProjectId == id)
            .Select(t => t.Id).ToListAsync();
        var project = await _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Tasks)
                .ThenInclude(t => t.AssignedToUser)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Category)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Comments)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Attachments)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Labels)
                    .ThenInclude(x => x.TaskLabel)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Manager)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .Include(p => p.Department)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (project is not null)
            project.Tasks = project.Tasks.Where(t => visibleTaskIds.Contains(t.Id)).ToList();
        return project;
    }

    /// <summary>
    /// Checks whether a user has access to a specific project.
    /// </summary>
    public async Task<bool> CanUserAccessProjectAsync(int projectId, string userId, bool isAdminOrManager)
    {
        return (await _access.ProjectAsync(projectId)).View;

    }

    public async Task CreateProjectAsync(Project project, string? userId = null, List<string>? memberIds = null)
    {
        AccessService.Require(await _access.CanCreateProjectAsync(project.DepartmentId));
        await ValidateAsync(project, memberIds);
        userId = (await _access.ActorAsync()).UserId;
        project.Id = 0;
        project.IsDeleted = false;
        project.DeletedAt = null;
        project.DeletedByUserId = null;
        project.DeletionBatchId = null;
        project.FirstAssignedByUserId = userId;
        project.AssignedToUserId = null;
        project.CreatedByUser = null;
        project.AssignedToUser = null;
        project.Manager = null;
        project.Department = null;
        project.Category = null;
        project.Members = new List<ProjectMember>();
        project.Tasks = new List<TaskItem>();
        project.Comments = new List<Comment>();
        project.Attachments = new List<Attachment>();
        using var transaction = await _context.Database.BeginTransactionAsync();
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
        await transaction.CommitAsync();
    }

    public async Task UpdateProjectAsync(Project project, string? userId = null, List<string>? memberIds = null)
    {
        var rights = await _access.ProjectAsync(project.Id);
        AccessService.Require(rights.Edit);
        var original = await _context.Projects.AsNoTracking().FirstAsync(x => x.Id == project.Id);
        AccessService.Require(project.Status == original.Status || rights.ChangeStatus);
        var actor = await _access.ActorAsync();
        if (project.DepartmentId != original.DepartmentId)
        {
            AccessService.Require(actor.IsAdmin);
            project.ManagerUserId = await _context.ManagerDepartments
                .Where(x => x.DepartmentId == project.DepartmentId && x.IsDefault && !x.Department.IsDeleted)
                .Select(x => x.UserId).SingleOrDefaultAsync()
                ?? throw new InvalidOperationException("Hedef departmanın varsayılan Manager'ı bulunmuyor.");
        }
        await ValidateAsync(project, memberIds);
        userId = actor.UserId;
        using var transaction = await _context.Database.BeginTransactionAsync();

        if (memberIds != null)
        {
            var existing = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id)
                .ToListAsync();
            var selected = memberIds.Distinct().Where(x => x != project.ManagerUserId).ToHashSet();
            _context.ProjectMembers.RemoveRange(existing.Where(x => !selected.Contains(x.UserId)));

            foreach (var memberId in memberIds.Distinct())
            {
                if (memberId == project.ManagerUserId || existing.Any(x => x.UserId == memberId)) continue;
                _context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = memberId });
            }
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Updated", "Project", project.Id, userId, $"Project '{project.Name}' updated.");
        await transaction.CommitAsync();
    }

    public async Task DeleteProjectAsync(int id, string? userId = null)
    {
        AccessService.Require((await _access.ProjectAsync(id)).Delete);
        userId = (await _access.ActorAsync()).UserId;
        var project = await _context.Projects.FindAsync(id);
        if (project is not null)
        {
            project.IsDeleted       = true;
            project.DeletionBatchId = Guid.NewGuid();
            project.DeletedAt       = DateTime.UtcNow;
            project.DeletedByUserId = userId;

            // Projeye ait tüm görevleri de soft-delete yap
            var tasks = await _context.TaskItems
                .Where(t => t.ProjectId == id && !t.IsDeleted)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.IsDeleted       = true;
                task.DeletionBatchId = project.DeletionBatchId;
                task.DeletedAt       = DateTime.UtcNow;
                task.DeletedByUserId = userId;
            }

            var deletedTaskIds = tasks.Select(t => t.Id).ToArray();
            var linkedTickets = await _context.Tickets.Where(t => t.LinkedTaskId.HasValue
                && deletedTaskIds.Contains(t.LinkedTaskId.Value)).ToListAsync();
            foreach (var linkedTicket in linkedTickets)
            {
                if (linkedTicket.Status == TicketStatus.Resolved && linkedTicket.ResolvedByLinkedTask)
                    linkedTicket.Status = TicketStatus.InProgress;
                linkedTicket.ResolvedByLinkedTask = false;
                linkedTicket.UpdatedAt = DateTime.UtcNow;
                linkedTicket.Events.Add(new TicketEvent { ActorUserId = userId,
                    Type = "LinkedTaskDeleted", Details = linkedTicket.LinkedTaskId.ToString() });
            }

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("SoftDelete", "Project", id, userId, $"Project '{project.Name}' soft deleted.");
        }
    }

    // ── Recycle Bin ──────────────────────────────────────────────────────

    public async Task<List<Project>> GetDeletedProjectsAsync()
    {
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
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
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
        var project = await _context.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is not null)
        {
            if (project.DeletionBatchId.HasValue)
            {
                var tasks = await _context.TaskItems.IgnoreQueryFilters().Where(t => t.ProjectId == id
                    && t.IsDeleted && t.DeletionBatchId == project.DeletionBatchId).ToListAsync();
                foreach (var task in tasks)
                {
                    task.IsDeleted = false;
                    task.DeletedAt = null;
                    task.DeletedByUserId = null;
                    task.DeletionBatchId = null;
                }
            }
            project.DeletionBatchId = null;
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
        return await _access.AssignableUsersAsync();
    }

    public async Task ChangeStatusAsync(int id, ProjectStatus status, bool fromKanban = false)
    {
        AccessService.Require(fromKanban ? (await _access.ActorAsync()).IsAdmin : (await _access.ProjectAsync(id)).ChangeStatus);
        if (!Enum.IsDefined(status)) throw new InvalidOperationException("Geçersiz proje durumu.");
        var project = await _context.Projects.FirstAsync(x => x.Id == id);
        var old = project.Status;
        project.Status = status;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("StatusChanged", "Project", id, (await _access.ActorAsync()).UserId, $"{old} → {status}");
    }

    private async Task ValidateAsync(Project project, List<string>? members)
    {
        if (!project.DepartmentId.HasValue || !await _context.Departments.AnyAsync(x => x.Id == project.DepartmentId))
            throw new InvalidOperationException("Geçerli bir ana departman seçin.");
        if (!(await _access.AssignableUsersAsync(managersOnly: true)).Any(x => x.Id == project.ManagerUserId))
            throw new InvalidOperationException("Proje sorumlusu olarak bir Manager seçin.");
        if (members is not null && members.Except((await _access.AssignableUsersAsync()).Select(x => x.Id)).Any())
            throw new InvalidOperationException("Ekip seçimi geçersiz.");
        if (!Enum.IsDefined(project.Status) || project.EndDate < project.StartDate)
            throw new InvalidOperationException("Proje durumu veya tarih aralığı geçersiz.");
    }
}
