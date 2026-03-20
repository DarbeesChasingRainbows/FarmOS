using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;
using FarmOS.IoT.Application;
using FarmOS.IoT.Application.Queries;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.IoT.Infrastructure;

public sealed class IoTProjection(IEventStore store) : IIoTProjection
{
    private const string CollectionName = "iot_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(DeviceRegistered)] = typeof(DeviceRegistered),
        [nameof(DeviceUpdated)] = typeof(DeviceUpdated),
        [nameof(DeviceDecommissioned)] = typeof(DeviceDecommissioned),
        [nameof(DeviceAssignedToZone)] = typeof(DeviceAssignedToZone),
        [nameof(DeviceUnassignedFromZone)] = typeof(DeviceUnassignedFromZone),
        [nameof(DeviceAssignedToAsset)] = typeof(DeviceAssignedToAsset),
        [nameof(DeviceUnassignedFromAsset)] = typeof(DeviceUnassignedFromAsset),

        [nameof(ZoneCreated)] = typeof(ZoneCreated),
        [nameof(ZoneUpdated)] = typeof(ZoneUpdated),
        [nameof(ZoneArchived)] = typeof(ZoneArchived)
    };

    private async Task<(Dictionary<string, DeviceState> Devices, Dictionary<string, ZoneState> Zones)> LoadAllStatesAsync(int batchSize, CancellationToken ct)
    {
        var devices = new Dictionary<string, DeviceState>();
        var zones = new Dictionary<string, ZoneState>();
        long position = 0;

        while (true)
        {
            var docs = await store.GetAllEventsAsync(CollectionName, position, batchSize, ct);
            if (docs.Count == 0) break;

            foreach (var doc in docs)
            {
                if (!EventTypeMap.TryGetValue(doc.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(doc.Payload, type) as IDomainEvent;
                if (evt is null) continue;
                
                ApplyToState(devices, zones, evt);
            }

            position += docs.Count;
            if (docs.Count < batchSize) break;
        }

        return (devices, zones);
    }

    public async Task<List<DeviceSummaryDto>> GetAllDevicesAsync(CancellationToken ct)
    {
        var (devices, _) = await LoadAllStatesAsync(500, ct);
        return devices.Values.Select(ToDeviceSummary).ToList();
    }

    public async Task<DeviceDetailDto?> GetDeviceDetailAsync(Guid deviceId, CancellationToken ct)
    {
        var (devices, _) = await LoadAllStatesAsync(500, ct);
        return devices.TryGetValue(deviceId.ToString(), out var state) ? ToDeviceDetail(state) : null;
    }

    public async Task<List<DeviceSummaryDto>> GetDevicesByZoneAsync(Guid zoneId, CancellationToken ct)
    {
        var (devices, _) = await LoadAllStatesAsync(500, ct);
        var zidStr = zoneId.ToString();
        return devices.Values.Where(d => d.ZoneId == zidStr).Select(ToDeviceSummary).ToList();
    }

    public async Task<List<ZoneSummaryDto>> GetAllZonesAsync(CancellationToken ct)
    {
        var (_, zones) = await LoadAllStatesAsync(500, ct);
        return zones.Values.Where(z => !z.IsArchived).Select(ToZoneSummary).ToList();
    }

    public async Task<ZoneDetailDto?> GetZoneDetailAsync(Guid zoneId, CancellationToken ct)
    {
        var (devices, zones) = await LoadAllStatesAsync(500, ct);
        var zidStr = zoneId.ToString();
        if (!zones.TryGetValue(zidStr, out var zoneState)) return null;

        var zoneDevices = devices.Values.Where(d => d.ZoneId == zidStr).Select(ToDeviceSummary).ToList();
        return ToZoneDetail(zoneState, zoneDevices);
    }

    // ─── State builder ──────────────────────────────────────────────────────

    private static void ApplyToState(Dictionary<string, DeviceState> devices, Dictionary<string, ZoneState> zones, IDomainEvent evt)
    {
        switch (evt)
        {
            // Device Events
            case DeviceRegistered dreg:
                devices[dreg.Id.ToString()] = new DeviceState
                {
                    Id = dreg.Id.ToString(),
                    DeviceCode = dreg.DeviceCode,
                    Name = dreg.Name,
                    SensorType = dreg.SensorType,
                    Status = DeviceStatus.Active,
                    ZoneId = dreg.ZoneId?.Value.ToString(),
                    GridPos = dreg.GridPos,
                    GeoPos = dreg.GeoPos,
                    Metadata = dreg.Metadata ?? new Dictionary<string, string>()
                };
                break;
            case DeviceUpdated dup when devices.TryGetValue(dup.Id.ToString(), out var dev):
                dev.Name = dup.Name;
                dev.Status = dup.Status;
                if (dup.Metadata != null)
                {
                    foreach(var kv in dup.Metadata) dev.Metadata[kv.Key] = kv.Value;
                }
                break;
            case DeviceDecommissioned ddec when devices.TryGetValue(ddec.Id.ToString(), out var dev):
                dev.Status = DeviceStatus.Decommissioned;
                break;
            case DeviceAssignedToZone daz when devices.TryGetValue(daz.DeviceId.ToString(), out var dev):
                dev.ZoneId = daz.ZoneId.Value.ToString();
                dev.GridPos = daz.GridPos;
                dev.GeoPos = daz.GeoPos;
                break;
            case DeviceUnassignedFromZone duz when devices.TryGetValue(duz.DeviceId.ToString(), out var dev):
                dev.ZoneId = null;
                // Keep last known position but maybe null it in the future
                break;
            case DeviceAssignedToAsset daa when devices.TryGetValue(daa.DeviceId.ToString(), out var dev):
                dev.Assignments.Add(daa.Assignment);
                break;
            case DeviceUnassignedFromAsset dua when devices.TryGetValue(dua.DeviceId.ToString(), out var dev):
                dev.Assignments.RemoveAll(a => a.Asset.Context == dua.Context && a.Asset.AssetType == dua.AssetType && a.Asset.AssetId == dua.AssetId);
                break;

            // Zone Events
            case ZoneCreated zc:
                zones[zc.Id.ToString()] = new ZoneState
                {
                    Id = zc.Id.ToString(),
                    Name = zc.Name,
                    ZoneType = zc.ZoneType,
                    Description = zc.Description,
                    GridPos = zc.GridPos,
                    GeoPos = zc.GeoPos,
                    ParentZoneId = zc.ParentZoneId?.Value.ToString()
                };
                break;
            case ZoneUpdated zu when zones.TryGetValue(zu.Id.ToString(), out var zone):
                zone.Name = zu.Name;
                zone.Description = zu.Description;
                break;
            case ZoneArchived za when zones.TryGetValue(za.Id.ToString(), out var zone):
                zone.IsArchived = true;
                break;
        }
    }

    // ─── Mapping ────────────────────────────────────────────────────────────

    private static DeviceSummaryDto ToDeviceSummary(DeviceState d) => new(
        Guid.Parse(d.Id), d.DeviceCode, d.Name, d.SensorType, d.Status,
        d.ZoneId != null ? Guid.Parse(d.ZoneId) : null);

    private static DeviceDetailDto ToDeviceDetail(DeviceState d) => new(
        Guid.Parse(d.Id), d.DeviceCode, d.Name, d.SensorType, d.Status,
        d.ZoneId != null ? Guid.Parse(d.ZoneId) : null,
        d.GridPos, d.GeoPos, d.Assignments, d.Metadata);

    private static ZoneSummaryDto ToZoneSummary(ZoneState z) => new(
        Guid.Parse(z.Id), z.Name, z.ZoneType,
        z.ParentZoneId != null ? Guid.Parse(z.ParentZoneId) : null);

    private static ZoneDetailDto ToZoneDetail(ZoneState z, List<DeviceSummaryDto> devices) => new(
        Guid.Parse(z.Id), z.Name, z.ZoneType, z.Description,
        z.GridPos, z.GeoPos,
        z.ParentZoneId != null ? Guid.Parse(z.ParentZoneId) : null,
        z.IsArchived, devices);

    // ─── Mutable state helpers ──────────────────────────────────────────────

    private sealed class DeviceState
    {
        public string Id { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string Name { get; set; } = "";
        public SensorType SensorType { get; set; }
        public DeviceStatus Status { get; set; }
        public string? ZoneId { get; set; }
        public GridPosition? GridPos { get; set; }
        public GeoPosition? GeoPos { get; set; }
        public List<DeviceAssignment> Assignments { get; set; } = [];
        public Dictionary<string, string> Metadata { get; set; } = [];
    }

    private sealed class ZoneState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ZoneType ZoneType { get; set; }
        public string? Description { get; set; }
        public GridPosition? GridPos { get; set; }
        public GeoPosition? GeoPos { get; set; }
        public string? ParentZoneId { get; set; }
        public bool IsArchived { get; set; }
    }
}
