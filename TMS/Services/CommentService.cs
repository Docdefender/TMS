using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class CommentService
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly AuditLogService _auditLogService;
    private readonly TaskService _taskService;

    public CommentService(ApplicationDbContext context, AccessService access, AuditLogService auditLogService,
        TaskService taskService)
    {
        _context = context;
        _access = access;
        _auditLogService = auditLogService;
        _taskService = taskService;
    }

    public async Task<List<Comment>> GetByProjectIdAsync(int projectId)
    {
        AccessService.Require((await _access.ProjectAsync(projectId)).View);
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Comment>> GetByTaskIdAsync(int taskItemId)
    {
        AccessService.Require((await _access.TaskAsync(taskItemId)).View);
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.TaskItemId == taskItemId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        return await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Creates a comment on a project (no status change).
    /// </summary>
    public async Task CreateProjectCommentAsync(int projectId, string userId, string content)
    {
        AccessService.Require((await _access.ProjectAsync(projectId)).View);
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000) throw new InvalidOperationException("Yorum 1–2000 karakter olmalıdır.");
        var comment = new Comment
        {
            Content = content,
            UserId = userId,
            ProjectId = projectId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Created", "Comment", comment.Id, userId, $"Comment on Project #{projectId}.");
    }

    /// <summary>
    /// Creates a comment on a task AND updates the task status together.
    /// </summary>
    public async Task CreateTaskCommentAsync(int taskItemId, string userId, string content, Models.TaskStatus newStatus)
    {
        AccessService.Require((await _access.TaskAsync(taskItemId)).Contribute);
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000 || !Enum.IsDefined(newStatus))
            throw new InvalidOperationException("Yorum veya görev durumu geçersiz.");
        var task = await _context.TaskItems
            .Include(x => x.PrerequisiteDependencies).ThenInclude(x => x.PrerequisiteTask)
            .FirstOrDefaultAsync(x => x.Id == taskItemId);
        if (task is null) return;

        if (newStatus == Models.TaskStatus.Done && task.PrerequisiteDependencies
            .Any(x => x.PrerequisiteTask.Status != Models.TaskStatus.Done))
            throw new InvalidOperationException("Bu görev tamamlanmadan önce tüm ön koşul görevlerin tamamlanması gerekir.");

        var oldStatus = task.Status;
        task.Status = newStatus;
        TaskService.ApplyStatusDates(task, oldStatus, newStatus);
        await _taskService.SyncLinkedTicketStatusAsync(taskItemId, oldStatus, newStatus);
        if (newStatus != Models.TaskStatus.Done && task.PipelineCheckpointId.HasValue)
        {
            var checkpoint = await _context.PipelineCheckpoints.FindAsync(task.PipelineCheckpointId.Value);
            if (checkpoint is not null)
            {
                checkpoint.ApprovedAt = null;
                checkpoint.ApprovedByUserId = null;
            }
        }

        var comment = new Comment
        {
            Content = content,
            UserId = userId,
            TaskItemId = taskItemId,
            NewTaskStatus = newStatus
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync("Created", "Comment", comment.Id, userId,
            $"Comment on Task #{taskItemId}. Status: {oldStatus} ? {newStatus}.");
        if (oldStatus != newStatus)
            await _auditLogService.LogAsync("StatusChanged", "TaskItem", task.Id, userId,
                $"Task '{task.Title}' status changed from {oldStatus} to {newStatus}.", task.ProjectId);
    }

    public async Task<bool> UpdateAsync(int commentId, string userId, string newContent)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment is null || comment.UserId != userId)
            return false;
        AccessService.Require(await CanWriteAsync(comment));

        comment.Content = newContent;
        comment.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Updated", "Comment", comment.Id, userId, "Comment updated.");
        return true;
    }

    public async Task<bool> DeleteAsync(int commentId, string userId, bool isAdmin)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment is null) return false;
        var actor = await _access.ActorAsync();
        if (!actor.IsAdmin && (comment.UserId != actor.UserId || !await CanWriteAsync(comment))) return false;

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedByUserId = userId;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("SoftDelete", "Comment", commentId, userId, "Comment soft deleted.");
        return true;
    }

    /// <summary>
    /// Check if a user can comment on a project (must be creator or assignee).
    /// </summary>
    public async Task<bool> CanCommentOnProjectAsync(int projectId, string userId)
    {
        return (await _access.ProjectAsync(projectId)).View;
    }

    /// <summary>
    /// Check if a user can comment on a task (must be creator or assignee).
    /// </summary>
    public async Task<bool> CanCommentOnTaskAsync(int taskItemId, string userId)
    {
        return (await _access.TaskAsync(taskItemId)).Contribute;
    }

    public async Task<List<Comment>> GetDeletedCommentsAsync()
    {
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
        return await _context.Comments
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .Include(c => c.User)
            .Include(c => c.Project)
            .Include(c => c.TaskItem)
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync();
    }

    public async Task<bool> RestoreCommentAsync(int id, string? userId = null)
    {
        AccessService.Require((await _access.ActorAsync()).IsAdmin);
        var comment = await _context.Comments.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (comment is null) return false;

        comment.IsDeleted = false;
        comment.DeletedAt = null;
        comment.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Restored", "Comment", id, userId, "Comment restored.");
        return true;
    }

    private async Task<bool> CanWriteAsync(Comment comment) => comment.ProjectId.HasValue
        ? (await _access.ProjectAsync(comment.ProjectId.Value)).View
        : comment.TaskItemId.HasValue && (await _access.TaskAsync(comment.TaskItemId.Value)).Contribute;
}
