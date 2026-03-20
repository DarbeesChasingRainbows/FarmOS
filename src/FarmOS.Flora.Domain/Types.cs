using FarmOS.SharedKernel;

namespace FarmOS.Flora.Domain;

// ─── Typed IDs ───────────────────────────────────────────────────────
public record OrchardGuildId(Guid Value) { public static OrchardGuildId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record FlowerBedId(Guid Value) { public static FlowerBedId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record SeedLotId(Guid Value) { public static SeedLotId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record SuccessionId(Guid Value) { public static SuccessionId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record PlantId(Guid Value) { public static PlantId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ───────────────────────────────────────────────────────────
public enum GuildType { NAP, Trio, Custom }
public enum GuildRole { NitrogenFixer, PrimaryFruit, SecondaryFruit, DynamicAccumulator, Pollinator, PestRepellent, GroundCover }

// ─── New Typed IDs ──────────────────────────────────────────────────
public record PostHarvestBatchId(Guid Value) { public static PostHarvestBatchId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record BouquetRecipeId(Guid Value) { public static BouquetRecipeId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CropPlanId(Guid Value) { public static CropPlanId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum HarvestGrade { Premium, Standard, Seconds, Cull }
public enum SalesChannel { FarmersMarket, CSA, Wholesale, Wedding, DirectSale }

// ─── Value Objects ───────────────────────────────────────────────────
public record GuildMember(PlantId PlantId, string Species, string Cultivar, GuildRole Role);
public record BedDimensions(decimal LengthFeet, decimal WidthFeet);
public record CropVariety(string Species, string Cultivar, int DaysToMaturity, string? Color = null);
public record Succession(SuccessionId Id, CropVariety Variety, DateOnly SowDate, DateOnly TransplantDate,
    DateOnly HarvestWindowStart, DateOnly? HarvestWindowEnd);

// ─── Post-Harvest Value Objects ─────────────────────────────────────
public record StemGrade(HarvestGrade Grade, int StemCount, decimal StemLengthInches);
public record RecipeItem(string Species, string Cultivar, int StemCount, string? Color, string Role); // Role: focal, filler, greenery, accent
public record CostEntry(string Category, decimal Amount, string? Notes); // Categories: seed, soil, labor, supplies
public record BedAssignment(FlowerBedId BedId, CropVariety Variety, int PlannedSuccessions);
