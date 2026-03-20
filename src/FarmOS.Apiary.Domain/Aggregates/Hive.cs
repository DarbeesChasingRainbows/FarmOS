using FarmOS.Apiary.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Apiary.Domain.Aggregates;

public sealed class Hive : AggregateRoot<HiveId>
{
    public string Name { get; private set; } = "";
    public HiveType Type { get; private set; }
    public GeoPosition Position { get; private set; } = new(0, 0);
    public DateOnly Established { get; private set; }
    public HiveStatus Status { get; private set; }
    public ApiaryId? ApiaryId { get; private set; }
    public QueenRecord? Queen { get; private set; }
    private readonly List<(InspectionId Id, InspectionData Data, DateOnly Date)> _inspections = [];
    public IReadOnlyList<(InspectionId Id, InspectionData Data, DateOnly Date)> Inspections => _inspections;
    private readonly List<HarvestData> _harvests = [];
    public IReadOnlyList<HarvestData> Harvests => _harvests;
    private readonly List<FeedingData> _feedings = [];
    public IReadOnlyList<FeedingData> Feedings => _feedings;
    private readonly List<ProductHarvestData> _productHarvests = [];
    public IReadOnlyList<ProductHarvestData> ProductHarvests => _productHarvests;

    public static Hive Create(string name, HiveType type, GeoPosition position, DateOnly established)
    {
        var hive = new Hive();
        hive.RaiseEvent(new HiveCreated(HiveId.New(), name, type, position, established, DateTimeOffset.UtcNow));
        return hive;
    }

    public InspectionId Inspect(InspectionData data, DateOnly date)
    {
        var id = InspectionId.New();
        RaiseEvent(new HiveInspected(Id, id, data, date, DateTimeOffset.UtcNow));
        return id;
    }

    public Result<HiveId, DomainError> HarvestHoney(HarvestData data, DateOnly date)
    {
        if (Status is HiveStatus.Dead or HiveStatus.Swarmed)
            return DomainError.Conflict($"Cannot harvest from a hive with status '{Status}'.");
        RaiseEvent(new HoneyHarvested(Id, data, date, DateTimeOffset.UtcNow));
        return Id;
    }

    public void RecordTreatment(TreatmentData data) =>
        RaiseEvent(new HiveTreated(Id, data, DateTimeOffset.UtcNow));

    public void ChangeStatus(HiveStatus next, string reason) =>
        RaiseEvent(new HiveStatusChanged(Id, Status, next, reason, DateTimeOffset.UtcNow));

    // ─── Feature 2: Queen Tracking ──────────────────────────────────
    public Result<HiveId, DomainError> IntroduceQueen(QueenRecord queen)
    {
        if (Queen is not null)
            return DomainError.Conflict("Hive already has a queen. Use ReplaceQueen instead.");
        RaiseEvent(new QueenIntroduced(Id, queen, DateTimeOffset.UtcNow));
        if (Status == HiveStatus.Queenless)
            RaiseEvent(new HiveStatusChanged(Id, Status, HiveStatus.Active, "Queen introduced", DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<HiveId, DomainError> MarkQueenLost(string reason, DateOnly date)
    {
        if (Queen is null)
            return DomainError.Conflict("Hive has no queen to mark as lost.");
        RaiseEvent(new QueenLost(Id, reason, date, DateTimeOffset.UtcNow));
        RaiseEvent(new HiveStatusChanged(Id, Status, HiveStatus.Queenless, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public void ReplaceQueen(QueenRecord newQueen, string reason) =>
        RaiseEvent(new QueenReplaced(Id, newQueen, reason, DateTimeOffset.UtcNow));

    // ─── Feature 3: Feeding ─────────────────────────────────────────
    public Result<HiveId, DomainError> Feed(FeedingData data)
    {
        if (Status == HiveStatus.Dead)
            return DomainError.Conflict("Cannot feed a dead hive.");
        RaiseEvent(new HiveFed(Id, data, DateTimeOffset.UtcNow));
        return Id;
    }

    // ─── Feature 6: Multi-Product Harvest ──────────────────────────
    public Result<HiveId, DomainError> RecordProductHarvest(ProductHarvestData data)
    {
        if (Status is HiveStatus.Dead or HiveStatus.Swarmed)
            return DomainError.Conflict($"Cannot harvest from a hive with status '{Status}'.");
        RaiseEvent(new ProductHarvested(Id, data, DateTimeOffset.UtcNow));
        return Id;
    }

    // ─── Feature 4: Colony Splitting & Merging ──────────────────────
    public Result<HiveId, DomainError> Split(string newHiveName, HiveType newHiveType, GeoPosition newPosition, DateOnly date)
    {
        if (Status is HiveStatus.Dead or HiveStatus.Swarmed)
            return DomainError.Conflict($"Cannot split a hive with status '{Status}'.");
        var newHiveId = HiveId.New();
        RaiseEvent(new ColonySplit(Id, newHiveId, newHiveName, newHiveType, newPosition, date, DateTimeOffset.UtcNow));
        return newHiveId;
    }

    public Result<HiveId, DomainError> AbsorbColony(HiveId weakHiveId, DateOnly date)
    {
        if (Status == HiveStatus.Dead)
            return DomainError.Conflict("Cannot merge into a dead hive.");
        if (weakHiveId == Id)
            return DomainError.Conflict("Cannot merge a hive with itself.");
        RaiseEvent(new ColoniesMerged(Id, weakHiveId, date, DateTimeOffset.UtcNow));
        return Id;
    }

    // ─── Feature 5: Equipment/Super Tracking ────────────────────────
    public HiveConfiguration? Configuration { get; private set; }

    public void UpdateConfiguration(HiveConfiguration config) =>
        RaiseEvent(new HiveConfigurationChanged(Id, config, DateTimeOffset.UtcNow));

    public void AddSuper()
    {
        var count = (Configuration?.HoneySupers ?? 0) + 1;
        RaiseEvent(new SuperAdded(Id, count, DateTimeOffset.UtcNow));
    }

    public Result<HiveId, DomainError> RemoveSuper()
    {
        var count = Configuration?.HoneySupers ?? 0;
        if (count <= 0)
            return DomainError.Conflict("No supers to remove.");
        RaiseEvent(new SuperRemoved(Id, count - 1, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case HiveCreated e: Id = e.Id; Name = e.Name; Type = e.Type; Position = e.Position; Established = e.Established; Status = HiveStatus.Active; break;
            case HiveInspected e: _inspections.Add((e.InspectionId, e.Data, e.Date)); break;
            case HoneyHarvested e: _harvests.Add(e.Data); break;
            case HiveStatusChanged e: Status = e.Next; break;
            case HiveSwarmed: Status = HiveStatus.Swarmed; break;
            case HiveMovedToApiary e: ApiaryId = e.NewApiaryId; break;
            case HiveRemovedFromApiary: ApiaryId = null; break;
            case QueenIntroduced e: Queen = e.Queen; break;
            case QueenLost: Queen = null; break;
            case QueenReplaced e: Queen = e.NewQueen; break;
            case HiveFed e: _feedings.Add(e.Data); break;
            case ProductHarvested e: _productHarvests.Add(e.Data); break;
            case SuperAdded e: Configuration = Configuration is null ? new(1, e.NewSuperCount, FrameType.LangstrothDeep, false) : Configuration with { HoneySupers = e.NewSuperCount }; break;
            case SuperRemoved e: Configuration = Configuration! with { HoneySupers = e.NewSuperCount }; break;
            case HiveConfigurationChanged e: Configuration = e.Config; break;
        }
    }
}
