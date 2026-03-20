using FarmOS.SharedKernel;
using FarmOS.IoT.Domain.Events;

namespace FarmOS.IoT.Domain.Aggregates;

/// <summary>
/// Aggregate root representing an IoT Device.
/// Implemented using modern C# 13 features and primary constructor patterns where appropriate.
/// </summary>
public class IoTDevice : AggregateRoot<IoTDeviceId>
{
    public string DeviceCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public SensorType SensorType { get; private set; }
    public DeviceStatus Status { get; private set; }
    
    public ZoneId? ZoneId { get; private set; }
    public GridPosition? GridPos { get; private set; }
    public GeoPosition? GeoPos { get; private set; }
    
    private readonly List<DeviceAssignment> _assignments = [];
    public IReadOnlyList<DeviceAssignment> Assignments => _assignments;

    private readonly Dictionary<string, string> _metadata = [];
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    // Required by event sourcing for rehydration
    private IoTDevice() { }

    public static IoTDevice Register(
        IoTDeviceId id,
        string deviceCode,
        string name,
        SensorType sensorType,
        ZoneId? zoneId = null,
        GridPosition? gridPos = null,
        GeoPosition? geoPos = null,
        Dictionary<string, string>? metadata = null)
    {
        var device = new IoTDevice();
        device.RaiseEvent(new DeviceRegistered(
            id,
            deviceCode,
            name,
            sensorType,
            DeviceStatus.Active,
            zoneId,
            gridPos,
            geoPos,
            metadata ?? [],
            DateTimeOffset.UtcNow));
            
        return device;
    }

    public void Update(string name, DeviceStatus status, Dictionary<string, string>? metadata = null)
    {
        if (Status == DeviceStatus.Decommissioned)
            throw new InvalidOperationException("Cannot update a decommissioned device.");

        RaiseEvent(new DeviceUpdated(Id, name, status, metadata ?? [], DateTimeOffset.UtcNow));
    }

    public void Decommission(string reason)
    {
        if (Status == DeviceStatus.Decommissioned) return;
        
        RaiseEvent(new DeviceDecommissioned(Id, reason, DateTimeOffset.UtcNow));
    }

    public void AssignToZone(ZoneId zoneId, GridPosition? gridPos = null, GeoPosition? geoPos = null)
    {
        if (Status == DeviceStatus.Decommissioned)
            throw new InvalidOperationException("Cannot assign a decommissioned device to a zone.");

        RaiseEvent(new DeviceAssignedToZone(Id, zoneId, gridPos, geoPos, DateTimeOffset.UtcNow));
    }

    public void UnassignFromZone()
    {
        if (ZoneId is null) return;
        
        RaiseEvent(new DeviceUnassignedFromZone(Id, ZoneId, DateTimeOffset.UtcNow));
    }

    public void AssignToAsset(AssetRef assetRef)
    {
        if (Status == DeviceStatus.Decommissioned)
            throw new InvalidOperationException("Cannot assign a decommissioned device to an asset.");

        // Check if already assigned
        if (_assignments.Any(a => a.Asset.Context == assetRef.Context 
                               && a.Asset.AssetType == assetRef.AssetType 
                               && a.Asset.AssetId == assetRef.AssetId))
        {
            return;
        }

        var assignment = new DeviceAssignment(assetRef, DateTimeOffset.UtcNow);
        RaiseEvent(new DeviceAssignedToAsset(Id, assignment, DateTimeOffset.UtcNow));
    }

    public void UnassignFromAsset(string context, string assetType, Guid assetId)
    {
        var exists = _assignments.Any(a => a.Asset.Context == context 
                                        && a.Asset.AssetType == assetType 
                                        && a.Asset.AssetId == assetId);
        if (!exists) return;

        RaiseEvent(new DeviceUnassignedFromAsset(Id, context, assetType, assetId, DateTimeOffset.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case DeviceRegistered e:
                Id = e.Id;
                DeviceCode = e.DeviceCode;
                Name = e.Name;
                SensorType = e.SensorType;
                Status = e.Status;
                ZoneId = e.ZoneId;
                GridPos = e.GridPos;
                GeoPos = e.GeoPos;
                if (e.Metadata is not null)
                {
                    foreach (var kvp in e.Metadata) _metadata[kvp.Key] = kvp.Value;
                }
                break;
                
            case DeviceUpdated e:
                Name = e.Name;
                Status = e.Status;
                if (e.Metadata is not null)
                {
                    _metadata.Clear();
                    foreach (var kvp in e.Metadata) _metadata[kvp.Key] = kvp.Value;
                }
                break;
                
            case DeviceDecommissioned e:
                Status = DeviceStatus.Decommissioned;
                break;
                
            case DeviceAssignedToZone e:
                ZoneId = e.ZoneId;
                if (e.GridPos is not null) GridPos = e.GridPos;
                if (e.GeoPos is not null) GeoPos = e.GeoPos;
                break;
                
            case DeviceUnassignedFromZone:
                ZoneId = null;
                GridPos = null;
                GeoPos = null;
                break;
                
            case DeviceAssignedToAsset e:
                _assignments.Add(e.Assignment);
                break;
                
            case DeviceUnassignedFromAsset e:
                _assignments.RemoveAll(a => a.Asset.Context == e.Context 
                                         && a.Asset.AssetType == e.AssetType 
                                         && a.Asset.AssetId == e.AssetId);
                break;
        }
    }
}
