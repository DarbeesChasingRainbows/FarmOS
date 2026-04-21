# SharedKernel — FarmOS

> Core contracts, base types, and infrastructure shared across all bounded contexts.

**Project**: `FarmOS.SharedKernel`
**Location**: `src/FarmOS.SharedKernel/`

---

## Purpose

The SharedKernel is the only project referenced by every bounded context. It provides:
- Domain base types (aggregate root, events, results)
- CQRS contracts (command/query interfaces)
- Event store abstraction (ArangoDB)
- Event bus abstraction (RabbitMQ)
- Authentication primitives
- Infrastructure implementations

> **Rule**: The SharedKernel contains NO business logic. It only defines contracts, base types, and cross-cutting infrastructure.

---

## Domain Layer (`Domain/`)

### AggregateRoot\<TId\>

[AggregateRoot.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Domain/AggregateRoot.cs) — Base class for all event-sourced aggregates.

```csharp
public abstract class AggregateRoot<TId> where TId : notnull
{
    public TenantId TenantId { get; protected set; }
    public TId Id { get; protected set; }
    public int Version { get; protected set; }

    protected void RaiseEvent(IDomainEvent @event);   // Apply + collect
    public void Rehydrate(IEnumerable<IDomainEvent> history); // Replay
    protected abstract void Apply(IDomainEvent @event); // Each aggregate implements
}
```

**Usage pattern**: Every aggregate inherits this and implements `Apply()` as a pattern-match router:

```csharp
protected override void Apply(IDomainEvent @event) => @event switch
{
    GrazingStarted e => /* mutate state */,
    GrazingEnded e => /* mutate state */,
    _ => throw new InvalidOperationException($"Unknown event: {@event.GetType().Name}")
};
```

### IDomainEvent

```csharp
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
```

### Result\<T, E\>

[Result.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Domain/Result.cs) — Discriminated union for command responses.

```csharp
public readonly struct Result<T, E>
{
    public bool IsSuccess { get; }
    public T Value { get; }    // throws if failure
    public E Error { get; }    // throws if success

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure);
}
```

All commands return `Result<Guid, DomainError>` (or `Result<Unit, DomainError>`). The endpoint handler uses `.Match()` to map to HTTP responses:

```csharp
var result = await m.Send(cmd, ct);
return result.Match(
    id  => Results.Created($"/api/pasture/paddocks/{id}", new { id }),
    err => Results.BadRequest(err));
```

### DomainError

```csharp
public record DomainError(string Code, string Message)
{
    public static DomainError Validation(string message);
    public static DomainError NotFound(string entity, string id);
    public static DomainError Conflict(string message);
    public static DomainError BusinessRule(string message);
}
```

Standard error codes: `VALIDATION_ERROR`, `NOT_FOUND`, `CONFLICT`, `BUSINESS_RULE`

### Value Objects

[ValueObjects.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Domain/ValueObjects.cs) — Shared value types (e.g., `GeoPosition`, `Measurement`).

### TenantId

[TenantId.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Domain/TenantId.cs) — Multi-tenancy support. Currently `TenantId.Sovereign` (single farm). Designed for future SaaS evolution.

---

## CQRS Layer (`CQRS/`)

[Contracts.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/CQRS/Contracts.cs) — MediatR-based command/query contracts.

```csharp
// Commands mutate state, return Result<TResponse, DomainError>
public interface ICommand<TResponse> : IRequest<Result<TResponse, DomainError>>;
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse, DomainError>>
    where TCommand : ICommand<TResponse>;

// Queries read projections, return nullable TResponse
public interface IQuery<TResponse> : IRequest<TResponse?>;
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse?>
    where TQuery : IQuery<TResponse>;
```

**Rules**:
- Commands NEVER return domain data — only success/failure or an ID
- Queries NEVER mutate state — they read flat projection models
- Both use MediatR's `Send()` for dispatch

---

## Event Store Layer (`EventStore/`)

### IEventStore

[IEventStore.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/EventStore/IEventStore.cs) — Append-only event persistence.

| Method | Purpose |
|--------|---------|
| `LoadAsync<TAggregate, TId>()` | Replay events to rebuild an aggregate |
| `AppendAsync()` | Append new events with optimistic concurrency |
| `GetAllEventsAsync()` | Stream events for projection rebuilding |

### EventEnvelope

The document shape stored in ArangoDB event collections:

| Field | Type | Description |
|-------|------|-------------|
| `_key` | string | ArangoDB document key |
| `AggregateId` | string | Which aggregate instance |
| `AggregateType` | string | e.g., "Paddock", "Hive" |
| `EventType` | string | e.g., "GrazingStarted" |
| `Version` | int | Monotonic per aggregate (concurrency) |
| `OccurredAt` | DateTimeOffset | When the event logically occurred |
| `StoredAt` | DateTimeOffset | When persisted |
| `Payload` | string | Base64-encoded MessagePack event data |
| `CorrelationId` | string | Trace a command through the system |
| `UserId` | string | Who issued the command |
| `TenantId` | string | Tenant isolation (sovereign) |

### IEventBus

[IEventBus.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/EventStore/IEventBus.cs) — Cross-context messaging via RabbitMQ. See [cross-context-events.md](cross-context-events.md) for detailed flow.

### IntegrationEvents

[IntegrationEvents.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/EventStore/IntegrationEvents.cs) — Shared DTOs for cross-context communication. See [cross-context-events.md](cross-context-events.md).

---

## Infrastructure Layer (`Infrastructure/`)

| File | Purpose |
|------|---------|
| `ArangoEventStore.cs` | `IEventStore` implementation using ArangoDB HTTP API |
| `RabbitMqEventBus.cs` | `IEventBus` implementation using RabbitMQ (topic exchange: `farm.events`) |
| `ArangoAuthService.cs` | PIN-based authentication against ArangoDB user collection |
| `MessagePackMiddleware.cs` | ASP.NET middleware for MessagePack content negotiation |
| `MsgPackOptions.cs` | MessagePack serialization configuration |

---

## Auth Layer (`Auth/`)

[AuthTypes.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Auth/AuthTypes.cs) — Authentication types (roles, user records, PIN validation).

---

## File Structure

```
FarmOS.SharedKernel/
  Auth/
    AuthTypes.cs             # Roles, user, PIN auth
  CQRS/
    Contracts.cs             # ICommand, IQuery, handlers
  Domain/
    AggregateRoot.cs         # Event-sourced base class
    IDomainEvent.cs          # Event marker interface
    Result.cs                # Result<T,E> + DomainError
    TenantId.cs              # Multi-tenancy identifier
    ValueObjects.cs          # GeoPosition, Measurement, etc.
  EventStore/
    IEventStore.cs           # Append-only store abstraction
    IEventBus.cs             # RabbitMQ bus abstraction
    IntegrationEvents.cs     # Cross-context event DTOs
  Infrastructure/
    ArangoEventStore.cs      # ArangoDB implementation
    RabbitMqEventBus.cs      # RabbitMQ implementation
    ArangoAuthService.cs     # Auth implementation
    MessagePackMiddleware.cs # Content negotiation
    MsgPackOptions.cs        # Serialization config
  StringNormalization.cs     # String utilities
```
