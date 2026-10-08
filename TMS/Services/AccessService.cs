using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public record AccessActor(string UserId, bool IsAdmin, bool IsManager, bool IsMember, int[] DepartmentIds,
    int? HomeDepartmentId = null);
public record ProjectAccess(bool View, bool Edit, bool Delete, bool ChangeStatus, bool CreateTask);
public record TaskAccess(bool View, bool Edit, bool Delete, bool Contribute);

public static class AccessRules
{
    public static ProjectAccess Project(AccessActor a, Project p)
    {
        if (p.IsDeleted) return new(false, false, false, false, false);
        if (a.IsAdmin) return new(true, true, true, true, true);
        var department = a.IsManager && p.DepartmentId.HasValue && a.DepartmentIds.Contains(p.DepartmentId.Value);
        var participant = p.ManagerUserId == a.UserId || p.Members.Any(x => x.UserId == a.UserId);
        var assigner = a.IsManager && p.FirstAssignedByUserId == a.UserId;
        return new(department || participant || assigner, department, department || assigner,
            a.IsManager && participant, department || participant && (a.IsManager || a.IsMember));
    }

    public static TaskAccess Task(AccessActor a, TaskItem t, bool projectVisible)
    {
        if (t.IsDeleted || t.Project.IsDeleted) return new(false, false, false, false);
        if (a.IsAdmin) return new(true, true, true, true);
        var department = a.IsManager && t.Project.DepartmentId.HasValue && a.DepartmentIds.Contains(t.Project.DepartmentId.Value);
        var creator = t.CreatedByUserId == a.UserId;
        var assigned = t.AssignedToUserId == a.UserId;
        var assigner = a.IsManager && t.FirstAssignedByUserId == a.UserId;
        var edit = a.IsManager && (department || creator || assigned);
        var view = department || projectVisible || creator || assigned || assigner || t.HistoryAccess.Any(x => x.UserId == a.UserId && x.RevokedAt == null);
        return new(view, edit, a.IsManager && (department || creator || assigner), edit || assigned);
    }
}

// Current request is authoritative; legacy caller-provided IDs and role flags cannot widen access.
public class AccessService(ApplicationDbContext db, IHttpContextAccessor http)
{
    private Task<AccessActor>? _actor;
    public Task<AccessActor> ActorAsync() => _actor ??= LoadActorAsync();

