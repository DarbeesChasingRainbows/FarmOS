using FarmOS.Compliance.Domain;
using FarmOS.Compliance.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Compliance.Application.Commands.Handlers;

public sealed class PermitCommandHandlers(IComplianceEventStore store) :
    ICommandHandler<RegisterPermitCommand, Guid>,
    ICommandHandler<RenewPermitCommand, Guid>,
    ICommandHandler<RevokePermitCommand, Guid>,
    ICommandHandler<MarkPermitExpiredCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterPermitCommand cmd, CancellationToken ct)
    {
        var permit = Permit.Register(cmd.Type, cmd.Name, cmd.IssuingAuthority, cmd.IssueDate, cmd.ExpiryDate, cmd.Fee, cmd.Notes);
        await store.SavePermitAsync(permit, "steward", ct);
        return permit.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RenewPermitCommand cmd, CancellationToken ct)
    {
        var permit = await store.LoadPermitAsync(cmd.PermitId.ToString(), ct);
        var result = permit.Renew(cmd.Renewal, cmd.NewExpiryDate);
        if (result.IsFailure) return result.Error;
        await store.SavePermitAsync(permit, "steward", ct);
        return permit.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RevokePermitCommand cmd, CancellationToken ct)
    {
        var permit = await store.LoadPermitAsync(cmd.PermitId.ToString(), ct);
        var result = permit.Revoke(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SavePermitAsync(permit, "steward", ct);
        return permit.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MarkPermitExpiredCommand cmd, CancellationToken ct)
    {
        var permit = await store.LoadPermitAsync(cmd.PermitId.ToString(), ct);
        permit.MarkExpired();
        await store.SavePermitAsync(permit, "steward", ct);
        return permit.Id.Value;
    }
}

public sealed class PolicyCommandHandlers(IComplianceEventStore store) :
    ICommandHandler<RegisterPolicyCommand, Guid>,
    ICommandHandler<RenewPolicyCommand, Guid>,
    ICommandHandler<MarkPolicyExpiredCommand, Guid>,
    ICommandHandler<UpdateCoveragesCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterPolicyCommand cmd, CancellationToken ct)
    {
        var policy = InsurancePolicy.Register(cmd.Type, cmd.Provider, cmd.PolicyNumber, cmd.EffectiveDate, cmd.ExpiryDate, cmd.AnnualPremium, cmd.Coverages);
        await store.SavePolicyAsync(policy, "steward", ct);
        return policy.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RenewPolicyCommand cmd, CancellationToken ct)
    {
        var policy = await store.LoadPolicyAsync(cmd.PolicyId.ToString(), ct);
        var result = policy.Renew(cmd.NewExpiryDate, cmd.NewPremium);
        if (result.IsFailure) return result.Error;
        await store.SavePolicyAsync(policy, "steward", ct);
        return policy.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MarkPolicyExpiredCommand cmd, CancellationToken ct)
    {
        var policy = await store.LoadPolicyAsync(cmd.PolicyId.ToString(), ct);
        policy.MarkExpired();
        await store.SavePolicyAsync(policy, "steward", ct);
        return policy.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateCoveragesCommand cmd, CancellationToken ct)
    {
        var policy = await store.LoadPolicyAsync(cmd.PolicyId.ToString(), ct);
        policy.UpdateCoverages(cmd.Coverages);
        await store.SavePolicyAsync(policy, "steward", ct);
        return policy.Id.Value;
    }
}
