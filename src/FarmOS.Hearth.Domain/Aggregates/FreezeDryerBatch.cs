using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class FreezeDryerBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; } = "";
    public FreezeDryerId DryerId { get; private set; } = new(Guid.Empty);
    public string ProductDescription { get; private set; } = "";
    public FreezeDryerPhase Phase { get; private set; }
    public decimal PreDryWeight { get; private set; }
    public decimal? PostDryWeight { get; private set; }
    private readonly List<FreezeDryerReading> _readings = [];
    public IReadOnlyList<FreezeDryerReading> Readings => _readings;

    public static FreezeDryerBatch Start(string batchCode, FreezeDryerId dryerId, string productDescription, decimal preDryWeight)
    {
        var batch = new FreezeDryerBatch();
        batch.RaiseEvent(new FreezeDryerBatchStarted(BatchId.New(), batchCode, dryerId, productDescription, preDryWeight, DateTimeOffset.UtcNow));
        return batch;
    }

    public Result<BatchId, DomainError> RecordReading(FreezeDryerReading reading)
    {
        if (Phase is FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted)
            return DomainError.Conflict($"Cannot record reading — batch is {Phase}.");
        if (Phase is FreezeDryerPhase.Loading)
            return DomainError.BusinessRule("Cannot record reading during Loading phase. Advance to Freezing first.");

        RaiseEvent(new FreezeDryerReadingRecorded(Id, reading, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BatchId, DomainError> AdvancePhase(FreezeDryerPhase next)
    {
        if (Phase is FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted)
            return DomainError.Conflict($"Batch is already {Phase}.");

        // Enforce forward-only phase transitions (no skipping)
        var validNext = Phase switch
        {
            FreezeDryerPhase.Loading => FreezeDryerPhase.Freezing,
            FreezeDryerPhase.Freezing => FreezeDryerPhase.PrimaryDrying,
            FreezeDryerPhase.PrimaryDrying => FreezeDryerPhase.SecondaryDrying,
            _ => (FreezeDryerPhase?)null
        };

        if (validNext is null || next != validNext)
            return DomainError.BusinessRule($"Cannot advance from {Phase} to {next}.");

        RaiseEvent(new FreezeDryerPhaseAdvanced(Id, Phase, next, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BatchId, DomainError> Complete(decimal postDryWeight)
    {
        if (Phase != FreezeDryerPhase.SecondaryDrying)
            return DomainError.BusinessRule($"Cannot complete batch — must be in SecondaryDrying phase (currently {Phase}).");

        RaiseEvent(new FreezeDryerBatchCompleted(Id, postDryWeight, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<BatchId, DomainError> Abort(string reason)
    {
        if (Phase is FreezeDryerPhase.Complete or FreezeDryerPhase.Aborted)
            return DomainError.Conflict($"Batch is already {Phase}.");

        RaiseEvent(new FreezeDryerBatchAborted(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case FreezeDryerBatchStarted e:
                Id = e.Id; BatchCode = e.BatchCode; DryerId = e.DryerId;
                ProductDescription = e.ProductDescription; PreDryWeight = e.PreDryWeight;
                Phase = FreezeDryerPhase.Loading; break;
            case FreezeDryerPhaseAdvanced e: Phase = e.Next; break;
            case FreezeDryerReadingRecorded e: _readings.Add(e.Reading); break;
            case FreezeDryerBatchCompleted e: Phase = FreezeDryerPhase.Complete; PostDryWeight = e.PostDryWeight; break;
            case FreezeDryerBatchAborted: Phase = FreezeDryerPhase.Aborted; break;
        }
    }
}
