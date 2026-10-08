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
    private readonly AccessService _access;
    private readonly DepartmentService _departmentService;
    private readonly CategoryService _categoryService;

    public CreateModel(ProjectService projectService, UserManager<ApplicationUser> userManager, AccessService access,
        DepartmentService departmentService, CategoryService categoryService)
    {
        _projectService = projectService;
        _userManager = userManager;
        _access = access;
        _departmentService = departmentService;
        _categoryService = categoryService;
    }

    [BindProperty]
    public Project Project { get; set; } = new();

    [BindProperty]
    public List<string> MemberUserIds { get; set; } = new();

    public TeamPickerModel TeamPicker { get; set; } = new([], [], []);
    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> DepartmentList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _access.CanCreateProjectAsync()) return Forbid();
        var userId = _userManager.GetUserId(User);
        Project.ManagerUserId = User.IsInRole("Admin") ? null : userId;
        await LoadDropdownsAsync();
        if (DepartmentList.Count == 1) Project.DepartmentId = int.Parse(DepartmentList[0].Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _access.CanCreateProjectAsync(Project.DepartmentId)) return Forbid();
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
        try { await _projectService.CreateProjectAsync(Project, userId, MemberUserIds); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadDropdownsAsync(); return Page(); }
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var users = await _access.AssignableUsersAsync(managersOnly: true);
        UserList = users.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        var departments = await _departmentService.GetAllAsync();
        TeamPicker = new(departments, await _access.AssignableUsersAsync(), MemberUserIds);
        var actor = await _access.ActorAsync();
        if (!actor.IsAdmin) departments = departments.Where(x => actor.DepartmentIds.Contains(x.Id)).ToList();
        DepartmentList = departments.Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();

        var categories = await _categoryService.GetAllAsync();
        CategoryList = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
