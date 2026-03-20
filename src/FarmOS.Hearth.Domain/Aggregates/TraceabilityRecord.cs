using FarmOS.SharedKernel;
using FarmOS.Hearth.Domain.Events;

namespace FarmOS.Hearth.Domain.Aggregates;

public sealed class TraceabilityRecord : AggregateRoot<TraceabilityRecordId>
{
    public CriticalTrackingEvent EventType { get; private set; }
    public ProductCategory Category { get; private set; }
    public string ProductDescription { get; private set; } = string.Empty;
    public string LotId { get; private set; } = string.Empty;
    public Quantity Amount { get; private set; } = default!;
    public string? SourceLocation { get; private set; }
    public string? DestinationLocation { get; private set; }
    public string? SourceLotId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public bool IsDirectToConsumer { get; private set; }

    /// <summary>
    /// FSMA 204: 2 years (730 days) for standard supply chain,
    /// 180 days for direct-to-consumer sales.
    /// </summary>
    public int RetentionDays => IsDirectToConsumer ? 180 : 730;

    public bool IsExpired(DateTimeOffset now) => now > RecordedAt.AddDays(RetentionDays);

    // Required for Rehydration
    private TraceabilityRecord() { }

    public static TraceabilityRecord LogReceiving(
        ProductCategory category, string description, string lotId, Quantity amount, string sourceSupplier, DateTimeOffset timestamp)
    {
        var record = new TraceabilityRecord();
        record.RaiseEvent(new TraceabilityEventLogged(
            TraceabilityRecordId.New(), CriticalTrackingEvent.Receiving, category, description, lotId, amount, sourceSupplier, null, null, timestamp));
        return record;
    }

    public static TraceabilityRecord LogTransformation(
        ProductCategory category, string description, string newLotId, Quantity amount, string sourceLotId, DateTimeOffset timestamp)
    {
        var record = new TraceabilityRecord();
        record.RaiseEvent(new TraceabilityEventLogged(
            TraceabilityRecordId.New(), CriticalTrackingEvent.Transformation, category, description, newLotId, amount, null, null, sourceLotId, timestamp));
        return record;
    }

    public static TraceabilityRecord LogShipping(
        ProductCategory category, string description, string lotId, Quantity amount, string destination, DateTimeOffset timestamp)
    {
        var record = new TraceabilityRecord();
        record.RaiseEvent(new TraceabilityEventLogged(
            TraceabilityRecordId.New(), CriticalTrackingEvent.Shipping, category, description, lotId, amount, null, destination, null, timestamp));
        return record;
    }

    protected override void Apply(IDomainEvent @event)
    {
        if (@event is TraceabilityEventLogged e)
        {
            Id = e.Id;
            EventType = e.EventType;
            Category = e.Category;
            ProductDescription = e.ProductDescription;
            LotId = e.LotId;
            Amount = e.Amount;
            SourceLocation = e.SourceLocation;
            DestinationLocation = e.DestinationLocation;
            SourceLotId = e.SourceLotId;
            RecordedAt = e.OccurredAt;
        }
    }
}
