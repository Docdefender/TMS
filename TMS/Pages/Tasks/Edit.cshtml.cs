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

    public EditModel(TaskService taskService, ProjectService projectService,
        CategoryService categoryService, UserManager<ApplicationUser> userManager)
    {
        _taskService = taskService;
        _projectService = projectService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    [BindProperty]
    public TaskItem TaskItem { get; set; } = null!;

    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                      task.CreatedByUserId == userId || task.AssignedToUserId == userId;

        if (!canEdit) return Forbid();

        TaskItem = task;
        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var existingTask = await _taskService.GetTaskByIdAsync(id);
        if (existingTask is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                      existingTask.CreatedByUserId == userId || existingTask.AssignedToUserId == userId;

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
        existingTask.DueDate = TaskItem.DueDate;
        existingTask.CategoryId = TaskItem.CategoryId;
        existingTask.AssignedToUserId = TaskItem.AssignedToUserId;

        await _taskService.UpdateTaskAsync(existingTask, userId);
        return RedirectToPage("Details", new { id });
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _projectService.GetAllUsersAsync();
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
