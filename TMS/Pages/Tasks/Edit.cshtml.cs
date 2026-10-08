using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tasks;

[Authorize]
public class EditModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly ProjectService _projectService;
    private readonly CategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccessService _access;

    public EditModel(TaskService taskService, ProjectService projectService,
        CategoryService categoryService, UserManager<ApplicationUser> userManager, AccessService access)
    {
        _taskService = taskService;
        _projectService = projectService;
        _categoryService = categoryService;
        _userManager = userManager;
        _access = access;
    }

    [BindProperty]
    public TaskItem TaskItem { get; set; } = null!;

    [BindProperty]
    public string? LabelText { get; set; }

    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = (await _access.TaskAsync(id)).Edit;

        if (!canEdit) return Forbid();

        TaskItem = task;
        LabelText = string.Join(", ", task.Labels.OrderBy(x => x.TaskLabel.Name).Select(x => x.TaskLabel.Name));
        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        TaskItem.Id = id;
        var existingTask = await _taskService.GetTaskByIdAsync(id);
        if (existingTask is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = (await _access.TaskAsync(id)).Edit;

        if (!canEdit) return Forbid();

        ModelState.Remove("TaskItem.Project");
        ModelState.Remove("TaskItem.CreatedByUser");
        ModelState.Remove("TaskItem.AssignedToUser");
        ModelState.Remove("TaskItem.Category");
        ModelState.Remove("TaskItem.Comments");
        ModelState.Remove("TaskItem.Attachments");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        existingTask.Title = TaskItem.Title;
        existingTask.Description = TaskItem.Description;
        existingTask.Status = TaskItem.Status;
        existingTask.Priority = TaskItem.Priority;
        existingTask.PlannedStartDate = TaskItem.PlannedStartDate;
        existingTask.DueDate = TaskItem.DueDate;
        existingTask.CategoryId = TaskItem.CategoryId;
        existingTask.AssignedToUserId = TaskItem.AssignedToUserId;

        try
        {
            await _taskService.UpdateTaskAsync(existingTask, userId, LabelText ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        return RedirectToPage("Details", new { id });
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _access.AssignableUsersAsync(TaskItem.ProjectId);
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
