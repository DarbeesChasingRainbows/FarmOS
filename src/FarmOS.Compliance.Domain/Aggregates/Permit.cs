using FarmOS.Compliance.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Aggregates;

public sealed class Permit : AggregateRoot<PermitId>
{
    public PermitType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string IssuingAuthority { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public PermitStatus Status { get; private set; }
    public decimal? Fee { get; private set; }
    private readonly List<RenewalInfo> _renewals = [];
    public IReadOnlyList<RenewalInfo> Renewals => _renewals;

    public static Permit Register(PermitType type, string name, string issuingAuthority, DateOnly issueDate, DateOnly expiryDate, decimal? fee, string? notes)
    {
        var permit = new Permit();
        permit.RaiseEvent(new PermitRegistered(PermitId.New(), type, name, issuingAuthority, issueDate, expiryDate, fee, notes, DateTimeOffset.UtcNow));
        return permit;
    }

    public Result<PermitId, DomainError> Renew(RenewalInfo renewal, DateOnly newExpiryDate)
    {
        if (Status == PermitStatus.Revoked)
            return DomainError.Conflict("Cannot renew a revoked permit.");
        RaiseEvent(new PermitRenewed(Id, renewal, newExpiryDate, DateTimeOffset.UtcNow));
        return Id;
    }

    public void MarkExpired() =>
        RaiseEvent(new PermitExpired(Id, DateTimeOffset.UtcNow));

    public Result<PermitId, DomainError> Revoke(string reason)
    {
        if (Status == PermitStatus.Revoked)
            return DomainError.Conflict("Permit is already revoked.");
        RaiseEvent(new PermitRevoked(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PermitRegistered e: Id = e.Id; Type = e.Type; Name = e.Name; IssuingAuthority = e.IssuingAuthority; IssueDate = e.IssueDate; ExpiryDate = e.ExpiryDate; Fee = e.Fee; Status = PermitStatus.Active; break;
            case PermitRenewed e: _renewals.Add(e.Renewal); ExpiryDate = e.NewExpiryDate; Status = PermitStatus.Active; break;
            case PermitExpired: Status = PermitStatus.Expired; break;
            case PermitRevoked: Status = PermitStatus.Revoked; break;
        }
    }
}
