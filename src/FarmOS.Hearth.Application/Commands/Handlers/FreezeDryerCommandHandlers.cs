using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands.Handlers;

public sealed class FreezeDryerCommandHandlers(IHearthEventStore store) :
    ICommandHandler<StartFreezeDryerBatchCommand, Guid>,
    ICommandHandler<RecordFreezeDryerReadingCommand, Guid>,
    ICommandHandler<AdvanceFreezeDryerPhaseCommand, Guid>,
    ICommandHandler<CompleteFreezeDryerBatchCommand, Guid>,
    ICommandHandler<AbortFreezeDryerBatchCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(StartFreezeDryerBatchCommand cmd, CancellationToken ct)
    {
        var batch = FreezeDryerBatch.Start(cmd.BatchCode, new FreezeDryerId(cmd.DryerId), cmd.ProductDescription, cmd.PreDryWeight);
        await store.SaveFreezeDryerAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordFreezeDryerReadingCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadFreezeDryerAsync(cmd.BatchId.ToString(), ct);
        var result = batch.RecordReading(cmd.Reading);
        if (result.IsFailure) return result.Error;
        await store.SaveFreezeDryerAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AdvanceFreezeDryerPhaseCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadFreezeDryerAsync(cmd.BatchId.ToString(), ct);
        var result = batch.AdvancePhase(cmd.NextPhase);
        if (result.IsFailure) return result.Error;
        await store.SaveFreezeDryerAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteFreezeDryerBatchCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadFreezeDryerAsync(cmd.BatchId.ToString(), ct);
        var result = batch.Complete(cmd.PostDryWeight);
        if (result.IsFailure) return result.Error;
        await store.SaveFreezeDryerAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AbortFreezeDryerBatchCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadFreezeDryerAsync(cmd.BatchId.ToString(), ct);
        var result = batch.Abort(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveFreezeDryerAsync(batch, "steward", ct);
        return batch.Id.Value;
    }
}
