using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.Flora.Domain;

namespace FarmOS.Flora.Application.Queries;

// ─── DTOs ──────────────────────────────────────────────────────────────────

public record FlowerBedSummaryDto(
    Guid Id,
    string Name,
    string Block,
    decimal LengthFeet,
    decimal WidthFeet,
    int SuccessionCount);

public record FlowerBedDetailDto(
    Guid Id,
    string Name,
    string Block,
    decimal LengthFeet,
    decimal WidthFeet,
    List<SuccessionDto> Successions);

public record SuccessionDto(
    Guid Id,
    string Species,
    string Cultivar,
    int DaysToMaturity,
    string? Color,
    DateOnly SowDate,
    DateOnly TransplantDate,
    DateOnly HarvestStart,
    DateOnly? HarvestEnd,
    List<HarvestEntryDto> Harvests);

public record HarvestEntryDto(
    decimal StemCount,
    string Unit,
    DateOnly Date);

public record SeedLotSummaryDto(
    Guid Id,
    string Species,
    string Cultivar,
    string Supplier,
    decimal QtyOnHand,
    string Unit,
    decimal GerminationPct,
    int HarvestYear,
    bool IsOrganic);

public record SeedLotDetailDto(
    Guid Id,
    string Species,
    string Cultivar,
    int DaysToMaturity,
    string? Color,
    string Supplier,
    decimal QtyOnHand,
    string Unit,
    decimal GerminationPct,
    int HarvestYear,
    bool IsOrganic,
    string? LotNumber,
    DateOnly? PurchaseDate);

public record GuildSummaryDto(
    Guid Id,
    string Name,
    GuildType Type,
    double Latitude,
    double Longitude,
    DateOnly Planted,
    int MemberCount);

public record GuildDetailDto(
    Guid Id,
    string Name,
    GuildType Type,
    double Latitude,
    double Longitude,
    DateOnly Planted,
    GeoJsonGeometry? Boundary,
    List<GuildMemberDto> Members);

public record GuildMemberDto(
    Guid PlantId,
    string Species,
    string Cultivar,
    GuildRole Role);

// ─── PostHarvestBatch DTOs ──────────────────────────────────────────────────

public record PostHarvestBatchSummaryDto(
    Guid Id,
    string Species,
    string Cultivar,
    int TotalStems,
    int StemsRemaining,
    DateOnly HarvestDate,
    bool IsConditioned,
    bool InCooler);

public record PostHarvestBatchDetailDto(
    Guid Id,
    Guid SourceBedId,
    Guid SuccessionId,
    string Species,
    string Cultivar,
    int TotalStems,
    int StemsUsed,
    int StemsRemaining,
    DateOnly HarvestDate,
    List<StemGradeDto> Grades,
    bool IsConditioned,
    string? ConditioningSolution,
    decimal? WaterTempF,
    bool InCooler,
    decimal? CoolerTempF,
    string? CoolerSlot);

public record StemGradeDto(HarvestGrade Grade, int StemCount, decimal StemLengthInches);

// ─── BouquetRecipe DTOs ────────────────────────────────────────────────────

public record BouquetRecipeSummaryDto(
    Guid Id,
    string Name,
    string Category,
    int ItemCount,
    int TotalStemsPerBouquet);

public record BouquetRecipeDetailDto(
    Guid Id,
    string Name,
    string Category,
    List<RecipeItemDto> Items,
    int TotalStemsPerBouquet,
    int TotalBouquetsMade);

public record RecipeItemDto(string Species, string Cultivar, int StemCount, string? Color, string Role);

// ─── CropPlan DTOs ─────────────────────────────────────────────────────────

public record CropPlanSummaryDto(
    Guid Id,
    int SeasonYear,
    string SeasonName,
    string PlanName,
    int BedCount,
    int TotalStemsHarvested,
    decimal TotalRevenue,
    decimal TotalCosts);

public record CropPlanDetailDto(
    Guid Id,
    int SeasonYear,
    string SeasonName,
    string PlanName,
    List<BedAssignmentDto> BedAssignments,
    int TotalStemsHarvested,
    decimal TotalRevenue,
    decimal TotalCosts,
    List<CostEntryDto> Costs,
    List<RevenueEntryDto> Revenues);

public record BedAssignmentDto(Guid BedId, string Species, string Cultivar, int PlannedSuccessions);
public record CostEntryDto(string Category, decimal Amount, string? Notes);
public record RevenueEntryDto(SalesChannel Channel, decimal Amount, DateOnly Date, string? Notes);

// ─── Queries ───────────────────────────────────────────────────────────────

public record GetAllBedsQuery() : IQuery<List<FlowerBedSummaryDto>>;
public record GetBedByIdQuery(Guid BedId) : IQuery<FlowerBedDetailDto>;

public record GetAllSeedLotsQuery() : IQuery<List<SeedLotSummaryDto>>;
public record GetSeedLotByIdQuery(Guid LotId) : IQuery<SeedLotDetailDto>;

public record GetAllGuildsQuery() : IQuery<List<GuildSummaryDto>>;
public record GetGuildByIdQuery(Guid GuildId) : IQuery<GuildDetailDto>;

public record GetAllPostHarvestBatchesQuery() : IQuery<List<PostHarvestBatchSummaryDto>>;
public record GetPostHarvestBatchByIdQuery(Guid BatchId) : IQuery<PostHarvestBatchDetailDto>;

public record GetAllBouquetRecipesQuery() : IQuery<List<BouquetRecipeSummaryDto>>;
public record GetBouquetRecipeByIdQuery(Guid RecipeId) : IQuery<BouquetRecipeDetailDto>;

public record GetAllCropPlansQuery() : IQuery<List<CropPlanSummaryDto>>;
public record GetCropPlanByIdQuery(Guid PlanId) : IQuery<CropPlanDetailDto>;

// ─── Projection Interface ──────────────────────────────────────────────────

public interface IFloraProjection
{
    Task<List<FlowerBedSummaryDto>> GetAllBedsAsync(CancellationToken ct);
    Task<FlowerBedDetailDto?> GetBedDetailAsync(Guid bedId, CancellationToken ct);

    Task<List<SeedLotSummaryDto>> GetAllSeedLotsAsync(CancellationToken ct);
    Task<SeedLotDetailDto?> GetSeedLotDetailAsync(Guid lotId, CancellationToken ct);

    Task<List<GuildSummaryDto>> GetAllGuildsAsync(CancellationToken ct);
    Task<GuildDetailDto?> GetGuildDetailAsync(Guid guildId, CancellationToken ct);

    Task<List<PostHarvestBatchSummaryDto>> GetAllBatchesAsync(CancellationToken ct);
    Task<PostHarvestBatchDetailDto?> GetBatchDetailAsync(Guid batchId, CancellationToken ct);

    Task<List<BouquetRecipeSummaryDto>> GetAllRecipesAsync(CancellationToken ct);
    Task<BouquetRecipeDetailDto?> GetRecipeDetailAsync(Guid recipeId, CancellationToken ct);

    Task<List<CropPlanSummaryDto>> GetAllCropPlansAsync(CancellationToken ct);
    Task<CropPlanDetailDto?> GetCropPlanDetailAsync(Guid planId, CancellationToken ct);
}
