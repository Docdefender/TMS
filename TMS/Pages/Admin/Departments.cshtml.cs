using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DepartmentsModel : PageModel
{
    private readonly DepartmentService _departmentService;

    public DepartmentsModel(DepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public List<Department> Departments { get; set; } = new();

    [BindProperty]
    public Department NewDepartment { get; set; } = new();

    public async Task OnGetAsync()
    {
        Departments = await _departmentService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDepartment.Name))
        {
            ModelState.AddModelError("NewDepartment.Name", "Name is required.");
            Departments = await _departmentService.GetAllAsync();
            return Page();
        }

        await _departmentService.CreateAsync(NewDepartment);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _departmentService.DeleteAsync(id);
        return RedirectToPage();
    }
}
