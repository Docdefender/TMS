using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public DetailsModel(TaskService taskService, CommentService commentService,
        AttachmentService attachmentService, UserManager<ApplicationUser> userManager)
    {
        _taskService = taskService;
        _commentService = commentService;
        _attachmentService = attachmentService;
        _userManager = userManager;
    }

    public TaskItem TaskItem { get; set; } = null!;
    public List<Comment> Comments { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public bool CanComment { get; set; }
    public bool CanSeeComments { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;

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
        await LoadCommentsAsync(id);
        Attachments = await _attachmentService.GetByTaskIdAsync(id);

        var userId = _userManager.GetUserId(User);
        CanEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                  task.CreatedByUserId == userId || task.AssignedToUserId == userId;
        CanDelete = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                    task.CreatedByUserId == userId;

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

        await _commentService.CreateTaskCommentAsync(id, userId, NewComment, NewTaskStatus.Value);
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
        var canDelete = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                        task.CreatedByUserId == userId;

        if (!canDelete) return Forbid();

        var projectId = task.ProjectId;
        await _taskService.DeleteTaskAsync(id, userId);
        return RedirectToPage("/Projects/Details", new { id = projectId });
    }

    private async Task LoadCommentsAsync(int taskItemId)
    {
        CanSeeComments = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (CanSeeComments)
        {
            Comments = await _commentService.GetByTaskIdAsync(taskItemId);
            var userId = _userManager.GetUserId(User);
            CanComment = userId is not null && await _commentService.CanCommentOnTaskAsync(taskItemId, userId);
            CurrentUserId = userId ?? string.Empty;
        }
    }
}
