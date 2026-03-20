using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Dev-mode notification channel that logs alerts to console.
/// Used when Twilio credentials are not configured.
/// </summary>
public sealed class ConsoleNotifier(ILogger<ConsoleNotifier> logger) : INotificationChannel
{
    public Task SendAlertAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct)
    {
        logger.LogWarning(
            "📱 NOTIFICATION [{Severity}] Zone={ZoneId} Sensor={SensorType}: {Message}",
            alert.Severity,
            alert.ZoneId,
            alert.SensorType,
            alert.AlertMessage);

        if (alert.CorrectiveAction is not null)
        {
            logger.LogWarning("   ⚡ Corrective Action: {Action}", alert.CorrectiveAction);
        }

        return Task.CompletedTask;
    }
}
