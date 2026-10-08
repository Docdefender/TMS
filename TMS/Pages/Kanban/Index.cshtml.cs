using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Kanban;

[Authorize]
public class IndexModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly AccessService _access;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(TaskService taskService, AccessService access, UserManager<ApplicationUser> userManager)
    {
        _taskService = taskService;
        _access = access;
        _userManager = userManager;
    }

    public TaskBoardViewModel Board { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        var movableIds = await _access.ContributableTaskIdsAsync(tasks.Select(x => x.Id));

        Board = new TaskBoardViewModel
        {
            Tasks = tasks,
            MovableTaskIds = movableIds,
            UpdateUrl = Url.Page("/Kanban/Index", "UpdateTaskStatus") ?? "?handler=UpdateTaskStatus",
            BoardKey = "general-tasks",
            ShowProjectFilter = true
        };
    }

    public async Task<IActionResult> OnPostUpdateTaskStatusAsync([FromBody] UpdateTaskStatusRequest request)
    {
        if (request is null || !Enum.TryParse<Models.TaskStatus>(request.NewStatus, true, out var status)
            || !Enum.IsDefined(status))
            return BadRequest(new { success = false, message = "Geçersiz görev durumu." });

        if (!(await _access.TaskAsync(request.TaskId)).Contribute)
            return Forbid();

        try
        {
            await _taskService.UpdateTaskStatusAsync(request.TaskId, status, _userManager.GetUserId(User));
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public sealed class UpdateTaskStatusRequest
    {
        public int TaskId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
