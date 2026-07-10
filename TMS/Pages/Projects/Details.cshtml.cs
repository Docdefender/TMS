using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CategoryService _categoryService;
    private readonly CommentService _commentService;
    private readonly AttachmentService _attachmentService;

    public DetailsModel(ProjectService projectService, TaskService taskService,
        UserManager<ApplicationUser> userManager, CategoryService categoryService,
        CommentService commentService, AttachmentService attachmentService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _userManager = userManager;
        _categoryService = categoryService;
        _commentService = commentService;
        _attachmentService = attachmentService;
    }

    public Project Project { get; set; } = null!;

    [BindProperty]
    public TaskItem NewTask { get; set; } = new();

    [BindProperty]
    public string? NewComment { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public bool CanComment { get; set; }
    public bool CanSeeComments { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project is null) return NotFound();

        Project = project;
        await LoadDropdownsAsync();
        await LoadCommentsAsync(id);
        Attachments = await _attachmentService.GetByProjectIdAsync(id);

        var userId = _userManager.GetUserId(User);
        CanEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                  project.CreatedByUserId == userId || project.ManagerUserId == userId ||
                  project.Members.Any(m => m.UserId == userId);
        CanDelete = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                    project.CreatedByUserId == userId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        NewTask.ProjectId = id;

        ModelState.Remove("NewTask.Project");
        ModelState.Remove("NewTask.CreatedByUser");
        ModelState.Remove("NewTask.AssignedToUser");
        ModelState.Remove("NewTask.Category");
        ModelState.Remove("NewComment");
        ModelState.Remove("UploadedFile");

        if (string.IsNullOrWhiteSpace(NewTask.Title))
        {
            ModelState.AddModelError("NewTask.Title", "Task title is required.");
        }

        if (!ModelState.IsValid)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project is null) return NotFound();
            Project = project;
            await LoadDropdownsAsync();
            await LoadCommentsAsync(id);
            Attachments = await _attachmentService.GetByProjectIdAsync(id);
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        await _taskService.CreateTaskAsync(NewTask, userId);
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostAddCommentAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(NewComment))
        {
            return RedirectToPage("Details", new { id });
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null) return Forbid();

        var canComment = await _commentService.CanCommentOnProjectAsync(id, userId);
        if (!canComment) return Forbid();

        await _commentService.CreateProjectCommentAsync(id, userId, NewComment);
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
            await _attachmentService.UploadAsync(UploadedFile, id, null, userId);
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

    public async Task<IActionResult> OnPostDeleteProjectAsync(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canDelete = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                        project.CreatedByUserId == userId;

        if (!canDelete) return Forbid();

        await _projectService.DeleteProjectAsync(id, userId);
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _projectService.GetAllUsersAsync();
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }

    private async Task LoadCommentsAsync(int projectId)
    {
        CanSeeComments = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (CanSeeComments)
        {
            Comments = await _commentService.GetByProjectIdAsync(projectId);
            var userId = _userManager.GetUserId(User);
            CanComment = userId is not null && await _commentService.CanCommentOnProjectAsync(projectId, userId);
            CurrentUserId = userId ?? string.Empty;
        }
    }
}
