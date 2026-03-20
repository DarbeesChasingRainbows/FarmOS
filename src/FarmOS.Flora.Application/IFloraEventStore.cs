using FarmOS.Flora.Domain.Aggregates;

namespace FarmOS.Flora.Application;

public interface IFloraEventStore
{
    Task<OrchardGuild> LoadGuildAsync(string guildId, CancellationToken ct);
    Task SaveGuildAsync(OrchardGuild guild, string userId, CancellationToken ct);

    Task<FlowerBed> LoadBedAsync(string bedId, CancellationToken ct);
    Task SaveBedAsync(FlowerBed bed, string userId, CancellationToken ct);

    Task<SeedLot> LoadSeedLotAsync(string lotId, CancellationToken ct);
    Task SaveSeedLotAsync(SeedLot lot, string userId, CancellationToken ct);

    Task<PostHarvestBatch> LoadPostHarvestBatchAsync(string batchId, CancellationToken ct);
    Task SavePostHarvestBatchAsync(PostHarvestBatch batch, string userId, CancellationToken ct);

    Task<BouquetRecipe> LoadBouquetRecipeAsync(string recipeId, CancellationToken ct);
    Task SaveBouquetRecipeAsync(BouquetRecipe recipe, string userId, CancellationToken ct);

    Task<CropPlan> LoadCropPlanAsync(string planId, CancellationToken ct);
    Task SaveCropPlanAsync(CropPlan plan, string userId, CancellationToken ct);
}
