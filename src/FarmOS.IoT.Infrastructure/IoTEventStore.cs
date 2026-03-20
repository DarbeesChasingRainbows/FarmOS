using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;
using FarmOS.IoT.Domain.Aggregates;
using FarmOS.IoT.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.IoT.Infrastructure;

public sealed class IoTEventStore(IEventStore store) : IIoTEventStore
{
    private const string CollectionName = "iot_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(DeviceRegistered)] = typeof(DeviceRegistered),
        [nameof(DeviceUpdated)] = typeof(DeviceUpdated),
        [nameof(DeviceDecommissioned)] = typeof(DeviceDecommissioned),
        [nameof(DeviceAssignedToZone)] = typeof(DeviceAssignedToZone),
        [nameof(DeviceUnassignedFromZone)] = typeof(DeviceUnassignedFromZone),
        [nameof(DeviceAssignedToAsset)] = typeof(DeviceAssignedToAsset),
        [nameof(DeviceUnassignedFromAsset)] = typeof(DeviceUnassignedFromAsset),

        [nameof(ZoneCreated)] = typeof(ZoneCreated),
        [nameof(ZoneUpdated)] = typeof(ZoneUpdated),
        [nameof(ZoneArchived)] = typeof(ZoneArchived),

        [nameof(TelemetryReadingRecorded)] = typeof(TelemetryReadingRecorded),
        [nameof(ExcursionStarted)] = typeof(ExcursionStarted),
        [nameof(ExcursionEnded)] = typeof(ExcursionEnded),
        [nameof(ExcursionAlertFired)] = typeof(ExcursionAlertFired)
    };

    public Task<IoTDevice> LoadDeviceAsync(string id, CancellationToken ct) =>
        store.LoadAsync<IoTDevice, IoTDeviceId>(CollectionName, id, () => (IoTDevice)Activator.CreateInstance(typeof(IoTDevice), nonPublic: true)!, DeserializeEvent, ct);

    public Task SaveDeviceAsync(IoTDevice device, string userId, CancellationToken ct) =>
        SaveAsync(device, device.Id.ToString(), "IoTDevice", userId, ct);

    public Task<Zone> LoadZoneAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Zone, ZoneId>(CollectionName, id, () => (Zone)Activator.CreateInstance(typeof(Zone), nonPublic: true)!, DeserializeEvent, ct);

    public Task SaveZoneAsync(Zone zone, string userId, CancellationToken ct) =>
        SaveAsync(zone, zone.Id.ToString(), "Zone", userId, ct);

    public Task SaveTelemetryStreamAsync(TelemetryStream stream, string userId, CancellationToken ct) =>
        SaveAsync(stream, stream.Id.ToString(), "TelemetryStream", userId, ct);

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
