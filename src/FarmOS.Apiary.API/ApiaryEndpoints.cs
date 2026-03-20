using MediatR;
using FarmOS.Apiary.Application.Commands;
using FarmOS.Apiary.Infrastructure;
using FarmOS.SharedKernel;

namespace FarmOS.Apiary.API;

public static class ApiaryEndpoints
{
    public static void MapApiaryEndpoints(this WebApplication app)
    {
        var hives = app.MapGroup("/api/apiary/hives");

        hives.MapPost("/", async (CreateHiveCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/apiary/hives/{id}", new { id }), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/inspect", async (Guid id, InspectHiveCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/harvest", async (Guid id, HarvestHoneyCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/treat", async (Guid id, TreatHiveCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/status", async (Guid id, ChangeHiveStatusCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 2: Queen Tracking ──────────────────────────────
        hives.MapPost("/{id:guid}/queen", async (Guid id, IntroduceQueenCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/queen/lost", async (Guid id, MarkQueenLostCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/queen/replace", async (Guid id, ReplaceQueenCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 3: Feeding ─────────────────────────────────────
        hives.MapPost("/{id:guid}/feed", async (Guid id, FeedHiveCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 6: Multi-Product Harvest ────────────────────────
        hives.MapPost("/{id:guid}/harvest/product", async (Guid id, HarvestProductCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 4: Colony Splitting & Merging ──────────────────
        hives.MapPost("/{id:guid}/split", async (Guid id, SplitColonyCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { OriginalHiveId = id }, ct);
            return result.Match(newId => Results.Created($"/api/apiary/hives/{newId}", new { id = newId }), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/merge", async (Guid id, MergeColoniesCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SurvivingHiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 5: Equipment/Super Tracking ────────────────────
        hives.MapPost("/{id:guid}/super/add", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new AddSuperCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/super/remove", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new RemoveSuperCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        hives.MapPost("/{id:guid}/configuration", async (Guid id, UpdateHiveConfigurationCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { HiveId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 1: Apiaries ────────────────────────────────────
        var apiaries = app.MapGroup("/api/apiary/apiaries");

        apiaries.MapPost("/", async (CreateApiaryCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/apiary/apiaries/{id}", new { id }), err => Results.BadRequest(err));
        });

        apiaries.MapPost("/{id:guid}/hives", async (Guid id, MoveHiveToApiaryCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ApiaryId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        apiaries.MapPost("/{id:guid}/retire", async (Guid id, RetireApiaryCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ApiaryId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Feature 7: Reporting & Analytics (Read-side) ───────────
        hives.MapGet("/", async (ApiaryProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetAllHivesAsync(ct)));

        apiaries.MapGet("/", async (ApiaryProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetAllApiariesAsync(ct)));

        var reports = app.MapGroup("/api/apiary/reports");

        reports.MapGet("/mite-trends", async (Guid? hiveId, ApiaryProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetMiteTrendsAsync(hiveId, ct)));

        reports.MapGet("/yield", async (int? year, ApiaryProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetYieldReportAsync(year, ct)));

        reports.MapGet("/survival", async (ApiaryProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetSurvivalReportAsync(ct)));

        // ─── Feature 8: Seasonal Task Calendar ─────────────────────
        app.MapGet("/api/apiary/calendar", (int? month, SeasonalTaskCalendar calendar) =>
        {
            if (month.HasValue)
                return Results.Ok(calendar.GetTasksForMonth(month.Value));
            return Results.Ok(calendar.GetAllTasks());
        });

        // ─── Feature 9: Financial Tracking ──────────────────────────
        var financials = app.MapGroup("/api/apiary/financials");

        financials.MapGet("/summary", async (ApiaryFinancialProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetSummaryAsync(ct)));

        financials.MapGet("/expenses", async (ApiaryFinancialProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetExpensesAsync(ct)));

        financials.MapGet("/revenue", async (ApiaryFinancialProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetRevenueAsync(ct)));

        // ─── Feature 10: IoT Sensor Integration ────────────────────
        hives.MapGet("/{id:guid}/sensors", async (Guid id, HiveSensorProjection projection, CancellationToken ct) =>
        {
            var readings = await projection.GetReadingsAsync(id.ToString(), ct);
            return Results.Ok(readings);
        });

        hives.MapGet("/{id:guid}/sensors/summary", async (Guid id, HiveSensorProjection projection, CancellationToken ct) =>
        {
            var summary = await projection.GetSummaryAsync(id.ToString(), ct);
            return summary is not null ? Results.Ok(summary) : Results.NotFound();
        });

        hives.MapGet("/{id:guid}/sensors/weight-trend", async (Guid id, HiveSensorProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetWeightTrendAsync(id.ToString(), ct)));

        // ─── Feature 11: Weather ────────────────────────────────────
        app.MapGet("/api/apiary/weather/current", async (double lat, double lng, IWeatherService weather, CancellationToken ct) =>
        {
            var snapshot = await weather.GetCurrentWeatherAsync(new SharedKernel.GeoPosition(lat, lng), ct);
            return snapshot is not null ? Results.Ok(snapshot) : Results.NoContent();
        });

        reports.MapGet("/weather-correlation", async (WeatherCorrelationProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetCorrelationsAsync(ct)));
    }
}
