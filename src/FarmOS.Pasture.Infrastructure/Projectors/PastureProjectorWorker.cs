using System.Text.Json;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.Pasture.Domain.Events;
using FarmOS.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.Pasture.Infrastructure.Projectors;

/// <summary>
/// Background worker that actively listens to the pasture_events collection
/// and updates the read-model projection collections in ArangoDB.
/// Provides eventual consistency, isolating costly write paths from blazing fast queries.
/// </summary>
public sealed class PastureProjectorWorker : BackgroundService
{
    private readonly IArangoDBClient _arango;
    private readonly ILogger<PastureProjectorWorker> _logger;
    // Kept for AQL JSON literal embedding (e.g., tag arrays in UPSERT statements)
    private static readonly JsonSerializerOptions AqlJsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PastureProjectorWorker(IArangoDBClient arango, ILogger<PastureProjectorWorker> logger)
    {
        _arango = arango;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FarmOS Pasture Projector started.");

        // For simplicity in this non-distributed local scenario, 
        // we start from the beginning of time and use UPSERTs to ensure idempotency.
        // In reality, you would load this from a 'system_cursors' collection.
        var lastProcessed = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cursor = await _arango.Cursor.PostCursorAsync<EventDoc>(
                    new PostCursorBody
                    {
                        Query = @"
                            FOR e IN pasture_events
                                FILTER e.StoredAt > @lastProcessed
                                SORT e.StoredAt ASC, e.Version ASC
                                LIMIT 100
                                RETURN e
                        ",
                        BindVars = new Dictionary<string, object>
                        {
                            ["lastProcessed"] = lastProcessed.ToString("O")
                        }
                    });

                if (!cursor.Result.Any())
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var e in cursor.Result)
                {
                    await TryProjectEventAsync(e);
                    lastProcessed = e.StoredAt;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Pasture projection loop.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task TryProjectEventAsync(EventDoc doc)
    {
        try
        {
            var aql = BuildAqlForEvent(doc);
            if (string.IsNullOrEmpty(aql)) return;

            await _arango.Cursor.PostCursorAsync<object>(
                new PostCursorBody
                {
                    Query = aql
                });

            _logger.LogInformation("Projected {EventType} for {AggregateId}", doc.EventType, doc.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to project {EventType} for {AggregateId}", doc.EventType, doc.AggregateId);
        }
    }

    private string BuildAqlForEvent(EventDoc doc)
    {
        // Paddock Updates
        if (doc.EventType == nameof(PaddockCreated))
        {
            var e = Deserialize<PaddockCreated>(doc.Payload);
            return $@"
                UPSERT {{ _key: '{doc.AggregateId}' }}
                INSERT {{ _key: '{doc.AggregateId}', Name: '{e.Name}', Acreage: {e.Size.Value}, LandType: '{e.LandType}', Status: 'Resting', RestDaysElapsed: 0 }}
                UPDATE {{ Name: '{e.Name}' }}
                IN pasture_paddock_view
            ";
        }
        if (doc.EventType == nameof(GrazingStarted))
        {
            var e = Deserialize<GrazingStarted>(doc.Payload);
            return $@"
                UPDATE '{doc.AggregateId}'
                WITH {{ Status: 'BeingGrazed', CurrentHerdId: '{e.HerdId.Value}', RestDaysElapsed: 0 }}
                IN pasture_paddock_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }
        if (doc.EventType == nameof(GrazingEnded))
        {
            return $@"
                UPDATE '{doc.AggregateId}'
                WITH {{ Status: 'Resting', CurrentHerdId: null, LastGrazedEnded: '{doc.OccurredAt:O}' }}
                IN pasture_paddock_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }

        // Animal Updates
        if (doc.EventType == nameof(AnimalRegistered))
        {
            var e = Deserialize<AnimalRegistered>(doc.Payload);
            var tagsJson = JsonSerializer.Serialize(e.Tags, AqlJsonOpts);
            return $@"
                UPSERT {{ _key: '{doc.AggregateId}' }}
                INSERT {{ _key: '{doc.AggregateId}', Species: '{e.Species}', Breed: '{e.Breed}', Sex: '{e.Sex}', DateAcquired: '{e.DateAcquired:O}', Nickname: '{e.Nickname}', Status: 'Active', Tags: {tagsJson} }}
                UPDATE {{}}
                IN pasture_animal_view
            ";
        }
        if (doc.EventType == nameof(AnimalIsolated))
        {
            return $@"
                UPDATE '{doc.AggregateId}'
                WITH {{ Status: 'Isolated' }}
                IN pasture_animal_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }
        if (doc.EventType == nameof(AnimalButchered))
        {
            return $@"
                UPDATE '{doc.AggregateId}'
                WITH {{ Status: 'Butchered' }}
                IN pasture_animal_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }

        // Herd Updates
        if (doc.EventType == nameof(HerdCreated))
        {
            var e = Deserialize<HerdCreated>(doc.Payload);
            return $@"
                UPSERT {{ _key: '{doc.AggregateId}' }}
                INSERT {{ _key: '{doc.AggregateId}', Name: '{e.Name}', Type: '{e.Type}', MemberCount: 0 }}
                UPDATE {{ Name: '{e.Name}' }}
                IN pasture_herd_view
            ";
        }
        if (doc.EventType == nameof(HerdMoved))
        {
            var e = Deserialize<HerdMoved>(doc.Payload);
            return $@"
                UPDATE '{doc.AggregateId}'
                WITH {{ CurrentPaddockId: '{e.ToPaddockId.Value}' }}
                IN pasture_herd_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }
        if (doc.EventType == nameof(AnimalAddedToHerd))
        {
            var e = Deserialize<AnimalAddedToHerd>(doc.Payload);
            return $@"
                // Update animal
                UPDATE '{e.AnimalId.Value}' WITH {{ CurrentHerdId: '{doc.AggregateId}' }} IN pasture_animal_view OPTIONS {{ ignoreErrors: true }}
                
                // Increment herd count via edge tracking or just let view handle it.
                // We'll keep it simple for now: read the current herd and update count.
                LET doc = DOCUMENT('pasture_herd_view', '{doc.AggregateId}')
                UPDATE doc WITH {{ MemberCount: doc.MemberCount + 1 }} IN pasture_herd_view OPTIONS {{ ignoreErrors: true }}

                // Create graph edge
                UPSERT {{ _from: 'pasture_animal_view/{e.AnimalId.Value}', _to: 'pasture_herd_view/{doc.AggregateId}' }}
                INSERT {{ _from: 'pasture_animal_view/{e.AnimalId.Value}', _to: 'pasture_herd_view/{doc.AggregateId}' }}
                UPDATE {{}}
                IN belongs_to OPTIONS {{ ignoreErrors: true }}
            ";
        }
        if (doc.EventType == nameof(AnimalRemovedFromHerd))
        {
            var e = Deserialize<AnimalRemovedFromHerd>(doc.Payload);
            return $@"
                // Update animal
                UPDATE '{e.AnimalId.Value}' WITH {{ CurrentHerdId: null }} IN pasture_animal_view OPTIONS {{ ignoreErrors: true }}
                
                // Decrement herd count
                LET doc = DOCUMENT('pasture_herd_view', '{doc.AggregateId}')
                UPDATE doc WITH {{ MemberCount: doc.MemberCount > 0 ? doc.MemberCount - 1 : 0 }} IN pasture_herd_view OPTIONS {{ ignoreErrors: true }}

                // Remove graph edge
                FOR edge IN belongs_to
                    FILTER edge._from == 'pasture_animal_view/{e.AnimalId.Value}' AND edge._to == 'pasture_herd_view/{doc.AggregateId}'
                    REMOVE edge IN belongs_to OPTIONS {{ ignoreErrors: true }}
            ";
        }

        return string.Empty;
    }

    private static T Deserialize<T>(string payload) =>
        (T)MsgPackOptions.DeserializeFromBase64(payload, typeof(T))!;

    private record EventDoc
    {
        public string AggregateId { get; init; } = "";
        public string EventType { get; init; } = "";
        public DateTimeOffset StoredAt { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public string Payload { get; init; } = "";
    }
}
