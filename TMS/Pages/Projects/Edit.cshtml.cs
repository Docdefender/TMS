using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

[Authorize]
public class EditModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly DepartmentService _departmentService;
    private readonly CategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(ProjectService projectService, DepartmentService departmentService,
        CategoryService categoryService, UserManager<ApplicationUser> userManager)
    {
        _projectService = projectService;
        _departmentService = departmentService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    [BindProperty]
    public Project Project { get; set; } = null!;

    [BindProperty]
    public List<string> MemberUserIds { get; set; } = new();

    public List<SelectListItem> UserList { get; set; } = new();
    public List<SelectListItem> DepartmentList { get; set; } = new();
    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                      project.CreatedByUserId == userId || project.ManagerUserId == userId ||
                      project.Members.Any(m => m.UserId == userId);

        if (!canEdit) return Forbid();

        Project = project;
        MemberUserIds = project.Members.Select(m => m.UserId).ToList();
        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var existingProject = await _projectService.GetProjectByIdAsync(id);
        if (existingProject is null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") ||
                      existingProject.CreatedByUserId == userId || existingProject.ManagerUserId == userId ||
                      existingProject.Members.Any(m => m.UserId == userId);

        if (!canEdit) return Forbid();

        ModelState.Remove("Project.CreatedByUser");
        ModelState.Remove("Project.AssignedToUser");
        ModelState.Remove("Project.Manager");
        ModelState.Remove("Project.Department");
        ModelState.Remove("Project.Category");
        ModelState.Remove("Project.Tasks");
        ModelState.Remove("Project.Comments");
        ModelState.Remove("Project.Attachments");
        ModelState.Remove("Project.Members");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        existingProject.Name = Project.Name;
        existingProject.Description = Project.Description;
        existingProject.StartDate = Project.StartDate;
        existingProject.EndDate = Project.EndDate;
        existingProject.Status = Project.Status;
        existingProject.DepartmentId = Project.DepartmentId;
        existingProject.CategoryId = Project.CategoryId;
        existingProject.ManagerUserId = Project.ManagerUserId;

        await _projectService.UpdateProjectAsync(existingProject, userId, MemberUserIds);
        return RedirectToPage("Details", new { id });
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
