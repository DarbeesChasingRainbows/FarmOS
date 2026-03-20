namespace FarmOS.SharedKernel.EventStore;

/// <summary>
/// The envelope that wraps every domain event before persisting to ArangoDB.
/// This is the actual document shape stored in event collections (e.g., pasture_events).
/// </summary>
public record EventEnvelope
{
    /// <summary>ArangoDB document key.</summary>
    public required string _key { get; init; }

    /// <summary>The aggregate instance this event belongs to.</summary>
    public required string AggregateId { get; init; }

    /// <summary>The type name of the aggregate (e.g., "Paddock", "Animal").</summary>
    public required string AggregateType { get; init; }

    /// <summary>The type name of the domain event (e.g., "GrazingStarted").</summary>
    public required string EventType { get; init; }

    /// <summary>Monotonically increasing version per aggregate for optimistic concurrency.</summary>
    public required int Version { get; init; }

    /// <summary>When the event logically occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>When the event was persisted to the store.</summary>
    public required DateTimeOffset StoredAt { get; init; }

    /// <summary>Base64-encoded MessagePack domain event payload.</summary>
    public required string Payload { get; init; }

    /// <summary>Correlation ID to trace a command through the system.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Who issued the command that produced this event.</summary>
    public required string UserId { get; init; }

    /// <summary>Tenant identifier (sovereign = single farm GUID).</summary>
    public required string TenantId { get; init; }
}

/// <summary>
/// Abstraction over the append-only event store backed by ArangoDB document collections.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Load an aggregate by replaying its events from the store.
    /// </summary>
    Task<TAggregate> LoadAsync<TAggregate, TId>(
        string collectionName,
        string aggregateId,
        Func<TAggregate> factory,
        Func<string, string, IDomainEvent?> deserializer,
        CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull;

    /// <summary>
    /// Append new events to the store with optimistic concurrency check.
    /// </summary>
    Task AppendAsync(
        string collectionName,
        string aggregateId,
        string aggregateType,
        int expectedVersion,
        IReadOnlyList<IDomainEvent> events,
        string userId,
        string correlationId,
        string tenantId,
        Func<IDomainEvent, string> serializer,
        CancellationToken ct);

    /// <summary>
    /// Stream all events from a collection for projection rebuilding.
    /// </summary>
    Task<IReadOnlyList<EventEnvelope>> GetAllEventsAsync(
        string collectionName,
        long fromPosition,
        int batchSize,
        CancellationToken ct);
}
