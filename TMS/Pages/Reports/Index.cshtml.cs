using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Reports;

[Authorize]
public class IndexModel(AccessService access, TicketService tickets) : PageModel
{
    public string ScopeLabel { get; private set; } = string.Empty;
    public int ProjectCount { get; private set; }
    public int ActiveProjectCount { get; private set; }
    public int TaskCount { get; private set; }
    public int CompletedTaskCount { get; private set; }
    public int OverdueTaskCount { get; private set; }
    public int OpenTicketCount { get; private set; }
    public int SlaBreachCount { get; private set; }
    public List<ReportSlice> ProjectStatuses { get; private set; } = [];
    public List<ReportSlice> TaskStatuses { get; private set; } = [];
    public List<ReportWorkload> Workloads { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var actor = await access.ActorAsync();
        ScopeLabel = actor.IsAdmin ? "Tüm organizasyon"
            : actor.IsManager ? "Yönettiğiniz ve katıldığınız çalışmalar"
            : "Erişebildiğiniz çalışmalar";

        var projects = (await access.ProjectsAsync()).AsNoTracking();
        var taskQuery = (await access.TasksAsync()).AsNoTracking();
        var ticketQuery = (await tickets.VisibleAsync()).AsNoTracking();
        ProjectCount = await projects.CountAsync();
        ActiveProjectCount = await projects.CountAsync(x => x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.Cancelled);
        TaskCount = await taskQuery.CountAsync();
        CompletedTaskCount = await taskQuery.CountAsync(x => x.Status == Models.TaskStatus.Done);
        OverdueTaskCount = await taskQuery.CountAsync(x => x.DueDate.HasValue && x.DueDate < DateTime.Today
            && x.Status != Models.TaskStatus.Done);
        OpenTicketCount = await ticketQuery.CountAsync(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
        SlaBreachCount = await ticketQuery.CountAsync(x => x.SlaDueAt < DateTime.UtcNow
            && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);

        var projectGroups = await projects.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync();
        ProjectStatuses = Enum.GetValues<ProjectStatus>().Select(status => new ReportSlice(
            ProjectStatusText(status), projectGroups.FirstOrDefault(x => x.Status == status)?.Count ?? 0,
            ProjectCount == 0 ? 0 : (int)Math.Round((projectGroups.FirstOrDefault(x => x.Status == status)?.Count ?? 0) * 100d / ProjectCount))).ToList();

        var taskGroups = await taskQuery.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync();
        TaskStatuses = Enum.GetValues<Models.TaskStatus>().Select(status => new ReportSlice(
            TaskStatusText(status), taskGroups.FirstOrDefault(x => x.Status == status)?.Count ?? 0,
            TaskCount == 0 ? 0 : (int)Math.Round((taskGroups.FirstOrDefault(x => x.Status == status)?.Count ?? 0) * 100d / TaskCount))).ToList();

        var workloadRows = await taskQuery.Where(x => x.AssignedToUserId != null && x.Status != Models.TaskStatus.Done)
            .GroupBy(x => new { x.AssignedToUserId, x.AssignedToUser!.FullName })
            .Select(x => new { Name = x.Key.FullName, Active = x.Count(),
                Overdue = x.Count(t => t.DueDate < DateTime.Today) })
            .OrderByDescending(x => x.Active).ThenBy(x => x.Name).Take(10).ToListAsync();
        Workloads = workloadRows.Select(x => new ReportWorkload(x.Name, x.Active, x.Overdue)).ToList();
    }

    private static string ProjectStatusText(ProjectStatus status) => status switch
    {
        ProjectStatus.NotStarted => "Başlamadı", ProjectStatus.InProgress => "Devam ediyor",
        ProjectStatus.OnHold => "Beklemede", ProjectStatus.Completed => "Tamamlandı",
        ProjectStatus.Cancelled => "İptal", _ => status.ToString()
    };

    private static string TaskStatusText(Models.TaskStatus status) => status switch
    {
        Models.TaskStatus.ToDo => "Yapılacak", Models.TaskStatus.InProgress => "Devam ediyor",
        Models.TaskStatus.InReview => "İncelemede", Models.TaskStatus.Done => "Tamamlandı", _ => status.ToString()
    };
}

public record ReportSlice(string Label, int Count, int Percent);
public record ReportWorkload(string Name, int Active, int Overdue);
