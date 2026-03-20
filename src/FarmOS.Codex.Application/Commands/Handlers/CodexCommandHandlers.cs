using FarmOS.Codex.Domain;
using FarmOS.Codex.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Codex.Application.Commands.Handlers;

public sealed class ProcedureCommandHandlers(ICodexEventStore store) :
    ICommandHandler<CreateProcedureCommand, Guid>,
    ICommandHandler<AddProcedureStepCommand, Guid>,
    ICommandHandler<PublishProcedureCommand, Guid>,
    ICommandHandler<ReviseProcedureCommand, Guid>,
    ICommandHandler<ArchiveProcedureCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = Procedure.Create(cmd.Title, cmd.Category, cmd.Audience, cmd.Description);
        await store.SaveProcedureAsync(procedure, "steward", ct);
        return procedure.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddProcedureStepCommand cmd, CancellationToken ct)
    {
        var procedure = await store.LoadProcedureAsync(cmd.ProcedureId.ToString(), ct);
        procedure.AddStep(cmd.Step);
        await store.SaveProcedureAsync(procedure, "steward", ct);
        return procedure.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(PublishProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = await store.LoadProcedureAsync(cmd.ProcedureId.ToString(), ct);
        var result = procedure.Publish();
        if (result.IsFailure) return result.Error;
        await store.SaveProcedureAsync(procedure, "steward", ct);
        return procedure.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ReviseProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = await store.LoadProcedureAsync(cmd.ProcedureId.ToString(), ct);
        var result = procedure.Revise(cmd.ChangeNotes);
        if (result.IsFailure) return result.Error;
        await store.SaveProcedureAsync(procedure, "steward", ct);
        return procedure.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ArchiveProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = await store.LoadProcedureAsync(cmd.ProcedureId.ToString(), ct);
        procedure.Archive();
        await store.SaveProcedureAsync(procedure, "steward", ct);
        return procedure.Id.Value;
    }
}

public sealed class PlaybookCommandHandlers(ICodexEventStore store) :
    ICommandHandler<CreatePlaybookCommand, Guid>,
    ICommandHandler<AddPlaybookTaskCommand, Guid>,
    ICommandHandler<RemovePlaybookTaskCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreatePlaybookCommand cmd, CancellationToken ct)
    {
        var playbook = Playbook.Create(cmd.Title, cmd.Description, cmd.Audience);
        await store.SavePlaybookAsync(playbook, "steward", ct);
        return playbook.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddPlaybookTaskCommand cmd, CancellationToken ct)
    {
        var playbook = await store.LoadPlaybookAsync(cmd.PlaybookId.ToString(), ct);
        playbook.AddTask(cmd.Task);
        await store.SavePlaybookAsync(playbook, "steward", ct);
        return playbook.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RemovePlaybookTaskCommand cmd, CancellationToken ct)
    {
        var playbook = await store.LoadPlaybookAsync(cmd.PlaybookId.ToString(), ct);
        var result = playbook.RemoveTask(cmd.Month, cmd.TaskTitle);
        if (result.IsFailure) return result.Error;
        await store.SavePlaybookAsync(playbook, "steward", ct);
        return playbook.Id.Value;
    }
}

public sealed class DecisionTreeCommandHandlers(ICodexEventStore store) :
    ICommandHandler<CreateDecisionTreeCommand, Guid>,
    ICommandHandler<AddDecisionNodeCommand, Guid>,
    ICommandHandler<UpdateDecisionNodeCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateDecisionTreeCommand cmd, CancellationToken ct)
    {
        var tree = DecisionTree.Create(cmd.Title, cmd.Category, cmd.Description);
        await store.SaveDecisionTreeAsync(tree, "steward", ct);
        return tree.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddDecisionNodeCommand cmd, CancellationToken ct)
    {
        var tree = await store.LoadDecisionTreeAsync(cmd.DecisionTreeId.ToString(), ct);
        tree.AddNode(cmd.Node);
        await store.SaveDecisionTreeAsync(tree, "steward", ct);
        return tree.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateDecisionNodeCommand cmd, CancellationToken ct)
    {
        var tree = await store.LoadDecisionTreeAsync(cmd.DecisionTreeId.ToString(), ct);
        var result = tree.UpdateNode(cmd.Node);
        if (result.IsFailure) return result.Error;
        await store.SaveDecisionTreeAsync(tree, "steward", ct);
        return tree.Id.Value;
    }
}
