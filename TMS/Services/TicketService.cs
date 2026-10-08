using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public record TicketAccess(bool View, bool Manage, bool ManageViewers, bool Reply);

public class TicketService(ApplicationDbContext db, AccessService access, TaskService tasks,
    NotificationService notifications)
{
    private async Task<bool> IsSupportAsync(string userId, int? departmentId) =>
        departmentId.HasValue && await db.Users.AnyAsync(u => u.Id == userId && u.DepartmentId == departmentId
            && u.Department != null && u.Department.IsTicketSupport && !u.Department.IsDeleted);

    public async Task<IQueryable<Ticket>> VisibleAsync()
    {
        var actor = await access.ActorAsync();
        var query = db.Tickets.AsQueryable();
        if (actor.IsAdmin) return query;
        return query.Where(t => t.RequesterUserId == actor.UserId || t.AssignedToUserId == actor.UserId
            || t.Viewers.Any(v => v.UserId == actor.UserId)
            || t.SupportDepartmentId != null && db.Users.Any(u => u.Id == actor.UserId
                && u.DepartmentId == t.SupportDepartmentId && u.Department != null
                && u.Department.IsTicketSupport && !u.Department.IsDeleted));
    }

    public async Task<TicketAccess> RightsAsync(int ticketId)
    {
        var actor = await access.ActorAsync();
        var ticket = await db.Tickets.AsNoTracking().Where(x => x.Id == ticketId)
            .Select(x => new { x.RequesterUserId, x.AssignedToUserId, x.SupportDepartmentId,
                Viewer = x.Viewers.Any(v => v.UserId == actor.UserId) }).FirstOrDefaultAsync();
        if (ticket is null) return new(false, false, false, false);
        var support = actor.IsAdmin || await IsSupportAsync(actor.UserId, ticket.SupportDepartmentId);
        var requester = ticket.RequesterUserId == actor.UserId;
        var assigned = ticket.AssignedToUserId == actor.UserId;
        return new(support || requester || assigned || ticket.Viewer, support, support || requester, support || requester);
    }

    public async Task<Ticket?> GetAsync(int ticketId)
    {
        if (!(await RightsAsync(ticketId)).View) return null;
        return await db.Tickets.AsNoTracking().Include(t => t.Requester).Include(t => t.AssignedToUser)
            .Include(t => t.Viewers).ThenInclude(v => v.User)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    public async Task<List<TicketMessage>> MessagesAsync(int ticketId)
    {
        var rights = await RightsAsync(ticketId);
        AccessService.Require(rights.View);
        return await db.TicketMessages.AsNoTracking().Include(x => x.AuthorUser)
            .Where(x => x.TicketId == ticketId && (rights.Manage || !x.IsInternal))
            .OrderBy(x => x.SentAt).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task<List<TicketEvent>> EventsAsync(int ticketId)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        return await db.TicketEvents.AsNoTracking().Include(x => x.ActorUser)
            .Where(x => x.TicketId == ticketId).OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id).ToListAsync();
    }

    public async Task<Ticket> CreateAsync(string subject, string requesterId, int supportDepartmentId, string body,
        TicketPriority priority = TicketPriority.Normal)
    {
        var actor = await access.ActorAsync();
        AccessService.Require(actor.IsAdmin || await IsSupportAsync(actor.UserId, supportDepartmentId));
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 200 || string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("Konu ve ilk mesaj gereklidir.");
        var requester = await db.Users.FirstOrDefaultAsync(u => u.Id == requesterId && u.IsActive)
            ?? throw new InvalidOperationException("Talep sahibi bulunamadı.");
        if (!await db.Departments.AnyAsync(d => d.Id == supportDepartmentId && d.IsTicketSupport && !d.IsDeleted))
            throw new InvalidOperationException("Ticket destek departmanı bulunamadı.");
        if (!Enum.IsDefined(priority)) throw new InvalidOperationException("Geçersiz Ticket önceliği.");
        var createdAt = DateTime.UtcNow;
        var assigneeId = await TicketAutoAssignment.SelectAssigneeAsync(db, supportDepartmentId);
        var ticket = new Ticket { Subject = subject.Trim(), RequesterUserId = requesterId,
            RequesterDepartmentId = requester.DepartmentId, SupportDepartmentId = supportDepartmentId,
            AssignedToUserId = assigneeId,
            Priority = priority, CreatedAt = createdAt, UpdatedAt = createdAt,
            SlaDueAt = TicketSla.DefaultDueAt(createdAt, priority) };
        ticket.Messages.Add(new TicketMessage { AuthorUserId = requesterId, Body = body.Trim(), IsIncoming = true });
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = "Created" });
        if (assigneeId is not null)
            ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = "AutoAssigned", Details = assigneeId });
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        if (assigneeId is not null)
            await notifications.CreateAsync(assigneeId, "Assignment", "Ticket size otomatik atandı",
                ticket.Subject, $"/Tickets/Details/{ticket.Id}", actor.UserId);
        return ticket;
    }

    public async Task AssignAsync(int ticketId, string? assigneeId)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        var ticket = await db.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException("Ticket bulunamadı.");
        if (!string.IsNullOrWhiteSpace(assigneeId)
            && (!await db.Users.AnyAsync(u => u.Id == assigneeId && u.IsActive)
                || !await IsSupportAsync(assigneeId, ticket.SupportDepartmentId)))
            throw new InvalidOperationException("Ticket yalnızca Sistem Geliştirme departmanına atanabilir.");
        if (ticket.AssignedToUserId == assigneeId) return;
        ticket.AssignedToUserId = assigneeId;
        ticket.UpdatedAt = DateTime.UtcNow;
        var actor = await access.ActorAsync();
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId,
            Type = "Assigned", Details = assigneeId });
        await db.SaveChangesAsync();
        await notifications.CreateAsync(assigneeId, "Assignment", "Ticket size atandı",
            ticket.Subject, $"/Tickets/Details/{ticket.Id}", actor.UserId);
    }

    public async Task ChangeStatusAsync(int ticketId, TicketStatus status)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        if (!Enum.IsDefined(status)) throw new InvalidOperationException("Geçersiz Ticket durumu.");
        var ticket = await db.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException("Ticket bulunamadı.");
        if (ticket.Status == status) return;
        var previous = ticket.Status;
        ticket.Status = status;
        if (TicketSla.IsFinished(status) && ticket.SlaDueAt.HasValue && ticket.SlaDueAt.Value < DateTime.UtcNow)
            ticket.SlaBreachedAt ??= DateTime.UtcNow;
        ticket.ResolvedByLinkedTask = false;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Events.Add(new TicketEvent { ActorUserId = (await access.ActorAsync()).UserId,
            Type = "StatusChanged", Details = $"{previous} -> {status}" });
        await db.SaveChangesAsync();
    }

    public async Task UpdateSlaAsync(int ticketId, TicketPriority priority, DateTime? localDueAt)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        if (!Enum.IsDefined(priority)) throw new InvalidOperationException("Geçersiz Ticket önceliği.");
        var ticket = await db.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException("Ticket bulunamadı.");
        var dueAt = localDueAt.HasValue
            ? DateTime.SpecifyKind(localDueAt.Value, DateTimeKind.Local).ToUniversalTime()
            : TicketSla.DefaultDueAt(ticket.CreatedAt, priority);
        if (dueAt < ticket.CreatedAt) throw new InvalidOperationException("SLA hedefi Ticket oluşturma tarihinden önce olamaz.");
        var previous = $"{ticket.Priority} / {ticket.SlaDueAt:O}";
        ticket.Priority = priority;
        ticket.SlaDueAt = dueAt;
        ticket.SlaBreachedAt = TicketSla.IsFinished(ticket.Status) && dueAt < DateTime.UtcNow ? DateTime.UtcNow : null;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Events.Add(new TicketEvent { ActorUserId = (await access.ActorAsync()).UserId,
            Type = "SlaChanged", Details = $"{previous} -> {priority} / {dueAt:O}" });
        await db.SaveChangesAsync();
    }

    public async Task<int> CreateLinkedTaskAsync(int ticketId, int projectId, string title, string? description)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        AccessService.Require((await access.ProjectAsync(projectId)).CreateTask);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || description?.Length > 1000)
            throw new InvalidOperationException("Görev başlığı veya açıklaması geçersiz.");
        var ticket = await db.Tickets.FirstOrDefaultAsync(x => x.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket bulunamadı.");
        if (ticket.LinkedTaskId.HasValue)
            throw new InvalidOperationException("Ticket'ın bağlı bir görevi var. Silinmişse önce geri yükleyin.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var actor = await access.ActorAsync();
        var task = new TaskItem { ProjectId = projectId, Title = title.Trim(), Description = description?.Trim() };
        await tasks.CreateTaskAsync(task);
        ticket.LinkedTaskId = task.Id;
        ticket.ResolvedByLinkedTask = false;
        if (ticket.Status == TicketStatus.Open) ticket.Status = TicketStatus.InProgress;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = "TaskLinked",
            Details = task.Id.ToString() });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return task.Id;
    }

    public async Task SetViewerAsync(int ticketId, string userId, bool visible)
    {
        AccessService.Require((await RightsAsync(ticketId)).ManageViewers);
        var ticket = await db.Tickets.Include(t => t.Viewers).FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket bulunamadı.");
        if (userId == ticket.RequesterUserId || userId == ticket.AssignedToUserId)
            throw new InvalidOperationException("Temel erişim kaldırılamaz veya yinelenemez.");
        var current = ticket.Viewers.FirstOrDefault(v => v.UserId == userId);
        if (visible && current is null)
        {
            if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive)) throw new InvalidOperationException("Etkin kullanıcı bulunamadı.");
            ticket.Viewers.Add(new TicketViewer { UserId = userId });
        }
        else if (!visible && current is not null) ticket.Viewers.Remove(current);
        else return;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Events.Add(new TicketEvent { ActorUserId = (await access.ActorAsync()).UserId,
            Type = visible ? "ViewerAdded" : "ViewerRemoved", Details = userId });
        await db.SaveChangesAsync();
    }

    public async Task SetFollowingAsync(int ticketId, bool following)
    {
        var actor = await access.ActorAsync();
        var ticket = await db.Tickets.Include(t => t.Viewers).FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket bulunamadı.");
        AccessService.Require(actor.IsAdmin || await IsSupportAsync(actor.UserId, ticket.SupportDepartmentId));
        var viewer = ticket.Viewers.FirstOrDefault(v => v.UserId == actor.UserId);
        if (viewer is null) ticket.Viewers.Add(new TicketViewer { UserId = actor.UserId, IsFollowing = following });
        else if (viewer.IsFollowing == following) return;
        else viewer.IsFollowing = following;
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId,
            Type = following ? "Followed" : "Unfollowed" });
        await db.SaveChangesAsync();
    }

    public async Task AddMessageAsync(int ticketId, string body, bool isInternal)
    {
        var rights = await RightsAsync(ticketId);
        AccessService.Require(isInternal ? rights.Manage : rights.Reply);
        if (string.IsNullOrWhiteSpace(body)) throw new InvalidOperationException("Boş mesaj gönderilemez.");
        var ticket = await db.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException("Ticket bulunamadı.");
        var actor = await access.ActorAsync();
        ticket.Messages.Add(new TicketMessage
        {
            AuthorUserId = actor.UserId,
            Body = body.Trim(),
            IsInternal = isInternal,
            IsIncoming = !rights.Manage && !isInternal
        });
        if (!rights.Manage && !isInternal) ticket.HasNewReply = true;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = isInternal ? "InternalNote" : "Reply" });
        await db.SaveChangesAsync();
    }

    public async Task MarkReviewedAsync(int ticketId)
    {
        AccessService.Require((await RightsAsync(ticketId)).Manage);
        var ticket = await db.Tickets.FindAsync(ticketId) ?? throw new InvalidOperationException("Ticket bulunamadı.");
        if (!ticket.HasNewReply) return;
        ticket.HasNewReply = false;
        ticket.Events.Add(new TicketEvent { ActorUserId = (await access.ActorAsync()).UserId, Type = "Reviewed" });
        await db.SaveChangesAsync();
    }
}
