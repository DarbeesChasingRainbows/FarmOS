using FarmOS.SharedKernel;

namespace FarmOS.Flora.Domain.Events;

public record GuildCreated(OrchardGuildId Id, string Name, GuildType Type, GeoPosition Position, DateOnly Planted, DateTimeOffset OccurredAt) : IDomainEvent;
public record GuildMemberAdded(OrchardGuildId GuildId, GuildMember Member, DateTimeOffset OccurredAt) : IDomainEvent;
public record GuildBoundaryUpdated(OrchardGuildId Id, GeoJsonGeometry Boundary, DateTimeOffset OccurredAt) : IDomainEvent;

public record FlowerBedCreated(FlowerBedId Id, string Name, string Block, BedDimensions Dimensions, DateTimeOffset OccurredAt) : IDomainEvent;
public record SuccessionPlanned(FlowerBedId BedId, SuccessionId SuccId, CropVariety Variety, DateOnly SowDate, DateOnly TransplantDate, DateOnly HarvestStart, DateTimeOffset OccurredAt) : IDomainEvent;
public record SeedingRecorded(FlowerBedId BedId, SuccessionId SuccId, SeedLotId SeedLot, Quantity Qty, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record TransplantRecorded(FlowerBedId BedId, SuccessionId SuccId, Quantity Qty, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record FlowerHarvestRecorded(FlowerBedId BedId, SuccessionId SuccId, Quantity Stems, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

public record SeedLotCreated(SeedLotId Id, CropVariety Variety, string Supplier, Quantity Quantity, decimal GerminationPct, int HarvestYear, bool IsOrganic, DateTimeOffset OccurredAt) : IDomainEvent;
public record SeedWithdrawn(SeedLotId Id, Quantity Qty, FlowerBedId Destination, DateTimeOffset OccurredAt) : IDomainEvent;
public record SeedRestocked(SeedLotId Id, Quantity Qty, string? LotNumber, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── PostHarvestBatch Events ────────────────────────────────────────

public record PostHarvestBatchCreated(PostHarvestBatchId Id, FlowerBedId SourceBed, SuccessionId SuccessionId,
    string Species, string Cultivar, int TotalStems, DateOnly HarvestDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record StemsGraded(PostHarvestBatchId BatchId, StemGrade Grade, DateTimeOffset OccurredAt) : IDomainEvent;
public record StemsConditioned(PostHarvestBatchId BatchId, string Solution, decimal WaterTempF, DateTimeOffset OccurredAt) : IDomainEvent;
public record BatchMovedToCooler(PostHarvestBatchId BatchId, decimal TemperatureF, string? SlotLabel, DateTimeOffset OccurredAt) : IDomainEvent;
public record BatchStemsUsed(PostHarvestBatchId BatchId, int StemsUsed, string Purpose, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── BouquetRecipe Events ───────────────────────────────────────────

public record BouquetRecipeCreated(BouquetRecipeId Id, string Name, string Category, DateTimeOffset OccurredAt) : IDomainEvent;
public record RecipeItemAdded(BouquetRecipeId RecipeId, RecipeItem Item, DateTimeOffset OccurredAt) : IDomainEvent;
public record RecipeItemRemoved(BouquetRecipeId RecipeId, string Species, string Cultivar, DateTimeOffset OccurredAt) : IDomainEvent;
public record BouquetMade(BouquetRecipeId RecipeId, int Quantity, DateOnly Date, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── CropPlan Events ────────────────────────────────────────────────

public record CropPlanCreated(CropPlanId Id, int SeasonYear, string SeasonName, string PlanName, DateTimeOffset OccurredAt) : IDomainEvent;
public record BedAssignedToPlan(CropPlanId PlanId, BedAssignment Assignment, DateTimeOffset OccurredAt) : IDomainEvent;
public record YieldRecorded(CropPlanId PlanId, FlowerBedId BedId, SuccessionId SuccessionId,
    int StemsHarvested, decimal StemsPerLinearFoot, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record CropCostRecorded(CropPlanId PlanId, CostEntry Cost, DateTimeOffset OccurredAt) : IDomainEvent;
public record CropRevenueRecorded(CropPlanId PlanId, SalesChannel Channel, decimal Amount, DateOnly Date, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
