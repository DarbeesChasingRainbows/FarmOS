using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.Flora.API.Workers;

/// <summary>
/// Background worker that subscribes to Flora domain events via the event bus
/// and publishes corresponding integration events for other bounded contexts.
///
/// Listens for IoT cooler excursion events that affect Flora batches,
/// enabling automatic alerts when cooler temperature drifts outside the
/// 32-35°F optimal range for cut flowers (ASCFG recommendation).
/// </summary>
public sealed class FloraEventPublisher(
    IEventBus eventBus,
    ILogger<FloraEventPublisher> logger) : BackgroundService
{
    private const string QueueName = "flora.cooler.excursion";
    private const string BindingKey = "iot.excursion.#";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FloraEventPublisher starting — subscribing to {BindingKey}", BindingKey);

        try
        {
            await eventBus.SubscribeAsync<ExcursionAlertIntegrationEvent>(
                QueueName,
                BindingKey,
                HandleCoolerExcursionAsync,
                stoppingToken);

            logger.LogInformation("FloraEventPublisher subscribed to {Queue}", QueueName);

            // Keep the service alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("FloraEventPublisher shutting down");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FloraEventPublisher failed to subscribe");
        }
    }

    private async Task HandleCoolerExcursionAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct)
    {
        // Only process alerts from cooler-related sensors
        if (!alert.SensorType.Contains("temperature", StringComparison.OrdinalIgnoreCase) &&
            !alert.SensorType.Contains("cooler", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "Cooler excursion alert for Flora: [{Severity}] Device={DeviceId} Zone={ZoneId} — {Message}",
            alert.Severity, alert.DeviceId, alert.ZoneId, alert.AlertMessage);

        // Future: auto-query batches in cooler and flag at-risk inventory
        // This is a reaction point — Commerce context can also subscribe
        // to create quality hold records.
    }
}
