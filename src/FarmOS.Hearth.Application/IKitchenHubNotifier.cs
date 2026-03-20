using FarmOS.Hearth.Domain;

namespace FarmOS.Hearth.Application;

public interface IKitchenHubNotifier
{
    Task BroadcastAsync(SensorReading reading, IoTAlert alert, CancellationToken ct);
    Task BroadcastFreezeDryerTelemetryAsync(FreezeDryerTelemetrySnapshot snapshot, CancellationToken ct);
}

public record FreezeDryerTelemetrySnapshot(
    string DryerSerial,
    string? BatchId,
    string Phase,
    decimal TemperatureF,
    decimal VacuumMTorr,
    decimal ProgressPercent,
    int ScreenNumber,
    string? AlertLevel,
    string? AlertMessage,
    DateTimeOffset Timestamp);
