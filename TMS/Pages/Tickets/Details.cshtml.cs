using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tickets;

[Authorize]
public class DetailsModel(ApplicationDbContext db, TicketService tickets, AccessService access,
    AttachmentService attachments) : PageModel
{
    public Ticket Item { get; private set; } = null!;
    public TicketAccess Rights { get; private set; } = null!;
    public List<TicketMessage> Messages { get; private set; } = [];
    public List<TicketEvent> Events { get; private set; } = [];
    public List<Attachment> Files { get; private set; } = [];
    public List<SelectListItem> Assignees { get; private set; } = [];
    public List<SelectListItem> Users { get; private set; } = [];
    public List<SelectListItem> Projects { get; private set; } = [];
    public bool IsFollowing { get; private set; }
    public bool LinkedTaskDeleted { get; private set; }

    public static string StatusText(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Açık",
        TicketStatus.InProgress => "İşlemde",
        TicketStatus.WaitingForInformation => "Bilgi Bekleniyor",
        TicketStatus.Resolved => "Çözüldü",
        TicketStatus.Closed => "Kapatıldı",
        _ => status.ToString()
    };

    public static string StatusClass(TicketStatus status) => status switch
    {
        TicketStatus.Open => "ticket-status-open",
        TicketStatus.InProgress => "ticket-status-progress",
        TicketStatus.WaitingForInformation => "ticket-status-waiting",
        TicketStatus.Resolved => "ticket-status-resolved",
        TicketStatus.Closed => "ticket-status-closed",
        _ => "ticket-status-open"
    };

    public static string PriorityText(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Düşük",
        TicketPriority.Normal => "Normal",
        TicketPriority.High => "Yüksek",
        TicketPriority.Critical => "Kritik",
        _ => priority.ToString()
    };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await tickets.GetAsync(id);
        if (item is null) return NotFound();
        Item = item;
        Rights = await tickets.RightsAsync(id);
        var actor = await access.ActorAsync();
        IsFollowing = item.Viewers.Any(v => v.UserId == actor.UserId && v.IsFollowing);
        LinkedTaskDeleted = item.LinkedTaskId.HasValue && !await db.TaskItems.AnyAsync(x => x.Id == item.LinkedTaskId);
        Messages = await tickets.MessagesAsync(id);
        Files = await attachments.GetByTicketIdAsync(id);
        if (Rights.Manage)
        {
            Events = await tickets.EventsAsync(id);
            Assignees = await db.Users.AsNoTracking().Where(u => u.IsActive
                    && (u.DepartmentId == item.SupportDepartmentId || u.Id == item.AssignedToUserId))
                .OrderBy(u => u.FullName).Select(u => new SelectListItem(u.FullName, u.Id)).ToListAsync();
            var eligible = await (await access.ProjectsAsync()).AsNoTracking().Include(p => p.Members)
                .Where(p => !p.IsDeleted).OrderBy(p => p.Name).ToListAsync();
            Projects = eligible.Where(p => AccessRules.Project(actor, p).CreateTask)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        }
        if (Rights.ManageViewers)
            Users = await db.Users.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.FullName)
                .Select(u => new SelectListItem(u.FullName + " (" + u.Email + ")", u.Id)).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, string? userId)
    {
        await tickets.AssignAsync(id, string.IsNullOrWhiteSpace(userId) ? null : userId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, string status)
    {
        if (!Enum.TryParse<TicketStatus>(status, out var parsed) || !Enum.IsDefined(parsed)) return BadRequest();
        await tickets.ChangeStatusAsync(id, parsed);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSlaAsync(int id, string priority, DateTime? dueAt)
    {
        if (!Enum.TryParse<TicketPriority>(priority, out var parsed) || !Enum.IsDefined(parsed)) return BadRequest();
        await tickets.UpdateSlaAsync(id, parsed, dueAt);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostViewerAsync(int id, string userId, bool visible)
    {
        await tickets.SetViewerAsync(id, userId, visible);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMessageAsync(int id, string body, bool isInternal)
    {
        await tickets.AddMessageAsync(id, body, isInternal);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReviewedAsync(int id)
    {
        await tickets.MarkReviewedAsync(id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFollowingAsync(int id, bool following)
    {
        await tickets.SetFollowingAsync(id, following);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(int id, int projectId, string title, string? description)
    {
        var taskId = await tickets.CreateLinkedTaskAsync(id, projectId, title, description);
        return RedirectToPage("/Tasks/Details", new { id = taskId });
    }

    public async Task<IActionResult> OnPostUploadAsync(int id, IFormFile? file, bool isInternal)
    {
        if (file is null) return BadRequest();
        await attachments.UploadAsync(file, null, null, (await access.ActorAsync()).UserId, id, isInternal);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteFileAsync(int id, int attachmentId)
    {
        var file = await attachments.GetByIdAsync(attachmentId);
        if (file?.TicketId != id) return NotFound();
        await attachments.DeleteAsync(attachmentId, (await access.ActorAsync()).UserId, User.IsInRole("Admin"));
        return RedirectToPage(new { id });
    }
}
