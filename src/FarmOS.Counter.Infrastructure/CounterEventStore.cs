using FarmOS.Counter.Domain;
using FarmOS.Counter.Domain.Aggregates;
using FarmOS.Counter.Domain.Events;
using FarmOS.Counter.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Counter.Infrastructure;

public sealed class CounterEventStore(IEventStore store) : ICounterEventStore
{
    private const string CollectionName = "counter_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(RegisterOpened)] = typeof(RegisterOpened),
        [nameof(RegisterClosed)] = typeof(RegisterClosed),

        [nameof(SaleCompleted)] = typeof(SaleCompleted),
        [nameof(SaleVoided)] = typeof(SaleVoided),
        [nameof(SaleRefunded)] = typeof(SaleRefunded),

        [nameof(CashDrawerOpened)] = typeof(CashDrawerOpened),
        [nameof(CashDrawerCounted)] = typeof(CashDrawerCounted),
        [nameof(CashDrawerReconciled)] = typeof(CashDrawerReconciled)
    };

    public Task<Register> LoadRegisterAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Register, RegisterId>(CollectionName, id, () => new Register(), DeserializeEvent, ct);

    public Task SaveRegisterAsync(Register register, string userId, CancellationToken ct) =>
        SaveAsync(register, register.Id.ToString(), "Register", userId, ct);

    public Task<Sale> LoadSaleAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Sale, SaleId>(CollectionName, id, () => new Sale(), DeserializeEvent, ct);

    public Task SaveSaleAsync(Sale sale, string userId, CancellationToken ct) =>
        SaveAsync(sale, sale.Id.ToString(), "Sale", userId, ct);

    public Task<CashDrawer> LoadCashDrawerAsync(string id, CancellationToken ct) =>
        store.LoadAsync<CashDrawer, CashDrawerId>(CollectionName, id, () => new CashDrawer(), DeserializeEvent, ct);

    public Task SaveCashDrawerAsync(CashDrawer drawer, string userId, CancellationToken ct) =>
        SaveAsync(drawer, drawer.Id.ToString(), "CashDrawer", userId, ct);

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
