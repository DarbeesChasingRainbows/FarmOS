using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.Commerce.Domain.Events;
using FarmOS.Commerce.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Commerce.Infrastructure;

public sealed class CommerceEventStore(IEventStore store) : ICommerceEventStore
{
    private const string CollectionName = "commerce_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(CSASeasonCreated)] = typeof(CSASeasonCreated),
        [nameof(CSAPickupScheduled)] = typeof(CSAPickupScheduled),
        [nameof(CSASeasonClosed)] = typeof(CSASeasonClosed),
        
        [nameof(CSAMemberRegistered)] = typeof(CSAMemberRegistered),
        [nameof(CSAPaymentRecorded)] = typeof(CSAPaymentRecorded),
        [nameof(CSASharePickedUp)] = typeof(CSASharePickedUp),
        
        [nameof(OrderCreated)] = typeof(OrderCreated),
        [nameof(OrderPacked)] = typeof(OrderPacked),
        [nameof(OrderFulfilled)] = typeof(OrderFulfilled),
        [nameof(OrderCancelled)] = typeof(OrderCancelled)
    };

    public Task<CSASeason> LoadSeasonAsync(string id, CancellationToken ct) =>
        store.LoadAsync<CSASeason, CSASeasonId>(CollectionName, id, () => new CSASeason(), DeserializeEvent, ct);

    public Task SaveSeasonAsync(CSASeason season, string userId, CancellationToken ct) =>
        SaveAsync(season, season.Id.ToString(), "CSASeason", userId, ct);

    public Task<CSAMember> LoadMemberAsync(string id, CancellationToken ct) =>
        store.LoadAsync<CSAMember, CSAMemberId>(CollectionName, id, () => new CSAMember(), DeserializeEvent, ct);

    public Task SaveMemberAsync(CSAMember member, string userId, CancellationToken ct) =>
        SaveAsync(member, member.Id.ToString(), "CSAMember", userId, ct);

    public Task<Order> LoadOrderAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Order, OrderId>(CollectionName, id, () => new Order(), DeserializeEvent, ct);

    public Task SaveOrderAsync(Order order, string userId, CancellationToken ct) =>
        SaveAsync(order, order.Id.ToString(), "Order", userId, ct);

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
