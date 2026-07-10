using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tasks;

[Authorize]
public class IndexModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(TaskService taskService, UserManager<ApplicationUser> userManager)
    {
        _taskService = taskService;
        _userManager = userManager;
    }

    public List<TaskItem> Tasks { get; set; } = new();

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

        Tasks = await _taskService.GetAllTasksAsync(
            statuses:  Statuses.Count > 0 ? Statuses : null,
            search:    Search,
            startDate: StartDate,
            endDate:   EndDate,
            userId:    userId,
            showAll:   showAll);
    }
}
