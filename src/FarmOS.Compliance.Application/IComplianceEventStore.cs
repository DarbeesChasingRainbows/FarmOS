using FarmOS.Compliance.Domain.Aggregates;

namespace FarmOS.Compliance.Application;

public interface IComplianceEventStore
{
    Task<Permit> LoadPermitAsync(string permitId, CancellationToken ct);
    Task SavePermitAsync(Permit permit, string userId, CancellationToken ct);

    Task<InsurancePolicy> LoadPolicyAsync(string policyId, CancellationToken ct);
    Task SavePolicyAsync(InsurancePolicy policy, string userId, CancellationToken ct);
}
