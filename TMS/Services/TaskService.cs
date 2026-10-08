using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class TaskService
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly AuditLogService _auditLogService;
    private readonly NotificationService _notifications;

    public TaskService(ApplicationDbContext context, AccessService access, AuditLogService auditLogService,
        NotificationService notifications)
    {
        _context = context;
        _access = access;
        _auditLogService = auditLogService;
        _notifications = notifications;
    }

    public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
    {
        return await (await _access.TasksAsync())
            .Include(t => t.AssignedToUser)
            .Include(t => t.Labels).ThenInclude(x => x.TaskLabel)
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Returns tasks visible to the user.
    /// Includes department/project access, direct assignments and retained reading access.
    /// </summary>
    public async Task<List<TaskItem>> GetAllTasksAsync(
        List<string>? statuses = null,
        string? search = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? userId = null,
        bool showAll = false)
    {
        var query = (await _access.TasksAsync())
            .Where(t => !t.IsDeleted)
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Category)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.Labels).ThenInclude(x => x.TaskLabel)
            .AsQueryable();


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
        AccessService.Require((await _access.TaskAsync(taskId)).Contribute);
        if (!Enum.IsDefined(newStatus)) throw new InvalidOperationException("Geçersiz görev durumu.");
        var task = await _context.TaskItems
            .Include(x => x.PrerequisiteDependencies).ThenInclude(x => x.PrerequisiteTask)
            .FirstOrDefaultAsync(x => x.Id == taskId);
        if (task is not null)
        {
            if (newStatus == Models.TaskStatus.Done && task.PrerequisiteDependencies
                .Any(x => x.PrerequisiteTask.Status != Models.TaskStatus.Done))
                throw new InvalidOperationException("Bu görev tamamlanmadan önce tüm ön koşul görevlerin tamamlanması gerekir.");
            var oldStatus = task.Status;
            task.Status = newStatus;
            ApplyStatusDates(task, oldStatus, newStatus);
            await SyncLinkedTicketStatusAsync(taskId, oldStatus, newStatus);
            if (newStatus != Models.TaskStatus.Done && task.PipelineCheckpointId.HasValue)
            {
                var checkpoint = await _context.PipelineCheckpoints.FindAsync(task.PipelineCheckpointId.Value);
                if (checkpoint is not null)
                {
                    checkpoint.ApprovedAt = null;
                    checkpoint.ApprovedByUserId = null;
                }
            }
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("StatusChanged", "TaskItem", task.Id, userId,
                $"Task '{task.Title}' status changed from {oldStatus} to {newStatus}.", task.ProjectId);
        }
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        if (!(await _access.TaskAsync(id)).View) return null;
        var task = await _context.TaskItems
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Category)
            .Include(t => t.Labels).ThenInclude(x => x.TaskLabel)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return null;

        // Direct task access does not reveal other tasks in the project.
        // Dependency titles are loaded only for users who can view the full project.
        if ((await _access.ProjectAsync(task.ProjectId)).View)
        {
            var visibleTaskIds = await (await _access.TasksAsync()).Where(x => x.ProjectId == task.ProjectId)
                .Select(x => x.Id).ToListAsync();
            await _context.Entry(task).Collection(t => t.PrerequisiteDependencies).Query()
                .Where(x => visibleTaskIds.Contains(x.PrerequisiteTaskId))
                .Include(x => x.PrerequisiteTask).LoadAsync();
            await _context.Entry(task).Collection(t => t.DependentDependencies).Query()
                .Where(x => visibleTaskIds.Contains(x.DependentTaskId))
                .Include(x => x.DependentTask).LoadAsync();
        }

        return task;
    }

    public async Task CreateTaskAsync(TaskItem task, string? userId = null, string? labelText = null)
    {
        AccessService.Require((await _access.ProjectAsync(task.ProjectId)).CreateTask);
        AccessService.Require(await _access.CanAssignAsync(task.ProjectId, task.AssignedToUserId));
        if (!Enum.IsDefined(task.Status)) throw new InvalidOperationException("Geçersiz görev durumu.");
        if (!Enum.IsDefined(task.Priority)) throw new InvalidOperationException("Geçersiz görev önceliği.");
        userId = (await _access.ActorAsync()).UserId;
        task.Id = 0;
        task.CreatedDate = DateTime.UtcNow;
        ApplyStatusDates(task, Models.TaskStatus.ToDo, task.Status);
        task.IsDeleted = false;
        task.DeletedAt = null;
        task.DeletedByUserId = null;
        task.DeletionBatchId = null;
        task.FirstAssignedByUserId = string.IsNullOrEmpty(task.AssignedToUserId) ? null : userId;
        task.Project = null!;
        task.CreatedByUser = null;
        task.AssignedToUser = null;
        task.Category = null;
        task.Comments = new List<Comment>();
        task.Attachments = new List<Attachment>();
        task.Labels = await BuildLabelLinksAsync(labelText);
        task.AssignmentHistory = new List<TaskAssignment>();
        task.HistoryAccess = new List<TaskHistoryAccess>();
        if (!string.IsNullOrEmpty(task.AssignedToUserId))
            task.AssignmentHistory.Add(new TaskAssignment { AssignedToUserId = task.AssignedToUserId, AssignedByUserId = userId });
        task.CreatedByUserId = userId;
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Created", "TaskItem", task.Id, userId, $"Task '{task.Title}' created.", task.ProjectId);
        await _notifications.CreateAsync(task.AssignedToUserId, "Assignment", "Yeni görev atandı",
            task.Title, $"/Tasks/Details/{task.Id}", userId);
    }

    public static void ApplyStatusDates(TaskItem task, Models.TaskStatus oldStatus, Models.TaskStatus newStatus)
    {
        var now = DateTime.UtcNow;
        if (newStatus != Models.TaskStatus.ToDo && !task.ActualStartedAt.HasValue)
            task.ActualStartedAt = now;
        if (newStatus == Models.TaskStatus.Done)
            task.CompletedAt ??= now;
        else if (oldStatus == Models.TaskStatus.Done)
            task.CompletedAt = null;
    }

    public async Task SyncLinkedTicketStatusAsync(int taskId, Models.TaskStatus oldStatus, Models.TaskStatus newStatus)
    {
        if (oldStatus == newStatus) return;
        var linkedTicket = await _context.Tickets.FirstOrDefaultAsync(x => x.LinkedTaskId == taskId);
        if (linkedTicket is null) return;
        if (newStatus == Models.TaskStatus.Done && linkedTicket.Status != TicketStatus.Closed)
        {
            linkedTicket.Status = TicketStatus.Resolved;
            linkedTicket.ResolvedByLinkedTask = true;
            linkedTicket.UpdatedAt = DateTime.UtcNow;
            linkedTicket.Events.Add(new TicketEvent { ActorUserId = (await _access.ActorAsync()).UserId,
                Type = "TaskResolved", Details = taskId.ToString() });
        }
        else if (oldStatus == Models.TaskStatus.Done && linkedTicket.Status == TicketStatus.Resolved
            && linkedTicket.ResolvedByLinkedTask)
        {
            linkedTicket.Status = TicketStatus.InProgress;
            linkedTicket.ResolvedByLinkedTask = false;
            linkedTicket.UpdatedAt = DateTime.UtcNow;
            linkedTicket.Events.Add(new TicketEvent { ActorUserId = (await _access.ActorAsync()).UserId,
                Type = "TaskReopened", Details = taskId.ToString() });
        }
    }

    public async Task UpdateTaskAsync(TaskItem task, string? userId = null, string? labelText = null)
    {
        AccessService.Require((await _access.TaskAsync(task.Id)).Edit);
        var original = await _context.TaskItems.AsNoTracking().FirstAsync(x => x.Id == task.Id);
        AccessService.Require(original.ProjectId == task.ProjectId);
        AccessService.Require(await _access.CanAssignAsync(task.ProjectId, task.AssignedToUserId));
        if (!Enum.IsDefined(task.Status)) throw new InvalidOperationException("Geçersiz görev durumu.");
        if (!Enum.IsDefined(task.Priority)) throw new InvalidOperationException("Geçersiz görev önceliği.");
        if (task.Status == Models.TaskStatus.Done && await _context.TaskDependencies
            .AnyAsync(x => x.DependentTaskId == task.Id && x.PrerequisiteTask.Status != Models.TaskStatus.Done))
            throw new InvalidOperationException("Bu görev tamamlanmadan önce tüm ön koşul görevlerin tamamlanması gerekir.");
        userId = (await _access.ActorAsync()).UserId;
        if (original.AssignedToUserId != task.AssignedToUserId)
        {
            if (!string.IsNullOrEmpty(original.AssignedToUserId))
            {
                var history = await _context.TaskHistoryAccesses.FindAsync(task.Id, original.AssignedToUserId);
                if (history is null) _context.TaskHistoryAccesses.Add(new TaskHistoryAccess { TaskItemId = task.Id, UserId = original.AssignedToUserId });
                else { history.RevokedAt = null; history.RevokedByUserId = null; }
            }
            if (task.FirstAssignedByUserId is null && original.AssignedToUserId is null
                && !await _context.TaskAssignments.AnyAsync(x => x.TaskItemId == task.Id) && !string.IsNullOrEmpty(task.AssignedToUserId))
                task.FirstAssignedByUserId = userId;
            _context.TaskAssignments.Add(new TaskAssignment { TaskItemId = task.Id, PreviousUserId = original.AssignedToUserId,
                AssignedToUserId = task.AssignedToUserId, AssignedByUserId = userId });
        }
        ApplyStatusDates(task, original.Status, task.Status);
        await SyncLinkedTicketStatusAsync(task.Id, original.Status, task.Status);
        if (task.Status != Models.TaskStatus.Done && task.PipelineCheckpointId.HasValue)
        {
            var checkpoint = await _context.PipelineCheckpoints.FindAsync(task.PipelineCheckpointId.Value);
            if (checkpoint is not null)
            {
                checkpoint.ApprovedAt = null;
                checkpoint.ApprovedByUserId = null;
            }
        }
        if (labelText is not null)
        {
            await _context.Entry(task).Collection(x => x.Labels).LoadAsync();
            _context.TaskItemLabels.RemoveRange(task.Labels);
            task.Labels = await BuildLabelLinksAsync(labelText);
        }
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Updated", "TaskItem", task.Id, userId, $"Task '{task.Title}' updated.", task.ProjectId);
        if (original.AssignedToUserId != task.AssignedToUserId)
            await _notifications.CreateAsync(task.AssignedToUserId, "Assignment", "Görev size atandı",
                task.Title, $"/Tasks/Details/{task.Id}", userId);
    }

    public async Task DeleteTaskAsync(int id, string? userId = null)
    {
        AccessService.Require((await _access.TaskAsync(id)).Delete);
        var task = await _context.TaskItems.FindAsync(id);
        if (task is not null)
        {
            var title = task.Title;
            task.IsDeleted = true;
            task.DeletedAt = DateTime.UtcNow;
            task.DeletedByUserId = userId;
            var linkedTicket = await _context.Tickets.FirstOrDefaultAsync(x => x.LinkedTaskId == id);
            if (linkedTicket is not null)
            {
                if (linkedTicket.Status == TicketStatus.Resolved && linkedTicket.ResolvedByLinkedTask)
                    linkedTicket.Status = TicketStatus.InProgress;
                linkedTicket.ResolvedByLinkedTask = false;
                linkedTicket.UpdatedAt = DateTime.UtcNow;
                linkedTicket.Events.Add(new TicketEvent { ActorUserId = (await _access.ActorAsync()).UserId,
                    Type = "LinkedTaskDeleted", Details = id.ToString() });
            }
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync("SoftDelete", "TaskItem", id, userId, $"Task '{title}' soft deleted.", task.ProjectId);
        }
    }

    public async Task<List<TaskItem>> GetDeletedTasksAsync()
    {
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
        return await _context.TaskItems
            .IgnoreQueryFilters()
            .Where(t => t.IsDeleted)
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.DeletedAt)
            .ToListAsync();
    }

    public static IReadOnlyList<string> ParseLabelNames(string? labelText)
    {
        if (string.IsNullOrWhiteSpace(labelText)) return [];
        var names = labelText.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        if (names.Count > 8) throw new InvalidOperationException("Bir göreve en fazla 8 etiket eklenebilir.");
        if (names.Any(x => x.Length > 40)) throw new InvalidOperationException("Etiketler en fazla 40 karakter olabilir.");
        return names;
    }

    private async Task<List<TaskItemLabel>> BuildLabelLinksAsync(string? labelText)
    {
        var names = ParseLabelNames(labelText);
        if (names.Count == 0) return [];

        var normalized = names.Select(x => x.ToUpperInvariant()).ToArray();
        var existing = await _context.TaskLabels.Where(x => normalized.Contains(x.NormalizedName)).ToListAsync();
        var byName = existing.ToDictionary(x => x.NormalizedName, StringComparer.Ordinal);
        var palette = new[] { "#0F8F83", "#3B82A0", "#7C5CFC", "#C5872E", "#B04463", "#4F7D3A" };
        var links = new List<TaskItemLabel>();

        for (var index = 0; index < names.Count; index++)
        {
            var key = normalized[index];
            if (!byName.TryGetValue(key, out var label))
            {
                var colorIndex = key.Aggregate(0, (sum, character) => sum + character) % palette.Length;
                label = new TaskLabel { Name = names[index], NormalizedName = key, Color = palette[colorIndex] };
                _context.TaskLabels.Add(label);
                byName[key] = label;
            }
            links.Add(new TaskItemLabel { TaskLabel = label });
        }

        return links;
    }

    public async Task<bool> RestoreTaskAsync(int id, string? userId = null)
    {
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
        var task = await _context.TaskItems.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
        if (task is null) return false;
        if (!await _context.Projects.AnyAsync(p => p.Id == task.ProjectId))
            throw new InvalidOperationException("Önce görevin projesini geri yükleyin.");

        task.IsDeleted = false;
        task.DeletionBatchId = null;
        task.DeletedAt = null;
        task.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Restored", "TaskItem", id, userId, $"Task '{task.Title}' restored.", task.ProjectId);
        return true;
    }
}
