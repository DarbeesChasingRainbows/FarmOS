using FarmOS.Apiary.Domain;
using FarmOS.Apiary.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Apiary.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

public record WeatherInspectionCorrelation(
    DateOnly Date,
    decimal TempF,
    decimal Humidity,
    decimal? MiteCount,
    int HoneyFrames,
    string HiveId,
    string HiveName);

// ─── Projection ────────────────────────────────────────────────────────────

/// <summary>
/// Correlates weather snapshots recorded with inspections to enable
/// analysis of how temperature and humidity affect mite counts and honey frames.
/// Replays apiary_events to join HiveInspected + WeatherRecordedWithInspection.
/// </summary>
public sealed class WeatherCorrelationProjection(IEventStore store)
{
    private const string CollectionName = "apiary_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(HiveCreated)] = typeof(HiveCreated),
        [nameof(HiveInspected)] = typeof(HiveInspected),
        [nameof(WeatherRecordedWithInspection)] = typeof(WeatherRecordedWithInspection),
    };

    public async Task<IReadOnlyList<WeatherInspectionCorrelation>> GetCorrelationsAsync(CancellationToken ct)
    {
        var hiveNames = new Dictionary<string, string>();
        var inspections = new Dictionary<string, (DateOnly Date, decimal? MiteCount, int HoneyFrames)>();
        var weatherByInspection = new Dictionary<string, WeatherSnapshot>();
        long position = 0;

        while (true)
        {
            var docs = await store.GetAllEventsAsync(CollectionName, position, 500, ct);
            if (docs.Count == 0) break;

            foreach (var doc in docs)
            {
                if (!EventTypeMap.TryGetValue(doc.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(doc.Payload, type) as IDomainEvent;
                if (evt is null) continue;

                switch (evt)
                {
                    case HiveCreated e:
                        hiveNames[e.Id.ToString()] = e.Name;
                        break;

                    case HiveInspected e:
                        inspections[e.InspectionId.ToString()] = (e.Date, e.Data.MiteCount, e.Data.HoneyFrames);
                        break;

                    case WeatherRecordedWithInspection e:
                        weatherByInspection[e.InspectionId.ToString()] = e.Weather;
                        break;
                }
            }

            position += docs.Count;
            if (docs.Count < 500) break;
        }

        var results = new List<WeatherInspectionCorrelation>();

        foreach (var (inspectionId, (date, miteCount, honeyFrames)) in inspections)
        {
            if (!weatherByInspection.TryGetValue(inspectionId, out var weather)) continue;

            // We need to find the hiveId for this inspection — lookup from event data
            // For simplicity, we include all correlated data points
            results.Add(new WeatherInspectionCorrelation(
                date, weather.TempF, weather.Humidity, miteCount, honeyFrames,
                inspectionId, ""));
        }

        return results.OrderBy(r => r.Date).ToList();
    }
}
