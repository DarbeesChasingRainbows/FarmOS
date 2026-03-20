using MediatR;
using FarmOS.Assets.Application.Commands;
using FarmOS.Assets.Infrastructure;
using FarmOS.Assets.Domain;
using FarmOS.SharedKernel;

namespace FarmOS.Assets.API;

public static class AssetsEndpoints
{
    public static void MapAssetsEndpoints(this WebApplication app)
    {
        var equipment = app.MapGroup("/api/assets/equipment");
        var structures = app.MapGroup("/api/assets/structures");
        var water = app.MapGroup("/api/assets/water");
        var compost = app.MapGroup("/api/assets/compost");
        var materials = app.MapGroup("/api/assets/materials");

        // ─── Equipment ──────────────────────────────────────────────
        equipment.MapGet("/", async (EquipmentProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetAllEquipmentAsync(ct)));

        equipment.MapGet("/{id:guid}", async (Guid id, EquipmentProjection projection, CancellationToken ct) =>
        {
            var all = await projection.GetAllEquipmentAsync(ct);
            var item = all.FirstOrDefault(e => e.Id == id.ToString());
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        equipment.MapPost("/", async (RegisterEquipmentCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/assets/equipment/{id}", new { id }), err => Results.BadRequest(err));
        });
        equipment.MapPost("/{id:guid}/maintenance", async (Guid id, RecordEquipmentMaintenanceCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { EquipmentId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));
        equipment.MapPost("/{id:guid}/move", async (Guid id, MoveEquipmentCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { EquipmentId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));
        equipment.MapPost("/{id:guid}/retire", async (Guid id, RetireEquipmentCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { EquipmentId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        // ─── Structures ─────────────────────────────────────────────
        structures.MapPost("/", async (RegisterStructureCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd, ct)).Match(id => Results.Created($"/api/assets/structures/{id}", new { id }), err => Results.BadRequest(err)));
        structures.MapPost("/{id:guid}/maintenance", async (Guid id, RecordStructureMaintenanceCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { StructureId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        // ─── Water Sources ──────────────────────────────────────────
        water.MapPost("/", async (RegisterWaterSourceCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd, ct)).Match(id => Results.Created($"/api/assets/water/{id}", new { id }), err => Results.BadRequest(err)));
        water.MapPost("/{id:guid}/test", async (Guid id, RecordWaterTestCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { WaterSourceId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        // ─── Compost Batches ────────────────────────────────────────
        // Read endpoints
        compost.MapGet("/", async (CompostProjection projection, CancellationToken ct) =>
            Results.Ok(await projection.GetAllBatchesAsync(ct)));

        compost.MapGet("/{id:guid}", async (Guid id, CompostProjection projection, CancellationToken ct) =>
        {
            var detail = await projection.GetBatchDetailAsync(id.ToString(), ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        // Write endpoints
        compost.MapPost("/", async (StartCompostBatchCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd, ct)).Match(id => Results.Created($"/api/assets/compost/{id}", new { id }), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/temp", async (Guid id, RecordCompostTempCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/turn", async (Guid id, TurnCompostCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/phase", async (Guid id, ChangeCompostPhaseCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/inoculate", async (Guid id, InoculateCompostCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/ph", async (Guid id, MeasureCompostPhCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/note", async (Guid id, AddCompostNoteCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        compost.MapPost("/{id:guid}/complete", async (Guid id, CompleteCompostBatchCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { BatchId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));


        // ─── Materials ──────────────────────────────────────────────
        materials.MapPost("/", async (RegisterMaterialCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd, ct)).Match(id => Results.Created($"/api/assets/materials/{id}", new { id }), err => Results.BadRequest(err)));
        materials.MapPost("/{id:guid}/use", async (Guid id, UseMaterialCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { MaterialId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));
        materials.MapPost("/{id:guid}/restock", async (Guid id, RestockMaterialCommand cmd, IMediator m, CancellationToken ct) =>
            (await m.Send(cmd with { MaterialId = id }, ct)).Match(_ => Results.NoContent(), err => Results.BadRequest(err)));

        // ─── Home Assistant Sensors ──────────────────────────────────
        var ha = app.MapGroup("/api/assets/ha");

        ha.MapGet("/sensors", async (HaSensorBridge bridge, CancellationToken ct) =>
            Results.Ok(await bridge.GetAllSensorsAsync(ct)));

        ha.MapGet("/sensors/{entityId}", async (string entityId, HaSensorBridge bridge, CancellationToken ct) =>
        {
            var detail = await bridge.GetSensorDetailAsync(entityId, ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        ha.MapGet("/sensors/{entityId}/history", async (string entityId, int? hours, HaSensorBridge bridge, CancellationToken ct) =>
            Results.Ok(await bridge.GetSensorHistoryAsync(entityId, hours ?? 24, ct)));

        ha.MapGet("/status", async (HaSensorBridge bridge, CancellationToken ct) =>
            Results.Ok(new { available = await bridge.IsAvailableAsync(ct) }));
    }
}
