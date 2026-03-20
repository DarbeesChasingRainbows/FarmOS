using FarmOS.Hearth.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.Hearth.API.Workers;

public class TraceabilityRecordPurgeWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TraceabilityRecordPurgeWorker> logger) : BackgroundService
{
    /// <summary>
    /// FSMA 204: Standard retention is 2 years (730 days).
    /// Direct-to-consumer records have a 180-day retention.
    /// This worker uses the standard 730-day cutoff; the event store
    /// implementation should handle per-record retention via IsExpired.
    /// </summary>
    private static readonly TimeSpan StandardRetention = TimeSpan.FromDays(730);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTimeOffset.UtcNow - StandardRetention;
                logger.LogInformation(
                    "TraceabilityRecordPurgeWorker: purging records older than {Cutoff}", cutoff);

                using var scope = scopeFactory.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<IHearthEventStore>();
                var purged = await eventStore.PurgeExpiredTraceabilityAsync(cutoff, stoppingToken);

                logger.LogInformation(
                    "TraceabilityRecordPurgeWorker: purged {Count} expired records", purged);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TraceabilityRecordPurgeWorker failed during purge cycle");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
