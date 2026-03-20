using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record WorkerId(Guid Value) { public static WorkerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record ShiftId(Guid Value) { public static ShiftId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum WorkerRole { Employee, Apprentice, Volunteer, Intern }
public enum WorkerStatus { Active, OnLeave, Completed, Terminated }
public enum CertificationType { FoodHandler, FirstAid, CPR, PesticideApplicator, EquipmentOperator, CDL, OrganicInspector, Custom }
public enum Enterprise { Pasture, Flora, Hearth, Apiary, Commerce, Assets, General }
public enum ShiftStatus { Scheduled, InProgress, Completed, Cancelled }

// ─── Value Objects ──────────────────────────────────────────────────
public record EmergencyContact(string Name, string Relationship, string Phone);
public record WorkerProfile(string Name, string Email, string? Phone, WorkerRole Role, EmergencyContact? Emergency, string? HousingAssignment, DateOnly StartDate);
public record Certification(CertificationType Type, string Name, DateOnly Issued, DateOnly? Expires, string? IssuingBody, string? DocumentPath);
public record ShiftEntry(WorkerId WorkerId, Enterprise Enterprise, DateOnly Date, TimeOnly Start, TimeOnly End, string? TaskDescription, string? Notes);
