using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Flora.Application.Commands.Handlers;

public sealed class GuildCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreateGuildCommand, Guid>,
    ICommandHandler<AddGuildMemberCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateGuildCommand cmd, CancellationToken ct)
    {
        var guild = OrchardGuild.Create(cmd.Name, cmd.Type, cmd.Position, cmd.Planted);
        await store.SaveGuildAsync(guild, "steward", ct);
        return guild.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddGuildMemberCommand cmd, CancellationToken ct)
    {
        var guild = await store.LoadGuildAsync(cmd.GuildId.ToString(), ct);
        guild.AddMember(cmd.Member);
        await store.SaveGuildAsync(guild, "steward", ct);
        return guild.Id.Value;
    }
}

public sealed class BedCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreateFlowerBedCommand, Guid>,
    ICommandHandler<PlanSuccessionCommand, Guid>,
    ICommandHandler<RecordSeedingCommand, Guid>,
    ICommandHandler<RecordTransplantCommand, Guid>,
    ICommandHandler<RecordHarvestCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateFlowerBedCommand cmd, CancellationToken ct)
    {
        var bed = FlowerBed.Create(cmd.Name, cmd.Block, cmd.Dimensions);
        await store.SaveBedAsync(bed, "steward", ct);
        return bed.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(PlanSuccessionCommand cmd, CancellationToken ct)
    {
        var bed = await store.LoadBedAsync(cmd.BedId.ToString(), ct);
        var succId = bed.PlanSuccession(cmd.Variety, cmd.SowDate, cmd.TransplantDate, cmd.HarvestStart);
        await store.SaveBedAsync(bed, "steward", ct);
        return succId.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordSeedingCommand cmd, CancellationToken ct)
    {
        var bed = await store.LoadBedAsync(cmd.BedId.ToString(), ct);
        bed.RecordSeeding(new SuccessionId(cmd.SuccessionId), new SeedLotId(cmd.SeedLotId), cmd.Quantity, cmd.Date);
        await store.SaveBedAsync(bed, "steward", ct);
        return bed.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordTransplantCommand cmd, CancellationToken ct)
    {
        var bed = await store.LoadBedAsync(cmd.BedId.ToString(), ct);
        bed.RecordTransplant(new SuccessionId(cmd.SuccessionId), cmd.Quantity, cmd.Date);
        await store.SaveBedAsync(bed, "steward", ct);
        return bed.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordHarvestCommand cmd, CancellationToken ct)
    {
        var bed = await store.LoadBedAsync(cmd.BedId.ToString(), ct);
        bed.RecordHarvest(new SuccessionId(cmd.SuccessionId), cmd.Stems, cmd.Date);
        await store.SaveBedAsync(bed, "steward", ct);
        return bed.Id.Value;
    }
}

public sealed class SeedLotCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreateSeedLotCommand, Guid>,
    ICommandHandler<WithdrawSeedCommand, Guid>,
    ICommandHandler<RestockSeedCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateSeedLotCommand cmd, CancellationToken ct)
    {
        var lot = SeedLot.Create(cmd.Variety, cmd.Supplier, cmd.Quantity, cmd.GerminationPct, cmd.HarvestYear, cmd.IsOrganic);
        await store.SaveSeedLotAsync(lot, "steward", ct);
        return lot.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(WithdrawSeedCommand cmd, CancellationToken ct)
    {
        var lot = await store.LoadSeedLotAsync(cmd.SeedLotId.ToString(), ct);
        var result = lot.Withdraw(cmd.Quantity, new FlowerBedId(cmd.DestinationBedId));
        if (result.IsFailure) return result.Error;
        await store.SaveSeedLotAsync(lot, "steward", ct);
        return lot.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RestockSeedCommand cmd, CancellationToken ct)
    {
        var lot = await store.LoadSeedLotAsync(cmd.SeedLotId.ToString(), ct);
        lot.Restock(cmd.Quantity, cmd.LotNumber);
        await store.SaveSeedLotAsync(lot, "steward", ct);
        return lot.Id.Value;
    }
}

