# CQRS Architecture — FarmOS

> Event Store design, command/query separation, projection rebuilding, and F# rules engine integration.

---

## Event Store Design (ArangoDB Document Collections)

Each bounded context has its own append-only event collection. Events are **never updated or deleted**.

### Event Envelope

Every event is wrapped in a standard envelope stored as an ArangoDB document:

```csharp
public record EventEnvelope
{
    public required string Id { get; init; }                  // ArangoDB _key
    public required string AggregateId { get; init; }         // e.g., "paddock-001"
    public required string AggregateType { get; init; }       // e.g., "Paddock"
    public required string EventType { get; init; }           // e.g., "GrazingStarted"
    public required int Version { get; init; }                // Monotonic per aggregate
    public required DateTimeOffset OccurredAt { get; init; }
    public required DateTimeOffset StoredAt { get; init; }
    public required JsonDocument Payload { get; init; }       // The serialized domain event
    public required string CorrelationId { get; init; }       // Traces a command through the system
    public required string CausationId { get; init; }         // What caused this event
    public required string UserId { get; init; }              // Who issued the command
}
```

### ArangoDB Collection per Context

```
pasture_events    → PaddockCreated, GrazingStarted, AnimalRegistered, HerdMoved, ...
flora_events      → GuildPlanted, SuccessionPlanned, SeedWithdrawn, HarvestRecorded, ...
hearth_events     → BatchStarted, CCPReadingRecorded, PHReadingRecorded, BatchCompleted, ...
apiary_events     → HiveCreated, InspectionRecorded, HoneyHarvested, QueenInstalled, ...
commerce_events   → SubscriptionCreated, OrderPlaced, OrderPacked, DeliveryRouted, ...
```

### Optimistic Concurrency

```csharp
// When appending events, check the expected version
public async Task AppendEventsAsync(string aggregateId, int expectedVersion, IEnumerable<EventEnvelope> events)
{
    // AQL transaction: check current max version for aggregate, fail if != expectedVersion
    var aql = @"
        LET currentVersion = FIRST(
            FOR e IN @@collection
                FILTER e.AggregateId == @aggregateId
                SORT e.Version DESC LIMIT 1
                RETURN e.Version
        ) ?? 0
        
        FILTER currentVersion == @expectedVersion
        
        FOR event IN @events
            INSERT event INTO @@collection
        RETURN true
    ";
}
```

---

## Command Pipeline

```
HTTP Request
    │
    ▼
Minimal API Endpoint  →  Validates shape (FluentValidation)
    │
    ▼
MediatR Command Handler
    │
    ├──  1. Load Aggregate (replay events from store)
    ├──  2. Execute F# Rule (pure function, returns Result<Event[], Error>)
    ├──  3. Append Events to Store (optimistic concurrency)
    ├──  4. Publish Events to RabbitMQ
    └──  5. Return Result<Guid, Error>  ← NEVER domain data
```

### Example Command

```csharp
// Command (record = immutable)
public record BeginGrazingCommand(Guid PaddockId, Guid HerdId, DateOnly Date) : ICommand<Guid>;

// Handler
public sealed class BeginGrazingHandler : ICommandHandler<BeginGrazingCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly IEventBus _eventBus;

    public async Task<Result<Guid, DomainError>> Handle(BeginGrazingCommand cmd, CancellationToken ct)
    {
        // 1. Rehydrate aggregate
        var paddock = await _eventStore.LoadAsync<Paddock>(cmd.PaddockId, ct);

        // 2. Execute F# business rule
        var ruleResult = GrazingRules.canBeginGrazing(paddock.RestDaysElapsed, paddock.Status);
        if (ruleResult.IsError)
            return Result.Failure<Guid, DomainError>(new DomainError(ruleResult.ErrorValue));

        // 3. Apply domain event
        var @event = new GrazingStarted(
            new PaddockId(cmd.PaddockId), new HerdId(cmd.HerdId), cmd.Date, DateTimeOffset.UtcNow);
        paddock.Apply(@event);

        // 4. Persist + publish
        await _eventStore.AppendAsync(paddock, ct);
        await _eventBus.PublishAsync(@event, ct);

        return Result.Success<Guid, DomainError>(cmd.PaddockId);
    }
}
```

---

## Query Pipeline

Queries **never** touch the event store. They hit pre-built flat read models.

```
HTTP Request
    │
    ▼
Minimal API Endpoint
    │
    ▼
MediatR Query Handler  →  Reads from projection collection  →  Returns DTO
```

### Example Query

