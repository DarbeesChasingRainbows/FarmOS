using FarmOS.Pasture.Domain;
using FarmOS.Pasture.Domain.Aggregates;
using FarmOS.Pasture.Domain.Events;
using FarmOS.Pasture.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Pasture.Infrastructure;

/// <summary>
/// Pasture-specific event store implementation that delegates to the generic ArangoDB event store.
/// Provides typed load/save methods with proper serialization for Pasture domain events.
/// </summary>
public sealed class PastureEventStore : IPastureEventStore
{
    private const string CollectionName = "pasture_events";
    private readonly IEventStore _store;

    // Maps event type names to their CLR types for deserialization
    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(PaddockCreated)] = typeof(PaddockCreated),
        [nameof(PaddockBoundaryUpdated)] = typeof(PaddockBoundaryUpdated),
        [nameof(GrazingStarted)] = typeof(GrazingStarted),
        [nameof(GrazingEnded)] = typeof(GrazingEnded),
        [nameof(BiomassUpdated)] = typeof(BiomassUpdated),
        [nameof(SoilTestRecorded)] = typeof(SoilTestRecorded),
        [nameof(AnimalRegistered)] = typeof(AnimalRegistered),
        [nameof(AnimalIsolated)] = typeof(AnimalIsolated),
        [nameof(AnimalReturnedToHerd)] = typeof(AnimalReturnedToHerd),
        [nameof(TreatmentRecorded)] = typeof(TreatmentRecorded),
        [nameof(PregnancyRecorded)] = typeof(PregnancyRecorded),
        [nameof(BirthRecorded)] = typeof(BirthRecorded),
        [nameof(AnimalButchered)] = typeof(AnimalButchered),
        [nameof(AnimalSold)] = typeof(AnimalSold),
        [nameof(WeightRecorded)] = typeof(WeightRecorded),
        [nameof(AnimalDeceased)] = typeof(AnimalDeceased),
        [nameof(HerdCreated)] = typeof(HerdCreated),
        [nameof(HerdMoved)] = typeof(HerdMoved),
        [nameof(AnimalAddedToHerd)] = typeof(AnimalAddedToHerd),
        [nameof(AnimalRemovedFromHerd)] = typeof(AnimalRemovedFromHerd),
    };

    public PastureEventStore(IEventStore store)
    {
        _store = store;
    }

    // ─── Paddock ─────────────────────────────────────────────────

    public Task<Paddock> LoadPaddockAsync(string paddockId, CancellationToken ct)
        => _store.LoadAsync<Paddock, PaddockId>(
            CollectionName, paddockId, () => new Paddock(), DeserializeEvent, ct);

    public Task SavePaddockAsync(Paddock paddock, string userId, CancellationToken ct)
        => SaveAsync(paddock, paddock.Id.ToString(), "Paddock", userId, ct);

    // ─── Animal ──────────────────────────────────────────────────

    public Task<Animal> LoadAnimalAsync(string animalId, CancellationToken ct)
        => _store.LoadAsync<Animal, AnimalId>(
            CollectionName, animalId, () => new Animal(), DeserializeEvent, ct);

    public Task SaveAnimalAsync(Animal animal, string userId, CancellationToken ct)
        => SaveAsync(animal, animal.Id.ToString(), "Animal", userId, ct);

    // ─── Herd ────────────────────────────────────────────────────

    public Task<Herd> LoadHerdAsync(string herdId, CancellationToken ct)
        => _store.LoadAsync<Herd, HerdId>(
            CollectionName, herdId, () => new Herd(), DeserializeEvent, ct);

    public Task SaveHerdAsync(Herd herd, string userId, CancellationToken ct)
        => SaveAsync(herd, herd.Id.ToString(), "Herd", userId, ct);

    // ─── Shared Helpers ──────────────────────────────────────────

    private async Task SaveAsync<TId>(
        AggregateRoot<TId> aggregate, string aggregateId, string aggregateType,
        string userId, CancellationToken ct) where TId : notnull
    {
        if (aggregate.UncommittedEvents.Count == 0) return;

        var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count;

        await _store.AppendAsync(
            CollectionName,
            aggregateId,
            aggregateType,
            expectedVersion,
            aggregate.UncommittedEvents,
            userId,
            Guid.NewGuid().ToString(),
            TenantId.Sovereign.Value.ToString(),
            SerializeEvent,
            ct);

        aggregate.ClearEvents();
    }

    private static string SerializeEvent(IDomainEvent @event)
        => MsgPackOptions.SerializeToBase64(@event, @event.GetType());

    private static IDomainEvent? DeserializeEvent(string eventType, string payload)
    {
        if (!EventTypeMap.TryGetValue(eventType, out var type))
            return null;

        return MsgPackOptions.DeserializeFromBase64(payload, type) as IDomainEvent;
    }
}
