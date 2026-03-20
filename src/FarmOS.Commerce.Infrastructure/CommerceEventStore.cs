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
        [nameof(OrderCancelled)] = typeof(OrderCancelled),

        [nameof(CustomerCreated)] = typeof(CustomerCreated),
        [nameof(CustomerProfileUpdated)] = typeof(CustomerProfileUpdated),
        [nameof(CustomerNoteAdded)] = typeof(CustomerNoteAdded),
        [nameof(DuplicateSuspected)] = typeof(DuplicateSuspected),
        [nameof(CustomersMerged)] = typeof(CustomersMerged),
        [nameof(DuplicateDismissed)] = typeof(DuplicateDismissed),

        [nameof(BuyingClubCreated)] = typeof(BuyingClubCreated),
        [nameof(DropSiteAdded)] = typeof(DropSiteAdded),
        [nameof(DropSiteRemoved)] = typeof(DropSiteRemoved),
        [nameof(OrderCycleOpened)] = typeof(OrderCycleOpened),
        [nameof(OrderCycleClosed)] = typeof(OrderCycleClosed),
        [nameof(BuyingClubPaused)] = typeof(BuyingClubPaused),
        [nameof(BuyingClubClosed)] = typeof(BuyingClubClosed),

        [nameof(WholesaleAccountOpened)] = typeof(WholesaleAccountOpened),
        [nameof(StandingOrderSet)] = typeof(StandingOrderSet),
        [nameof(StandingOrderCancelled)] = typeof(StandingOrderCancelled),
        [nameof(DeliveryRouteAssigned)] = typeof(DeliveryRouteAssigned),
        [nameof(WholesaleAccountClosed)] = typeof(WholesaleAccountClosed)
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

    public Task<Customer> LoadCustomerAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Customer, CustomerId>(CollectionName, id, () => new Customer(), DeserializeEvent, ct);

    public Task SaveCustomerAsync(Customer customer, string userId, CancellationToken ct) =>
        SaveAsync(customer, customer.Id.ToString(), "Customer", userId, ct);

    public Task<BuyingClub> LoadBuyingClubAsync(string id, CancellationToken ct) =>
        store.LoadAsync<BuyingClub, BuyingClubId>(CollectionName, id, () => new BuyingClub(), DeserializeEvent, ct);

    public Task SaveBuyingClubAsync(BuyingClub club, string userId, CancellationToken ct) =>
        SaveAsync(club, club.Id.ToString(), "BuyingClub", userId, ct);

    public Task<WholesaleAccount> LoadWholesaleAccountAsync(string id, CancellationToken ct) =>
        store.LoadAsync<WholesaleAccount, WholesaleAccountId>(CollectionName, id, () => new WholesaleAccount(), DeserializeEvent, ct);

    public Task SaveWholesaleAccountAsync(WholesaleAccount account, string userId, CancellationToken ct) =>
        SaveAsync(account, account.Id.ToString(), "WholesaleAccount", userId, ct);

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
