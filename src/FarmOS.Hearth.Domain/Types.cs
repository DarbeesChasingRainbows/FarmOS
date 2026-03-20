using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Domain;

public record BatchId(Guid Value) { public static BatchId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record LivingCultureId(Guid Value) { public static LivingCultureId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum BatchPhase { Mixing, BulkFerment, Shaping, Proofing, Baking, Cooling, Complete, Discarded }
public enum FermentationPhase { Primary, Secondary, Bottled, Complete, Discarded }
public enum KombuchaType { Jun, Standard }
public enum CultureType { SourdoughStarter, JunSCOBY, StandardSCOBY }
public enum CultureHealth { Thriving, NeedsFeed, Dormant, Retired }

public record HACCPReading(DateTimeOffset Timestamp, string CriticalControlPoint, decimal TemperatureF, decimal? pH, bool WithinLimits, string? CorrectiveAction);
public record Ingredient(string Name, Quantity Amount, string? LotNumber, string? Supplier);
public record PHReading(DateTimeOffset Timestamp, decimal pH, string? Notes);
public record Flavoring(string Ingredient, Quantity Amount);
public record FeedingRecord(DateTimeOffset Timestamp, string Flour, Quantity FlourAmount, Quantity WaterAmount, decimal? pH);

// ─── Mushroom Types ───────────────────────────────────────────────
public enum MushroomPhase { Inoculation, Colonization, Fruiting, Resting, Harvest, Complete, Contaminated }
public record EnvironmentReading(DateTimeOffset Timestamp, decimal Value, string Unit, string? Notes);

// ─── Sanitation Types ─────────────────────────────────────────────
public record SanitationRecordId(Guid Value) { public static SanitationRecordId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public enum SanitationSurfaceType { FloorDrain, CuttingBoard, PrepTable, Equipment, HandwashSink, GeneralSurface }
public enum SanitizerType { Quat, Bleach, StarSan, None }

// ─── IoT / Sensor Types ───────────────────────────────────────────
public record EquipmentId(Guid Value) { public static EquipmentId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record AssetId(Guid Value) { public static AssetId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public enum SensorType { Temperature, PH, Humidity, CO2 }
public record SensorReading(string DeviceId, SensorType SensorType, decimal Value, string Unit, DateTimeOffset Timestamp);
public enum AlertLevel { Safe, Warning, Critical }
public record IoTAlert(string DeviceId, AlertLevel Level, string Message, string? CorrectiveAction, DateTimeOffset OccurredAt);

// ─── Traceability Types ───────────────────────────────────────────
public record TraceabilityRecordId(Guid Value) { public static TraceabilityRecordId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public enum CriticalTrackingEvent { Receiving, Transformation, Shipping }
public enum ProductCategory { Mushroom, Jun, Kombucha, Sourdough, Beef, Wheat, Ingredients, Other }

// ─── HACCP / cGMP Types ──────────────────────────────────────────
public record HACCPPlanId(Guid Value) { public static HACCPPlanId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CAPAId(Guid Value) { public static CAPAId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record FreezeDryerId(Guid Value) { public static FreezeDryerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record MonitoringLogId(Guid Value) { public static MonitoringLogId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum HazardType { Biological, Chemical, Physical, Regulatory }
public enum CAPAStatus { Open, InProgress, Closed, Verified }
public enum FreezeDryerPhase { Loading, Freezing, PrimaryDrying, SecondaryDrying, Complete, Aborted }

public record CCPDefinition(
    string Product,
    string CCPName,
    HazardType HazardType,
    string CriticalLimitExpression,
    string MonitoringProcedure,
    string DefaultCorrectiveAction);

public record FreezeDryerReading(
    DateTimeOffset Timestamp,
    decimal ShelfTempF,
    decimal VacuumMTorr,
    decimal? ProductTempF,
    string? Notes);

// ─── Harvest Right Cloud Integration ─────────────────────────────
/// <summary>Maps a physical Harvest Right unit to our domain FreezeDryerId.</summary>
public record HarvestRightDryerMapping(
    FreezeDryerId DomainId,
    int HarvestRightCloudId,
    string Serial,
    string Name);
