using FarmOS.IoT.Application.Commands;
using FarmOS.IoT.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FarmOS.IoT.API;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetry(this IEndpointRouteBuilder app)
    {
        var telemetry = app.MapGroup("/api/iot/telemetry").WithTags("IoT Telemetry");

        // Ingest a sensor reading (used by HA polling worker or direct POST)
        telemetry.MapPost("/readings", async (RecordTelemetryReadingCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Accepted() : Results.BadRequest(result.Error);
        });

        // Get climate log for a zone (time-series readings)
        telemetry.MapGet("/zones/{zoneId:guid}/climate-log", async (
            Guid zoneId, IMediator mediator, CancellationToken ct,
            DateTimeOffset? from = null, DateTimeOffset? to = null) =>
        {
            var query = new GetZoneClimateLogQuery(zoneId, from, to);
            var result = await mediator.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        // Get compliance report for a zone
        telemetry.MapGet("/zones/{zoneId:guid}/compliance-report", async (
            Guid zoneId, IMediator mediator, CancellationToken ct,
            DateTimeOffset? from = null, DateTimeOffset? to = null) =>
        {
            var query = new GetZoneComplianceReportQuery(zoneId, from, to);
            var result = await mediator.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        // Get active excursions
        telemetry.MapGet("/excursions/active", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActiveExcursionsQuery(), ct);
            return Results.Ok(result);
        });

        return app;
    }
}
