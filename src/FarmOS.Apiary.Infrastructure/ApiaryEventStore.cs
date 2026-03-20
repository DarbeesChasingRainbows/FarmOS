using FarmOS.Apiary.Domain;
using FarmOS.Apiary.Domain.Aggregates;
using FarmOS.Apiary.Domain.Events;
using FarmOS.Apiary.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;
using ApiaryAggregate = FarmOS.Apiary.Domain.Aggregates.Apiary;

namespace FarmOS.Apiary.Infrastructure;

public sealed class ApiaryEventStore(IEventStore store) : IApiaryEventStore
{
    private const string CollectionName = "apiary_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(HiveCreated)] = typeof(HiveCreated),
        [nameof(HiveInspected)] = typeof(HiveInspected),
        [nameof(HoneyHarvested)] = typeof(HoneyHarvested),
        [nameof(HiveTreated)] = typeof(HiveTreated),
        [nameof(HiveStatusChanged)] = typeof(HiveStatusChanged),
        [nameof(HiveSwarmed)] = typeof(HiveSwarmed),
        // Feature 1: Apiary
        [nameof(ApiaryCreated)] = typeof(ApiaryCreated),
        [nameof(HiveMovedToApiary)] = typeof(HiveMovedToApiary),
        [nameof(HiveRemovedFromApiary)] = typeof(HiveRemovedFromApiary),
        [nameof(ApiaryRetired)] = typeof(ApiaryRetired),
        // Feature 2: Queen Tracking
        [nameof(QueenIntroduced)] = typeof(QueenIntroduced),
        [nameof(QueenLost)] = typeof(QueenLost),
        [nameof(QueenReplaced)] = typeof(QueenReplaced),
        // Feature 3: Feeding
        [nameof(HiveFed)] = typeof(HiveFed),
        // Feature 6: Multi-Product Harvest
        [nameof(ProductHarvested)] = typeof(ProductHarvested),
        // Feature 4: Colony Split/Merge
        [nameof(ColonySplit)] = typeof(ColonySplit),
        [nameof(ColoniesMerged)] = typeof(ColoniesMerged),
        // Feature 5: Equipment/Supers
        [nameof(SuperAdded)] = typeof(SuperAdded),
        [nameof(SuperRemoved)] = typeof(SuperRemoved),
        [nameof(HiveConfigurationChanged)] = typeof(HiveConfigurationChanged),
        // Feature 11: Weather
        [nameof(WeatherRecordedWithInspection)] = typeof(WeatherRecordedWithInspection)
    };

    public Task<Hive> LoadHiveAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Hive, HiveId>(CollectionName, id, () => new Hive(), DeserializeEvent, ct);

    public Task SaveHiveAsync(Hive hive, string userId, CancellationToken ct) =>
        SaveAsync(hive, hive.Id.ToString(), "Hive", userId, ct);

    public Task<ApiaryAggregate> LoadApiaryAsync(string id, CancellationToken ct) =>
        store.LoadAsync<ApiaryAggregate, ApiaryId>(CollectionName, id, () => new ApiaryAggregate(), DeserializeEvent, ct);

    public Task SaveApiaryAsync(ApiaryAggregate apiary, string userId, CancellationToken ct) =>
        SaveAsync(apiary, apiary.Id.ToString(), "Apiary", userId, ct);

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
