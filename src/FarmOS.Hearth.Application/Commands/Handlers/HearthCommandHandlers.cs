using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands.Handlers;

public sealed class SourdoughCommandHandlers(IHearthEventStore store) :
    ICommandHandler<StartSourdoughCommand, Guid>,
    ICommandHandler<RecordSourdoughCCPCommand, Guid>,
    ICommandHandler<AdvanceSourdoughPhaseCommand, Guid>,
    ICommandHandler<CompleteSourdoughCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(StartSourdoughCommand cmd, CancellationToken ct)
    {
        var batch = SourdoughBatch.Start(cmd.BatchCode, new LivingCultureId(cmd.StarterId), cmd.Ingredients);
        await store.SaveSourdoughAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordSourdoughCCPCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadSourdoughAsync(cmd.BatchId.ToString(), ct);
        var result = batch.RecordCCP(cmd.Reading);
        if (result.IsFailure) return result.Error;
        await store.SaveSourdoughAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AdvanceSourdoughPhaseCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadSourdoughAsync(cmd.BatchId.ToString(), ct);
        var result = batch.AdvancePhase(cmd.NextPhase);
        if (result.IsFailure) return result.Error;
        await store.SaveSourdoughAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteSourdoughCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadSourdoughAsync(cmd.BatchId.ToString(), ct);
        batch.Complete(cmd.Yield);
        await store.SaveSourdoughAsync(batch, "steward", ct);
        return batch.Id.Value;
    }
}

public sealed class KombuchaCommandHandlers(IHearthEventStore store) :
    ICommandHandler<StartKombuchaCommand, Guid>,
    ICommandHandler<RecordKombuchaPHCommand, Guid>,
    ICommandHandler<AddKombuchaFlavoringCommand, Guid>,
    ICommandHandler<AdvanceKombuchaPhaseCommand, Guid>,
    ICommandHandler<CompleteKombuchaCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(StartKombuchaCommand cmd, CancellationToken ct)
    {
        var batch = KombuchaBatch.Start(cmd.BatchCode, cmd.Type, new LivingCultureId(cmd.SCOBYId), cmd.TeaType, cmd.Sweetener, cmd.Volume, cmd.StartingPH);
        await store.SaveKombuchaAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordKombuchaPHCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadKombuchaAsync(cmd.BatchId.ToString(), ct);
        batch.RecordPH(cmd.pH, cmd.Notes);
        await store.SaveKombuchaAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddKombuchaFlavoringCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadKombuchaAsync(cmd.BatchId.ToString(), ct);
        batch.AddFlavoring(cmd.Flavoring);
        await store.SaveKombuchaAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AdvanceKombuchaPhaseCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadKombuchaAsync(cmd.BatchId.ToString(), ct);
        var result = batch.AdvancePhase(cmd.NextPhase);
        if (result.IsFailure) return result.Error;
        await store.SaveKombuchaAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteKombuchaCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadKombuchaAsync(cmd.BatchId.ToString(), ct);
        batch.Complete(cmd.BottleCount);
        await store.SaveKombuchaAsync(batch, "steward", ct);
        return batch.Id.Value;
    }
}

public sealed class CultureCommandHandlers(IHearthEventStore store) :
    ICommandHandler<CreateCultureCommand, Guid>,
    ICommandHandler<FeedCultureCommand, Guid>,
    ICommandHandler<SplitCultureCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateCultureCommand cmd, CancellationToken ct)
    {
        var culture = LivingCulture.Create(cmd.Name, cmd.Type, cmd.BirthDate, cmd.ParentId.HasValue ? new LivingCultureId(cmd.ParentId.Value) : null);
        await store.SaveCultureAsync(culture, "steward", ct);
        return culture.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(FeedCultureCommand cmd, CancellationToken ct)
    {
        var culture = await store.LoadCultureAsync(cmd.CultureId.ToString(), ct);
        culture.Feed(cmd.Feeding);
        await store.SaveCultureAsync(culture, "steward", ct);
        return culture.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(SplitCultureCommand cmd, CancellationToken ct)
    {
        var culture = await store.LoadCultureAsync(cmd.CultureId.ToString(), ct);
        var newId = culture.Split(cmd.NewName, cmd.Date);
        await store.SaveCultureAsync(culture, "steward", ct);
        return newId.Value;
    }
}
