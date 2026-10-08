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
    private readonly AccessService _access;

    public CreateModel(TaskService taskService, ProjectService projectService,
        CategoryService categoryService, UserManager<ApplicationUser> userManager, AccessService access)
    {
        _taskService = taskService;
        _projectService = projectService;
        _categoryService = categoryService;
        _userManager = userManager;
        _access = access;
    }

    [BindProperty]
    public TaskItem TaskItem { get; set; } = new();

    [BindProperty]
    public string? LabelText { get; set; }

    public List<SelectListItem> ProjectList { get; set; } = new();
    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task OnGetAsync(int? projectId)
    {
        if (projectId.HasValue)
            TaskItem.ProjectId = projectId.Value;

        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnGetAssigneesAsync(int projectId)
    {
        if (!(await _access.ProjectAsync(projectId)).CreateTask) return Forbid();
        return new JsonResult((await _access.AssignableUsersAsync(projectId)).Select(x => new { id = x.Id, name = x.FullName }));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!(await _access.ProjectAsync(TaskItem.ProjectId)).CreateTask) return Forbid();
        if (!await _access.CanAssignAsync(TaskItem.ProjectId, TaskItem.AssignedToUserId))
            ModelState.AddModelError("TaskItem.AssignedToUserId", "Bu kullanıcıya görev atayamazsınız.");
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
        try
        {
            await _taskService.CreateTaskAsync(TaskItem, userId, LabelText);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(LabelText), ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        return RedirectToPage("Details", new { id = TaskItem.Id });
    }

    private async Task LoadDropdownsAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        var available = new List<Project>();
        foreach (var p in projects) if ((await _access.ProjectAsync(p.Id)).CreateTask) available.Add(p);
        projects = available;
        ProjectList = projects.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();

        if (TaskItem.ProjectId == 0 && projects.Count > 0) TaskItem.ProjectId = projects[0].Id;
        var users = await _access.AssignableUsersAsync(TaskItem.ProjectId);
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
