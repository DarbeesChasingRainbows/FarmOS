using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.Hearth.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Hearth.Infrastructure;

public sealed class HearthEventStore(IEventStore store, IArangoDBClient arango) : IHearthEventStore
{
    private const string CollectionName = "hearth_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(SourdoughBatchStarted)] = typeof(SourdoughBatchStarted),
        [nameof(CCPReadingRecorded)] = typeof(CCPReadingRecorded),
        [nameof(SourdoughPhaseAdvanced)] = typeof(SourdoughPhaseAdvanced),
        [nameof(SourdoughBatchCompleted)] = typeof(SourdoughBatchCompleted),
        [nameof(SourdoughBatchDiscarded)] = typeof(SourdoughBatchDiscarded),
        
        [nameof(KombuchaBatchStarted)] = typeof(KombuchaBatchStarted),
        [nameof(KombuchaPHRecorded)] = typeof(KombuchaPHRecorded),
        [nameof(KombuchaFlavoringAdded)] = typeof(KombuchaFlavoringAdded),
        [nameof(KombuchaPhaseAdvanced)] = typeof(KombuchaPhaseAdvanced),
        [nameof(KombuchaBatchCompleted)] = typeof(KombuchaBatchCompleted),
        [nameof(KombuchaBatchDiscarded)] = typeof(KombuchaBatchDiscarded),
        
        [nameof(CultureCreated)] = typeof(CultureCreated),
        [nameof(CultureFed)] = typeof(CultureFed),
        [nameof(CultureSplit)] = typeof(CultureSplit),

        [nameof(SensorReadingIngested)] = typeof(SensorReadingIngested),
        [nameof(SanitationRecordCreated)] = typeof(SanitationRecordCreated),
        
        [nameof(TraceabilityEventLogged)] = typeof(TraceabilityEventLogged),

        // HACCP Plan
        [nameof(HACCPPlanCreated)] = typeof(HACCPPlanCreated),
        [nameof(CCPDefinitionAdded)] = typeof(CCPDefinitionAdded),
        [nameof(CCPDefinitionRemoved)] = typeof(CCPDefinitionRemoved),

        // CAPA
        [nameof(CAPAOpened)] = typeof(CAPAOpened),
        [nameof(CAPAClosed)] = typeof(CAPAClosed),

        // Equipment Monitoring
        [nameof(EquipmentTemperatureLogged)] = typeof(EquipmentTemperatureLogged),
        [nameof(MonitoringCorrectionAppended)] = typeof(MonitoringCorrectionAppended),

        // Freeze-Dryer
        [nameof(FreezeDryerBatchStarted)] = typeof(FreezeDryerBatchStarted),
        [nameof(FreezeDryerPhaseAdvanced)] = typeof(FreezeDryerPhaseAdvanced),
        [nameof(FreezeDryerReadingRecorded)] = typeof(FreezeDryerReadingRecorded),
        [nameof(FreezeDryerBatchCompleted)] = typeof(FreezeDryerBatchCompleted),
        [nameof(FreezeDryerBatchAborted)] = typeof(FreezeDryerBatchAborted),

