using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class CommentService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public CommentService(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<Comment>> GetByProjectIdAsync(int projectId)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Comment>> GetByTaskIdAsync(int taskItemId)
    {
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
        var task = await _context.TaskItems.FindAsync(taskItemId);
        if (task is null) return;

        var oldStatus = task.Status;
        task.Status = newStatus;

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
    }

    public async Task<bool> UpdateAsync(int commentId, string userId, string newContent)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment is null || comment.UserId != userId)
            return false;

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
        if (comment.UserId != userId && !isAdmin) return false;

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
        var project = await _context.Projects.FindAsync(projectId);
        if (project is null) return false;
        return project.CreatedByUserId == userId || project.AssignedToUserId == userId;
    }

    /// <summary>
    /// Check if a user can comment on a task (must be creator or assignee).
    /// </summary>
    public async Task<bool> CanCommentOnTaskAsync(int taskItemId, string userId)
    {
        var task = await _context.TaskItems.FindAsync(taskItemId);
        if (task is null) return false;
        return task.CreatedByUserId == userId || task.AssignedToUserId == userId;
    }

    public async Task<List<Comment>> GetDeletedCommentsAsync()
    {
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
        var comment = await _context.Comments.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (comment is null) return false;

        comment.IsDeleted = false;
        comment.DeletedAt = null;
        comment.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("Restored", "Comment", id, userId, "Comment restored.");
        return true;
    }
}
