using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Projects;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ProjectService _projectService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ProjectService projectService, UserManager<ApplicationUser> userManager)
    {
        _projectService = projectService;
        _userManager = userManager;
    }

    public List<Project> Projects { get; set; } = new();

    [BindProperty(SupportsGet = true)] public List<string> Statuses  { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? Search         { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? StartDate    { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate      { get; set; }

    public bool HasActiveFilter => Statuses.Count > 0
        || !string.IsNullOrEmpty(Search)
        || StartDate.HasValue
        || EndDate.HasValue;

    public async Task OnGetAsync()
    {
        var userId  = _userManager.GetUserId(User)!;
        var showAll = User.IsInRole("Admin") || User.IsInRole("Manager");

        Projects = await _projectService.GetAllProjectsAsync(
            statuses:  Statuses.Count > 0 ? Statuses : null,
            search:    Search,
            startDate: StartDate,
            endDate:   EndDate,
            userId:    userId,
            showAll:   showAll);
    }
}
