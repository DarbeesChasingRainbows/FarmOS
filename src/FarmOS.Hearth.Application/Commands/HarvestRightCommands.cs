using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

/// <summary>
/// Ingests a single telemetry snapshot from a Harvest Right freeze-dryer
/// received via the MQTT cloud broker. The handler maps screen state to
/// domain phase, records a reading on the active batch, evaluates F# rules,
/// and broadcasts alerts via SignalR.
/// </summary>
public record IngestHarvestRightTelemetryCommand(
    int HarvestRightDryerId,
    string DryerSerial,
    decimal TemperatureF,
    decimal VacuumMTorr,
    decimal ProgressPercent,
    int ScreenNumber,
    string? BatchName,
    decimal? BatchElapsedSeconds,
    decimal? PhaseElapsedSeconds,
    DateTimeOffset Timestamp) : ICommand<HarvestRightTelemetryResult>;

/// <summary>Result of telemetry ingestion — tells the caller what happened.</summary>
public record HarvestRightTelemetryResult(
    FreezeDryerPhase? MappedPhase,
    IoTAlert? Alert,
    bool BatchAutoCreated,
    bool BatchAutoAdvanced,
    bool BatchAutoCompleted);
