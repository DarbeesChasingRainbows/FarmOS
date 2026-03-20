using FarmOS.SharedKernel;
using FarmOS.Hearth.Domain.Events;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class SanitationRecord : AggregateRoot<SanitationRecordId>
{
    public SanitationSurfaceType SurfaceType { get; private set; }
    public string Area { get; private set; } = string.Empty;
    public string CleaningMethod { get; private set; } = string.Empty;
    public SanitizerType Sanitizer { get; private set; }
    public decimal? SanitizerPpm { get; private set; }
    public string CleanedBy { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }

    public static SanitationRecord Create(
        SanitationRecordId id,
        SanitationSurfaceType surfaceType,
        string area,
        string cleaningMethod,
        SanitizerType sanitizer,
        decimal? sanitizerPpm,
        string cleanedBy,
        DateTimeOffset timestamp)
    {
        var record = new SanitationRecord();
        record.RaiseEvent(new SanitationRecordCreated(id, surfaceType, area, cleaningMethod, sanitizer, sanitizerPpm, cleanedBy, timestamp));
        return record;
    }

    protected override void Apply(IDomainEvent @event)
    {
        if (@event is SanitationRecordCreated e)
        {
            Id = e.Id;
            SurfaceType = e.SurfaceType;
            Area = e.Area;
            CleaningMethod = e.CleaningMethod;
            Sanitizer = e.Sanitizer;
            SanitizerPpm = e.SanitizerPpm;
            CleanedBy = e.CleanedBy;
            Timestamp = e.OccurredAt;
        }
    }
}
