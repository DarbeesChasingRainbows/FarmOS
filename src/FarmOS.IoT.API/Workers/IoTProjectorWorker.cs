using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;
using FarmOS.SharedKernel.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Background worker that polls iot_events and projects telemetry readings
/// and excursions into ArangoDB view collections for fast querying.
/// Follows the same pattern as PastureProjectorWorker.
/// </summary>
public sealed class IoTProjectorWorker : BackgroundService
{
    private readonly IArangoDBClient _arango;
    private readonly ILogger<IoTProjectorWorker> _logger;

    public IoTProjectorWorker(IArangoDBClient arango, ILogger<IoTProjectorWorker> logger)
    {
        _arango = arango;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IoT Projector Worker started.");

        var lastProcessed = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cursor = await _arango.Cursor.PostCursorAsync<EventDoc>(
                    new PostCursorBody
                    {
                        Query = @"
                            FOR e IN iot_events
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
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                foreach (var e in cursor.Result)
                {
                    await TryProjectEventAsync(e);
                    lastProcessed = e.StoredAt;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in IoT projection loop.");
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
                new PostCursorBody { Query = aql });

            _logger.LogDebug("Projected {EventType} for {AggregateId}", doc.EventType, doc.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to project {EventType} for {AggregateId}", doc.EventType, doc.AggregateId);
        }
    }

    private string BuildAqlForEvent(EventDoc doc)
    {
        if (doc.EventType == nameof(TelemetryReadingRecorded))
        {
            var e = Deserialize<TelemetryReadingRecorded>(doc.Payload);
            var key = $"{e.DeviceCode}_{e.Timestamp:yyyyMMddHHmmss}";
            return $@"
                INSERT {{
                    _key: '{key}',
                    DeviceId: '{e.DeviceId.Value}',
                    DeviceCode: '{e.DeviceCode}',
                    ZoneId: {(e.ZoneId != null ? $"'{e.ZoneId.Value}'" : "null")},
                    ZoneType: {(e.ZoneType != null ? $"'{e.ZoneType}'" : "null")},
                    SensorType: '{e.SensorType}',
                    Value: {e.Value},
                    Unit: '{e.Unit}',
                    Timestamp: '{e.Timestamp:O}'
                }}
                INTO iot_telemetry_view
                OPTIONS {{ overwriteMode: 'ignore' }}
            ";
        }

        if (doc.EventType == nameof(ExcursionStarted))
        {
            var e = Deserialize<ExcursionStarted>(doc.Payload);
            return $@"
                UPSERT {{ _key: '{e.Id.Value}' }}
                INSERT {{
                    _key: '{e.Id.Value}',
                    DeviceId: '{e.DeviceId.Value}',
                    ZoneId: '{e.ZoneId.Value}',
                    SensorType: '{e.SensorType}',
                    ThresholdLimit: {e.ThresholdLimit},
                    ThresholdDirection: '{e.ThresholdDirection}',
                    StartValue: {e.Value},
                    StartedAt: '{e.StartedAt:O}',
                    IsActive: true,
                    AlertFired: false
                }}
                UPDATE {{ }}
                IN iot_excursion_view
            ";
        }

        if (doc.EventType == nameof(ExcursionAlertFired))
        {
            var e = Deserialize<ExcursionAlertFired>(doc.Payload);
            return $@"
                UPDATE '{e.ExcursionId.Value}'
                WITH {{
                    AlertFired: true,
                    Severity: '{e.Severity}',
                    AlertMessage: '{EscapeAql(e.AlertMessage)}',
                    CorrectiveAction: '{EscapeAql(e.CorrectiveAction ?? "")}',
                    AlertFiredAt: '{e.FiredAt:O}'
                }}
                IN iot_excursion_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }

        if (doc.EventType == nameof(ExcursionEnded))
        {
            var e = Deserialize<ExcursionEnded>(doc.Payload);
            return $@"
                UPDATE '{e.Id.Value}'
                WITH {{
                    IsActive: false,
                    EndedAt: '{e.EndedAt:O}',
                    DurationSeconds: {e.Duration.TotalSeconds}
                }}
                IN iot_excursion_view
                OPTIONS {{ ignoreErrors: true }}
            ";
        }

        return string.Empty;
    }

    private static T Deserialize<T>(string payload) =>
        (T)MsgPackOptions.DeserializeFromBase64(payload, typeof(T))!;

    private static string EscapeAql(string s) =>
        s.Replace("'", "\\'").Replace("\n", " ");

    private record EventDoc
    {
        public string AggregateId { get; init; } = "";
        public string EventType { get; init; } = "";
        public DateTimeOffset StoredAt { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public string Payload { get; init; } = "";
    }
}
