using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tasks;

[Authorize]
public class CreateModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly ProjectService _projectService;
    private readonly CategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(TaskService taskService, ProjectService projectService,
        CategoryService categoryService, UserManager<ApplicationUser> userManager)
    {
        _taskService = taskService;
        _projectService = projectService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    [BindProperty]
    public TaskItem TaskItem { get; set; } = new();

    public List<SelectListItem> ProjectList { get; set; } = new();
    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task OnGetAsync(int? projectId)
    {
        if (projectId.HasValue)
            TaskItem.ProjectId = projectId.Value;

        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
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

        var userId = _userManager.GetUserId(User);
        await _taskService.CreateTaskAsync(TaskItem, userId);
        return RedirectToPage("Details", new { id = TaskItem.Id });
    }

    private async Task LoadDropdownsAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        ProjectList = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();

        var users = await _projectService.GetAllUsersAsync();
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
