using FarmOS.Compliance.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Aggregates;

public sealed class Grant : AggregateRoot<GrantId>
{
    public string Name { get; private set; } = string.Empty;
    public string Grantor { get; private set; } = string.Empty;
    public decimal RequestedAmount { get; private set; }
    public decimal? AwardedAmount { get; private set; }
    public GrantStatus Status { get; private set; }
    public DateOnly ApplicationDate { get; private set; }
    private readonly List<GrantMilestone> _milestones = [];
    public IReadOnlyList<GrantMilestone> Milestones => _milestones;

    public static Grant Apply(string name, string grantor, decimal amount, DateOnly applicationDate, string? notes)
    {
        var grant = new Grant();
        grant.RaiseEvent(new GrantApplied(GrantId.New(), name, grantor, amount, applicationDate, notes, DateTimeOffset.UtcNow));
        return grant;
    }

    public Result<GrantId, DomainError> Award(decimal awardedAmount, DateOnly awardDate)
    {
        if (Status != GrantStatus.Applied)
            return DomainError.Conflict("Grant can only be awarded when in Applied status.");
        RaiseEvent(new GrantAwarded(Id, awardedAmount, awardDate, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<GrantId, DomainError> Deny(string? reason)
    {
        if (Status != GrantStatus.Applied)
            return DomainError.Conflict("Grant can only be denied when in Applied status.");
        RaiseEvent(new GrantDenied(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<GrantId, DomainError> AddMilestone(GrantMilestone milestone)
    {
        if (Status is GrantStatus.Denied or GrantStatus.Closed)
            return DomainError.Conflict("Cannot add milestones to a denied or closed grant.");
        RaiseEvent(new GrantMilestoneAdded(Id, milestone, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<GrantId, DomainError> CompleteMilestone(string description, string? reportPath)
    {
        var milestone = _milestones.Find(m => m.Description == description);
        if (milestone is null)
            return DomainError.NotFound("GrantMilestone", description);
        if (milestone.Completed)
            return DomainError.Conflict("Milestone is already completed.");
        RaiseEvent(new GrantMilestoneCompleted(Id, description, reportPath, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<GrantId, DomainError> Close()
    {
        if (Status == GrantStatus.Closed)
            return DomainError.Conflict("Grant is already closed.");
        RaiseEvent(new GrantClosed(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case GrantApplied e: Id = e.Id; Name = e.Name; Grantor = e.Grantor; RequestedAmount = e.Amount; ApplicationDate = e.ApplicationDate; Status = GrantStatus.Applied; break;
            case GrantAwarded e: AwardedAmount = e.AwardedAmount; Status = GrantStatus.Awarded; break;
            case GrantDenied: Status = GrantStatus.Denied; break;
            case GrantMilestoneAdded e: _milestones.Add(e.Milestone); break;
            case GrantMilestoneCompleted e:
                var idx = _milestones.FindIndex(m => m.Description == e.MilestoneDescription);
                if (idx >= 0) _milestones[idx] = _milestones[idx] with { Completed = true, ReportPath = e.ReportPath };
                break;
            case GrantClosed: Status = GrantStatus.Closed; break;
        }
    }
}
