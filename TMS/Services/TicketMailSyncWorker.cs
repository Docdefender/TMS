using Microsoft.Extensions.Options;

namespace TMS.Services;

public sealed class TicketMailSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TicketMailOptions> options,
    ILogger<TicketMailSyncWorker> logger) : BackgroundService
{
    private readonly TicketMailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Microsoft 365 ticket mailbox synchronization is disabled.");
            return;
        }
        if (!_options.IsConfigured)
        {
            logger.LogError("Microsoft 365 ticket mailbox synchronization is enabled but its settings are incomplete.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Clamp(_options.PollIntervalSeconds, 30, 3600));
        await RunOnceAsync(stoppingToken);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(stoppingToken);
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var synchronization = scope.ServiceProvider.GetRequiredService<TicketMailSynchronizationService>();
            var processed = await synchronization.SynchronizeOnceAsync(cancellationToken);
            if (processed > 0)
                logger.LogInformation("Processed {Count} new ticket mailbox messages.", processed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ticket mailbox synchronization failed; the next scheduled cycle will retry.");
        }
    }
}
