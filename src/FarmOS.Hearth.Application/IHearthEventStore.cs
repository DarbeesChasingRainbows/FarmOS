using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Application;

public interface IHearthEventStore
{
    Task<SourdoughBatch> LoadSourdoughAsync(string batchId, CancellationToken ct);
    Task SaveSourdoughAsync(SourdoughBatch batch, string userId, CancellationToken ct);

    Task<KombuchaBatch> LoadKombuchaAsync(string batchId, CancellationToken ct);
    Task SaveKombuchaAsync(KombuchaBatch batch, string userId, CancellationToken ct);

    Task<LivingCulture> LoadCultureAsync(string cultureId, CancellationToken ct);
    Task SaveCultureAsync(LivingCulture culture, string userId, CancellationToken ct);

    /// <summary>Append a raw IoT event (no aggregate load/save cycle needed).</summary>
    Task AppendRawIoTEventAsync(SensorReadingIngested @event, CancellationToken ct);

    Task SaveSanitationAsync(SanitationRecord record, string userId, CancellationToken ct);

    Task SaveTraceabilityAsync(TraceabilityRecord record, string userId, CancellationToken ct);

    /// <summary>Permanently purge traceability records older than the given cutoff date.</summary>
    Task<int> PurgeExpiredTraceabilityAsync(DateTimeOffset cutoff, CancellationToken ct);

    // ─── Freeze-Dryer ─────────────────────────────────────────────────
    Task<FreezeDryerBatch> LoadFreezeDryerAsync(string batchId, CancellationToken ct);
    Task SaveFreezeDryerAsync(FreezeDryerBatch batch, string userId, CancellationToken ct);

    // ─── HACCP Plan ───────────────────────────────────────────────────
    Task<HACCPPlan> LoadHACCPPlanAsync(string planId, CancellationToken ct);
    Task SaveHACCPPlanAsync(HACCPPlan plan, string userId, CancellationToken ct);

    // ─── Equipment Monitoring (immutable append-only) ─────────────────
    Task AppendEquipmentTempEventAsync(EquipmentTemperatureLogged @event, CancellationToken ct);
    Task AppendMonitoringCorrectionAsync(MonitoringCorrectionAppended @event, CancellationToken ct);

    // ─── CAPA ─────────────────────────────────────────────────────────
    Task AppendCAPAEventAsync(IDomainEvent @event, CancellationToken ct);

    // ─── Harvest Right Telemetry ─────────────────────────────────────
    /// <summary>Append a raw Harvest Right telemetry event (no aggregate cycle).</summary>
    Task AppendHarvestRightTelemetryAsync(HarvestRightTelemetryIngested @event, CancellationToken ct);

    /// <summary>
    /// Load the currently active (non-completed, non-aborted) freeze-dryer batch
    /// for a given dryer. Returns null if no active batch exists.
    /// </summary>
    Task<FreezeDryerBatch?> LoadActiveFreezeDryerByDryerIdAsync(string dryerId, CancellationToken ct);

    /// <summary>List all freeze-dryer batches (most recent first).</summary>
    Task<IReadOnlyList<FreezeDryerBatchSummary>> ListFreezeDryerBatchesAsync(CancellationToken ct);
}

/// <summary>Read-only projection of a freeze-dryer batch for list views.</summary>
public record FreezeDryerBatchSummary(
    string Id,
    string BatchCode,
    string ProductDescription,
    int Phase,
    decimal PreDryWeight,
    decimal? PostDryWeight);
