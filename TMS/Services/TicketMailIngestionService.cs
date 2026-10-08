using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public enum TicketMailIngestionResult
{
    Ignored,
    Duplicate,
    TicketCreated,
    ReplyAdded,
    StagedForReview
}

public sealed partial class TicketMailIngestionService(ApplicationDbContext db)
{
    public async Task<TicketMailIngestionResult> ProcessAsync(
        GraphMailMessage message, string mailboxAddress, string? requiredSubjectPrefix = null,
        CancellationToken cancellationToken = default)
    {
        if (message.Removed.HasValue || message.IsDraft) return TicketMailIngestionResult.Ignored;

        var rawSubject = message.Subject?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(requiredSubjectPrefix))
        {
            requiredSubjectPrefix = requiredSubjectPrefix.Trim();
            if (!rawSubject.StartsWith(requiredSubjectPrefix, StringComparison.OrdinalIgnoreCase))
                return TicketMailIngestionResult.Ignored;
            rawSubject = rawSubject[requiredSubjectPrefix.Length..].TrimStart(' ', '-', '—', ':');
        }

        var senderEmail = NormalizeEmail(message.From?.EmailAddress?.Address);
        var normalizedMailbox = NormalizeEmail(mailboxAddress);
        if (senderEmail is null || normalizedMailbox is null
            || string.Equals(senderEmail, normalizedMailbox, StringComparison.OrdinalIgnoreCase))
            return TicketMailIngestionResult.Ignored;

        var providerMessageId = Clean(message.Id, 1000);
        var externalMessageId = Clean(message.InternetMessageId, 500)
            ?? (providerMessageId is null ? null : $"graph:{normalizedMailbox}:{providerMessageId}");
        if (externalMessageId is null) return TicketMailIngestionResult.Ignored;

        if (await db.TicketMessages.AnyAsync(x => x.ExternalMessageId == externalMessageId, cancellationToken)
            || await db.TicketIntakes.AnyAsync(x => x.ExternalMessageId == externalMessageId, cancellationToken))
            return TicketMailIngestionResult.Duplicate;

        var subject = Clean(rawSubject, 200) ?? "Konusuz e-posta";
        var body = NormalizeBody(message.Body?.Content, message.Body?.ContentType, message.BodyPreview);
        var conversationId = Clean(message.ConversationId, 500);
        var receivedAt = message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow;
        var supportDepartment = await db.Departments.FirstOrDefaultAsync(
            x => x.IsTicketSupport && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Ticket destek departmanı bulunamadı.");
        var sender = await db.Users.FirstOrDefaultAsync(
            x => x.IsActive && x.NormalizedEmail == senderEmail.ToUpperInvariant(), cancellationToken);

        if (sender is not null && conversationId is not null)
        {
            var ticket = await db.Tickets.Include(x => x.Viewers)
                .FirstOrDefaultAsync(x => x.SourceMailboxAddress == normalizedMailbox
                    && x.ExternalConversationId == conversationId, cancellationToken);
            if (ticket is not null && CanAppend(ticket, sender, supportDepartment.Id))
            {
                ticket.Messages.Add(NewMessage(sender.Id, body, externalMessageId, providerMessageId,
                    conversationId, receivedAt));
                ticket.HasNewReply = true;
                ticket.UpdatedAt = receivedAt > ticket.UpdatedAt ? receivedAt : DateTime.UtcNow;
                ticket.Events.Add(new TicketEvent
                {
                    ActorUserId = sender.Id,
                    Type = "EmailReplyReceived",
                    Details = externalMessageId,
                    OccurredAt = receivedAt
                });
                await db.SaveChangesAsync(cancellationToken);
                return TicketMailIngestionResult.ReplyAdded;
            }
        }

        if (sender is null)
        {
            db.TicketIntakes.Add(new TicketIntake
            {
                SenderEmail = senderEmail,
                Subject = subject,
                Body = body,
                ExternalMessageId = externalMessageId,
                ProviderMessageId = providerMessageId,
                ExternalConversationId = conversationId,
                SourceMailboxAddress = normalizedMailbox,
                ReceivedAt = receivedAt,
                SupportDepartmentId = supportDepartment.Id
            });
            await db.SaveChangesAsync(cancellationToken);
            return TicketMailIngestionResult.StagedForReview;
        }

        var autoAssigneeId = await TicketAutoAssignment.SelectAssigneeAsync(db, supportDepartment.Id, cancellationToken);
        var newTicket = new Ticket
        {
            Subject = subject,
            RequesterUserId = sender.Id,
            RequesterDepartmentId = sender.DepartmentId,
            SupportDepartmentId = supportDepartment.Id,
            AssignedToUserId = autoAssigneeId,
            ExternalConversationId = conversationId,
            SourceMailboxAddress = normalizedMailbox,
            CreatedAt = receivedAt,
            UpdatedAt = receivedAt,
            SlaDueAt = TicketSla.DefaultDueAt(receivedAt, TicketPriority.Normal)
        };
        newTicket.Messages.Add(NewMessage(sender.Id, body, externalMessageId, providerMessageId,
            conversationId, receivedAt));
        newTicket.Events.Add(new TicketEvent
        {
            ActorUserId = sender.Id,
            Type = "EmailReceived",
            Details = externalMessageId,
            OccurredAt = receivedAt
        });
        if (autoAssigneeId is not null)
            newTicket.Events.Add(new TicketEvent { Type = "AutoAssigned", Details = autoAssigneeId, OccurredAt = receivedAt });
        db.Tickets.Add(newTicket);
        await db.SaveChangesAsync(cancellationToken);
        return TicketMailIngestionResult.TicketCreated;
    }

    private static bool CanAppend(Ticket ticket, ApplicationUser sender, int supportDepartmentId) =>
        ticket.RequesterUserId == sender.Id
        || ticket.AssignedToUserId == sender.Id
        || ticket.Viewers.Any(x => x.UserId == sender.Id)
        || sender.DepartmentId == supportDepartmentId;

    private static TicketMessage NewMessage(string authorUserId, string body, string externalMessageId,
        string? providerMessageId, string? conversationId, DateTime sentAt) => new()
    {
        AuthorUserId = authorUserId,
        Body = body,
        IsIncoming = true,
        ExternalMessageId = externalMessageId,
        ProviderMessageId = providerMessageId,
        ExternalConversationId = conversationId,
        SentAt = sentAt
    };

    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320
            || !MailAddress.TryCreate(value.Trim(), out var parsed)) return null;
        return parsed.Address.Trim().ToLowerInvariant();
    }

    private static string? Clean(string? value, int maxLength)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string NormalizeBody(string? content, string? contentType, string? preview)
    {
        var body = content;
        if (string.Equals(contentType, "html", StringComparison.OrdinalIgnoreCase) && body is not null)
            body = WebUtility.HtmlDecode(HtmlTagRegex().Replace(body, " "));
        body = WhitespaceRegex().Replace(body ?? string.Empty, " ").Trim();
        if (string.IsNullOrWhiteSpace(body)) body = preview?.Trim();
        if (string.IsNullOrWhiteSpace(body)) body = "(İleti içeriği boş)";
        return body.Length <= 100000 ? body : body[..100000];
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
