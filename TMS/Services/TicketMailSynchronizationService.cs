using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public sealed class TicketMailSynchronizationService(
    ApplicationDbContext db,
    MicrosoftGraphMailClient graph,
    TicketMailIngestionService ingestion,
    IOptions<TicketMailOptions> options,
    ILogger<TicketMailSynchronizationService> logger)
{
    private readonly TicketMailOptions _options = options.Value;

    public async Task<int> SynchronizeOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) return 0;

        var mailbox = _options.MailboxAddress.Trim().ToLowerInvariant();
        var state = await db.TicketMailboxSyncStates.FirstOrDefaultAsync(
            x => x.MailboxAddress == mailbox, cancellationToken);
        if (state is null)
        {
            state = new TicketMailboxSyncState { MailboxAddress = mailbox };
            db.TicketMailboxSyncStates.Add(state);
        }

        state.LastAttemptAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var link = state.SyncLink ?? graph.BuildInitialDeltaUrl(
                DateTime.UtcNow.AddDays(-Math.Clamp(_options.InitialLookbackDays, 1, 90)));
            var processed = 0;
            var maxPages = Math.Clamp(_options.MaxPagesPerCycle, 1, 100);

            for (var pageNumber = 0; pageNumber < maxPages; pageNumber++)
            {
                GraphMailDeltaPage page;
                try
                {
                    page = await graph.GetDeltaPageAsync(link, cancellationToken);
                }
                catch (MicrosoftGraphMailException ex) when (ex.StatusCode == HttpStatusCode.Gone
                    && state.SyncLink is not null)
                {
                    logger.LogWarning("Mailbox delta token expired for {Mailbox}; a bounded resync will start.", mailbox);
                    state.SyncLink = null;
                    await db.SaveChangesAsync(cancellationToken);
                    return processed;
                }

                foreach (var message in page.Messages)
                {
                    if (!PassesSubjectFilter(message.Subject, _options.RequiredSubjectPrefix)) continue;
                    var completeMessage = message.Removed.HasValue ? message
                        : await graph.GetMessageAsync(message.Id ?? string.Empty, cancellationToken) ?? message;
                    var result = await ingestion.ProcessAsync(completeMessage, mailbox,
                        _options.RequiredSubjectPrefix, cancellationToken);
                    if (result is not TicketMailIngestionResult.Ignored
                        and not TicketMailIngestionResult.Duplicate) processed++;
                }

                link = page.NextLink ?? page.DeltaLink
                    ?? throw new InvalidOperationException("Microsoft Graph senkronizasyon işareti dönmedi.");
                state.SyncLink = link;
                state.LastSuccessfulSyncAt = DateTime.UtcNow;
                state.LastError = null;
                state.ConsecutiveFailures = 0;
                await db.SaveChangesAsync(cancellationToken);

                if (page.NextLink is null) return processed;
            }

            logger.LogInformation("Mailbox synchronization paused at page limit for {Mailbox}; it will continue next cycle.", mailbox);
            return processed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            state.LastError = ex.Message.Length <= 2000 ? ex.Message : ex.Message[..2000];
            state.ConsecutiveFailures++;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool PassesSubjectFilter(string? subject, string? requiredPrefix) =>
        string.IsNullOrWhiteSpace(requiredPrefix)
        || (subject?.Trim().StartsWith(requiredPrefix.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);
}
