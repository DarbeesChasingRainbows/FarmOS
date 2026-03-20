using FarmOS.Apiary.Domain;
using FarmOS.Apiary.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using ApiaryAggregate = FarmOS.Apiary.Domain.Aggregates.Apiary;

namespace FarmOS.Apiary.Application.Commands.Handlers;

public sealed class HiveCommandHandlers(IApiaryEventStore store) :
    ICommandHandler<CreateHiveCommand, Guid>,
    ICommandHandler<InspectHiveCommand, Guid>,
    ICommandHandler<HarvestHoneyCommand, Guid>,
    ICommandHandler<TreatHiveCommand, Guid>,
    ICommandHandler<ChangeHiveStatusCommand, Guid>,
    ICommandHandler<IntroduceQueenCommand, Guid>,
    ICommandHandler<MarkQueenLostCommand, Guid>,
    ICommandHandler<ReplaceQueenCommand, Guid>,
    ICommandHandler<FeedHiveCommand, Guid>,
    ICommandHandler<SplitColonyCommand, Guid>,
    ICommandHandler<MergeColoniesCommand, Guid>,
    ICommandHandler<AddSuperCommand, Guid>,
    ICommandHandler<RemoveSuperCommand, Guid>,
    ICommandHandler<UpdateHiveConfigurationCommand, Guid>,
    ICommandHandler<HarvestProductCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateHiveCommand cmd, CancellationToken ct)
    {
        var hive = Hive.Create(cmd.Name, cmd.Type, cmd.Position, cmd.Established);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(InspectHiveCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.Inspect(cmd.Data, cmd.Date);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(HarvestHoneyCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.HarvestHoney(cmd.Data, cmd.Date);
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(TreatHiveCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.RecordTreatment(cmd.Data);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ChangeHiveStatusCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.ChangeStatus(cmd.Status, cmd.Reason);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    // ─── Feature 2: Queen Tracking ──────────────────────────────────
    public async Task<Result<Guid, DomainError>> Handle(IntroduceQueenCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.IntroduceQueen(cmd.Queen);
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MarkQueenLostCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.MarkQueenLost(cmd.Reason, cmd.Date);
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ReplaceQueenCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.ReplaceQueen(cmd.NewQueen, cmd.Reason);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    // ─── Feature 3: Feeding ─────────────────────────────────────────
    public async Task<Result<Guid, DomainError>> Handle(FeedHiveCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.Feed(cmd.Data);
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    // ─── Feature 6: Multi-Product Harvest ──────────────────────────
    public async Task<Result<Guid, DomainError>> Handle(HarvestProductCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.RecordProductHarvest(cmd.Data);
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    // ─── Feature 4: Colony Splitting & Merging ──────────────────────
    public async Task<Result<Guid, DomainError>> Handle(SplitColonyCommand cmd, CancellationToken ct)
    {
        var original = await store.LoadHiveAsync(cmd.OriginalHiveId.ToString(), ct);
        var splitResult = original.Split(cmd.NewHiveName, cmd.NewHiveType, cmd.NewPosition, cmd.Date);
        if (splitResult.IsFailure) return splitResult.Error;

        var newHive = Hive.Create(cmd.NewHiveName, cmd.NewHiveType, cmd.NewPosition, cmd.Date);
        await store.SaveHiveAsync(original, "steward", ct);
        await store.SaveHiveAsync(newHive, "steward", ct);
        return newHive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MergeColoniesCommand cmd, CancellationToken ct)
    {
        var surviving = await store.LoadHiveAsync(cmd.SurvivingHiveId.ToString(), ct);
        var absorbed = await store.LoadHiveAsync(cmd.AbsorbedHiveId.ToString(), ct);

        if (absorbed.Status == HiveStatus.Dead)
            return DomainError.Conflict("Cannot merge a dead hive.");

        var mergeResult = surviving.AbsorbColony(absorbed.Id, cmd.Date);
        if (mergeResult.IsFailure) return mergeResult.Error;

        absorbed.ChangeStatus(HiveStatus.Dead, $"Merged into {surviving.Name}");

        await store.SaveHiveAsync(surviving, "steward", ct);
        await store.SaveHiveAsync(absorbed, "steward", ct);
        return surviving.Id.Value;
    }

    // ─── Feature 5: Equipment/Super Tracking ────────────────────────
    public async Task<Result<Guid, DomainError>> Handle(AddSuperCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.AddSuper();
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RemoveSuperCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        var result = hive.RemoveSuper();
        if (result.IsFailure) return result.Error;
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateHiveConfigurationCommand cmd, CancellationToken ct)
    {
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        hive.UpdateConfiguration(cmd.Config);
        await store.SaveHiveAsync(hive, "steward", ct);
        return hive.Id.Value;
    }
}

// ─── Feature 1: Apiary Aggregate Handlers ───────────────────────────
public sealed class ApiaryAggregateCommandHandlers(IApiaryEventStore store) :
    ICommandHandler<CreateApiaryCommand, Guid>,
    ICommandHandler<MoveHiveToApiaryCommand, Guid>,
    ICommandHandler<RetireApiaryCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateApiaryCommand cmd, CancellationToken ct)
    {
        var apiary = ApiaryAggregate.Create(cmd.Name, cmd.Position, cmd.MaxCapacity, cmd.Notes);
        await store.SaveApiaryAsync(apiary, "steward", ct);
        return apiary.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MoveHiveToApiaryCommand cmd, CancellationToken ct)
    {
        var apiary = await store.LoadApiaryAsync(cmd.ApiaryId.ToString(), ct);
        var hiveId = new HiveId(cmd.HiveId);
        var result = apiary.AddHive(hiveId);
        if (result.IsFailure) return result.Error;
        // Also update hive's ApiaryId reference
        var hive = await store.LoadHiveAsync(cmd.HiveId.ToString(), ct);
        await store.SaveApiaryAsync(apiary, "steward", ct);
        await store.SaveHiveAsync(hive, "steward", ct);
        return apiary.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RetireApiaryCommand cmd, CancellationToken ct)
    {
        var apiary = await store.LoadApiaryAsync(cmd.ApiaryId.ToString(), ct);
        var result = apiary.Retire(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveApiaryAsync(apiary, "steward", ct);
        return apiary.Id.Value;
    }
}
