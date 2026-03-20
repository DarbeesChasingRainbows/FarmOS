using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;
using FarmOS.IoT.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.IoT.Infrastructure;

/// <summary>
/// Lightweight lookup that resolves device info by code from the event stream.
/// Used during telemetry ingestion to map a device code → device ID + zone info.
/// </summary>
public sealed class IoTProjectionLookup(IEventStore store) : IIoTProjectionLookup
{
    private const string CollectionName = "iot_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(DeviceRegistered)] = typeof(DeviceRegistered),
        [nameof(DeviceUpdated)] = typeof(DeviceUpdated),
        [nameof(DeviceDecommissioned)] = typeof(DeviceDecommissioned),
        [nameof(DeviceAssignedToZone)] = typeof(DeviceAssignedToZone),
        [nameof(DeviceUnassignedFromZone)] = typeof(DeviceUnassignedFromZone),
        [nameof(ZoneCreated)] = typeof(ZoneCreated),
    };

    public async Task<DeviceLookupDto?> GetDeviceByCodeAsync(string deviceCode, CancellationToken ct)
    {
        var devices = new Dictionary<string, DeviceInfo>();
        var zones = new Dictionary<string, ZoneType>();
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
                    case DeviceRegistered dreg:
                        devices[dreg.DeviceCode] = new DeviceInfo
                        {
                            Id = dreg.Id.Value,
                            DeviceCode = dreg.DeviceCode,
                            Name = dreg.Name,
                            SensorType = dreg.SensorType,
                            ZoneId = dreg.ZoneId?.Value,
                        };
                        break;
                    case DeviceUpdated dup:
                        var key = devices.Values.FirstOrDefault(d => d.Id == dup.Id.Value);
                        if (key is not null) key.Name = dup.Name;
                        break;
                    case DeviceAssignedToZone daz:
                        var dev = devices.Values.FirstOrDefault(d => d.Id == daz.DeviceId.Value);
                        if (dev is not null) dev.ZoneId = daz.ZoneId.Value;
                        break;
                    case DeviceUnassignedFromZone duz:
                        var dev2 = devices.Values.FirstOrDefault(d => d.Id == duz.DeviceId.Value);
                        if (dev2 is not null) dev2.ZoneId = null;
                        break;
                    case ZoneCreated zc:
                        zones[zc.Id.Value.ToString()] = zc.ZoneType;
                        break;
                }
            }

            position += docs.Count;
            if (docs.Count < 500) break;
        }

        if (!devices.TryGetValue(deviceCode, out var found))
            return null;

        ZoneType? zoneType = found.ZoneId.HasValue && zones.TryGetValue(found.ZoneId.Value.ToString(), out var zt)
            ? zt : null;

        return new DeviceLookupDto(found.Id, found.DeviceCode, found.Name, found.SensorType, found.ZoneId, zoneType);
    }

    private sealed class DeviceInfo
    {
        public Guid Id { get; set; }
        public string DeviceCode { get; set; } = "";
        public string Name { get; set; } = "";
        public SensorType SensorType { get; set; }
        public Guid? ZoneId { get; set; }
    }
}
