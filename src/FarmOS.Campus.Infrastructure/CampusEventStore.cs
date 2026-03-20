using FarmOS.Campus.Domain;
using FarmOS.Campus.Domain.Aggregates;
using FarmOS.Campus.Domain.Events;
using FarmOS.Campus.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Campus.Infrastructure;

public sealed class CampusEventStore(IEventStore store) : ICampusEventStore
{
    private const string CollectionName = "campus_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(FarmEventCreated)] = typeof(FarmEventCreated),
        [nameof(FarmEventPublished)] = typeof(FarmEventPublished),
        [nameof(FarmEventCancelled)] = typeof(FarmEventCancelled),
        [nameof(FarmEventCompleted)] = typeof(FarmEventCompleted),
        [nameof(SpotReserved)] = typeof(SpotReserved),

        [nameof(BookingCreated)] = typeof(BookingCreated),
        [nameof(BookingConfirmed)] = typeof(BookingConfirmed),
        [nameof(BookingCheckedIn)] = typeof(BookingCheckedIn),
        [nameof(BookingCancelled)] = typeof(BookingCancelled),
        [nameof(WaiverSigned)] = typeof(WaiverSigned)
    };

    public Task<FarmEvent> LoadFarmEventAsync(string id, CancellationToken ct) =>
        store.LoadAsync<FarmEvent, EventId>(CollectionName, id, () => new FarmEvent(), DeserializeEvent, ct);

    public Task SaveFarmEventAsync(FarmEvent farmEvent, string userId, CancellationToken ct) =>
        SaveAsync(farmEvent, farmEvent.Id.ToString(), "FarmEvent", userId, ct);

    public Task<Booking> LoadBookingAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Booking, BookingId>(CollectionName, id, () => new Booking(), DeserializeEvent, ct);

    public Task SaveBookingAsync(Booking booking, string userId, CancellationToken ct) =>
        SaveAsync(booking, booking.Id.ToString(), "Booking", userId, ct);

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