    private async Task<AccessActor> LoadActorAsync()
    {
        var id = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var roles = await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id
                           where ur.UserId == id select role.Name).ToListAsync();
        var manager = roles.Contains("Manager");
        var departments = manager ? await db.ManagerDepartments.Where(x => x.UserId == id && !x.Department.IsDeleted)
            .Select(x => x.DepartmentId).ToArrayAsync() : [];
        var user = await db.Users.Where(x => x.Id == id).Select(x => new { x.IsActive, x.DepartmentId }).FirstOrDefaultAsync();
        Require(user is { IsActive: true });
        var homeDepartmentId = user!.DepartmentId;
        return new(id, roles.Contains("Admin"), manager, roles.Contains("Member"), departments, homeDepartmentId);
    }

    public async Task<IQueryable<Project>> ProjectsAsync()
    {
        var a = await ActorAsync();
        var query = db.Projects.Where(p => !p.IsDeleted);
        if (a.IsAdmin) return query;
        return query.Where(p => a.IsManager && p.DepartmentId.HasValue && a.DepartmentIds.Contains(p.DepartmentId.Value)
            || p.ManagerUserId == a.UserId || p.Members.Any(m => m.UserId == a.UserId)
            || a.IsManager && p.FirstAssignedByUserId == a.UserId);
    }

    public async Task<IQueryable<TaskItem>> TasksAsync()
    {
        var a = await ActorAsync();
        var projects = await ProjectsAsync();
        var query = db.TaskItems.Where(t => !t.IsDeleted && !t.Project.IsDeleted);
        if (a.IsAdmin) return query;
        return query.Where(t =>
            db.Tickets.Any(ticket => ticket.LinkedTaskId == t.Id
                && (ticket.RequesterUserId == a.UserId
                    || a.HomeDepartmentId.HasValue && ticket.SupportDepartmentId == a.HomeDepartmentId
                        && db.Departments.Any(d => d.Id == a.HomeDepartmentId && d.IsTicketSupport)
                    || ticket.Viewers.Any(viewer => viewer.UserId == a.UserId)))
            || !db.Tickets.Any(ticket => ticket.LinkedTaskId == t.Id)
                && (projects.Any(p => p.Id == t.ProjectId) || t.AssignedToUserId == a.UserId
                    || t.CreatedByUserId == a.UserId || a.IsManager && t.FirstAssignedByUserId == a.UserId
                    || t.HistoryAccess.Any(h => h.UserId == a.UserId && h.RevokedAt == null)));
    }

    public async Task<ProjectAccess> ProjectAsync(int id)
    {
        var p = await db.Projects.AsNoTracking().Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? new(false, false, false, false, false) : AccessRules.Project(await ActorAsync(), p);
    }

    public async Task<TaskAccess> TaskAsync(int id)
    {
        var t = await db.TaskItems.AsNoTracking().Include(x => x.Project).Include(x => x.HistoryAccess).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || t.IsDeleted || t.Project.IsDeleted) return new(false, false, false, false);
        var actor = await ActorAsync();
        var ticket = await db.Tickets.AsNoTracking().Include(x => x.Viewers)
            .FirstOrDefaultAsync(x => x.LinkedTaskId == id);
        if (ticket is not null)
        {
            if (actor.IsAdmin) return new(true, true, true, true);
            var support = actor.HomeDepartmentId.HasValue && ticket.SupportDepartmentId == actor.HomeDepartmentId
                && await db.Departments.AnyAsync(d => d.Id == actor.HomeDepartmentId && d.IsTicketSupport);
            var view = support || ticket.RequesterUserId == actor.UserId
                || ticket.Viewers.Any(v => v.UserId == actor.UserId);
            return new(view, support, support, support);
        }
        return AccessRules.Task(actor, t, (await ProjectAsync(t.ProjectId)).View);
    }

    public async Task<HashSet<int>> ContributableTaskIdsAsync(IEnumerable<int> taskIds)
    {
        var ids = taskIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        var actor = await ActorAsync();
        if (actor.IsAdmin) return ids.ToHashSet();

        var query = db.TaskItems.Where(t => ids.Contains(t.Id) && !t.IsDeleted && !t.Project.IsDeleted);
        query = query.Where(t =>
            db.Tickets.Any(ticket => ticket.LinkedTaskId == t.Id
                && actor.HomeDepartmentId.HasValue
                && ticket.SupportDepartmentId == actor.HomeDepartmentId
                && db.Departments.Any(d => d.Id == actor.HomeDepartmentId && d.IsTicketSupport))
            || !db.Tickets.Any(ticket => ticket.LinkedTaskId == t.Id)
                && (t.AssignedToUserId == actor.UserId
                    || actor.IsManager && (t.CreatedByUserId == actor.UserId
                        || t.Project.DepartmentId.HasValue
                            && actor.DepartmentIds.Contains(t.Project.DepartmentId.Value))));

        return (await query.Select(t => t.Id).ToListAsync()).ToHashSet();
    }

    public async Task<bool> CanCreateProjectAsync(int? departmentId = null)
    {
        var a = await ActorAsync();
        return a.IsAdmin || a.IsManager && (departmentId.HasValue ? a.DepartmentIds.Contains(departmentId.Value) : a.DepartmentIds.Length > 0);
    }

    public async Task<List<ApplicationUser>> AssignableUsersAsync(int? projectId = null, bool managersOnly = false)
    {
        var a = await ActorAsync();
        var admins = from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Name == "Admin" select ur.UserId;
        var users = db.Users.Where(u => u.IsActive && !admins.Contains(u.Id));
        if (managersOnly)
            users = users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Manager")));
        else if (!a.IsAdmin && !a.IsManager)
            users = users.Where(u => projectId.HasValue && db.ProjectMembers.Any(m => m.ProjectId == projectId.Value && m.UserId == u.Id)
                && db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Member")));
        return await users.OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task<bool> CanAssignAsync(int projectId, string? userId) => string.IsNullOrEmpty(userId)
        || (await AssignableUsersAsync(projectId)).Any(x => x.Id == userId);

    public static void Require(bool allowed)
    {
        if (!allowed) throw new UnauthorizedAccessException("Bu işlem için yetkiniz bulunmuyor.");
    }
}
