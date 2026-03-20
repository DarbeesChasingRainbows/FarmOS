using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using Microsoft.FSharp.Core;

// IoT rules interop: F# module via compiled DU → C# interop
using FsRules = FarmOS.Hearth.Rules.IoTRules;
using FsSensorType = FarmOS.Hearth.Rules.IoTRules.SensorType;
using FsAlertLevel = FarmOS.Hearth.Rules.IoTRules.AlertLevel;

namespace FarmOS.Hearth.Application.Commands.Handlers;

/// <summary>
/// Ingests a raw sensor reading, evaluates it against F# domain rules,
/// persists a SensorReadingIngested event, and notifies the SignalR hub.
/// </summary>
public sealed class IoTCommandHandlers(
    IHearthEventStore store,
    IKitchenHubNotifier notifier)
    : ICommandHandler<IngestSensorReadingCommand, IoTAlert>
{
    public async Task<Result<IoTAlert, DomainError>> Handle(
        IngestSensorReadingCommand cmd, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var reading = new SensorReading(
            cmd.DeviceId,
            cmd.SensorType,
            cmd.Value,
            cmd.Unit,
            now);

        // ── Evaluate against F# rules ──────────────────────────────────────
        var fsSensorType = MapSensorType(cmd.SensorType);
        var fsReading = new FsRules.SensorReading(
            cmd.DeviceId,
            fsSensorType,
            cmd.Value,
            cmd.Unit,
            now);

        var fsResult = FsRules.evaluate(fsReading);

        var alert = new IoTAlert(
            DeviceId: cmd.DeviceId,
            Level: MapAlertLevel(fsResult.Level),
            Message: fsResult.Message,
            CorrectiveAction: FSharpOption<string>.get_IsNone(fsResult.CorrectiveAction)
                ? null
                : fsResult.CorrectiveAction.Value,
            OccurredAt: now);

        // ── Persist event ─────────────────────────────────────────────────
        var @event = new SensorReadingIngested(cmd.DeviceId, reading, alert, now);
        await store.AppendRawIoTEventAsync(@event, ct);

        // ── Broadcast via SignalR ──────────────────────────────────────────
        await notifier.BroadcastAsync(reading, alert, ct);

        return alert;
    }

    private static FsSensorType MapSensorType(SensorType t) => t switch
    {
        SensorType.Temperature => FsSensorType.Temperature,
        SensorType.PH         => FsSensorType.PH,
        SensorType.Humidity   => FsSensorType.Humidity,
        SensorType.CO2        => FsSensorType.CO2,
        _                     => FsSensorType.Temperature,
    };

    private static AlertLevel MapAlertLevel(FsAlertLevel l) => l switch
    {
        var x when x.IsSafe     => AlertLevel.Safe,
        var x when x.IsWarning  => AlertLevel.Warning,
        var x when x.IsCritical => AlertLevel.Critical,
        _                       => AlertLevel.Safe,
    };
}
