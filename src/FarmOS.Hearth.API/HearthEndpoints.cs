using MediatR;
using FarmOS.Hearth.Application.Commands;
using FarmOS.SharedKernel;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.API.Hubs;
using CsvHelper;
using System.Globalization;
using FarmOS.Hearth.Application.Queries;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Infrastructure.HarvestRight;

namespace FarmOS.Hearth.API;

public static class HearthEndpoints
{
    public static void MapHearthEndpoints(this WebApplication app)
    {
        var sourdough = app.MapGroup("/api/hearth/sourdough");
        var kombucha = app.MapGroup("/api/hearth/kombucha");
        var cultures = app.MapGroup("/api/hearth/cultures");
        var iot = app.MapGroup("/api/hearth/iot");
        var kitchen = app.MapGroup("/api/hearth/kitchen");
        var traceability = app.MapGroup("/api/hearth/compliance/traceability");

        // ─── Sourdough ──────────────────────────────────────────

        sourdough.MapPost("/", async (StartSourdoughCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/sourdough/{id}", new { id }), err => Results.BadRequest(err));
        });

        sourdough.MapPost("/{id:guid}/ccp", async (Guid id, RecordSourdoughCCPCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        sourdough.MapPost("/{id:guid}/advance", async (Guid id, AdvanceSourdoughPhaseCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        sourdough.MapPost("/{id:guid}/complete", async (Guid id, CompleteSourdoughCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Kombucha ────────────────────────────────────────────

        kombucha.MapPost("/", async (StartKombuchaCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/kombucha/{id}", new { id }), err => Results.BadRequest(err));
        });

        kombucha.MapPost("/{id:guid}/ph", async (Guid id, RecordKombuchaPHCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        kombucha.MapPost("/{id:guid}/flavor", async (Guid id, AddKombuchaFlavoringCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        kombucha.MapPost("/{id:guid}/advance", async (Guid id, AdvanceKombuchaPhaseCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        kombucha.MapPost("/{id:guid}/complete", async (Guid id, CompleteKombuchaCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Cultures ────────────────────────────────────────────

        cultures.MapPost("/", async (CreateCultureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/cultures/{id}", new { id }), err => Results.BadRequest(err));
        });

        cultures.MapPost("/{id:guid}/feed", async (Guid id, FeedCultureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CultureId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        cultures.MapPost("/{id:guid}/split", async (Guid id, SplitCultureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CultureId = id }, ct);
            return result.Match(newId => Results.Ok(new { id = newId }), err => Results.BadRequest(err));
        });

        // ─── IoT ───────────────────────────────────────────────

        iot.MapPost("/readings", async (
            IngestSensorReadingCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(
                alert => Results.Ok(alert),
                err => Results.BadRequest(err));
        });

        // ─── Kitchen Compliance (Stub endpoints) ───────────────────
        // These accept the JSON body and return 202 Accepted as stub.
        // Full command handling to be wired in a follow-up.

        kitchen.MapPost("/temps", async (LogEquipmentTemperatureCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Ok(new { id }), err => Results.BadRequest(err));
        });
        kitchen.MapPost("/temps/{id:guid}/correction", async (Guid id, AppendMonitoringCorrectionCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { OriginalLogId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
        kitchen.MapGet("/sanitation", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new FarmOS.Hearth.Application.Queries.GetRecentSanitationRecordsQuery(), ct);
            return Results.Ok(result);
        });
        kitchen.MapPost("/sanitation", async (RecordSanitationCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(
                id => Results.Created($"/api/hearth/kitchen/sanitation/{id}", new { id }),
                err => Results.BadRequest(err));
        });
        kitchen.MapPost("/certs", (HttpContext _) => Results.Accepted());
        kitchen.MapPost("/deliveries", (HttpContext _) => Results.Accepted());

        // ─── FSMA 204 Traceability ─────────────────────────────
        
        traceability.MapPost("/receiving", async (LogReceivingEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Ok(new { id }), err => Results.BadRequest(err));
        });

        traceability.MapPost("/transformation", async (LogTransformationEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Ok(new { id }), err => Results.BadRequest(err));
        });

        traceability.MapPost("/shipping", async (LogShippingEventCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Ok(new { id }), err => Results.BadRequest(err));
        });

        traceability.MapGet("/audit-report", async (IMediator m, CancellationToken ct) =>
        {
            // Send query to get DTOs
            var records = await m.Send(new Get24HourAuditReportQuery(DateTimeOffset.UtcNow), ct);
            
            // Generate CSV
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            
            await csv.WriteRecordsAsync(records, ct);
            await streamWriter.FlushAsync();
            
            return Results.File(memoryStream.ToArray(), "text/csv", $"FSMA_24H_Audit_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        });

        // ─── HACCP Plan ──────────────────────────────────────────

        var haccp = app.MapGroup("/api/hearth/compliance/haccp");

        haccp.MapPost("/plans", async (CreateHACCPPlanCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/compliance/haccp/plans/{id}", new { id }), err => Results.BadRequest(err));
        });

        haccp.MapPost("/plans/{id:guid}/ccps", async (Guid id, AddCCPDefinitionCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlanId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        haccp.MapDelete("/plans/{id:guid}/ccps", async (Guid id, string product, string ccpName, IMediator m, CancellationToken ct) =>
        {
            var cmd = new RemoveCCPDefinitionCommand(id, product, ccpName);
            var result = await m.Send(cmd, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── CAPA ────────────────────────────────────────────────

        var capa = app.MapGroup("/api/hearth/compliance/capa");

        capa.MapPost("/", async (OpenCAPACommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/compliance/capa/{id}", new { id }), err => Results.BadRequest(err));
        });

        capa.MapPost("/{id:guid}/close", async (Guid id, CloseCAPACommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CAPAId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Freeze-Dryer ────────────────────────────────────────

        var freezeDryer = app.MapGroup("/api/hearth/freeze-dryer");

        freezeDryer.MapGet("/", async (IHearthEventStore store, CancellationToken ct) =>
            Results.Ok(await store.ListFreezeDryerBatchesAsync(ct)));

        freezeDryer.MapPost("/", async (StartFreezeDryerBatchCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/hearth/freeze-dryer/{id}", new { id }), err => Results.BadRequest(err));
        });

        freezeDryer.MapPost("/{id:guid}/readings", async (Guid id, RecordFreezeDryerReadingCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        freezeDryer.MapPost("/{id:guid}/advance", async (Guid id, AdvanceFreezeDryerPhaseCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        freezeDryer.MapPost("/{id:guid}/complete", async (Guid id, CompleteFreezeDryerBatchCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        freezeDryer.MapPost("/{id:guid}/abort", async (Guid id, AbortFreezeDryerBatchCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Fermentation Analytics ──────────────────────────────

        var fermentation = app.MapGroup("/api/hearth/fermentation");

        fermentation.MapGet("/{id:guid}/analytics", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new FarmOS.Hearth.Application.Queries.GetFermentationPHTimelineQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        fermentation.MapGet("/active-monitoring", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new FarmOS.Hearth.Application.Queries.GetActiveFermentationMonitoringQuery(), ct);
            return Results.Ok(result);
        });

        // ─── Traceability Graph ──────────────────────────────

        var graph = app.MapGroup("/api/hearth/compliance/traceability/graph");

        graph.MapPost("/lots", async (TraceabilityLotDto dto, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var key = await svc.CreateLotAsync(dto, ct);
            return Results.Created($"/api/hearth/compliance/traceability/graph/lots/{key}", new { key });
        });

        graph.MapPost("/batches", async (TraceabilityBatchDto dto, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var key = await svc.CreateBatchAsync(dto, ct);
            return Results.Created($"/api/hearth/compliance/traceability/graph/batches/{key}", new { key });
        });

        graph.MapPost("/customers", async (TraceabilityCustomerDto dto, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var key = await svc.CreateCustomerAsync(dto, ct);
            return Results.Created($"/api/hearth/compliance/traceability/graph/customers/{key}", new { key });
        });

        graph.MapPost("/suppliers", async (TraceabilitySupplierDto dto, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var key = await svc.CreateSupplierAsync(dto, ct);
            return Results.Created($"/api/hearth/compliance/traceability/graph/suppliers/{key}", new { key });
        });

        graph.MapPost("/lots/{lotId}/used-in/{batchId}", async (string lotId, string batchId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            await svc.LinkLotToBatchAsync(lotId, batchId, ct);
            return Results.NoContent();
        });

        graph.MapPost("/batches/{batchId}/sold-to/{customerId}", async (string batchId, string customerId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            await svc.LinkBatchToCustomerAsync(batchId, customerId, ct);
            return Results.NoContent();
        });

        graph.MapPost("/lots/{lotId}/sourced-from/{supplierId}", async (string lotId, string supplierId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            await svc.LinkLotToSupplierAsync(lotId, supplierId, ct);
            return Results.NoContent();
        });

        graph.MapGet("/recall/{lotId}", async (string lotId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var result = await svc.GetFullRecallGraphAsync(lotId, ct);
            return Results.Ok(result);
        });

        graph.MapGet("/recall/{lotId}/forward", async (string lotId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var result = await svc.TraceRecallForwardAsync(lotId, ct);
            return Results.Ok(result);
        });

        graph.MapGet("/recall/{batchId}/backward", async (string batchId, ITraceabilityGraphService svc, CancellationToken ct) =>
        {
            var result = await svc.TraceRecallBackwardAsync(batchId, ct);
            return Results.Ok(result);
        });

        // ─── Harvest Right IoT ─────────────────────────────────
        var harvestRight = app.MapGroup("/api/hearth/harvest-right").WithTags("HarvestRight");

        harvestRight.MapGet("/status", (HarvestRightMqttWorker worker) =>
            Results.Ok(new
            {
                Connected = worker.IsConnected,
                Dryers = worker.RegisteredDryers,
                LastTelemetryAt = worker.LastTelemetryAt
            }));

        // ─── SignalR Hub ───────────────────────────────────────
        app.MapHub<KitchenHub>("/hubs/kitchen");
    }
}
