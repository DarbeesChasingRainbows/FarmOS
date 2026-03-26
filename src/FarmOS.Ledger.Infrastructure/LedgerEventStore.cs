using FarmOS.Ledger.Domain;
using FarmOS.Ledger.Domain.Aggregates;
using FarmOS.Ledger.Domain.Events;
using FarmOS.Ledger.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Ledger.Infrastructure;

public sealed class LedgerEventStore(IEventStore store) : ILedgerEventStore
{
    private const string CollectionName = "ledger_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(ExpenseRecorded)] = typeof(ExpenseRecorded),
        [nameof(ExpenseVoided)] = typeof(ExpenseVoided),
        [nameof(RevenueRecorded)] = typeof(RevenueRecorded),
        [nameof(RevenueVoided)] = typeof(RevenueVoided),
        [nameof(ExpenseEnterpriseTagged)] = typeof(ExpenseEnterpriseTagged),
        [nameof(RevenueEnterpriseTagged)] = typeof(RevenueEnterpriseTagged)
    };

    public Task<Expense> LoadExpenseAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Expense, ExpenseId>(CollectionName, id, () => new Expense(), DeserializeEvent, ct);

    public Task SaveExpenseAsync(Expense expense, string userId, CancellationToken ct) =>
        SaveAsync(expense, expense.Id.ToString(), "Expense", userId, ct);

    public Task<Revenue> LoadRevenueAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Revenue, RevenueId>(CollectionName, id, () => new Revenue(), DeserializeEvent, ct);

    public Task SaveRevenueAsync(Revenue revenue, string userId, CancellationToken ct) =>
        SaveAsync(revenue, revenue.Id.ToString(), "Revenue", userId, ct);

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
