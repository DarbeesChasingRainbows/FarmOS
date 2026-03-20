using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class SourdoughBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; } = "";
    public LivingCultureId StarterId { get; private set; } = new(Guid.Empty);
    private readonly List<Ingredient> _ingredients = [];
    public IReadOnlyList<Ingredient> Ingredients => _ingredients;
    private readonly List<HACCPReading> _ccpReadings = [];
    public IReadOnlyList<HACCPReading> CCPReadings => _ccpReadings;
    public BatchPhase Phase { get; private set; }
    public Quantity? Yield { get; private set; }

    public static SourdoughBatch Start(string batchCode, LivingCultureId starterId, IReadOnlyList<Ingredient> ingredients)
    {
        var batch = new SourdoughBatch();
        batch.RaiseEvent(new SourdoughBatchStarted(BatchId.New(), batchCode, starterId, ingredients, DateTimeOffset.UtcNow));
        return batch;
    }

    public Result<BatchId, DomainError> RecordCCP(HACCPReading reading)
    {
        if (!reading.WithinLimits && string.IsNullOrWhiteSpace(reading.CorrectiveAction))
            return DomainError.BusinessRule("Corrective action is required when CCP reading is out of limits.");

        RaiseEvent(new CCPReadingRecorded(Id, reading, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BatchId, DomainError> AdvancePhase(BatchPhase next)
    {
        if (Phase == BatchPhase.Complete || Phase == BatchPhase.Discarded)
            return DomainError.Conflict($"Batch is already {Phase}.");
        RaiseEvent(new SourdoughPhaseAdvanced(Id, Phase, next, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Complete(Quantity yield) => RaiseEvent(new SourdoughBatchCompleted(Id, yield, DateTimeOffset.UtcNow));
    public void Discard(string reason) => RaiseEvent(new SourdoughBatchDiscarded(Id, reason, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SourdoughBatchStarted e: Id = e.Id; BatchCode = e.BatchCode; StarterId = e.StarterId; _ingredients.AddRange(e.Ingredients); Phase = BatchPhase.Mixing; break;
            case CCPReadingRecorded e: _ccpReadings.Add(e.Reading); break;
            case SourdoughPhaseAdvanced e: Phase = e.Next; break;
            case SourdoughBatchCompleted e: Phase = BatchPhase.Complete; Yield = e.Yield; break;
            case SourdoughBatchDiscarded: Phase = BatchPhase.Discarded; break;
        }
    }
}

public sealed class KombuchaBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; } = "";
    public KombuchaType Type { get; private set; }
    public LivingCultureId SCOBYId { get; private set; } = new(Guid.Empty);
    public FermentationPhase Phase { get; private set; }
    public decimal StartingPH { get; private set; }
    public decimal? CurrentPH { get; private set; }
    private readonly List<PHReading> _phLog = [];
    public IReadOnlyList<PHReading> PHLog => _phLog;
    public string TeaType { get; private set; } = "";
    public string Sweetener { get; private set; } = "";
    public Quantity Volume { get; private set; } = new(0, "gallons", "volume");
    private readonly List<Flavoring> _flavorings = [];
    public IReadOnlyList<Flavoring> Flavorings => _flavorings;

    public static KombuchaBatch Start(string batchCode, KombuchaType type, LivingCultureId scobyId,
        string teaType, string sweetener, Quantity volume, decimal startingPH)
    {
        var batch = new KombuchaBatch();
        batch.RaiseEvent(new KombuchaBatchStarted(BatchId.New(), batchCode, type, scobyId, teaType, sweetener, volume, startingPH, DateTimeOffset.UtcNow));
        return batch;
    }

    public void RecordPH(decimal pH, string? notes = null) =>
        RaiseEvent(new KombuchaPHRecorded(Id, new PHReading(DateTimeOffset.UtcNow, pH, notes), DateTimeOffset.UtcNow));

    public void AddFlavoring(Flavoring flavoring) =>
        RaiseEvent(new KombuchaFlavoringAdded(Id, flavoring, DateTimeOffset.UtcNow));

    public Result<BatchId, DomainError> AdvancePhase(FermentationPhase next)
    {
        if (Phase == FermentationPhase.Complete || Phase == FermentationPhase.Discarded)
            return DomainError.Conflict($"Batch is already {Phase}.");
        RaiseEvent(new KombuchaPhaseAdvanced(Id, Phase, next, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Complete(Quantity bottleCount) => RaiseEvent(new KombuchaBatchCompleted(Id, bottleCount, DateTimeOffset.UtcNow));
    public void Discard(string reason) => RaiseEvent(new KombuchaBatchDiscarded(Id, reason, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case KombuchaBatchStarted e:
                Id = e.Id; BatchCode = e.BatchCode; Type = e.Type; SCOBYId = e.SCOBYId;
                TeaType = e.TeaType; Sweetener = e.Sweetener; Volume = e.Volume;
                StartingPH = e.StartingPH; CurrentPH = e.StartingPH; Phase = FermentationPhase.Primary; break;
            case KombuchaPHRecorded e: _phLog.Add(e.Reading); CurrentPH = e.Reading.pH; break;
            case KombuchaFlavoringAdded e: _flavorings.Add(e.Flavoring); break;
            case KombuchaPhaseAdvanced e: Phase = e.Next; break;
            case KombuchaBatchCompleted: Phase = FermentationPhase.Complete; break;
            case KombuchaBatchDiscarded: Phase = FermentationPhase.Discarded; break;
        }
    }
}

public sealed class LivingCulture : AggregateRoot<LivingCultureId>
{
    public string Name { get; private set; } = "";
    public CultureType Type { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public LivingCultureId? ParentId { get; private set; }
    private readonly List<FeedingRecord> _feedingLog = [];
    public IReadOnlyList<FeedingRecord> FeedingLog => _feedingLog;
    public CultureHealth Health { get; private set; }

    public static LivingCulture Create(string name, CultureType type, DateOnly birthDate, LivingCultureId? parentId = null)
    {
        var culture = new LivingCulture();
        culture.RaiseEvent(new CultureCreated(LivingCultureId.New(), name, type, birthDate, parentId, DateTimeOffset.UtcNow));
        return culture;
    }

    public void Feed(FeedingRecord feeding) => RaiseEvent(new CultureFed(Id, feeding, DateTimeOffset.UtcNow));

    public LivingCultureId Split(string newName, DateOnly date)
    {
        var offspringId = LivingCultureId.New();
        RaiseEvent(new CultureSplit(Id, offspringId, newName, date, DateTimeOffset.UtcNow));
        return offspringId;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CultureCreated e: Id = e.Id; Name = e.Name; Type = e.Type; BirthDate = e.BirthDate; ParentId = e.ParentId; Health = CultureHealth.Thriving; break;
            case CultureFed e: _feedingLog.Add(e.Feeding); break;
            case CultureSplit: break; // Parent doesn't change state on split
        }
    }
}
