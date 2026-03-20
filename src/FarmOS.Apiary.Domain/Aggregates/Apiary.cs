using FarmOS.Apiary.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Apiary.Domain.Aggregates;

public sealed class Apiary : AggregateRoot<ApiaryId>
{
    public string Name { get; private set; } = "";
    public GeoPosition Position { get; private set; } = new(0, 0);
    public int MaxCapacity { get; private set; }
    public string? Notes { get; private set; }
    public ApiaryStatus Status { get; private set; }
    private readonly List<HiveId> _hiveIds = [];
    public IReadOnlyList<HiveId> HiveIds => _hiveIds;

    public static Apiary Create(string name, GeoPosition position, int maxCapacity, string? notes)
    {
        var apiary = new Apiary();
        apiary.RaiseEvent(new ApiaryCreated(ApiaryId.New(), name, position, maxCapacity, notes, DateTimeOffset.UtcNow));
        return apiary;
    }

    public Result<ApiaryId, DomainError> AddHive(HiveId hiveId)
    {
        if (Status == ApiaryStatus.Retired)
            return DomainError.Conflict("Cannot add hive to a retired apiary.");
        if (_hiveIds.Count >= MaxCapacity)
            return DomainError.Conflict($"Apiary is at maximum capacity ({MaxCapacity}).");
        if (_hiveIds.Contains(hiveId))
            return DomainError.Conflict("Hive is already in this apiary.");
        RaiseEvent(new HiveMovedToApiary(hiveId, null, Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public void RemoveHive(HiveId hiveId)
    {
        if (_hiveIds.Contains(hiveId))
            RaiseEvent(new HiveRemovedFromApiary(hiveId, Id, DateTimeOffset.UtcNow));
    }

    public Result<ApiaryId, DomainError> Retire(string reason)
    {
        if (_hiveIds.Count > 0)
            return DomainError.Conflict("Cannot retire apiary with assigned hives. Move or remove them first.");
        RaiseEvent(new ApiaryRetired(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ApiaryCreated e: Id = e.Id; Name = e.Name; Position = e.Position; MaxCapacity = e.MaxCapacity; Notes = e.Notes; Status = ApiaryStatus.Active; break;
            case HiveMovedToApiary e: if (!_hiveIds.Contains(e.HiveId)) _hiveIds.Add(e.HiveId); break;
            case HiveRemovedFromApiary e: _hiveIds.Remove(e.HiveId); break;
            case ApiaryRetired: Status = ApiaryStatus.Retired; break;
        }
    }
}