        // Harvest Right Telemetry
        [nameof(HarvestRightTelemetryIngested)] = typeof(HarvestRightTelemetryIngested)
    };

    public Task<SourdoughBatch> LoadSourdoughAsync(string id, CancellationToken ct) =>
        store.LoadAsync<SourdoughBatch, BatchId>(CollectionName, id, () => new SourdoughBatch(), DeserializeEvent, ct);

    public Task SaveSourdoughAsync(SourdoughBatch batch, string userId, CancellationToken ct) =>
        SaveAsync(batch, batch.Id.ToString(), "SourdoughBatch", userId, ct);

    public Task<KombuchaBatch> LoadKombuchaAsync(string id, CancellationToken ct) =>
        store.LoadAsync<KombuchaBatch, BatchId>(CollectionName, id, () => new KombuchaBatch(), DeserializeEvent, ct);

    public Task SaveKombuchaAsync(KombuchaBatch batch, string userId, CancellationToken ct) =>
        SaveAsync(batch, batch.Id.ToString(), "KombuchaBatch", userId, ct);

    public Task<LivingCulture> LoadCultureAsync(string id, CancellationToken ct) =>
        store.LoadAsync<LivingCulture, LivingCultureId>(CollectionName, id, () => new LivingCulture(), DeserializeEvent, ct);

    public Task SaveCultureAsync(LivingCulture culture, string userId, CancellationToken ct) =>
        SaveAsync(culture, culture.Id.ToString(), "LivingCulture", userId, ct);

    public Task AppendRawIoTEventAsync(SensorReadingIngested @event, CancellationToken ct) =>
        store.AppendAsync(
            CollectionName,
            @event.DeviceId,
            "IoTDevice",
            -1,         // -1 = no optimistic concurrency check for sensor streams
            [@event],
            "iot-gateway",
            Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(),
            SerializeEvent,
            ct);

    public Task SaveSanitationAsync(SanitationRecord record, string userId, CancellationToken ct) =>
        SaveAsync(record, record.Id.ToString(), "SanitationRecord", userId, ct);

    public Task SaveTraceabilityAsync(TraceabilityRecord record, string userId, CancellationToken ct) =>
        SaveAsync(record, record.Id.ToString(), "TraceabilityRecord", userId, ct);

    // ─── Freeze-Dryer ─────────────────────────────────────────────────

    public Task<FreezeDryerBatch> LoadFreezeDryerAsync(string id, CancellationToken ct) =>
        store.LoadAsync<FreezeDryerBatch, BatchId>(CollectionName, id, () => new FreezeDryerBatch(), DeserializeEvent, ct);

    public Task SaveFreezeDryerAsync(FreezeDryerBatch batch, string userId, CancellationToken ct) =>
        SaveAsync(batch, batch.Id.ToString(), "FreezeDryerBatch", userId, ct);

    // ─── HACCP Plan ───────────────────────────────────────────────────

    public Task<HACCPPlan> LoadHACCPPlanAsync(string id, CancellationToken ct) =>
        store.LoadAsync<HACCPPlan, HACCPPlanId>(CollectionName, id, () => new HACCPPlan(), DeserializeEvent, ct);

    public Task SaveHACCPPlanAsync(HACCPPlan plan, string userId, CancellationToken ct) =>
        SaveAsync(plan, plan.Id.ToString(), "HACCPPlan", userId, ct);

    // ─── Equipment Monitoring (immutable append-only) ─────────────────

    public Task AppendEquipmentTempEventAsync(EquipmentTemperatureLogged @event, CancellationToken ct) =>
        store.AppendAsync(
            CollectionName, @event.EquipmentId.ToString(), "EquipmentMonitoring", -1,
            [@event], @event.LoggedBy, Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(), SerializeEvent, ct);

    public Task AppendMonitoringCorrectionAsync(MonitoringCorrectionAppended @event, CancellationToken ct) =>
        store.AppendAsync(
            CollectionName, @event.OriginalLogId.ToString(), "MonitoringCorrection", -1,
            [@event], @event.CorrectedBy, Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(), SerializeEvent, ct);

    // ─── CAPA ─────────────────────────────────────────────────────────

    public Task AppendCAPAEventAsync(IDomainEvent @event, CancellationToken ct)
    {
        var aggregateId = @event switch
        {
            CAPAOpened e => e.Id.ToString(),
            CAPAClosed e => e.Id.ToString(),
            _ => Guid.NewGuid().ToString()
        };
        return store.AppendAsync(
            CollectionName, aggregateId, "CAPA", -1,
            [@event], "steward", Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(), SerializeEvent, ct);
    }

    // ─── Harvest Right Telemetry ─────────────────────────────────────

    public Task AppendHarvestRightTelemetryAsync(HarvestRightTelemetryIngested @event, CancellationToken ct) =>
        store.AppendAsync(
            CollectionName,
            @event.DryerId.ToString(),
            "HarvestRightTelemetry",
            -1,
            [@event],
            "harvest-right-mqtt",
            Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(),
            SerializeEvent,
            ct);

    public async Task<FreezeDryerBatch?> LoadActiveFreezeDryerByDryerIdAsync(string dryerId, CancellationToken ct)
    {
        // Query for the latest FreezeDryerBatchStarted event matching this dryerId,
        // then load and check if the batch is still active (not completed/aborted).
        try
        {
            var batch = await store.LoadAsync<FreezeDryerBatch, BatchId>(
                CollectionName,
                $"dryer:{dryerId}:active",
                () => new FreezeDryerBatch(),
                DeserializeEvent,
                ct);

            // Check if the batch is in a terminal phase
            if (batch.Phase is FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted)
                return null;

            return batch;
        }
        catch
        {
            return null; // No active batch found
        }
    }

    public async Task<IReadOnlyList<FreezeDryerBatchSummary>> ListFreezeDryerBatchesAsync(CancellationToken ct)
    {
        // Find distinct FreezeDryerBatch aggregate IDs, then replay each to get current state.
        var aql = @"
            FOR e IN hearth_events
                FILTER e.AggregateType == 'FreezeDryerBatch'
                COLLECT aggregateId = e.AggregateId
                SORT aggregateId DESC
                LIMIT 50
                RETURN aggregateId
        ";

        var cursor = await arango.Cursor.PostCursorAsync<string>(
            new PostCursorBody { Query = aql });

        var summaries = new List<FreezeDryerBatchSummary>();
        foreach (var aggregateId in cursor.Result)
        {
            try
            {
                var batch = await store.LoadAsync<FreezeDryerBatch, BatchId>(
                    CollectionName, aggregateId, () => new FreezeDryerBatch(), DeserializeEvent, ct);
                summaries.Add(new FreezeDryerBatchSummary(
                    batch.Id.ToString(),
                    batch.BatchCode,
                    batch.ProductDescription,
                    (int)batch.Phase,
                    batch.PreDryWeight,
                    batch.PostDryWeight));
            }
            catch
            {
                // Skip batches that fail to rehydrate
            }
        }

        return summaries;
    }

    public Task<int> PurgeExpiredTraceabilityAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        // TODO: Implement AQL query to delete TraceabilityRecord streams older than cutoff.
        // Example: FOR doc IN hearth_events FILTER doc.aggregateType == "TraceabilityRecord"
        //          AND doc.timestamp < @cutoff REMOVE doc IN hearth_events
        return Task.FromResult(0);
    }

    private async Task SaveAsync<TId>(AggregateRoot<TId> aggregate, string aggregateId, string aggregateType, string userId, CancellationToken ct) where TId : notnull
    {
        if (aggregate.UncommittedEvents.Count == 0) return;
        var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count;

        await store.AppendAsync(CollectionName, aggregateId, aggregateType, expectedVersion,
            aggregate.UncommittedEvents, userId, Guid.NewGuid().ToString(), TenantId.Sovereign.Value.ToString(), SerializeEvent, ct);

        aggregate.ClearEvents();
    }

    private static string SerializeEvent(IDomainEvent @event) => MsgPackOptions.SerializeToBase64(@event, @event.GetType());

    private static IDomainEvent? DeserializeEvent(string eventType, string payload) =>
        EventTypeMap.TryGetValue(eventType, out var type) ? MsgPackOptions.DeserializeFromBase64(payload, type) as IDomainEvent : null;
}
