using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record ProcedureId(Guid Value) { public static ProcedureId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record PlaybookId(Guid Value) { public static PlaybookId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum ProcedureCategory { Pasture, Flora, Hearth, Apiary, Commerce, Assets, Safety, Compliance, Onboarding, General }
public enum ProcedureStatus { Draft, Published, Archived }
public enum AudienceRole { Everyone, Employee, Apprentice, Manager }

// ─── Value Objects ──────────────────────────────────────────────────
public record ProcedureStep(int Order, string Title, string Instructions, string? ImagePath, string? WarningNote, int? EstimatedMinutes);
public record PlaybookTask(int Month, string Title, string Description, ProcedureCategory Category, string? LinkedProcedureId, string Priority);
public record DecisionTreeId(Guid Value) { public static DecisionTreeId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record DecisionNode(string NodeId, string Question, string? YesNodeId, string? NoNodeId, string? ActionIfTerminal, string? Notes);
