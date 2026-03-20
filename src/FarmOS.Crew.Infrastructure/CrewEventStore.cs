using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FarmOS.Crew.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Crew.Infrastructure;

public sealed class CrewEventStore(IEventStore store) : ICrewEventStore
{
    private const string CollectionName = "crew_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(WorkerRegistered)] = typeof(WorkerRegistered),
        [nameof(WorkerProfileUpdated)] = typeof(WorkerProfileUpdated),
        [nameof(WorkerDeactivated)] = typeof(WorkerDeactivated),
        [nameof(CertificationAdded)] = typeof(CertificationAdded),

        [nameof(ShiftScheduled)] = typeof(ShiftScheduled),
        [nameof(ShiftStarted)] = typeof(ShiftStarted),
        [nameof(ShiftCompleted)] = typeof(ShiftCompleted),
        [nameof(ShiftCancelled)] = typeof(ShiftCancelled),

        [nameof(ProgramCreated)] = typeof(ProgramCreated),
        [nameof(ApprenticeEnrolled)] = typeof(ApprenticeEnrolled),
        [nameof(ApprenticeRotated)] = typeof(ApprenticeRotated),
        [nameof(ProgramCompleted)] = typeof(ProgramCompleted),
        [nameof(ProgramCancelled)] = typeof(ProgramCancelled)
    };

    public Task<Worker> LoadWorkerAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Worker, WorkerId>(CollectionName, id, () => new Worker(), DeserializeEvent, ct);

    public Task SaveWorkerAsync(Worker worker, string userId, CancellationToken ct) =>
        SaveAsync(worker, worker.Id.ToString(), "Worker", userId, ct);

    public Task<Shift> LoadShiftAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Shift, ShiftId>(CollectionName, id, () => new Shift(), DeserializeEvent, ct);

    public Task SaveShiftAsync(Shift shift, string userId, CancellationToken ct) =>
        SaveAsync(shift, shift.Id.ToString(), "Shift", userId, ct);

    public Task<ApprenticeProgram> LoadApprenticeProgramAsync(string id, CancellationToken ct) =>
        store.LoadAsync<ApprenticeProgram, ApprenticeProgramId>(CollectionName, id, () => new ApprenticeProgram(), DeserializeEvent, ct);

    public Task SaveApprenticeProgramAsync(ApprenticeProgram program, string userId, CancellationToken ct) =>
        SaveAsync(program, program.Id.ToString(), "ApprenticeProgram", userId, ct);

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
