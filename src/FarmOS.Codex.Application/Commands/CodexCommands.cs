using FarmOS.Codex.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Codex.Application.Commands;

// --- Procedure ----------------------------------------------------------------

public record CreateProcedureCommand(string Title, ProcedureCategory Category, AudienceRole Audience, string? Description) : ICommand<Guid>;
public record AddProcedureStepCommand(Guid ProcedureId, ProcedureStep Step) : ICommand<Guid>;
public record PublishProcedureCommand(Guid ProcedureId) : ICommand<Guid>;
public record ReviseProcedureCommand(Guid ProcedureId, string? ChangeNotes) : ICommand<Guid>;
public record ArchiveProcedureCommand(Guid ProcedureId) : ICommand<Guid>;

// --- Playbook -----------------------------------------------------------------

public record CreatePlaybookCommand(string Title, string? Description, AudienceRole Audience) : ICommand<Guid>;
public record AddPlaybookTaskCommand(Guid PlaybookId, PlaybookTask Task) : ICommand<Guid>;
public record RemovePlaybookTaskCommand(Guid PlaybookId, int Month, string TaskTitle) : ICommand<Guid>;

// --- DecisionTree ------------------------------------------------------------

public record CreateDecisionTreeCommand(string Title, ProcedureCategory Category, string? Description) : ICommand<Guid>;
public record AddDecisionNodeCommand(Guid DecisionTreeId, DecisionNode Node) : ICommand<Guid>;
public record UpdateDecisionNodeCommand(Guid DecisionTreeId, string NodeId, DecisionNode Node) : ICommand<Guid>;
