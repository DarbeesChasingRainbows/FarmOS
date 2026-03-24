using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Events;

// ─── Procedure ──────────────────────────────────────────────────────
public record ProcedureCreated(ProcedureId Id, string Title, ProcedureCategory Category, AudienceRole Audience, string? Description, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureStepAdded(ProcedureId Id, ProcedureStep Step, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedurePublished(ProcedureId Id, int Revision, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureRevised(ProcedureId Id, string? ChangeNotes, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureArchived(ProcedureId Id, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Playbook ───────────────────────────────────────────────────────
public record PlaybookCreated(PlaybookId Id, string Title, string? Description, AudienceRole Audience, DateTimeOffset OccurredAt) : IDomainEvent;
public record PlaybookTaskAdded(PlaybookId Id, PlaybookTask Task, DateTimeOffset OccurredAt) : IDomainEvent;
public record PlaybookTaskRemoved(PlaybookId Id, int Month, string TaskTitle, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── DecisionTree ──────────────────────────────────────────────────
public record DecisionTreeCreated(DecisionTreeId Id, string Title, ProcedureCategory Category, string? Description, DateTimeOffset OccurredAt) : IDomainEvent;
public record DecisionNodeAdded(DecisionTreeId Id, DecisionNode Node, DateTimeOffset OccurredAt) : IDomainEvent;
public record DecisionNodeUpdated(DecisionTreeId Id, DecisionNode Node, DateTimeOffset OccurredAt) : IDomainEvent;
