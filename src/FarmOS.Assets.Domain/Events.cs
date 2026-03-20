using FarmOS.SharedKernel;

namespace FarmOS.Assets.Domain.Events;

// Equipment
public record EquipmentRegistered(EquipmentId Id, string Name, string Make, string Model, int? Year, GeoPosition CurrentLocation, DateTimeOffset OccurredAt) : IDomainEvent;
public record EquipmentMaintenanceRecorded(EquipmentId Id, MaintenanceRecord Record, DateTimeOffset OccurredAt) : IDomainEvent;
public record EquipmentMoved(EquipmentId Id, GeoPosition NewLocation, DateTimeOffset OccurredAt) : IDomainEvent;
public record EquipmentRetired(EquipmentId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// Structure
public record StructureRegistered(StructureId Id, string Name, StructureType Type, GeoJsonGeometry? Footprint, DateTimeOffset OccurredAt) : IDomainEvent;
public record StructureMaintenanceRecorded(StructureId Id, MaintenanceRecord Record, DateTimeOffset OccurredAt) : IDomainEvent;

// Water Source
public record WaterSourceRegistered(WaterSourceId Id, string Name, WaterSourceType Type, GeoPosition Position, Quantity? FlowRate, DateTimeOffset OccurredAt) : IDomainEvent;
public record WaterTestRecorded(WaterSourceId Id, decimal pH, decimal? TDS, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Compost Batch ──────────────────────────────────────────────────────────

/// <summary>Starts a new compost batch with method, optional C:N ratio tracking, and initial inputs.</summary>
public record CompostBatchStarted(
    CompostBatchId Id,
    string BatchCode,
    CompostMethod Method,
    GeoPosition Location,
    IReadOnlyList<CompostInput> Inputs,
    decimal? CarbonRatio,
    decimal? NitrogenRatio,
    string? Notes,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Records a core pile temperature. TemperatureF allows auto-detection of composting phase zones.</summary>
public record CompostTempRecorded(CompostBatchId Id, TemperatureReading Reading, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>The pile was physically turned, introducing oxygen and redistributing heat zones.</summary>
public record CompostTurned(CompostBatchId Id, DateOnly Date, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Explicit phase transition — Active → Turning → Fermentation → Inoculation → Curing → Finished.</summary>
public record CompostPhaseChanged(CompostBatchId Id, CompostPhase NewPhase, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Korean Natural Farming inoculation event.
/// Tracks IMO1-4 culture applications, LAB, FPJ, FAA, WSCA, OHN inputs.
/// </summary>
public record CompostInoculated(CompostBatchId Id, KnfInput Input, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>pH measurement — critical for Bokashi (target 3.5-4.5) and general monitoring.</summary>
public record CompostPhMeasured(CompostBatchId Id, PhMeasurement Measurement, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Freeform typed note: Observation, Amendment, Issue, Milestone, or Harvest.</summary>
public record CompostNoteAdded(CompostBatchId Id, CompostNote Note, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Batch is complete — records final yield volume.</summary>
public record CompostBatchCompleted(CompostBatchId Id, Quantity YieldCuYd, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;


// Material
public record MaterialRegistered(MaterialId Id, string Name, string Category, Quantity OnHand, string? Supplier, bool IsOrganic, DateTimeOffset OccurredAt) : IDomainEvent;
public record MaterialUsed(MaterialId Id, Quantity Qty, string Purpose, DateTimeOffset OccurredAt) : IDomainEvent;
public record MaterialRestocked(MaterialId Id, Quantity Qty, decimal? CostDollars, DateTimeOffset OccurredAt) : IDomainEvent;
