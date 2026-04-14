using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

public class IndexModel : PageModel
{
    private readonly ProjectService _projectService;

    public IndexModel(ProjectService projectService)
    {
        _projectService = projectService;
    }

    public List<Project> Projects { get; set; } = new();

    public async Task OnGetAsync()
    {
        Projects = await _projectService.GetAllProjectsAsync();
    }
}
