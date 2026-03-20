using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Background worker that subscribes to IoT excursion events via RabbitMQ
/// and dispatches notifications (SMS, email, push) through the configured
/// INotificationChannel implementation.
/// </summary>
public sealed class NotificationWorker(
    IEventBus eventBus,
    INotificationChannel notificationChannel,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private const string QueueName = "notification.excursion";
    private const string BindingKey = "iot.excursion.#";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationWorker starting — subscribing to {BindingKey}", BindingKey);

        try
        {
            await eventBus.SubscribeAsync<ExcursionAlertIntegrationEvent>(
                QueueName,
                BindingKey,
                HandleAlertAsync,
                stoppingToken);

            logger.LogInformation("NotificationWorker subscribed to {Queue}", QueueName);

            // Keep alive until shutdown
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("NotificationWorker shutting down");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NotificationWorker failed to subscribe");
        }
    }

    private async Task HandleAlertAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct)
    {
        logger.LogInformation(
            "Dispatching notification for excursion {ExcursionId} [{Severity}]",
            alert.ExcursionId, alert.Severity);

        try
        {
            await notificationChannel.SendAlertAsync(alert, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification dispatch failed for excursion {ExcursionId}", alert.ExcursionId);
        }
    }
}
