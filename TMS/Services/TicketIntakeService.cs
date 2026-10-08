using System.Data;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class TicketIntakeService(ApplicationDbContext db, AccessService access)
{
    private async Task<bool> CanManageAsync(int departmentId)
    {
        var actor = await access.ActorAsync();
        return actor.IsAdmin || actor.HomeDepartmentId == departmentId
            && await db.Departments.AnyAsync(x => x.Id == departmentId && x.IsTicketSupport && !x.IsDeleted);
    }

    public async Task<IQueryable<TicketIntake>> PendingAsync()
    {
        var actor = await access.ActorAsync();
        var query = db.TicketIntakes.Where(x => x.MatchedTicketId == null);
        return actor.IsAdmin ? query : query.Where(x => x.SupportDepartmentId == actor.HomeDepartmentId
            && x.SupportDepartment.IsTicketSupport);
    }

    public async Task<TicketIntake> StageAsync(string senderEmail, string subject, string body,
        int supportDepartmentId, DateTime? receivedAt = null, string? externalMessageId = null)
    {
        AccessService.Require(await CanManageAsync(supportDepartmentId));
        senderEmail = senderEmail.Trim().ToLowerInvariant();
        subject = subject.Trim();
        body = body.Trim();
        if (senderEmail.Length > 320 || subject.Length is < 1 or > 200 || body.Length is < 1 or > 100000
            || externalMessageId?.Length > 500 || !MailAddress.TryCreate(senderEmail, out var parsed)
            || !string.Equals(parsed.Address, senderEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gönderen, konu veya mesaj geçersiz.");
        if (!await db.Departments.AnyAsync(x => x.Id == supportDepartmentId && x.IsTicketSupport && !x.IsDeleted))
            throw new InvalidOperationException("Ticket destek departmanı bulunamadı.");
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == senderEmail.ToUpperInvariant()))
            throw new InvalidOperationException("Bu gönderenin kullanıcı hesabı var; doğrudan Ticket açılabilir.");
        if (externalMessageId is not null && await db.TicketIntakes.AnyAsync(x => x.ExternalMessageId == externalMessageId))
            throw new InvalidOperationException("Bu ileti daha önce kaydedildi.");
        var intake = new TicketIntake { SenderEmail = senderEmail, Subject = subject, Body = body,
            SupportDepartmentId = supportDepartmentId, ReceivedAt = receivedAt ?? DateTime.UtcNow,
            ExternalMessageId = externalMessageId };
        db.TicketIntakes.Add(intake);
        await db.SaveChangesAsync();
        return intake;
    }

    public async Task<TicketIntake?> GetAsync(int id)
    {
        var intake = await db.TicketIntakes.AsNoTracking().Include(x => x.SupportDepartment)
            .Include(x => x.MatchedTicket).FirstOrDefaultAsync(x => x.Id == id);
        return intake is not null && await CanManageAsync(intake.SupportDepartmentId) ? intake : null;
    }

    public async Task<int> MatchAsync(int intakeId, string requesterUserId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var intake = await db.TicketIntakes.FirstOrDefaultAsync(x => x.Id == intakeId)
            ?? throw new InvalidOperationException("İnceleme kaydı bulunamadı.");
        AccessService.Require(await CanManageAsync(intake.SupportDepartmentId));
        if (intake.MatchedTicketId.HasValue) return intake.MatchedTicketId.Value;
        var requester = await db.Users.FirstOrDefaultAsync(x => x.Id == requesterUserId && x.IsActive)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");
        if (!string.Equals(requester.Email?.Trim(), intake.SenderEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Seçilen kullanıcının e-posta adresi gönderenle aynı olmalıdır.");
        var actor = await access.ActorAsync();
        var autoAssigneeId = await TicketAutoAssignment.SelectAssigneeAsync(db, intake.SupportDepartmentId);
        var ticket = new Ticket { Subject = intake.Subject, RequesterUserId = requesterUserId,
            RequesterDepartmentId = requester.DepartmentId, SupportDepartmentId = intake.SupportDepartmentId,
            AssignedToUserId = autoAssigneeId,
            ExternalConversationId = intake.ExternalConversationId,
            SourceMailboxAddress = intake.SourceMailboxAddress,
            CreatedAt = intake.ReceivedAt, UpdatedAt = intake.ReceivedAt,
            SlaDueAt = TicketSla.DefaultDueAt(intake.ReceivedAt, TicketPriority.Normal) };
        ticket.Messages.Add(new TicketMessage { AuthorUserId = requesterUserId, Body = intake.Body,
            IsIncoming = true, SentAt = intake.ReceivedAt, ExternalMessageId = intake.ExternalMessageId,
            ProviderMessageId = intake.ProviderMessageId, ExternalConversationId = intake.ExternalConversationId });
        ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = "IntakeMatched",
            Details = $"Intake #{intake.Id}" });
        if (autoAssigneeId is not null)
            ticket.Events.Add(new TicketEvent { ActorUserId = actor.UserId, Type = "AutoAssigned", Details = autoAssigneeId });
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        intake.MatchedTicketId = ticket.Id;
        intake.MatchedAt = DateTime.UtcNow;
        intake.MatchedByUserId = actor.UserId;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return ticket.Id;
    }
}