```csharp
public record GetPaddockStatusQuery(Guid PaddockId) : IQuery<PaddockStatusDto>;

public record PaddockStatusDto(
    Guid Id, string Name, decimal Acreage, string Status,
    int RestDaysElapsed, decimal? BiomassEstimate, string? CurrentHerdName);

public sealed class GetPaddockStatusHandler : IQueryHandler<GetPaddockStatusQuery, PaddockStatusDto>
{
    private readonly IArangoReadRepository _repo;

    public async Task<PaddockStatusDto?> Handle(GetPaddockStatusQuery query, CancellationToken ct)
    {
        return await _repo.GetByKeyAsync<PaddockStatusDto>("pasture_paddock_view", query.PaddockId.ToString(), ct);
    }
}
```

---

## Projection Engine

Background workers subscribe to event streams and rebuild read models:

```csharp
public sealed class PaddockProjector : IEventProjector
{
    private readonly IArangoWriteRepository _repo;

    public async Task ProjectAsync(GrazingStarted @event, CancellationToken ct)
    {
        await _repo.UpsertAsync("pasture_paddock_view", @event.PaddockId.Value.ToString(), doc =>
        {
            doc.Status = "ActiveGrazing";
            doc.CurrentHerdId = @event.HerdId.Value;
            doc.LastGrazedDate = @event.Date;
            doc.RestDaysElapsed = 0;
        }, ct);
    }

    public async Task ProjectAsync(GrazingEnded @event, CancellationToken ct)
    {
        await _repo.UpsertAsync("pasture_paddock_view", @event.PaddockId.Value.ToString(), doc =>
        {
            doc.Status = "Resting";
            doc.CurrentHerdId = null;
        }, ct);
    }
}
```

### Projection Rebuild

If a read model becomes corrupted or a new projection is added, replay all events:

```csharp
public async Task RebuildProjection<TProjector>(string eventCollection, CancellationToken ct)
    where TProjector : IEventProjector
{
    // Drop and recreate the read collection
    // Stream ALL events from the event collection, ordered by Version
    // Route each event to the projector
}
```

---

## F# Rules Engine Integration

F# modules provide **pure, testable domain logic** with algebraic types:

```fsharp
module FarmOS.Hearth.Rules.KombuchaRules

open System

let maxFermentationDays = 7
let safePHThreshold = 4.2m

type PHValidationResult =
    | Safe
    | NeedsMoreTime
    | MustDiscard of reason: string

let validatePH (startDate: DateOnly) (currentDate: DateOnly) (currentPH: decimal) : PHValidationResult =
    let days = currentDate.DayNumber - startDate.DayNumber
    match currentPH, days with
    | ph, _ when ph <= safePHThreshold -> Safe
    | _, d when d >= maxFermentationDays -> MustDiscard $"pH {currentPH} not below {safePHThreshold} after {d} days"
    | _ -> NeedsMoreTime
```

### Why F# for Rules

| Concern | C# Handles | F# Handles |
|---------|-----------|------------|
| HTTP endpoints | ✅ | |
| Infrastructure (DB, queue) | ✅ | |
| Command/Query handlers | ✅ | |
| **Domain invariants** | | ✅ |
| **Business rules** | | ✅ |
| **Validation logic** | | ✅ |

F# gives you:
- **Exhaustive pattern matching** — the compiler catches missing cases
- **Discriminated unions** — model domain states precisely (no null checks)
- **Pure functions** — rules are testable without mocking infrastructure
- **Interop** — F# compiles to .NET IL, called from C# seamlessly

---

## Cross-Context Event Flow (RabbitMQ)

```
Hearth Context                    Commerce Context
    │                                 │
    │ BatchCompleted event            │
    │ (sourdough: 24 loaves)          │
    ├──► RabbitMQ ──────────────────► │
    │    exchange: "farm.events"      │  InventoryProjection updated
    │    routing: "hearth.batch.*"    │  (24 loaves available)
    │                                 │
    │                                 │  ProductionRequested event
    │  ◄──────────────────── RabbitMQ ◄──┤  
    │    routing: "commerce.demand.*" │  (need 30 loaves Saturday)
    │                                 │
    │ Hearth sees demand signal       │
    │ (displayed on HearthOS UI)      │
```

### Exchange Topology

```
Exchange: "farm.events" (topic exchange)
    │
    ├── Queue: "pasture.events"        Binding: "pasture.#"
    ├── Queue: "flora.events"          Binding: "flora.#"
    ├── Queue: "hearth.events"         Binding: "hearth.#"
    ├── Queue: "apiary.events"         Binding: "apiary.#"
    ├── Queue: "commerce.events"       Binding: "commerce.#"
    │
    ├── Queue: "commerce.cross"        Binding: "hearth.batch.completed"
    │                                  Binding: "apiary.harvest.completed"
    │                                  Binding: "flora.stems.available"
    │                                  Binding: "pasture.meat.available"
    │
    └── Queue: "hearth.demand"         Binding: "commerce.demand.*"
```
