using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Queries;

public record TraceabilityReportDto(
    string EventType, 
    string Category, 
    string ProductDescription, 
    string LotId, 
    decimal Amount, 
    string Unit, 
    string? SourceLocation, 
    string? DestinationLocation, 
    string? SourceLotId, 
    DateTimeOffset RecordedAt);

public record Get24HourAuditReportQuery(DateTimeOffset Date) : IQuery<IReadOnlyList<TraceabilityReportDto>>;

public class Get24HourAuditReportQueryHandler : IQueryHandler<Get24HourAuditReportQuery, IReadOnlyList<TraceabilityReportDto>>
{
    public Task<IReadOnlyList<TraceabilityReportDto>?> Handle(Get24HourAuditReportQuery request, CancellationToken cancellationToken)
    {
        // Stub implementation returning some sample data to prove CSV export
        IReadOnlyList<TraceabilityReportDto> list = new List<TraceabilityReportDto>
        {
            new("Receiving", "Wheat", "Heritage Red Fife", "WHT-RCV-001", 100, "lbs", "FieldOps Zone 1", "Hearth Origin", null, DateTimeOffset.UtcNow.AddMinutes(-120)),
            new("Transformation", "Sourdough", "Sourdough Starter", "SOUR-TRF-001", 5, "lbs", "Hearth Origin", "Hearth Bakery", "WHT-RCV-001", DateTimeOffset.UtcNow.AddMinutes(-60)),
            new("Transformation", "Mushroom", "Lion's Mane Blend", "MUSH-TRF-001", 20, "lbs", "EdgePortal Prep", "Grow Tent A", null, DateTimeOffset.UtcNow.AddMinutes(-30)),
            new("Shipping", "Sourdough", "Baked Sourdough Loaves", "SOUR-SHP-001", 10, "loaves", "Hearth Bakery", "B2C EdgePortal", "SOUR-TRF-001", DateTimeOffset.UtcNow)
        };

        return Task.FromResult<IReadOnlyList<TraceabilityReportDto>?>(list);
    }
}
