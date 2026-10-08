using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly AccessService _access;
    private readonly TicketService _tickets;

    public DashboardService(ApplicationDbContext context, AccessService access, TicketService tickets)
    {
        _context = context;
        _access = access;
        _tickets = tickets;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string userId, bool showAll = false)
    {
        var projectsQuery = await _access.ProjectsAsync();
        var tasksQuery = await _access.TasksAsync(); // ← düzeltildi


        var myProjects = await projectsQuery.ToListAsync();
        var myTasks = await tasksQuery.ToListAsync();

        var now = DateTime.Today;
        var nextWeek = now.AddDays(7);

        // My assigned projects and tasks for widgets
        var assignedProjectsQuery = projectsQuery
            .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled);
        if (!showAll)
            assignedProjectsQuery = assignedProjectsQuery
                .Where(p => p.ManagerUserId == userId || p.Members.Any(m => m.UserId == userId));

        var myAssignedProjects = await assignedProjectsQuery
            .Include(p => p.Tasks)
            .Include(p => p.Manager)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Department)
            .Include(p => p.Category)
            .OrderBy(p => p.EndDate)
            .Take(6)
            .AsNoTracking().ToListAsync();
        var visibleTaskIds = myTasks.Select(t => t.Id).ToHashSet();
        foreach (var project in myAssignedProjects)
            project.Tasks = project.Tasks.Where(t => visibleTaskIds.Contains(t.Id)).ToList();

        var myAssignedTasks = await tasksQuery
            .Where(t => !t.IsDeleted && t.AssignedToUserId == userId && t.Status != Models.TaskStatus.Done) // ← düzeltildi
            .Include(t => t.Project)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .ToListAsync();

        // Recent activity (last 10 audit logs)
        var recentActivity = await _context.AuditLogs.Where(a => a.UserId == userId)
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();

        // Upcoming deadlines (projects and tasks due within 7 days)
        var upcomingProjects = await projectsQuery
            .Where(p => p.EndDate.HasValue && p.EndDate.Value >= now && p.EndDate.Value <= nextWeek
                && p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
            .Select(p => new { Type = "Project", Name = p.Name, DueDate = p.EndDate ?? DateTime.MinValue, Id = p.Id })
            .ToListAsync();

        var upcomingTasks = await tasksQuery
            .Where(t => !t.IsDeleted && t.DueDate.HasValue && t.DueDate.Value >= now && t.DueDate.Value <= nextWeek // ← düzeltildi
                && t.Status != Models.TaskStatus.Done)
            .Select(t => new { Type = "Task", Name = t.Title, DueDate = t.DueDate ?? DateTime.MinValue, Id = t.Id })
            .ToListAsync();

        var upcomingDeadlines = upcomingProjects.Cast<object>()
            .Concat(upcomingTasks.Cast<object>())
            .ToList();

        var viewModel = new DashboardViewModel
        {
            TotalProjects = myProjects.Count,
            NotStartedProjects = myProjects.Count(p => p.Status == ProjectStatus.NotStarted),
            InProgressProjects = myProjects.Count(p => p.Status == ProjectStatus.InProgress),
            CompletedProjects = myProjects.Count(p => p.Status == ProjectStatus.Completed),
            OnHoldProjects = myProjects.Count(p => p.Status == ProjectStatus.OnHold),
            CancelledProjects = myProjects.Count(p => p.Status == ProjectStatus.Cancelled),
            OverdueProjectsCount = myProjects.Count(p => p.EndDate.HasValue && p.EndDate.Value < now && p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled),

            TotalTasks = myTasks.Count,
            ToDoTasks = myTasks.Count(t => t.Status == Models.TaskStatus.ToDo),
            InProgressTasks = myTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
            DoneTasks = myTasks.Count(t => t.Status == Models.TaskStatus.Done),
            InReviewTasks = myTasks.Count(t => t.Status == Models.TaskStatus.InReview),
            OverdueTasksCount = myTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Models.TaskStatus.Done),

            OpenTickets = await (await _tickets.VisibleAsync())
                .CountAsync(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),

            RecentActivity = recentActivity,
            MyAssignedProjects = myAssignedProjects,
            MyAssignedTasks = myAssignedTasks,
            UpcomingDeadlines = upcomingDeadlines
        };

        if ((await _access.ActorAsync()).IsAdmin)
        {
            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.TotalDepartments = await _context.Departments.CountAsync();
            viewModel.TotalCategories = await _context.Categories.CountAsync();
        }

        return viewModel;
    }
}

public class DashboardViewModel
{
    public int TotalProjects { get; set; }
    public int NotStartedProjects { get; set; }
    public int InProgressProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int OnHoldProjects { get; set; }
    public int CancelledProjects { get; set; }
    public int OverdueProjectsCount { get; set; }

    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int DoneTasks { get; set; }
    public int InReviewTasks { get; set; }
    public int OverdueTasksCount { get; set; }
    public int OpenTickets { get; set; }

    public List<AuditLog> RecentActivity { get; set; } = new();
    public List<Project> MyAssignedProjects { get; set; } = new();
    public List<TaskItem> MyAssignedTasks { get; set; } = new();
    public List<object> UpcomingDeadlines { get; set; } = new();

    public int TotalUsers { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalCategories { get; set; }
}
