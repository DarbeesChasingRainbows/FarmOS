using FarmOS.Assets.Domain;
using FarmOS.Assets.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Assets.Infrastructure;

/// <summary>
/// Read-model projection: streams assets_events and builds an in-memory
/// EquipmentSummary list. In a larger system this would be a persistent
/// read-model store; for the vertical slice a per-request replay is fine.
/// </summary>
public sealed class EquipmentProjection(IEventStore store)
{
    private const string CollectionName = "assets_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(EquipmentRegistered)] = typeof(EquipmentRegistered),
        [nameof(EquipmentMaintenanceRecorded)] = typeof(EquipmentMaintenanceRecorded),
        [nameof(EquipmentMoved)] = typeof(EquipmentMoved),
        [nameof(EquipmentRetired)] = typeof(EquipmentRetired),
    };

    public async Task<IReadOnlyList<EquipmentSummary>> GetAllEquipmentAsync(CancellationToken ct)
    {
        var summaries = new Dictionary<string, EquipmentSummary>();

        long position = 0;
        const int batchSize = 500;

        while (true)
        {
            var batch = await store.GetAllEventsAsync(CollectionName, position, batchSize, ct);
            if (batch.Count == 0) break;

            foreach (var envelope in batch)
            {
                if (!EventTypeMap.TryGetValue(envelope.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(envelope.Payload, type) as IDomainEvent;
                if (evt is null) continue;

                switch (evt)
                {
                    case EquipmentRegistered e:
                        summaries[e.Id.Value.ToString()] = new EquipmentSummary(
                            e.Id.Value.ToString(), e.Name, e.Make, e.Model, e.Year,
                            "Active", 0, e.CurrentLocation.Latitude, e.CurrentLocation.Longitude);
                        break;

                    case EquipmentMaintenanceRecorded e:
                    {
                        var k = e.Id.Value.ToString();
                        if (summaries.TryGetValue(k, out var s))
                            summaries[k] = s with { MaintenanceCount = s.MaintenanceCount + 1 };
                        break;
                    }
                    case EquipmentMoved e:
                    {
                        var k = e.Id.Value.ToString();
                        if (summaries.TryGetValue(k, out var s))
                            summaries[k] = s with { Lat = e.NewLocation.Latitude, Lng = e.NewLocation.Longitude };
                        break;
                    }
                    case EquipmentRetired e:
                    {
                        var k = e.Id.Value.ToString();
                        if (summaries.TryGetValue(k, out var s))
                            summaries[k] = s with { Status = "Retired" };
                        break;
                    }
                }
            }

            position += batch.Count;
            if (batch.Count < batchSize) break;
        }

        return [.. summaries.Values];
    }
}

/// <summary>Read model for Equipment list view.</summary>
public record EquipmentSummary(
    string Id,
    string Name,
    string Make,
    string Model,
    int? Year,
    string Status,
    int MaintenanceCount,
    double Lat,
    double Lng);
