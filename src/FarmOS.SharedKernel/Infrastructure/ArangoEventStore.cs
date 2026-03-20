using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.SharedKernel.EventStore;

namespace FarmOS.SharedKernel.Infrastructure;

/// <summary>
/// ArangoDB implementation of the append-only event store.
/// Uses AQL transactions for optimistic concurrency on event appends.
/// </summary>
public sealed class ArangoEventStore : IEventStore
{
    private readonly IArangoDBClient _client;
    private readonly string _databaseName;

    public ArangoEventStore(IArangoDBClient client, string databaseName = "_system")
    {
        _client = client;
        _databaseName = databaseName;
    }

    public async Task<TAggregate> LoadAsync<TAggregate, TId>(
        string collectionName,
        string aggregateId,
        Func<TAggregate> factory,
        Func<string, string, IDomainEvent?> deserializer,
        CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull
    {
        var aql = @"
            FOR e IN @@collection
                FILTER e.AggregateId == @aggregateId
                SORT e.Version ASC
                RETURN e
        ";

        var cursor = await _client.Cursor.PostCursorAsync<EventEnvelope>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = collectionName,
                    ["aggregateId"] = aggregateId
                }
            });

        var aggregate = factory();
        var events = new List<IDomainEvent>();

        foreach (var envelope in cursor.Result)
        {
            var domainEvent = deserializer(envelope.EventType, envelope.Payload);
            if (domainEvent is not null)
                events.Add(domainEvent);
        }

        if (events.Count == 0)
            throw new InvalidOperationException($"Aggregate '{aggregateId}' not found in '{collectionName}'.");

        aggregate.Rehydrate(events);
        return aggregate;
    }

    public async Task AppendAsync(
        string collectionName,
        string aggregateId,
        string aggregateType,
        int expectedVersion,
        IReadOnlyList<IDomainEvent> events,
        string userId,
        string correlationId,
        string tenantId,
        Func<IDomainEvent, string> serializer,
        CancellationToken ct)
    {
        // First, verify optimistic concurrency
        var checkAql = @"
            LET currentV = FIRST(
                FOR e IN @@collection
                    FILTER e.AggregateId == @aggregateId
                    SORT e.Version DESC
                    LIMIT 1
                    RETURN e.Version
            )
            RETURN currentV == null ? 0 : currentV
        ";

        var versionCursor = await _client.Cursor.PostCursorAsync<int>(
            new PostCursorBody
            {
                Query = checkAql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = collectionName,
                    ["aggregateId"] = aggregateId
                }
            });

        var currentVersion = versionCursor.Result.FirstOrDefault();
        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(
                $"Expected version {expectedVersion} for aggregate '{aggregateId}', but found {currentVersion}.");
        }

        // Append all events
        var envelopes = new List<EventEnvelope>();
        var version = expectedVersion;
        var storedAt = DateTimeOffset.UtcNow;

        foreach (var @event in events)
        {
            version++;
            envelopes.Add(new EventEnvelope
            {
                _key = $"{aggregateId}:{version}",
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = @event.GetType().Name,
                Version = version,
                OccurredAt = @event.OccurredAt,
                StoredAt = storedAt,
                Payload = serializer(@event),
                CorrelationId = correlationId,
                UserId = userId,
                TenantId = tenantId
            });
        }

        // Insert all envelopes in batch
        var insertAql = @"
            FOR envelope IN @envelopes
                INSERT envelope INTO @@collection
        ";

        await _client.Cursor.PostCursorAsync<object>(
            new PostCursorBody
            {
                Query = insertAql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = collectionName,
                    ["envelopes"] = envelopes
                }
            });
    }

    public async Task<IReadOnlyList<EventEnvelope>> GetAllEventsAsync(
        string collectionName,
        long fromPosition,
        int batchSize,
        CancellationToken ct)
    {
        var aql = @"
            FOR e IN @@collection
                SORT e.Version ASC
                LIMIT @offset, @batchSize
                RETURN e
        ";

        var cursor = await _client.Cursor.PostCursorAsync<EventEnvelope>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = collectionName,
                    ["offset"] = fromPosition,
                    ["batchSize"] = batchSize
                }
            });

        return cursor.Result.ToList();
    }
}

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected during event append.
/// </summary>
public sealed class ConcurrencyException(string message) : Exception(message);
