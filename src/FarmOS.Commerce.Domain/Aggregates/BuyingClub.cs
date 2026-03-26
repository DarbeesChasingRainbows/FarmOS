using FarmOS.Commerce.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Aggregates;

public sealed class BuyingClub : AggregateRoot<BuyingClubId>
{
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public OrderCycleFrequency Frequency { get; private set; }
    public ClubStatus Status { get; private set; }
    private readonly List<DropSite> _dropSites = [];
    public IReadOnlyList<DropSite> DropSites => _dropSites;
    public bool CurrentCycleOpen { get; private set; }
    public DateOnly? CurrentCycleDate { get; private set; }

    public static BuyingClub Create(string name, string? description, OrderCycleFrequency frequency)
    {
        var club = new BuyingClub();
        club.RaiseEvent(new BuyingClubCreated(BuyingClubId.New(), name, description, frequency, DateTimeOffset.UtcNow));
        return club;
    }

    public Result<BuyingClubId, DomainError> AddDropSite(DropSite site)
    {
        if (Status == ClubStatus.Closed)
            return DomainError.Conflict("Cannot add drop site to a closed buying club.");
        RaiseEvent(new DropSiteAdded(Id, site, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BuyingClubId, DomainError> RemoveDropSite(string siteName)
    {
        if (!_dropSites.Any(s => s.Name == siteName))
            return DomainError.NotFound("DropSite", siteName);
        RaiseEvent(new DropSiteRemoved(Id, siteName, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BuyingClubId, DomainError> OpenCycle(DateOnly cycleDate)
    {
        if (Status == ClubStatus.Closed)
            return DomainError.Conflict("Cannot open cycle for a closed buying club.");
        if (CurrentCycleOpen)
            return DomainError.Conflict("An order cycle is already open.");
        RaiseEvent(new OrderCycleOpened(Id, cycleDate, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BuyingClubId, DomainError> CloseCycle(DateOnly cycleDate)
    {
        if (!CurrentCycleOpen)
            return DomainError.Conflict("No order cycle is currently open.");
        RaiseEvent(new OrderCycleClosed(Id, cycleDate, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Pause(string? reason) =>
        RaiseEvent(new BuyingClubPaused(Id, reason, DateTimeOffset.UtcNow));

    public Result<BuyingClubId, DomainError> Close(string? reason)
    {
        if (Status == ClubStatus.Closed)
            return DomainError.Conflict("Buying club is already closed.");
        RaiseEvent(new BuyingClubClosed(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BuyingClubCreated e: Id = e.Id; Name = e.Name; Description = e.Description; Frequency = e.Frequency; Status = ClubStatus.Active; break;
            case DropSiteAdded e: _dropSites.Add(e.Site); break;
            case DropSiteRemoved e: _dropSites.RemoveAll(s => s.Name == e.SiteName); break;
            case OrderCycleOpened e: CurrentCycleOpen = true; CurrentCycleDate = e.CycleDate; break;
            case OrderCycleClosed: CurrentCycleOpen = false; CurrentCycleDate = null; break;
            case BuyingClubPaused: Status = ClubStatus.Paused; break;
            case BuyingClubClosed: Status = ClubStatus.Closed; break;
        }
    }
}
