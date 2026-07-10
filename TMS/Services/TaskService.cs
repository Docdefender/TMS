using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class TaskService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public TaskService(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
    {
        return await _context.TaskItems
            .Include(t => t.AssignedToUser)
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Returns tasks visible to the user.
    /// Admins/Managers see all. Regular users see tasks assigned to them
    /// OR tasks in projects they belong to.
    /// </summary>
    public async Task<List<TaskItem>> GetAllTasksAsync(
        List<string>? statuses = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        bool showAll = false)
    {
        var query = _context.TaskItems
            .Where(t => !t.IsDeleted)
            .Include(t => t.Project)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Category)
            .AsQueryable();

        // Scope to user's tasks if not admin/manager
        if (!showAll && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(t =>
                t.AssignedToUserId == userId ||
                t.Project.CreatedByUserId == userId ||
                t.Project.ManagerUserId   == userId ||
                t.Project.Members.Any(m => m.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search));

        if (startDate.HasValue)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= endDate.Value);

        if (statuses != null && statuses.Count > 0)
        {
            var statusEnums = new List<Models.TaskStatus>();
            bool includeOverdue = false;

            foreach (var s in statuses)
            {
                if (s == "overdue")
                    includeOverdue = true;
                else if (Enum.TryParse<Models.TaskStatus>(s, true, out var ts))
                    statusEnums.Add(ts);
            }

            var today = DateTime.Today;
            if (statusEnums.Count > 0 && includeOverdue)
                query = query.Where(t => statusEnums.Contains(t.Status)
                    || (t.DueDate.HasValue && t.DueDate.Value < today
                        && t.Status != Models.TaskStatus.Done));
            else if (statusEnums.Count > 0)
                query = query.Where(t => statusEnums.Contains(t.Status));
            else if (includeOverdue)
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < today
                    && t.Status != Models.TaskStatus.Done);
        }

        return await query.OrderBy(t => t.DueDate).ToListAsync();
    }

    public async Task UpdateTaskStatusAsync(int taskId, Models.TaskStatus newStatus, string? userId = null)
    {
        var task = await _context.TaskItems.FindAsync(taskId);
        if (task is not null)
        {
            var oldStatus = task.Status;
            task.Status = newStatus;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("StatusChanged", "TaskItem", task.Id, userId,
                $"Task '{task.Title}' status changed from {oldStatus} to {newStatus}.");
        }
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        return await _context.TaskItems
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task CreateTaskAsync(TaskItem task, string? userId = null)
    {
        task.CreatedByUserId = userId;
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Created", "TaskItem", task.Id, userId, $"Task '{task.Title}' created.");
    }

    public async Task UpdateTaskAsync(TaskItem task, string? userId = null)
    {
        _context.TaskItems.Update(task);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Updated", "TaskItem", task.Id, userId, $"Task '{task.Title}' updated.");
    }

    public async Task DeleteTaskAsync(int id, string? userId = null)
    {
        var task = await _context.TaskItems.FindAsync(id);
        if (task is not null)
        {
            var title = task.Title;
            task.IsDeleted = true;
            task.DeletedAt = DateTime.UtcNow;
            task.DeletedByUserId = userId;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("SoftDelete", "TaskItem", id, userId, $"Task '{title}' soft deleted.");
        }
    }

    public async Task<List<TaskItem>> GetDeletedTasksAsync()
    {
        return await _context.TaskItems
            .IgnoreQueryFilters()
            .Where(t => t.IsDeleted)
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.DeletedAt)
            .ToListAsync();
    }

    public async Task<bool> RestoreTaskAsync(int id, string? userId = null)
    {
        var task = await _context.TaskItems.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
        if (task is null) return false;

        task.IsDeleted = false;
        task.DeletedAt = null;
        task.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Restored", "TaskItem", id, userId, $"Task '{task.Title}' restored.");
        return true;
    }
}
