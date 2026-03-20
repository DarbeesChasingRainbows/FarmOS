using FarmOS.Assets.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Assets.Domain.Aggregates;

public sealed class Equipment : AggregateRoot<EquipmentId>
{
    public string Name { get; private set; } = "";
    public string Make { get; private set; } = "";
    public string Model { get; private set; } = "";
    public int? Year { get; private set; }
    public EquipmentStatus Status { get; private set; }
    public GeoPosition CurrentLocation { get; private set; } = new(0, 0);
    private readonly List<MaintenanceRecord> _maintenance = [];
    public IReadOnlyList<MaintenanceRecord> MaintenanceHistory => _maintenance;

    public static Equipment Register(string name, string make, string model, int? year, GeoPosition location)
    {
        var eq = new Equipment();
        eq.RaiseEvent(new EquipmentRegistered(EquipmentId.New(), name, make, model, year, location, DateTimeOffset.UtcNow));
        return eq;
    }

    public void RecordMaintenance(MaintenanceRecord record) =>
        RaiseEvent(new EquipmentMaintenanceRecorded(Id, record, DateTimeOffset.UtcNow));
    public void Move(GeoPosition newLocation) =>
        RaiseEvent(new EquipmentMoved(Id, newLocation, DateTimeOffset.UtcNow));
    public void Retire(string reason) =>
        RaiseEvent(new EquipmentRetired(Id, reason, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case EquipmentRegistered e: Id = e.Id; Name = e.Name; Make = e.Make; Model = e.Model; Year = e.Year; CurrentLocation = e.CurrentLocation; Status = EquipmentStatus.Active; break;
            case EquipmentMaintenanceRecorded e: _maintenance.Add(e.Record); break;
            case EquipmentMoved e: CurrentLocation = e.NewLocation; break;
            case EquipmentRetired: Status = EquipmentStatus.Retired; break;
        }
    }
}

public sealed class Structure : AggregateRoot<StructureId>
{
    public string Name { get; private set; } = "";
    public StructureType Type { get; private set; }
    public GeoJsonGeometry? Footprint { get; private set; }
    private readonly List<MaintenanceRecord> _maintenance = [];
    public IReadOnlyList<MaintenanceRecord> MaintenanceHistory => _maintenance;

    public static Structure Register(string name, StructureType type, GeoJsonGeometry? footprint)
    {
        var s = new Structure();
        s.RaiseEvent(new StructureRegistered(StructureId.New(), name, type, footprint, DateTimeOffset.UtcNow));
        return s;
    }

    public void RecordMaintenance(MaintenanceRecord record) =>
        RaiseEvent(new StructureMaintenanceRecorded(Id, record, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case StructureRegistered e: Id = e.Id; Name = e.Name; Type = e.Type; Footprint = e.Footprint; break;
            case StructureMaintenanceRecorded e: _maintenance.Add(e.Record); break;
        }
    }
}

public sealed class WaterSource : AggregateRoot<WaterSourceId>
{
    public string Name { get; private set; } = "";
    public WaterSourceType Type { get; private set; }
    public GeoPosition Position { get; private set; } = new(0, 0);
    public Quantity? FlowRate { get; private set; }

    public static WaterSource Register(string name, WaterSourceType type, GeoPosition position, Quantity? flowRate)
    {
        var ws = new WaterSource();
        ws.RaiseEvent(new WaterSourceRegistered(WaterSourceId.New(), name, type, position, flowRate, DateTimeOffset.UtcNow));
        return ws;
    }

    public void RecordWaterTest(decimal pH, decimal? tds, DateOnly date) =>
        RaiseEvent(new WaterTestRecorded(Id, pH, tds, date, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WaterSourceRegistered e: Id = e.Id; Name = e.Name; Type = e.Type; Position = e.Position; FlowRate = e.FlowRate; break;
        }
    }
}

