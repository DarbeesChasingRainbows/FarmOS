using FarmOS.Assets.Domain;
using FarmOS.Assets.Domain.Aggregates;
using FarmOS.Assets.Domain.Events;
using FarmOS.Assets.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Assets.Infrastructure;

public sealed class AssetsEventStore(IEventStore store) : IAssetsEventStore
{
    private const string CollectionName = "assets_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(EquipmentRegistered)] = typeof(EquipmentRegistered),
        [nameof(EquipmentMaintenanceRecorded)] = typeof(EquipmentMaintenanceRecorded),
        [nameof(EquipmentMoved)] = typeof(EquipmentMoved),
        [nameof(EquipmentRetired)] = typeof(EquipmentRetired),

        [nameof(StructureRegistered)] = typeof(StructureRegistered),
        [nameof(StructureMaintenanceRecorded)] = typeof(StructureMaintenanceRecorded),

        [nameof(WaterSourceRegistered)] = typeof(WaterSourceRegistered),
        [nameof(WaterTestRecorded)] = typeof(WaterTestRecorded),

        [nameof(CompostBatchStarted)] = typeof(CompostBatchStarted),
        [nameof(CompostTempRecorded)] = typeof(CompostTempRecorded),
        [nameof(CompostTurned)] = typeof(CompostTurned),
        [nameof(CompostPhaseChanged)] = typeof(CompostPhaseChanged),
        [nameof(CompostInoculated)] = typeof(CompostInoculated),
        [nameof(CompostPhMeasured)] = typeof(CompostPhMeasured),
        [nameof(CompostNoteAdded)] = typeof(CompostNoteAdded),
        [nameof(CompostBatchCompleted)] = typeof(CompostBatchCompleted),


        [nameof(MaterialRegistered)] = typeof(MaterialRegistered),
        [nameof(MaterialUsed)] = typeof(MaterialUsed),
        [nameof(MaterialRestocked)] = typeof(MaterialRestocked)
    };

    public Task<Equipment> LoadEquipmentAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Equipment, EquipmentId>(CollectionName, id, () => new Equipment(), DeserializeEvent, ct);
    public Task SaveEquipmentAsync(Equipment eq, string userId, CancellationToken ct) =>
        SaveAsync(eq, eq.Id.ToString(), "Equipment", userId, ct);

    public Task<Structure> LoadStructureAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Structure, StructureId>(CollectionName, id, () => new Structure(), DeserializeEvent, ct);
    public Task SaveStructureAsync(Structure s, string userId, CancellationToken ct) =>
        SaveAsync(s, s.Id.ToString(), "Structure", userId, ct);

    public Task<WaterSource> LoadWaterSourceAsync(string id, CancellationToken ct) =>
        store.LoadAsync<WaterSource, WaterSourceId>(CollectionName, id, () => new WaterSource(), DeserializeEvent, ct);
    public Task SaveWaterSourceAsync(WaterSource ws, string userId, CancellationToken ct) =>
        SaveAsync(ws, ws.Id.ToString(), "WaterSource", userId, ct);

    public Task<CompostBatch> LoadCompostBatchAsync(string id, CancellationToken ct) =>
        store.LoadAsync<CompostBatch, CompostBatchId>(CollectionName, id, () => new CompostBatch(), DeserializeEvent, ct);
    public Task SaveCompostBatchAsync(CompostBatch cb, string userId, CancellationToken ct) =>
        SaveAsync(cb, cb.Id.ToString(), "CompostBatch", userId, ct);


    public Task<Material> LoadMaterialAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Material, MaterialId>(CollectionName, id, () => new Material(), DeserializeEvent, ct);
    public Task SaveMaterialAsync(Material mat, string userId, CancellationToken ct) =>
        SaveAsync(mat, mat.Id.ToString(), "Material", userId, ct);

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
