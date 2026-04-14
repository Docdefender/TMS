using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

public class DetailsModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;

    public DetailsModel(ProjectService projectService, TaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    public Project Project { get; set; } = null!;

    [BindProperty]
    public TaskItem NewTask { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project is null)
        {
            return NotFound();
        }

        Project = project;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        NewTask.ProjectId = id;

        if (string.IsNullOrWhiteSpace(NewTask.Title))
        {
            ModelState.AddModelError("NewTask.Title", "Task title is required.");
        }

        if (!ModelState.IsValid)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project is null)
            {
                return NotFound();
            }
            Project = project;
            return Page();
        }

        await _taskService.CreateTaskAsync(NewTask);
        return RedirectToPage("Details", new { id });
    }
}
