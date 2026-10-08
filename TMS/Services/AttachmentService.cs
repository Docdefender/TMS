using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class AttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly AuditLogService _auditLogService;
    private readonly IWebHostEnvironment _environment;
    private readonly TicketService _tickets;

    public AttachmentService(ApplicationDbContext context, AccessService access, AuditLogService auditLogService,
        IWebHostEnvironment environment, TicketService tickets)
    {
        _context = context;
        _access = access;
        _auditLogService = auditLogService;
        _environment = environment;
        _tickets = tickets;
    }

    public async Task<List<Attachment>> GetByProjectIdAsync(int projectId)
    {
        AccessService.Require((await _access.ProjectAsync(projectId)).View);
        return await _context.Attachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<Attachment>> GetByTaskIdAsync(int taskItemId)
    {
        AccessService.Require((await _access.TaskAsync(taskItemId)).View);
        return await _context.Attachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.TaskItemId == taskItemId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<Attachment>> GetByTicketIdAsync(int ticketId)
    {
        var rights = await _tickets.RightsAsync(ticketId);
        AccessService.Require(rights.View);
        return await _context.Attachments.AsNoTracking().Include(a => a.UploadedByUser)
            .Where(a => a.TicketId == ticketId && (rights.Manage || !a.IsInternal))
            .OrderByDescending(a => a.UploadedAt).ToListAsync();
    }

    public async Task<Attachment?> GetByIdAsync(int id)
    {
        return await _context.Attachments.FindAsync(id);
    }

    public async Task<Attachment> UploadAsync(IFormFile file, int? projectId, int? taskItemId, string userId,
        int? ticketId = null, bool isInternal = false)
    {
        AccessService.Require((projectId.HasValue ? 1 : 0) + (taskItemId.HasValue ? 1 : 0)
            + (ticketId.HasValue ? 1 : 0) == 1);
        if (ticketId.HasValue)
        {
            var rights = await _tickets.RightsAsync(ticketId.Value);
            AccessService.Require(isInternal ? rights.Manage : rights.Reply);
        }
        else
        {
            AccessService.Require(!isInternal && (projectId.HasValue
                ? (await _access.ProjectAsync(projectId.Value)).View
                : (await _access.TaskAsync(taskItemId!.Value)).Contribute));
        }
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length == 0 || file.Length > maxFileSize)
        {
            throw new InvalidOperationException("Dosya boş olamaz ve 10 MB sınırını aşamaz.");
        }

        var uploadsFolder = projectId.HasValue
            ? Path.Combine(_environment.WebRootPath, "uploads", "projects", projectId.Value.ToString())
            : taskItemId.HasValue ? Path.Combine(_environment.WebRootPath, "uploads", "tasks", taskItemId.Value.ToString())
            : Path.Combine(_environment.WebRootPath, "uploads", "tickets", ticketId!.Value.ToString());

        Directory.CreateDirectory(uploadsFolder);

        var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 200)
            throw new InvalidOperationException("Dosya adı geçersiz veya çok uzun.");
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(safeName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = projectId.HasValue
            ? $"/uploads/projects/{projectId}/{uniqueFileName}"
            : taskItemId.HasValue ? $"/uploads/tasks/{taskItemId}/{uniqueFileName}"
            : $"/uploads/tickets/{ticketId}/{uniqueFileName}";

        var attachment = new Attachment
        {
            FileName = safeName,
            FilePath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedByUserId = userId,
            ProjectId = projectId,
            TaskItemId = taskItemId,
            TicketId = ticketId,
            IsInternal = isInternal
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        var entityType = projectId.HasValue ? "Project" : taskItemId.HasValue ? "TaskItem" : "Ticket";
        var entityId = projectId ?? taskItemId ?? ticketId!.Value;
        await _auditLogService.LogAsync("FileUploaded", entityType, entityId, userId, $"File '{file.FileName}' uploaded.");

        return attachment;
    }

    public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment is null) return false;
        var actor = await _access.ActorAsync();
        var ticketRights = attachment.TicketId.HasValue ? await _tickets.RightsAsync(attachment.TicketId.Value) : null;
        var canWrite = attachment.ProjectId.HasValue ? (await _access.ProjectAsync(attachment.ProjectId.Value)).View
            : attachment.TaskItemId.HasValue ? (await _access.TaskAsync(attachment.TaskItemId.Value)).Contribute
            : ticketRights?.Reply == true && (!attachment.IsInternal || ticketRights.Manage);
        if (!actor.IsAdmin && !(ticketRights?.Manage == true)
            && (attachment.UploadedByUserId != actor.UserId || !canWrite)) return false;

        // Delete physical file
        var physicalPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/')));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;
        if (!physicalPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Dosya yolu geçersiz.");
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