public sealed class PostHarvestCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreatePostHarvestBatchCommand, Guid>,
    ICommandHandler<GradeStemsCommand, Guid>,
    ICommandHandler<ConditionStemsCommand, Guid>,
    ICommandHandler<MoveBatchToCoolerCommand, Guid>,
    ICommandHandler<UseBatchStemsCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreatePostHarvestBatchCommand cmd, CancellationToken ct)
    {
        var batch = PostHarvestBatch.Create(
            new FlowerBedId(cmd.SourceBedId), new SuccessionId(cmd.SuccessionId),
            cmd.Species, cmd.Cultivar, cmd.TotalStems, cmd.HarvestDate);
        await store.SavePostHarvestBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(GradeStemsCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadPostHarvestBatchAsync(cmd.BatchId.ToString(), ct);
        batch.GradeStems(cmd.Grade);
        await store.SavePostHarvestBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ConditionStemsCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadPostHarvestBatchAsync(cmd.BatchId.ToString(), ct);
        batch.Condition(cmd.Solution, cmd.WaterTempF);
        await store.SavePostHarvestBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MoveBatchToCoolerCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadPostHarvestBatchAsync(cmd.BatchId.ToString(), ct);
        batch.MoveToCooler(cmd.TemperatureF, cmd.SlotLabel);
        await store.SavePostHarvestBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UseBatchStemsCommand cmd, CancellationToken ct)
    {
        var batch = await store.LoadPostHarvestBatchAsync(cmd.BatchId.ToString(), ct);
        var result = batch.UseStems(cmd.StemsUsed, cmd.Purpose);
        if (result.IsFailure) return result.Error;
        await store.SavePostHarvestBatchAsync(batch, "steward", ct);
        return batch.Id.Value;
    }
}

public sealed class RecipeCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreateBouquetRecipeCommand, Guid>,
    ICommandHandler<AddRecipeItemCommand, Guid>,
    ICommandHandler<RemoveRecipeItemCommand, Guid>,
    ICommandHandler<MakeBouquetCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateBouquetRecipeCommand cmd, CancellationToken ct)
    {
        var recipe = BouquetRecipe.Create(cmd.Name, cmd.Category);
        await store.SaveBouquetRecipeAsync(recipe, "steward", ct);
        return recipe.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddRecipeItemCommand cmd, CancellationToken ct)
    {
        var recipe = await store.LoadBouquetRecipeAsync(cmd.RecipeId.ToString(), ct);
        recipe.AddItem(cmd.Item);
        await store.SaveBouquetRecipeAsync(recipe, "steward", ct);
        return recipe.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RemoveRecipeItemCommand cmd, CancellationToken ct)
    {
        var recipe = await store.LoadBouquetRecipeAsync(cmd.RecipeId.ToString(), ct);
        recipe.RemoveItem(cmd.Species, cmd.Cultivar);
        await store.SaveBouquetRecipeAsync(recipe, "steward", ct);
        return recipe.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MakeBouquetCommand cmd, CancellationToken ct)
    {
        var recipe = await store.LoadBouquetRecipeAsync(cmd.RecipeId.ToString(), ct);
        recipe.MakeBouquet(cmd.Quantity, cmd.Date, cmd.Notes);
        await store.SaveBouquetRecipeAsync(recipe, "steward", ct);
        return recipe.Id.Value;
    }
}

public sealed class CropPlanCommandHandlers(IFloraEventStore store) :
    ICommandHandler<CreateCropPlanCommand, Guid>,
    ICommandHandler<AssignBedToPlanCommand, Guid>,
    ICommandHandler<RecordYieldCommand, Guid>,
    ICommandHandler<RecordCropCostCommand, Guid>,
    ICommandHandler<RecordCropRevenueCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateCropPlanCommand cmd, CancellationToken ct)
    {
        var plan = CropPlan.Create(cmd.SeasonYear, cmd.SeasonName, cmd.PlanName);
        await store.SaveCropPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AssignBedToPlanCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadCropPlanAsync(cmd.PlanId.ToString(), ct);
        plan.AssignBed(cmd.Assignment);
        await store.SaveCropPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordYieldCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadCropPlanAsync(cmd.PlanId.ToString(), ct);
        plan.RecordYield(new FlowerBedId(cmd.BedId), new SuccessionId(cmd.SuccessionId),
            cmd.StemsHarvested, cmd.StemsPerLinearFoot, cmd.Date);
        await store.SaveCropPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordCropCostCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadCropPlanAsync(cmd.PlanId.ToString(), ct);
        plan.RecordCost(cmd.Cost);
        await store.SaveCropPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RecordCropRevenueCommand cmd, CancellationToken ct)
    {
        var plan = await store.LoadCropPlanAsync(cmd.PlanId.ToString(), ct);
        plan.RecordRevenue(cmd.Channel, cmd.Amount, cmd.Date, cmd.Notes);
        await store.SaveCropPlanAsync(plan, "steward", ct);
        return plan.Id.Value;
    }
}
