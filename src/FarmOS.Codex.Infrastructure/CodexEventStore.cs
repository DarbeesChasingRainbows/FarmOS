using FarmOS.Codex.Domain;
using FarmOS.Codex.Domain.Aggregates;
using FarmOS.Codex.Domain.Events;
using FarmOS.Codex.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Codex.Infrastructure;

public sealed class CodexEventStore(IEventStore store) : ICodexEventStore
{
    private const string CollectionName = "codex_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(ProcedureCreated)] = typeof(ProcedureCreated),
        [nameof(ProcedureStepAdded)] = typeof(ProcedureStepAdded),
        [nameof(ProcedurePublished)] = typeof(ProcedurePublished),
        [nameof(ProcedureRevised)] = typeof(ProcedureRevised),
        [nameof(ProcedureArchived)] = typeof(ProcedureArchived),

        [nameof(PlaybookCreated)] = typeof(PlaybookCreated),
        [nameof(PlaybookTaskAdded)] = typeof(PlaybookTaskAdded),
        [nameof(PlaybookTaskRemoved)] = typeof(PlaybookTaskRemoved),

        [nameof(DecisionTreeCreated)] = typeof(DecisionTreeCreated),
        [nameof(DecisionNodeAdded)] = typeof(DecisionNodeAdded),
        [nameof(DecisionNodeUpdated)] = typeof(DecisionNodeUpdated)
    };

    public Task<Procedure> LoadProcedureAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Procedure, ProcedureId>(CollectionName, id, () => new Procedure(), DeserializeEvent, ct);

    public Task SaveProcedureAsync(Procedure procedure, string userId, CancellationToken ct) =>
        SaveAsync(procedure, procedure.Id.ToString(), "Procedure", userId, ct);

    public Task<Playbook> LoadPlaybookAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Playbook, PlaybookId>(CollectionName, id, () => new Playbook(), DeserializeEvent, ct);

    public Task SavePlaybookAsync(Playbook playbook, string userId, CancellationToken ct) =>
        SaveAsync(playbook, playbook.Id.ToString(), "Playbook", userId, ct);

    public Task<DecisionTree> LoadDecisionTreeAsync(string id, CancellationToken ct) =>
        store.LoadAsync<DecisionTree, DecisionTreeId>(CollectionName, id, () => new DecisionTree(), DeserializeEvent, ct);

    public Task SaveDecisionTreeAsync(DecisionTree decisionTree, string userId, CancellationToken ct) =>
        SaveAsync(decisionTree, decisionTree.Id.ToString(), "DecisionTree", userId, ct);

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
