using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tasks;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly CommentService _commentService;
    private readonly AttachmentService _attachmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccessService _access;
    private readonly ApplicationDbContext _db;
    private readonly TaskTimeService _taskTimeService;

    public DetailsModel(TaskService taskService, CommentService commentService,
        AttachmentService attachmentService, UserManager<ApplicationUser> userManager, AccessService access,
        ApplicationDbContext db, TaskTimeService taskTimeService)
    {
        _taskService = taskService;
        _commentService = commentService;
        _attachmentService = attachmentService;
        _userManager = userManager;
        _access = access;
        _db = db;
        _taskTimeService = taskTimeService;
    }

    public TaskItem TaskItem { get; set; } = null!;
    public List<Comment> Comments { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public bool CanComment { get; set; }
    public bool CanSeeComments { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public int? LinkedTicketId { get; set; }
    public bool LinkedTicketHasNewReply { get; set; }
    public string? LinkedTicketRequester { get; set; }
    public DateTime? LinkedTicketCreatedAt { get; set; }
    public bool CanViewProject { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public List<TaskTimeEntry> TimeEntries { get; set; } = [];
    public TaskTimeEntry? ActiveTimeEntry { get; set; }
    public int TotalTrackedMinutes { get; set; }
    public bool CanTrackTime { get; set; }

    [BindProperty]
    public string? NewComment { get; set; }

    [BindProperty]
    public Models.TaskStatus? NewTaskStatus { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task is null) return NotFound();

        TaskItem = task;
        var ticket = await _db.Tickets.AsNoTracking().Where(x => x.LinkedTaskId == id)
            .Select(x => new { x.Id, x.HasNewReply, Requester = x.Requester.FullName, x.CreatedAt })
            .FirstOrDefaultAsync();
        LinkedTicketId = ticket?.Id;
        LinkedTicketHasNewReply = ticket?.HasNewReply ?? false;
        LinkedTicketRequester = ticket?.Requester;
        LinkedTicketCreatedAt = ticket?.CreatedAt;
        CanViewProject = (await _access.ProjectAsync(task.ProjectId)).View;
        await LoadCommentsAsync(id);
        Attachments = await _attachmentService.GetByTaskIdAsync(id);

        var userId = _userManager.GetUserId(User);
        var rights = await _access.TaskAsync(id);
        CanEdit = rights.Edit;
        CanDelete = rights.Delete;
        CanTrackTime = rights.Contribute;
        TimeEntries = await _taskTimeService.GetAsync(id);
        ActiveTimeEntry = TimeEntries.FirstOrDefault(x => x.UserId == userId && x.EndedAt == null);
        TotalTrackedMinutes = TimeEntries.Sum(x => x.DurationMinutes ?? 0);

        return Page();
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(NewComment) || NewTaskStatus is null)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task is null) return NotFound();
            TaskItem = task;
            await LoadCommentsAsync(id);
            Attachments = await _attachmentService.GetByTaskIdAsync(id);
            ModelState.AddModelError(string.Empty, "Comment and new status are required.");
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        var canComment = await _commentService.CanCommentOnTaskAsync(id, userId);
        if (!canComment) return Forbid();

        try
        {
            await _commentService.CreateTaskCommentAsync(id, userId, NewComment, NewTaskStatus.Value);
        }
        catch (InvalidOperationException ex)
        {
            TempData["TaskError"] = ex.Message;
        }
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteCommentAsync(int id, int commentId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        var isAdmin = User.IsInRole("Admin");
        await _commentService.DeleteAsync(commentId, userId, isAdmin);
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostUploadFileAsync(int id)
    {
        if (UploadedFile is null)
        {
            return RedirectToPage("Details", new { id });
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        try
        {
            await _attachmentService.UploadAsync(UploadedFile, null, id, userId);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteFileAsync(int id, int attachmentId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        var isAdmin = User.IsInRole("Admin");
        await _attachmentService.DeleteAsync(attachmentId, userId, isAdmin);
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canDelete = (await _access.TaskAsync(id)).Delete;

        if (!canDelete) return Forbid();

        var projectId = task.ProjectId;
        await _taskService.DeleteTaskAsync(id, userId);
        return RedirectToPage("/Projects/Details", new { id = projectId });
    }

    public async Task<IActionResult> OnPostStartTimerAsync(int id, string? note)
    {
        try { await _taskTimeService.StartAsync(id, note); }
        catch (InvalidOperationException ex) { TempData["TaskError"] = ex.Message; }
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostStopTimerAsync(int id)
    {
        try { await _taskTimeService.StopAsync(id); }
        catch (InvalidOperationException ex) { TempData["TaskError"] = ex.Message; }
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostAddTimeAsync(int id, int minutes, DateTime? workedOn, string? note)
    {
        try { await _taskTimeService.AddManualAsync(id, minutes, workedOn, note); }
        catch (InvalidOperationException ex) { TempData["TaskError"] = ex.Message; }
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteTimeAsync(int id, int entryId)
    {
        await _taskTimeService.DeleteAsync(id, entryId);
        return RedirectToPage("Details", new { id });
    }

    private async Task LoadCommentsAsync(int taskItemId)
    {
        CanEdit = (await _access.TaskAsync(taskItemId)).Edit;
        CanDelete = (await _access.TaskAsync(taskItemId)).Delete;
        CanSeeComments = (await _access.TaskAsync(taskItemId)).View;
        if (CanSeeComments)
        {
            Comments = await _commentService.GetByTaskIdAsync(taskItemId);
            var userId = _userManager.GetUserId(User);
            CanComment = userId is not null && await _commentService.CanCommentOnTaskAsync(taskItemId, userId);
            CurrentUserId = userId ?? string.Empty;
        }
    }
}
