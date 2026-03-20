using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Events;

// ─── Permit ─────────────────────────────────────────────────────────
public record PermitRegistered(PermitId Id, PermitType Type, string Name, string IssuingAuthority, DateOnly IssueDate, DateOnly ExpiryDate, decimal? Fee, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitRenewed(PermitId Id, RenewalInfo Renewal, DateOnly NewExpiryDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitExpired(PermitId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitRevoked(PermitId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Policy ─────────────────────────────────────────────────────────
public record PolicyRegistered(PolicyId Id, PolicyType Type, string Provider, string PolicyNumber, DateOnly EffectiveDate, DateOnly ExpiryDate, decimal AnnualPremium, IReadOnlyList<CoverageDetail> Coverages, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyRenewed(PolicyId Id, DateOnly NewExpiryDate, decimal NewPremium, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyExpired(PolicyId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyCancelled(PolicyId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyCoverageUpdated(PolicyId Id, IReadOnlyList<CoverageDetail> Coverages, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Grant ─────────────────────────────────────────────────────────
public record GrantApplied(GrantId Id, string Name, string Grantor, decimal Amount, DateOnly ApplicationDate, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record GrantAwarded(GrantId Id, decimal AwardedAmount, DateOnly AwardDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record GrantDenied(GrantId Id, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record GrantMilestoneAdded(GrantId Id, GrantMilestone Milestone, DateTimeOffset OccurredAt) : IDomainEvent;
public record GrantMilestoneCompleted(GrantId Id, string MilestoneDescription, string? ReportPath, DateTimeOffset OccurredAt) : IDomainEvent;
public record GrantClosed(GrantId Id, DateTimeOffset OccurredAt) : IDomainEvent;
