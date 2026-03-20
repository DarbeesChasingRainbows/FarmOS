using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Aggregates;
using FarmOS.IoT.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.Application.Commands.Handlers;

/// <summary>
/// Handles telemetry reading ingestion: resolves device → zone, records reading,
/// evaluates threshold rules, fires excursion alerts, and notifies downstream.
/// </summary>
public sealed class TelemetryCommandHandler(
    IIoTEventStore eventStore,
    IIoTProjectionLookup projectionLookup,
    IThresholdRuleProvider thresholdRules,
    IAlertNotifier alertNotifier,
    ILogger<TelemetryCommandHandler> logger)
    : ICommandHandler<RecordTelemetryReadingCommand, Unit>
{
    public async Task<Result<Unit, DomainError>> Handle(
        RecordTelemetryReadingCommand request, CancellationToken ct)
    {
        try
        {
            // Resolve device by code to get its zone assignment
            var deviceInfo = await projectionLookup.GetDeviceByCodeAsync(request.DeviceCode, ct);
            if (deviceInfo is null)
            {
                return DomainError.NotFound("Device", request.DeviceCode);
            }

            // Build or retrieve telemetry stream for this device
            var stream = TelemetryStream.Initialize(
                new IoTDeviceId(deviceInfo.Id),
                deviceInfo.DeviceCode,
                deviceInfo.ZoneId.HasValue ? new ZoneId(deviceInfo.ZoneId.Value) : null,
                deviceInfo.ZoneType);

            // Determine applicable threshold rule
            ThresholdRule? rule = null;
            if (deviceInfo.ZoneType.HasValue)
            {
                rule = thresholdRules.GetRule(deviceInfo.ZoneType.Value, request.SensorType);
            }

            var now = DateTimeOffset.UtcNow;

            // Record the reading (may also raise excursion events)
            stream.RecordReading(request.SensorType, request.Value, request.Unit, now, rule);

            // Persist all uncommitted events
            await eventStore.SaveTelemetryStreamAsync(stream, "iot-ingestion", ct);

            // If any excursion alerts were fired, dispatch notifications
            foreach (var evt in stream.UncommittedEvents)
            {
                if (evt is ExcursionAlertFired alertEvt)
                {
                    try
                    {
                        await alertNotifier.NotifyExcursionAlertAsync(alertEvt, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to dispatch excursion alert for device {DeviceCode}", request.DeviceCode);
                    }
                }
            }

            return Result<Unit, DomainError>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record telemetry for device {DeviceCode}", request.DeviceCode);
            return Result<Unit, DomainError>.Failure(new DomainError("TelemetryError", ex.Message));
        }
    }
}
