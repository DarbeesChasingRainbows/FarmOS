using FarmOS.IoT.Application;
using FarmOS.IoT.Domain.Events;
using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.Infrastructure;

/// <summary>
/// Stub alert notifier that logs excursion alerts and publishes to RabbitMQ.
/// Swap for Twilio/SMTP adapter in production via DI.
/// </summary>
public sealed class LoggingAlertNotifier(
    ILogger<LoggingAlertNotifier> logger,
    IEventBus? eventBus = null) : IAlertNotifier
{
    public async Task NotifyExcursionAlertAsync(ExcursionAlertFired alert, CancellationToken ct)
    {
        logger.LogWarning(
            "EXCURSION ALERT [{Severity}] Device={DeviceId} Zone={ZoneId} Sensor={SensorType}: {Message}. Action: {CorrectiveAction}",
            alert.Severity,
            alert.DeviceId,
            alert.ZoneId,
            alert.SensorType,
            alert.AlertMessage,
            alert.CorrectiveAction ?? "N/A");

        if (eventBus is not null)
        {
            try
            {
                var integrationEvent = new ExcursionAlertIntegrationEvent(
                    alert.ExcursionId.Value,
                    alert.DeviceId.Value,
                    alert.ZoneId.Value,
                    alert.SensorType.ToString(),
                    alert.Severity,
                    alert.AlertMessage,
                    alert.CorrectiveAction,
                    alert.FiredAt);

                await eventBus.PublishAsync(integrationEvent, $"iot.excursion.{alert.Severity.ToLowerInvariant()}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish excursion alert to RabbitMQ");
            }
        }
    }
}
