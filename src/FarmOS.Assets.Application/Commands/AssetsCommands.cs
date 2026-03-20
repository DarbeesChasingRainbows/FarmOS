using FarmOS.Assets.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Assets.Application.Commands;

// ─── Equipment ───────────────────────────────────────────────────────
public record RegisterEquipmentCommand(string Name, string Make, string Model, int? Year, GeoPosition Location) : ICommand<Guid>;
public record RecordEquipmentMaintenanceCommand(Guid EquipmentId, MaintenanceRecord Record) : ICommand<Guid>;
public record MoveEquipmentCommand(Guid EquipmentId, GeoPosition NewLocation) : ICommand<Guid>;
public record RetireEquipmentCommand(Guid EquipmentId, string Reason) : ICommand<Guid>;

// ─── Structure ───────────────────────────────────────────────────────
public record RegisterStructureCommand(string Name, StructureType Type, GeoJsonGeometry? Footprint) : ICommand<Guid>;
public record RecordStructureMaintenanceCommand(Guid StructureId, MaintenanceRecord Record) : ICommand<Guid>;

// ─── Water Source ────────────────────────────────────────────────────
public record RegisterWaterSourceCommand(string Name, WaterSourceType Type, GeoPosition Position, Quantity? FlowRate) : ICommand<Guid>;
public record RecordWaterTestCommand(Guid WaterSourceId, decimal pH, decimal? TDS, DateOnly Date) : ICommand<Guid>;

// ─── Compost Batch ───────────────────────────────────────────────────

/// <summary>
/// Start a new compost batch. Method determines how it will be tracked going forward.
/// CarbonRatio/NitrogenRatio are optional parts used to compute the C:N ratio display.
/// Ideal: C:N = 25-30:1 for hot aerobic; Bokashi uses inoculant ratio instead.
/// </summary>
public record StartCompostBatchCommand(
    string BatchCode,
    CompostMethod Method,
    GeoPosition Location,
    IReadOnlyList<CompostInput> Inputs,
    decimal? CarbonRatio,
    decimal? NitrogenRatio,
    string? Notes) : ICommand<Guid>;

public record RecordCompostTempCommand(Guid BatchId, TemperatureReading Reading) : ICommand<Guid>;
public record TurnCompostCommand(Guid BatchId, DateOnly Date, string? Notes) : ICommand<Guid>;
public record ChangeCompostPhaseCommand(Guid BatchId, CompostPhase NewPhase, string? Notes) : ICommand<Guid>;
public record InoculateCompostCommand(Guid BatchId, KnfInput Input) : ICommand<Guid>;
public record MeasureCompostPhCommand(Guid BatchId, PhMeasurement Measurement) : ICommand<Guid>;
public record AddCompostNoteCommand(Guid BatchId, CompostNote Note) : ICommand<Guid>;
public record CompleteCompostBatchCommand(Guid BatchId, Quantity YieldCuYd, string? Notes) : ICommand<Guid>;


// ─── Material ────────────────────────────────────────────────────────
public record RegisterMaterialCommand(string Name, string Category, Quantity OnHand, string? Supplier, bool IsOrganic) : ICommand<Guid>;
public record UseMaterialCommand(Guid MaterialId, Quantity Qty, string Purpose) : ICommand<Guid>;
public record RestockMaterialCommand(Guid MaterialId, Quantity Qty, decimal? CostDollars) : ICommand<Guid>;
