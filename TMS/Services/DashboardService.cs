using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string userId, bool showAll = false)
    {
        var projectsQuery = _context.Projects.AsQueryable();
        var tasksQuery = _context.TaskItems.Where(t => !t.IsDeleted).AsQueryable(); // ← düzeltildi

        if (!showAll)
        {
            projectsQuery = projectsQuery.Where(p => p.AssignedToUserId == userId);
            tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == userId);
        }

        var myProjects = await projectsQuery.ToListAsync();
        var myTasks = await tasksQuery.ToListAsync();

        var now = DateTime.Today;
        var nextWeek = now.AddDays(7);

        // My assigned projects and tasks for widgets
        var myAssignedProjects = await _context.Projects
            .Where(p => p.AssignedToUserId == userId && p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
            .Include(p => p.Tasks)
            .OrderBy(p => p.EndDate)
            .Take(5)
            .ToListAsync();

        var myAssignedTasks = await _context.TaskItems
            .Where(t => !t.IsDeleted && t.AssignedToUserId == userId && t.Status != Models.TaskStatus.Done) // ← düzeltildi
            .Include(t => t.Project)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .ToListAsync();

        // Recent activity (last 10 audit logs)
        var recentActivity = await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();

        // Upcoming deadlines (projects and tasks due within 7 days)
        var upcomingProjects = await _context.Projects
            .Where(p => p.EndDate.HasValue && p.EndDate.Value >= now && p.EndDate.Value <= nextWeek
                && p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
            .Select(p => new { Type = "Project", Name = p.Name, DueDate = p.EndDate ?? DateTime.MinValue, Id = p.Id })
            .ToListAsync();

        var upcomingTasks = await _context.TaskItems
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

            RecentActivity = recentActivity,
            MyAssignedProjects = myAssignedProjects,
            MyAssignedTasks = myAssignedTasks,
            UpcomingDeadlines = upcomingDeadlines
        };

        if (showAll)
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

    public List<AuditLog> RecentActivity { get; set; } = new();
    public List<Project> MyAssignedProjects { get; set; } = new();
    public List<TaskItem> MyAssignedTasks { get; set; } = new();
    public List<object> UpcomingDeadlines { get; set; } = new();

    public int TotalUsers { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalCategories { get; set; }
}
