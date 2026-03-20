using FarmOS.Compliance.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Compliance.Application.Commands;

// --- Permit ----------------------------------------------------------------

public record RegisterPermitCommand(PermitType Type, string Name, string IssuingAuthority, DateOnly IssueDate, DateOnly ExpiryDate, decimal? Fee, string? Notes) : ICommand<Guid>;
public record RenewPermitCommand(Guid PermitId, RenewalInfo Renewal, DateOnly NewExpiryDate) : ICommand<Guid>;
public record RevokePermitCommand(Guid PermitId, string Reason) : ICommand<Guid>;
public record MarkPermitExpiredCommand(Guid PermitId) : ICommand<Guid>;

// --- Policy ----------------------------------------------------------------

public record RegisterPolicyCommand(PolicyType Type, string Provider, string PolicyNumber, DateOnly EffectiveDate, DateOnly ExpiryDate, decimal AnnualPremium, IReadOnlyList<CoverageDetail> Coverages) : ICommand<Guid>;
public record RenewPolicyCommand(Guid PolicyId, DateOnly NewExpiryDate, decimal NewPremium) : ICommand<Guid>;
public record MarkPolicyExpiredCommand(Guid PolicyId) : ICommand<Guid>;
public record UpdateCoveragesCommand(Guid PolicyId, IReadOnlyList<CoverageDetail> Coverages) : ICommand<Guid>;