/// <summary>
/// CompostBatch aggregate — tracks a single composting operation from start to completion.
/// Supports 6 methods: HotAerobic, ColdPassive, Permaculture, KoreanNaturalFarming, Bokashi, Vermicompost.
/// 
/// Hot Aerobic target: 55-65°C (131-149°F), turn every 3-5 days, C:N 25-30:1
/// Bokashi target: pH 3.5-4.5, 14-21 days fermentation, anaerobic
/// KNF: IMO1→4 culture progression, enhanced with LAB/FPJ/FAA/WSCA
/// Vermicompost: 18-35°C worm range, 60-70% moisture
/// </summary>
public sealed class CompostBatch : AggregateRoot<CompostBatchId>
{
    public string BatchCode { get; private set; } = "";
    public CompostMethod Method { get; private set; }
    public GeoPosition Location { get; private set; } = new(0, 0);
    public CompostPhase Phase { get; private set; }
    public decimal? CarbonRatio { get; private set; }
    public decimal? NitrogenRatio { get; private set; }
    public string? StartNotes { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public Quantity? YieldCuYd { get; private set; }

    private readonly List<CompostInput> _inputs = [];
    public IReadOnlyList<CompostInput> Inputs => _inputs;

    private readonly List<TemperatureReading> _temps = [];
    public IReadOnlyList<TemperatureReading> TemperatureLog => _temps;

    private readonly List<(DateOnly Date, string? Notes)> _turns = [];
    public IReadOnlyList<(DateOnly Date, string? Notes)> TurnLog => _turns;
    public int TurnCount => _turns.Count;

    private readonly List<KnfInput> _inoculations = [];
    public IReadOnlyList<KnfInput> Inoculations => _inoculations;

    private readonly List<PhMeasurement> _phLog = [];
    public IReadOnlyList<PhMeasurement> PhLog => _phLog;

    private readonly List<CompostNote> _notes = [];
    public IReadOnlyList<CompostNote> Notes => _notes;

    /// <summary>Calculated C:N ratio as a display string, e.g. "25:1"</summary>
    public string? CnRatioDisplay => CarbonRatio.HasValue && NitrogenRatio.HasValue && NitrogenRatio > 0
        ? $"{CarbonRatio / NitrogenRatio:F0}:1"
        : null;

    public static CompostBatch Start(
        string batchCode,
        CompostMethod method,
        GeoPosition location,
        IReadOnlyList<CompostInput> inputs,
        decimal? carbonRatio,
        decimal? nitrogenRatio,
        string? notes)
    {
        var batch = new CompostBatch();
        batch.RaiseEvent(new CompostBatchStarted(
            CompostBatchId.New(), batchCode, method, location,
            inputs, carbonRatio, nitrogenRatio, notes, DateTimeOffset.UtcNow));
        return batch;
    }

    public void RecordTemp(TemperatureReading reading) =>
        RaiseEvent(new CompostTempRecorded(Id, reading, DateTimeOffset.UtcNow));

    public void Turn(DateOnly date, string? notes = null) =>
        RaiseEvent(new CompostTurned(Id, date, notes, DateTimeOffset.UtcNow));

    public void ChangePhase(CompostPhase newPhase, string? notes = null) =>
        RaiseEvent(new CompostPhaseChanged(Id, newPhase, notes, DateTimeOffset.UtcNow));

    public void Inoculate(KnfInput input) =>
        RaiseEvent(new CompostInoculated(Id, input, DateTimeOffset.UtcNow));

    public void MeasurePH(PhMeasurement measurement) =>
        RaiseEvent(new CompostPhMeasured(Id, measurement, DateTimeOffset.UtcNow));

    public void AddNote(CompostNote note) =>
        RaiseEvent(new CompostNoteAdded(Id, note, DateTimeOffset.UtcNow));

    public void Complete(Quantity yieldCuYd, string? notes = null) =>
        RaiseEvent(new CompostBatchCompleted(Id, yieldCuYd, notes, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CompostBatchStarted e:
                Id = e.Id; BatchCode = e.BatchCode; Method = e.Method;
                Location = e.Location; _inputs.AddRange(e.Inputs);
                CarbonRatio = e.CarbonRatio; NitrogenRatio = e.NitrogenRatio;
                StartNotes = e.Notes; StartedAt = e.OccurredAt;
                Phase = CompostPhase.Active;
                break;
            case CompostTempRecorded e: _temps.Add(e.Reading); break;
            case CompostTurned e: _turns.Add((e.Date, e.Notes)); Phase = CompostPhase.Turning; break;
            case CompostPhaseChanged e: Phase = e.NewPhase; break;
            case CompostInoculated e: _inoculations.Add(e.Input); break;
            case CompostPhMeasured e: _phLog.Add(e.Measurement); break;
            case CompostNoteAdded e: _notes.Add(e.Note); break;
            case CompostBatchCompleted e: YieldCuYd = e.YieldCuYd; Phase = CompostPhase.Finished; break;
        }
    }
}


public sealed class Material : AggregateRoot<MaterialId>
{
    public string Name { get; private set; } = "";
    public string Category { get; private set; } = "";
    public Quantity OnHand { get; private set; } = new(0, "", "");
    public string? Supplier { get; private set; }
    public bool IsOrganic { get; private set; }

    public static Material Register(string name, string category, Quantity onHand, string? supplier, bool isOrganic)
    {
        var mat = new Material();
        mat.RaiseEvent(new MaterialRegistered(MaterialId.New(), name, category, onHand, supplier, isOrganic, DateTimeOffset.UtcNow));
        return mat;
    }

    public Result<MaterialId, DomainError> Use(Quantity qty, string purpose)
    {
        if (qty.Value > OnHand.Value)
            return DomainError.BusinessRule($"Insufficient material: have {OnHand.Value}, need {qty.Value}.");
        RaiseEvent(new MaterialUsed(Id, qty, purpose, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Restock(Quantity qty, decimal? cost) =>
        RaiseEvent(new MaterialRestocked(Id, qty, cost, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case MaterialRegistered e: Id = e.Id; Name = e.Name; Category = e.Category; OnHand = e.OnHand; Supplier = e.Supplier; IsOrganic = e.IsOrganic; break;
            case MaterialUsed e: OnHand = OnHand with { Value = OnHand.Value - e.Qty.Value }; break;
            case MaterialRestocked e: OnHand = OnHand with { Value = OnHand.Value + e.Qty.Value }; break;
        }
    }
}
