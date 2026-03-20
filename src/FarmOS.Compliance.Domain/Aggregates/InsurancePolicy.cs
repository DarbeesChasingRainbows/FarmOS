using FarmOS.Compliance.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Aggregates;

public sealed class InsurancePolicy : AggregateRoot<PolicyId>
{
    public PolicyType Type { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string PolicyNumber { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public decimal AnnualPremium { get; private set; }
    public PolicyStatus Status { get; private set; }
    private readonly List<CoverageDetail> _coverages = [];
    public IReadOnlyList<CoverageDetail> Coverages => _coverages;

    public static InsurancePolicy Register(PolicyType type, string provider, string policyNumber, DateOnly effectiveDate, DateOnly expiryDate, decimal annualPremium, IReadOnlyList<CoverageDetail> coverages)
    {
        var policy = new InsurancePolicy();
        policy.RaiseEvent(new PolicyRegistered(PolicyId.New(), type, provider, policyNumber, effectiveDate, expiryDate, annualPremium, coverages, DateTimeOffset.UtcNow));
        return policy;
    }

    public Result<PolicyId, DomainError> Renew(DateOnly newExpiryDate, decimal newPremium)
    {
        if (Status == PolicyStatus.Cancelled)
            return DomainError.Conflict("Cannot renew a cancelled policy.");
        RaiseEvent(new PolicyRenewed(Id, newExpiryDate, newPremium, DateTimeOffset.UtcNow));
        return Id;
    }

    public void MarkExpired() =>
        RaiseEvent(new PolicyExpired(Id, DateTimeOffset.UtcNow));

    public Result<PolicyId, DomainError> Cancel(string reason)
    {
        if (Status == PolicyStatus.Cancelled)
            return DomainError.Conflict("Policy is already cancelled.");
        RaiseEvent(new PolicyCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public void UpdateCoverages(IReadOnlyList<CoverageDetail> coverages) =>
        RaiseEvent(new PolicyCoverageUpdated(Id, coverages, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PolicyRegistered e: Id = e.Id; Type = e.Type; Provider = e.Provider; PolicyNumber = e.PolicyNumber; EffectiveDate = e.EffectiveDate; ExpiryDate = e.ExpiryDate; AnnualPremium = e.AnnualPremium; _coverages.AddRange(e.Coverages); Status = PolicyStatus.Active; break;
            case PolicyRenewed e: ExpiryDate = e.NewExpiryDate; AnnualPremium = e.NewPremium; Status = PolicyStatus.Active; break;
            case PolicyExpired: Status = PolicyStatus.Expired; break;
            case PolicyCancelled: Status = PolicyStatus.Cancelled; break;
            case PolicyCoverageUpdated e: _coverages.Clear(); _coverages.AddRange(e.Coverages); break;
        }
    }
}
