using FarmOS.SharedKernel;
using FarmOS.IoT.Domain.Events;

namespace FarmOS.IoT.Domain.Aggregates;

/// <summary>
/// Aggregate root representing a physical layout Zone for devices to be placed.
/// </summary>
public class Zone : AggregateRoot<ZoneId>
{
    public string Name { get; private set; } = string.Empty;
    public ZoneType ZoneType { get; private set; }
    public string? Description { get; private set; }
    public GridPosition? GridPos { get; private set; }
    public GeoPosition? GeoPos { get; private set; }
    public ZoneId? ParentZoneId { get; private set; }

    public bool IsArchived { get; private set; }

    // Required by event sourcing for rehydration
    private Zone() { }

    public static Zone Create(
        ZoneId id,
        string name,
        ZoneType zoneType,
        string? description = null,
        GridPosition? gridPos = null,
        GeoPosition? geoPos = null,
        ZoneId? parentZoneId = null)
    {
        var zone = new Zone();
        zone.RaiseEvent(new ZoneCreated(
            id,
            name,
            zoneType,
            description,
            gridPos,
            geoPos,
            parentZoneId,
            DateTimeOffset.UtcNow));
            
        return zone;
    }

    public void Update(string name, string? description)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot update an archived zone.");

        RaiseEvent(new ZoneUpdated(Id, name, description, DateTimeOffset.UtcNow));
    }

    public void Archive(string reason)
    {
        if (IsArchived) return;
        
        RaiseEvent(new ZoneArchived(Id, reason, DateTimeOffset.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ZoneCreated e:
                Id = e.Id;
                Name = e.Name;
                ZoneType = e.ZoneType;
                Description = e.Description;
                GridPos = e.GridPos;
                GeoPos = e.GeoPos;
                ParentZoneId = e.ParentZoneId;
                IsArchived = false;
                break;
                
            case ZoneUpdated e:
                Name = e.Name;
                Description = e.Description;
                break;
                
            case ZoneArchived:
                IsArchived = true;
                break;
        }
    }
}
