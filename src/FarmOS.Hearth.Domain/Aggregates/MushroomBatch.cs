using FarmOS.SharedKernel;
using FarmOS.Hearth.Domain.Events;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class MushroomBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; } = "";
    public string Species { get; private set; } = "";
    public string Cultivar { get; private set; } = "";
    public string SubstrateType { get; private set; } = "";
    public MushroomPhase Phase { get; private set; }
    
    private readonly List<EnvironmentReading> _temperatureLog = [];
    public IReadOnlyList<EnvironmentReading> TemperatureLog => _temperatureLog;
    
    private readonly List<EnvironmentReading> _humidityLog = [];
    public IReadOnlyList<EnvironmentReading> HumidityLog => _humidityLog;
    
    public DateTimeOffset InoculatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Quantity? Yield { get; private set; }
    public int FlushCount { get; private set; }
    public bool IsContaminated { get; private set; }
    public string? ContaminationNotes { get; private set; }
    public AssetId? GrowRoomId { get; private set; }

    public static MushroomBatch Start(BatchId id, string batchCode, string species, string cultivar, string substrateType, AssetId? growRoomId, DateTimeOffset timestamp)
    {
        var batch = new MushroomBatch();
        batch.RaiseEvent(new MushroomBatchStarted(id, batchCode, species, substrateType, timestamp, DateTimeOffset.UtcNow));
        return batch;
    }

    public void RecordTemperature(EnvironmentReading reading) => 
        RaiseEvent(new MushroomTemperatureRecorded(Id, reading, DateTimeOffset.UtcNow));

    public void RecordHumidity(EnvironmentReading reading) => 
        RaiseEvent(new MushroomHumidityRecorded(Id, reading, DateTimeOffset.UtcNow));

    public Result<BatchId, DomainError> AdvancePhase(MushroomPhase next, DateTimeOffset timestamp)
    {
        if (IsContaminated) return DomainError.Conflict("Cannot advance phase on a contaminated batch.");
        if (Phase == MushroomPhase.Complete) return DomainError.Conflict("Cannot advance phase on a completed batch.");
        
        RaiseEvent(new MushroomPhaseAdvanced(Id, Phase.ToString(), next.ToString(), timestamp));
        return Id;
    }

    public Result<BatchId, DomainError> RecordFlush(Quantity yield, DateOnly date)
    {
        if (Phase != MushroomPhase.Fruiting && Phase != MushroomPhase.Resting)
            return DomainError.Conflict("Can only record flushes during fruiting or resting phases.");

        RaiseEvent(new MushroomFlushRecorded(Id, yield, FlushCount + 1, date, DateTimeOffset.UtcNow));
        RaiseEvent(new MushroomHarvestAvailable(Species, yield, date, DateTimeOffset.UtcNow)); // Cross-context
        return Id;
    }

    public void MarkContaminated(string reason, DateTimeOffset timestamp) => 
        RaiseEvent(new MushroomBatchContaminated(Id, reason, timestamp, DateTimeOffset.UtcNow));

    public void Complete(Quantity totalYield) => 
        RaiseEvent(new MushroomBatchCompleted(Id, totalYield, FlushCount, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case MushroomBatchStarted e:
                Id = e.Id;
                BatchCode = e.BatchCode;
                Species = e.Species;
                SubstrateType = e.SubstrateType;
                InoculatedAt = e.InoculatedAt;
                Phase = MushroomPhase.Inoculation;
                FlushCount = 0;
                IsContaminated = false;
                break;
            case MushroomTemperatureRecorded e:
                _temperatureLog.Add(e.Reading);
                break;
            case MushroomHumidityRecorded e:
                _humidityLog.Add(e.Reading);
                break;
            case MushroomPhaseAdvanced e:
                if (Enum.TryParse<MushroomPhase>(e.NewPhase, out var next))
                    Phase = next;
                break;
            case MushroomFlushRecorded e:
                FlushCount = e.FlushNumber;
                Yield = Yield is null ? e.Yield : Yield with { Value = Yield.Value + e.Yield.Value };
                break;
            case MushroomBatchContaminated e:
                Phase = MushroomPhase.Contaminated;
                IsContaminated = true;
                ContaminationNotes = e.Reason;
                break;
            case MushroomBatchCompleted e:
                Phase = MushroomPhase.Complete;
                Yield = e.TotalYield;
                FlushCount = e.TotalFlushes;
                CompletedAt = e.CompletedAt;
                break;
        }
    }
}
