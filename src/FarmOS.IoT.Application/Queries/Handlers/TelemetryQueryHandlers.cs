using FarmOS.SharedKernel.CQRS;

namespace FarmOS.IoT.Application.Queries.Handlers;

/// <summary>
/// Handles telemetry read-model queries by delegating to the telemetry projection.
/// </summary>
public sealed class TelemetryQueryHandlers(ITelemetryProjection projection) :
    IQueryHandler<GetZoneClimateLogQuery, ZoneClimateLogDto>,
    IQueryHandler<GetZoneComplianceReportQuery, ZoneComplianceReportDto>,
    IQueryHandler<GetActiveExcursionsQuery, List<ActiveExcursionDto>>
{
    public Task<ZoneClimateLogDto?> Handle(GetZoneClimateLogQuery request, CancellationToken ct) =>
        projection.GetZoneClimateLogAsync(request.ZoneId, request.From, request.To, ct);

    public Task<ZoneComplianceReportDto?> Handle(GetZoneComplianceReportQuery request, CancellationToken ct) =>
        projection.GetZoneComplianceReportAsync(request.ZoneId, request.From, request.To, ct);

    public async Task<List<ActiveExcursionDto>?> Handle(GetActiveExcursionsQuery request, CancellationToken ct) =>
        await projection.GetActiveExcursionsAsync(ct);
}

/// <summary>
/// Projection interface for telemetry read models.
/// </summary>
public interface ITelemetryProjection
{
    Task<ZoneClimateLogDto?> GetZoneClimateLogAsync(Guid zoneId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<ZoneComplianceReportDto?> GetZoneComplianceReportAsync(Guid zoneId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<List<ActiveExcursionDto>> GetActiveExcursionsAsync(CancellationToken ct);
}
