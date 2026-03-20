using FarmOS.SharedKernel;

namespace FarmOS.Assets.Domain;

public record EquipmentId(Guid Value) { public static EquipmentId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record StructureId(Guid Value) { public static StructureId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record WaterSourceId(Guid Value) { public static WaterSourceId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CompostBatchId(Guid Value) { public static CompostBatchId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record MaterialId(Guid Value) { public static MaterialId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum EquipmentStatus { Active, Maintenance, Retired }
public enum StructureType { Barn, Greenhouse, Hoop, Cooler, Workshop, CuttingStation, BakeryKitchen, BottlingRoom }
public enum WaterSourceType { Well, Pond, Creek, CityWater, Rainwater, IrrigationLine }

// ─── Compost ────────────────────────────────────────────────────────────────

/// <summary>
/// Composting methodology. Different methods require tracking different inputs and metrics.
/// HotAerobic: thermophilic bacteria, 55-65°C, regular turning, C:N 25-30:1
/// ColdPassive: slow anaerobic, 6-12 months, minimal management
/// Permaculture: trench, sheet mulch, Hugelkultur — soil-building focus
/// KoreanNaturalFarming: IMO1-4 culture, fermented inputs (FPJ, LAB, FAA, WSCA)
/// Bokashi: anaerobic fermentation with inoculated bran, pH tracking
/// Vermicompost: worm-mediated, 18-35°C, castings-yield tracking
/// </summary>
public enum CompostMethod
{
    HotAerobic,
    ColdPassive,
    Permaculture,
    KoreanNaturalFarming,
    Bokashi,
    Vermicompost
}

/// <summary>
/// Lifecycle phase of a compost batch.
/// Specific meaning varies by method:
/// - HotAerobic: Active → Turning → Curing → Finished
/// - Bokashi: Active → Fermentation → Curing → Finished
/// - KNF: Active → Inoculation → Curing → Finished
/// - Vermicompost: Active → Curing → Finished
/// </summary>
public enum CompostPhase { Active, Turning, Fermentation, Inoculation, Curing, Finished, Abandoned }

public record MaintenanceRecord(DateOnly Date, string Description, decimal? CostDollars, string? Technician);

/// <summary>
/// A single input material added to a compost batch.
/// Type: "Browns" (carbon-rich) or "Greens" (nitrogen-rich) or "Inoculant" etc.
/// CnRatio: approximate C:N ratio of this material (Browns ~50-100:1, Greens ~10-20:1)
/// </summary>
public record CompostInput(string Material, Quantity Amount, string Type, decimal? CnRatio = null);

public record TemperatureReading(DateTimeOffset Timestamp, decimal TemperatureF);

/// <summary>
/// Korean Natural Farming fermented/inoculated input.
/// InputType: IMO1, IMO2, IMO3, IMO4, LAB (Lactic Acid Bacteria),
///            FPJ (Fermented Plant Juice), FAA (Fish Amino Acid),
///            WSCA (Water Soluble Calcium), OHN (Oriental Herbal Nutrient)
/// </summary>
public record KnfInput(string InputType, string Description, DateOnly PreparedDate, Quantity Amount);

/// <summary>
/// A pH measurement — critical for Bokashi (target 3.5-4.5) and
/// monitoring general compost health.
/// </summary>
public record PhMeasurement(DateOnly Date, decimal PH, string? Notes = null);

/// <summary>
/// A general observation, amendment note, issue report, or milestone record.
/// Category: Observation | Amendment | Issue | Milestone | Harvest
/// </summary>
public record CompostNote(DateOnly Date, string Category, string Body);
