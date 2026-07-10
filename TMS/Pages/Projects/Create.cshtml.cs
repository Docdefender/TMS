using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DepartmentService _departmentService;
    private readonly CategoryService _categoryService;

    public CreateModel(ProjectService projectService, UserManager<ApplicationUser> userManager,
        DepartmentService departmentService, CategoryService categoryService)
    {
        _projectService = projectService;
        _userManager = userManager;
        _departmentService = departmentService;
        _categoryService = categoryService;
    }

    [BindProperty]
    public Project Project { get; set; } = new();

    [BindProperty]
    public List<string> MemberUserIds { get; set; } = new();

    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> DepartmentList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        Project.ManagerUserId = userId;
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Project.CreatedByUser");
        ModelState.Remove("Project.AssignedToUser");
        ModelState.Remove("Project.Manager");
        ModelState.Remove("Project.Department");
        ModelState.Remove("Project.Category");
        ModelState.Remove("Project.Members");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        await _projectService.CreateProjectAsync(Project, userId, MemberUserIds);
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _projectService.GetAllUsersAsync();
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var departments = await _departmentService.GetAllAsync();
        DepartmentList = departments.Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
