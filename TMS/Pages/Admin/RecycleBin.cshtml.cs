using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RecycleBinModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly CommentService _commentService;
    private readonly DepartmentService _departmentService;
    private readonly CategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecycleBinModel(
        ProjectService projectService,
        TaskService taskService,
        CommentService commentService,
        DepartmentService departmentService,
        CategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _projectService = projectService;
        _taskService = taskService;
        _commentService = commentService;
        _departmentService = departmentService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public List<Project> DeletedProjects { get; set; } = new();
    public List<TaskItem> DeletedTasks { get; set; } = new();
    public List<Comment> DeletedComments { get; set; } = new();
    public List<Department> DeletedDepartments { get; set; } = new();
    public List<Category> DeletedCategories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    public async Task OnGetAsync()
    {
        DeletedProjects = await _projectService.GetDeletedProjectsAsync();
        DeletedTasks = await _taskService.GetDeletedTasksAsync();
        DeletedComments = await _commentService.GetDeletedCommentsAsync();
        DeletedDepartments = await _departmentService.GetDeletedAsync();
        DeletedCategories = await _categoryService.GetDeletedAsync();
    }

    public async Task<IActionResult> OnPostRestoreProjectAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        await _projectService.RestoreProjectAsync(id, userId);
        return RedirectToPage(new { tab = "projects" });
    }

    public async Task<IActionResult> OnPostRestoreTaskAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        await _taskService.RestoreTaskAsync(id, userId);
        return RedirectToPage(new { tab = "tasks" });
    }

    public async Task<IActionResult> OnPostRestoreCommentAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        await _commentService.RestoreCommentAsync(id, userId);
        return RedirectToPage(new { tab = "comments" });
    }

    public async Task<IActionResult> OnPostRestoreDepartmentAsync(int id)
    {
        await _departmentService.RestoreAsync(id);
        return RedirectToPage(new { tab = "departments" });
    }

    public async Task<IActionResult> OnPostRestoreCategoryAsync(int id)
    {
        await _categoryService.RestoreAsync(id);
        return RedirectToPage(new { tab = "categories" });
    }
}
