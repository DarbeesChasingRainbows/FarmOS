using FarmOS.Flora.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Flora.Application.Commands;

// ─── Guild Commands ──────────────────────────────────────────

public record CreateGuildCommand(string Name, GuildType Type, GeoPosition Position, DateOnly Planted) : ICommand<Guid>;
public record AddGuildMemberCommand(Guid GuildId, GuildMember Member) : ICommand<Guid>;

// ─── FlowerBed Commands ──────────────────────────────────────

public record CreateFlowerBedCommand(string Name, string Block, BedDimensions Dimensions) : ICommand<Guid>;
public record PlanSuccessionCommand(Guid BedId, CropVariety Variety, DateOnly SowDate, DateOnly TransplantDate, DateOnly HarvestStart) : ICommand<Guid>;

// ─── SeedLot Commands ────────────────────────────────────────

public record CreateSeedLotCommand(CropVariety Variety, string Supplier, Quantity Quantity, decimal GerminationPct, int HarvestYear, bool IsOrganic) : ICommand<Guid>;
public record WithdrawSeedCommand(Guid SeedLotId, Quantity Quantity, Guid DestinationBedId) : ICommand<Guid>;
public record RestockSeedCommand(Guid SeedLotId, Quantity Quantity, string? LotNumber) : ICommand<Guid>;

// ─── FlowerBed Lifecycle Commands ───────────────────────────

public record RecordSeedingCommand(Guid BedId, Guid SuccessionId, Guid SeedLotId, Quantity Quantity, DateOnly Date) : ICommand<Guid>;
public record RecordTransplantCommand(Guid BedId, Guid SuccessionId, Quantity Quantity, DateOnly Date) : ICommand<Guid>;
public record RecordHarvestCommand(Guid BedId, Guid SuccessionId, Quantity Stems, DateOnly Date) : ICommand<Guid>;

// ─── PostHarvestBatch Commands ──────────────────────────────

public record CreatePostHarvestBatchCommand(Guid SourceBedId, Guid SuccessionId,
    string Species, string Cultivar, int TotalStems, DateOnly HarvestDate) : ICommand<Guid>;
public record GradeStemsCommand(Guid BatchId, StemGrade Grade) : ICommand<Guid>;
public record ConditionStemsCommand(Guid BatchId, string Solution, decimal WaterTempF) : ICommand<Guid>;
public record MoveBatchToCoolerCommand(Guid BatchId, decimal TemperatureF, string? SlotLabel) : ICommand<Guid>;
public record UseBatchStemsCommand(Guid BatchId, int StemsUsed, string Purpose) : ICommand<Guid>;

// ─── BouquetRecipe Commands ─────────────────────────────────

public record CreateBouquetRecipeCommand(string Name, string Category) : ICommand<Guid>;
public record AddRecipeItemCommand(Guid RecipeId, RecipeItem Item) : ICommand<Guid>;
public record RemoveRecipeItemCommand(Guid RecipeId, string Species, string Cultivar) : ICommand<Guid>;
public record MakeBouquetCommand(Guid RecipeId, int Quantity, DateOnly Date, string? Notes) : ICommand<Guid>;

// ─── CropPlan Commands ──────────────────────────────────────

public record CreateCropPlanCommand(int SeasonYear, string SeasonName, string PlanName) : ICommand<Guid>;
public record AssignBedToPlanCommand(Guid PlanId, BedAssignment Assignment) : ICommand<Guid>;
public record RecordYieldCommand(Guid PlanId, Guid BedId, Guid SuccessionId,
    int StemsHarvested, decimal StemsPerLinearFoot, DateOnly Date) : ICommand<Guid>;
public record RecordCropCostCommand(Guid PlanId, CostEntry Cost) : ICommand<Guid>;
public record RecordCropRevenueCommand(Guid PlanId, SalesChannel Channel, decimal Amount, DateOnly Date, string? Notes) : ICommand<Guid>;
