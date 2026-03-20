using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using Microsoft.Extensions.Logging;

// F# interop for freeze-dryer rules
using FsRules = FarmOS.Hearth.Rules.FreezeDryerRules;

namespace FarmOS.Hearth.Application.Commands.Handlers;

/// <summary>
/// Handles telemetry ingested from the Harvest Right MQTT cloud broker.
/// Maps screen numbers to domain phases, auto-manages batch lifecycle,
/// evaluates F# safety rules, persists events, and broadcasts alerts.
/// </summary>
public sealed class HarvestRightCommandHandlers(
    IHearthEventStore store,
    IKitchenHubNotifier notifier,
    ILogger<HarvestRightCommandHandlers> logger)
    : ICommandHandler<IngestHarvestRightTelemetryCommand, HarvestRightTelemetryResult>
{
    public async Task<Result<HarvestRightTelemetryResult, DomainError>> Handle(
        IngestHarvestRightTelemetryCommand cmd, CancellationToken ct)
    {
        var now = cmd.Timestamp;
        var dryerId = new FreezeDryerId(DeterministicGuid(cmd.DryerSerial));
        var batchAutoCreated = false;
        var batchAutoAdvanced = false;
        var batchAutoCompleted = false;

        // ── Map screen → domain phase ────────────────────────────────────
        var fsPhase = FsRules.MapScreenToPhase(cmd.ScreenNumber);
        var isRunning = FsRules.IsRunningScreen(cmd.ScreenNumber);
        var isError = FsRules.IsErrorScreen(cmd.ScreenNumber);
        var isComplete = FsRules.IsCompleteScreen(cmd.ScreenNumber);

        FreezeDryerPhase? mappedPhase = null;
        if (Microsoft.FSharp.Core.FSharpOption<FsRules.FreezeDryerPhase>.get_IsSome(fsPhase))
            mappedPhase = MapFsPhase(fsPhase.Value);

        // ── Find or auto-create active batch ─────────────────────────────
        var batch = await store.LoadActiveFreezeDryerByDryerIdAsync(dryerId.ToString(), ct);
        IoTAlert? alert = null;

        if (batch is null && isRunning)
        {
            // Auto-create a batch when the dryer starts a cycle
            var batchCode = $"HR-{cmd.DryerSerial}-{now:yyyyMMdd-HHmm}";
            var productDesc = cmd.BatchName ?? "Harvest Right Auto-Batch";
            batch = FreezeDryerBatch.Start(batchCode, dryerId, productDesc, 0m);
            await store.SaveFreezeDryerAsync(batch, "harvest-right-mqtt", ct);
            batchAutoCreated = true;
            logger.LogInformation(
                "Auto-created freeze-dryer batch {BatchCode} for dryer {Serial} (screen {Screen})",
                batchCode, cmd.DryerSerial, cmd.ScreenNumber);
        }

        if (batch is not null)
        {
            // ── Auto-advance phase if telemetry shows a new phase ────────
            if (mappedPhase.HasValue && mappedPhase.Value != batch.Phase
                && ShouldAutoAdvancePhase(batch.Phase, mappedPhase.Value))
            {
                var advanceResult = batch.AdvancePhase(mappedPhase.Value);
                if (advanceResult.IsSuccess)
                {
                    batchAutoAdvanced = true;
                    logger.LogInformation(
                        "Auto-advanced batch {BatchId} from {Old} → {New} based on telemetry screen {Screen}",
                        batch.Id, batch.Phase, mappedPhase.Value, cmd.ScreenNumber);
                }
            }

            // ── Auto-complete if dryer reports completion ────────────────
            if (isComplete && batch.Phase is not (FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted))
            {
                var completeResult = batch.Complete(0m); // Weight unknown from telemetry
                if (completeResult.IsSuccess)
                {
                    batchAutoCompleted = true;
                    logger.LogInformation("Auto-completed batch {BatchId} — dryer screen {Screen}", batch.Id, cmd.ScreenNumber);
                }
            }

            // ── Auto-abort on error screens ─────────────────────────────
            if (isError && batch.Phase is not (FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted))
            {
                var abortResult = batch.Abort($"Harvest Right error screen {cmd.ScreenNumber}");
                if (abortResult.IsSuccess)
                    logger.LogWarning("Auto-aborted batch {BatchId} — dryer error screen {Screen}", batch.Id, cmd.ScreenNumber);
            }

            // ── Record reading (only in active drying phases) ────────────
            if (batch.Phase is not (FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted or FreezeDryerPhase.Loading))
            {
                var reading = new FreezeDryerReading(
                    Timestamp: now,
                    ShelfTempF: cmd.TemperatureF,
                    VacuumMTorr: cmd.VacuumMTorr,
                    ProductTempF: null,
                    Notes: $"MQTT telemetry (screen {cmd.ScreenNumber}, progress {cmd.ProgressPercent:F1}%)");

                batch.RecordReading(reading); // Ignore result — telemetry readings are best-effort
            }

            // ── Evaluate F# rules ────────────────────────────────────────
            if (mappedPhase.HasValue)
            {
                var elapsedHrs = cmd.PhaseElapsedSeconds.HasValue
                    ? (double)(cmd.PhaseElapsedSeconds.Value / 3600m)
                    : 0.0;

                var fsAlert = FsRules.EvaluateTelemetry(
                    MapToFsPhase(mappedPhase.Value),
                    cmd.TemperatureF,
                    cmd.VacuumMTorr,
                    elapsedHrs);

                var level = MapAlertLevel(fsAlert.Level);
                alert = new IoTAlert(
                    DeviceId: cmd.DryerSerial,
                    Level: level,
                    Message: fsAlert.Message,
                    CorrectiveAction: Microsoft.FSharp.Core.FSharpOption<string>.get_IsNone(fsAlert.CorrectiveAction)
                        ? null
                        : fsAlert.CorrectiveAction.Value,
                    OccurredAt: now);
            }

            await store.SaveFreezeDryerAsync(batch, "harvest-right-mqtt", ct);
        }

        // ── Persist raw telemetry event ──────────────────────────────────
        var telemetryEvent = new HarvestRightTelemetryIngested(
            dryerId, cmd.ScreenNumber, cmd.TemperatureF,
            cmd.VacuumMTorr, cmd.ProgressPercent, alert, now);
        await store.AppendHarvestRightTelemetryAsync(telemetryEvent, ct);

        // ── Broadcast via SignalR if alert is Warning or higher ──────────
        if (alert is not null && alert.Level >= AlertLevel.Warning)
        {
            var sensorReading = new SensorReading(
                cmd.DryerSerial, SensorType.Temperature, cmd.TemperatureF, "°F", now);
            await notifier.BroadcastAsync(sensorReading, alert, ct);
        }

        // ── Broadcast all telemetry for live frontend display ──────────
        await notifier.BroadcastFreezeDryerTelemetryAsync(new FreezeDryerTelemetrySnapshot(
            cmd.DryerSerial, batch?.Id.ToString(), mappedPhase?.ToString() ?? "Unknown",
            cmd.TemperatureF, cmd.VacuumMTorr, cmd.ProgressPercent,
            cmd.ScreenNumber, alert?.Level.ToString(), alert?.Message, now), ct);

        return new HarvestRightTelemetryResult(
            MappedPhase: mappedPhase,
            Alert: alert,
            BatchAutoCreated: batchAutoCreated,
            BatchAutoAdvanced: batchAutoAdvanced,
            BatchAutoCompleted: batchAutoCompleted);
    }

    // ─── Auto-Advance Policy ──────────────────────────────────────────
    // TODO: This is a meaningful design choice for the user to customize.
    // Current policy: only advance forward, never backward. Complete/Aborted
    // transitions are handled separately above via isComplete/isError screens.
    private static bool ShouldAutoAdvancePhase(
        FreezeDryerPhase currentDomainPhase,
        FreezeDryerPhase telemetryPhase)
    {
        // Only advance forward in the natural phase sequence
        return (currentDomainPhase, telemetryPhase) switch
        {
            (FreezeDryerPhase.Loading, FreezeDryerPhase.Freezing) => true,
            (FreezeDryerPhase.Freezing, FreezeDryerPhase.PrimaryDrying) => true,
            (FreezeDryerPhase.PrimaryDrying, FreezeDryerPhase.SecondaryDrying) => true,
            _ => false, // Never go backward; never skip phases
        };
    }

    // ─── Mapping Helpers ──────────────────────────────────────────────

    /// <summary>Generate a deterministic GUID from a dryer serial for stable FreezeDryerId mapping.</summary>
    private static Guid DeterministicGuid(string serial)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"harvest-right:{serial}"));
        return new Guid(bytes);
    }

    private static FreezeDryerPhase MapFsPhase(FsRules.FreezeDryerPhase fs) => fs switch
    {
        var x when x.IsLoading => FreezeDryerPhase.Loading,
        var x when x.IsFreezing => FreezeDryerPhase.Freezing,
        var x when x.IsPrimaryDrying => FreezeDryerPhase.PrimaryDrying,
        var x when x.IsSecondaryDrying => FreezeDryerPhase.SecondaryDrying,
        _ => FreezeDryerPhase.Loading,
    };

    private static FsRules.FreezeDryerPhase MapToFsPhase(FreezeDryerPhase phase) => phase switch
    {
        FreezeDryerPhase.Loading => FsRules.FreezeDryerPhase.Loading,
        FreezeDryerPhase.Freezing => FsRules.FreezeDryerPhase.Freezing,
        FreezeDryerPhase.PrimaryDrying => FsRules.FreezeDryerPhase.PrimaryDrying,
        FreezeDryerPhase.SecondaryDrying => FsRules.FreezeDryerPhase.SecondaryDrying,
        _ => FsRules.FreezeDryerPhase.Loading,
    };

    private static AlertLevel MapAlertLevel(FarmOS.Hearth.Rules.IoTRules.AlertLevel level) => level switch
    {
        var x when x.IsSafe => AlertLevel.Safe,
        var x when x.IsWarning => AlertLevel.Warning,
        var x when x.IsCritical => AlertLevel.Critical,
        _ => AlertLevel.Safe,
    };
}
