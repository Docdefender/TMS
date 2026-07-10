using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class AttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly IWebHostEnvironment _environment;

    public AttachmentService(ApplicationDbContext context, AuditLogService auditLogService, IWebHostEnvironment environment)
    {
        _context = context;
        _auditLogService = auditLogService;
        _environment = environment;
    }

    public async Task<List<Attachment>> GetByProjectIdAsync(int projectId)
    {
        return await _context.Attachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<Attachment>> GetByTaskIdAsync(int taskItemId)
    {
        return await _context.Attachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.TaskItemId == taskItemId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<Attachment?> GetByIdAsync(int id)
    {
        return await _context.Attachments.FindAsync(id);
    }

    public async Task<Attachment> UploadAsync(IFormFile file, int? projectId, int? taskItemId, string userId)
    {
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            throw new InvalidOperationException("File size exceeds 10MB limit.");
        }

        var uploadsFolder = projectId.HasValue
            ? Path.Combine(_environment.WebRootPath, "uploads", "projects", projectId.Value.ToString())
            : Path.Combine(_environment.WebRootPath, "uploads", "tasks", taskItemId!.Value.ToString());

        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = projectId.HasValue
            ? $"/uploads/projects/{projectId}/{uniqueFileName}"
            : $"/uploads/tasks/{taskItemId}/{uniqueFileName}";

        var attachment = new Attachment
        {
            FileName = file.FileName,
            FilePath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedByUserId = userId,
            ProjectId = projectId,
            TaskItemId = taskItemId
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        var entityType = projectId.HasValue ? "Project" : "TaskItem";
        var entityId = projectId ?? taskItemId!.Value;
        await _auditLogService.LogAsync("FileUploaded", entityType, entityId, userId, $"File '{file.FileName}' uploaded.");

        return attachment;
    }

    public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment is null) return false;
        if (attachment.UploadedByUserId != userId && !isAdmin) return false;

        // Delete physical file
        var physicalPath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        attachment.DeletedByUserId = userId;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync("FileDeleted", "Attachment", id, userId, $"File '{attachment.FileName}' deleted.");
        return true;
    }
}
