using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record PermitId(Guid Value) { public static PermitId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record PolicyId(Guid Value) { public static PolicyId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum PermitType { BusinessLicense, FoodProcessing, RetailFood, SalesTax, ZoningUse, OrganicCertification, GAPCertification, CottageFoodExemption, HealthDepartment, WeightsAndMeasures, Custom }
public enum PermitStatus { Active, PendingRenewal, Expired, Revoked }
public enum PolicyType { GeneralLiability, Property, Equipment, WorkersComp, ProductLiability, CommercialAuto, UmbrellaPolicy }
public enum PolicyStatus { Active, PendingRenewal, Expired, Cancelled }

// ─── Value Objects ──────────────────────────────────────────────────
public record RenewalInfo(DateOnly RenewalDate, decimal? Fee, string? Notes);
public record CoverageDetail(string CoverageType, decimal Limit, decimal Deductible);
