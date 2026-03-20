using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Flora.Application.Queries.Handlers;

public class BedQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllBedsQuery, List<FlowerBedSummaryDto>>,
    IQueryHandler<GetBedByIdQuery, FlowerBedDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<FlowerBedSummaryDto>?> Handle(GetAllBedsQuery request, CancellationToken ct)
    {
        return await _projection.GetAllBedsAsync(ct);
    }

    public async Task<FlowerBedDetailDto?> Handle(GetBedByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetBedDetailAsync(request.BedId, ct);
    }
}

public class SeedLotQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllSeedLotsQuery, List<SeedLotSummaryDto>>,
    IQueryHandler<GetSeedLotByIdQuery, SeedLotDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<SeedLotSummaryDto>?> Handle(GetAllSeedLotsQuery request, CancellationToken ct)
    {
        return await _projection.GetAllSeedLotsAsync(ct);
    }

    public async Task<SeedLotDetailDto?> Handle(GetSeedLotByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetSeedLotDetailAsync(request.LotId, ct);
    }
}

public class GuildQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllGuildsQuery, List<GuildSummaryDto>>,
    IQueryHandler<GetGuildByIdQuery, GuildDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<GuildSummaryDto>?> Handle(GetAllGuildsQuery request, CancellationToken ct)
    {
        return await _projection.GetAllGuildsAsync(ct);
    }

    public async Task<GuildDetailDto?> Handle(GetGuildByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetGuildDetailAsync(request.GuildId, ct);
    }
}

public class BatchQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllPostHarvestBatchesQuery, List<PostHarvestBatchSummaryDto>>,
    IQueryHandler<GetPostHarvestBatchByIdQuery, PostHarvestBatchDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<PostHarvestBatchSummaryDto>?> Handle(GetAllPostHarvestBatchesQuery request, CancellationToken ct)
    {
        return await _projection.GetAllBatchesAsync(ct);
    }

    public async Task<PostHarvestBatchDetailDto?> Handle(GetPostHarvestBatchByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetBatchDetailAsync(request.BatchId, ct);
    }
}

public class RecipeQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllBouquetRecipesQuery, List<BouquetRecipeSummaryDto>>,
    IQueryHandler<GetBouquetRecipeByIdQuery, BouquetRecipeDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<BouquetRecipeSummaryDto>?> Handle(GetAllBouquetRecipesQuery request, CancellationToken ct)
    {
        return await _projection.GetAllRecipesAsync(ct);
    }

    public async Task<BouquetRecipeDetailDto?> Handle(GetBouquetRecipeByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetRecipeDetailAsync(request.RecipeId, ct);
    }
}

public class CropPlanQueryHandlers(IFloraProjection projection) :
    IQueryHandler<GetAllCropPlansQuery, List<CropPlanSummaryDto>>,
    IQueryHandler<GetCropPlanByIdQuery, CropPlanDetailDto>
{
    private readonly IFloraProjection _projection = projection;

    public async Task<List<CropPlanSummaryDto>?> Handle(GetAllCropPlansQuery request, CancellationToken ct)
    {
        return await _projection.GetAllCropPlansAsync(ct);
    }

    public async Task<CropPlanDetailDto?> Handle(GetCropPlanByIdQuery request, CancellationToken ct)
    {
        return await _projection.GetCropPlanDetailAsync(request.PlanId, ct);
    }
}
