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
    private readonly AccessService _access;
    private readonly CategoryService _categoryService;
    private readonly CommentService _commentService;
    private readonly AttachmentService _attachmentService;
    private readonly PipelineService _pipelineService;

    public DetailsModel(ProjectService projectService, TaskService taskService,
        UserManager<ApplicationUser> userManager, AccessService access, CategoryService categoryService,
        CommentService commentService, AttachmentService attachmentService, PipelineService pipelineService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _userManager = userManager;
        _access = access;
        _categoryService = categoryService;
        _commentService = commentService;
        _attachmentService = attachmentService;
        _pipelineService = pipelineService;
    }

    public Project Project { get; set; } = null!;

    [BindProperty]
    public TaskItem NewTask { get; set; } = new();

    [BindProperty]
    public string? NewTaskLabelText { get; set; }

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
    public bool CanCreateTask { get; set; }
    public bool CanChangeStatus { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public TaskBoardViewModel Board { get; set; } = new();
    public ProjectPipelineView? PipelineSummary { get; set; }
    public List<PipelineHistoryEntry> RecentPipelineHistory { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var rights = await _access.ProjectAsync(id);
        if (!rights.View) return RedirectToPage("Basic", new { id });
        CanCreateTask = rights.CreateTask;
        CanChangeStatus = rights.ChangeStatus;
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project is null) return NotFound();

        Project = project;
        await LoadBoardAsync(id);
        await LoadDropdownsAsync();
        await LoadCommentsAsync(id);
        Attachments = await _attachmentService.GetByProjectIdAsync(id);
        await LoadProjectInsightsAsync(id);

        var userId = _userManager.GetUserId(User);
        CanEdit = (await _access.ProjectAsync(id)).Edit;
        CanDelete = (await _access.ProjectAsync(id)).Delete;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!(await _access.ProjectAsync(id)).CreateTask) return Forbid();
        if (!await _access.CanAssignAsync(id, NewTask.AssignedToUserId)) return Forbid();
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
            await LoadBoardAsync(id);
            await LoadDropdownsAsync();
            await LoadCommentsAsync(id);
            Attachments = await _attachmentService.GetByProjectIdAsync(id);
            await LoadProjectInsightsAsync(id);
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        try
        {
            await _taskService.CreateTaskAsync(NewTask, userId, NewTaskLabelText);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(NewTaskLabelText), ex.Message);
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project is null) return NotFound();
            Project = project;
            await LoadBoardAsync(id);
            await LoadDropdownsAsync();
            await LoadCommentsAsync(id);
            Attachments = await _attachmentService.GetByProjectIdAsync(id);
            await LoadProjectInsightsAsync(id);
            return Page();
        }
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
        var canDelete = (await _access.ProjectAsync(id)).Delete;

        if (!canDelete) return Forbid();

        await _projectService.DeleteProjectAsync(id, userId);
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _access.AssignableUsersAsync(Project?.Id);
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }

    private async Task LoadCommentsAsync(int projectId)
    {
        var rights = await _access.ProjectAsync(projectId);
        CanEdit = rights.Edit;
        CanDelete = rights.Delete;
        CanCreateTask = rights.CreateTask;
        CanChangeStatus = rights.ChangeStatus;
        CanSeeComments = (await _access.ProjectAsync(projectId)).View;
        if (CanSeeComments)
        {
            Comments = await _commentService.GetByProjectIdAsync(projectId);
            var userId = _userManager.GetUserId(User);
            CanComment = userId is not null && await _commentService.CanCommentOnProjectAsync(projectId, userId);
            CurrentUserId = userId ?? string.Empty;
        }
    }

    private async Task LoadBoardAsync(int projectId)
    {
        var movableIds = await _access.ContributableTaskIdsAsync(Project.Tasks.Select(x => x.Id));

        Board = new TaskBoardViewModel
        {
            Tasks = Project.Tasks.OrderBy(x => x.DueDate).ToList(),
            MovableTaskIds = movableIds,
            UpdateUrl = Url.Page("/Projects/Details", "TaskStatus", new { id = projectId })
                ?? $"/Projects/Details/{projectId}?handler=TaskStatus",
            BoardKey = $"project-{projectId}",
            ShowProjectFilter = false
        };
    }

    private async Task LoadProjectInsightsAsync(int projectId)
    {
        PipelineSummary = await _pipelineService.GetProjectAsync(projectId);
        RecentPipelineHistory = await _pipelineService.GetHistoryAsync(projectId, take: 5) ?? new();
    }

    public async Task<IActionResult> OnPostTaskStatusAsync(int id, [FromBody] TaskStatusRequest request)
    {
        if (request is null || !Enum.TryParse<Models.TaskStatus>(request.NewStatus, true, out var status)
            || !Enum.IsDefined(status))
            return BadRequest(new { success = false, message = "Geçersiz görev durumu." });

        var task = await _taskService.GetTaskByIdAsync(request.TaskId);
        if (task is null || task.ProjectId != id) return NotFound();
        if (!(await _access.TaskAsync(request.TaskId)).Contribute) return Forbid();

        try
        {
            await _taskService.UpdateTaskStatusAsync(request.TaskId, status, _userManager.GetUserId(User));
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public sealed class TaskStatusRequest
    {
        public int TaskId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, string status)
    {
        if (!(await _access.ProjectAsync(id)).ChangeStatus) return Forbid();
        if (!Enum.TryParse<ProjectStatus>(status, out var value) || !Enum.IsDefined(value)) return BadRequest();
        await _projectService.ChangeStatusAsync(id, value);
        return RedirectToPage(new { id });
    }
}
