using FarmOS.Hearth.Application.Commands;
using FarmOS.SharedKernel.EventStore;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.Hearth.API.Workers;

/// <summary>
/// Background worker that subscribes to IoT excursion events via RabbitMQ
/// and auto-creates CAPA (Corrective and Preventive Action) records in the
/// Compliance context when critical excursion alerts fire.
/// </summary>
public sealed class ComplianceEventSubscriber(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<ComplianceEventSubscriber> logger) : BackgroundService
{
    private const string QueueName = "hearth.compliance.excursion";
    private const string BindingKey = "iot.excursion.#";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ComplianceEventSubscriber starting — subscribing to {BindingKey}", BindingKey);

        try
        {
            await eventBus.SubscribeAsync<ExcursionAlertIntegrationEvent>(
                QueueName,
                BindingKey,
                HandleExcursionAlertAsync,
                stoppingToken);

            logger.LogInformation("ComplianceEventSubscriber subscribed to {Queue}", QueueName);

            // Keep the service alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ComplianceEventSubscriber shutting down");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ComplianceEventSubscriber failed to subscribe");
        }
    }

    private async Task HandleExcursionAlertAsync(ExcursionAlertIntegrationEvent alert, CancellationToken ct)
    {
        logger.LogWarning(
            "Received excursion alert: [{Severity}] Device={DeviceId} Zone={ZoneId} Sensor={SensorType}",
            alert.Severity, alert.DeviceId, alert.ZoneId, alert.SensorType);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var description = $"Auto-CAPA: {alert.SensorType} excursion alert [{alert.Severity}] — {alert.AlertMessage}";
            var deviationSource = $"IoT Device {alert.DeviceId} / Zone {alert.ZoneId}";

            var command = new OpenCAPACommand(
                Description: description,
                DeviationSource: deviationSource,
                RelatedCTE: null);

            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Auto-created CAPA {CAPAId} for excursion {ExcursionId}",
                    result.Value, alert.ExcursionId);
            }
            else
            {
                logger.LogError(
                    "Failed to create CAPA for excursion {ExcursionId}: {Error}",
                    alert.ExcursionId, result.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle excursion alert for device {DeviceId}", alert.DeviceId);
        }
    }
}
