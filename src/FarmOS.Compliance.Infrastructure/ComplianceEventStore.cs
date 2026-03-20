using FarmOS.Compliance.Domain;
using FarmOS.Compliance.Domain.Aggregates;
using FarmOS.Compliance.Domain.Events;
using FarmOS.Compliance.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Compliance.Infrastructure;

public sealed class ComplianceEventStore(IEventStore store) : IComplianceEventStore
{
    private const string CollectionName = "compliance_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(PermitRegistered)] = typeof(PermitRegistered),
        [nameof(PermitRenewed)] = typeof(PermitRenewed),
        [nameof(PermitExpired)] = typeof(PermitExpired),
        [nameof(PermitRevoked)] = typeof(PermitRevoked),

        [nameof(PolicyRegistered)] = typeof(PolicyRegistered),
        [nameof(PolicyRenewed)] = typeof(PolicyRenewed),
        [nameof(PolicyExpired)] = typeof(PolicyExpired),
        [nameof(PolicyCancelled)] = typeof(PolicyCancelled),
        [nameof(PolicyCoverageUpdated)] = typeof(PolicyCoverageUpdated)
    };

    public Task<Permit> LoadPermitAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Permit, PermitId>(CollectionName, id, () => new Permit(), DeserializeEvent, ct);

    public Task SavePermitAsync(Permit permit, string userId, CancellationToken ct) =>
        SaveAsync(permit, permit.Id.ToString(), "Permit", userId, ct);

    public Task<InsurancePolicy> LoadPolicyAsync(string id, CancellationToken ct) =>
        store.LoadAsync<InsurancePolicy, PolicyId>(CollectionName, id, () => new InsurancePolicy(), DeserializeEvent, ct);

    public Task SavePolicyAsync(InsurancePolicy policy, string userId, CancellationToken ct) =>
        SaveAsync(policy, policy.Id.ToString(), "InsurancePolicy", userId, ct);

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
