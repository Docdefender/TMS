using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

public class CreateModel : PageModel
{
    private readonly ProjectService _projectService;

    public CreateModel(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [BindProperty]
    public Project Project { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _projectService.CreateProjectAsync(Project);
        return RedirectToPage("Index");
    }
}
