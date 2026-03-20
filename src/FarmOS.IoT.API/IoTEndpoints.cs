using FarmOS.IoT.Application.Commands;
using FarmOS.IoT.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FarmOS.IoT.API;

public static class IoTEndpoints
{
    public static IEndpointRouteBuilder MapIoT(this IEndpointRouteBuilder app)
    {
        var iot = app.MapGroup("/api/iot").WithTags("IoT");

        // -- Devices --
        iot.MapGet("/devices", async (IMediator mediator, CancellationToken ct) => 
            Results.Ok(await mediator.Send(new GetAllDevicesQuery(), ct)));

        iot.MapGet("/devices/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) => 
        {
            var result = await mediator.Send(new GetDeviceDetailQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        iot.MapPost("/devices", async (RegisterDeviceCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(new { Id = result.Value }) : Results.BadRequest(result.Error);
        });

        iot.MapPut("/devices/{id:guid}", async (Guid id, UpdateDeviceCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            if (id != cmd.DeviceId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        iot.MapPost("/devices/{id:guid}/decommission", async (Guid id, DecommissionDeviceCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            if (id != cmd.DeviceId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        // Device Assignments
        iot.MapPut("/devices/{id:guid}/zone", async (Guid id, AssignDeviceToZoneCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            if (id != cmd.DeviceId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        iot.MapDelete("/devices/{id:guid}/zone", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UnassignDeviceFromZoneCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        iot.MapPut("/devices/{id:guid}/asset", async (Guid id, AssignDeviceToAssetCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            if (id != cmd.DeviceId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        iot.MapDelete("/devices/{id:guid}/asset/{context}/{assetType}/{assetId:guid}", async (Guid id, string context, string assetType, Guid assetId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UnassignDeviceFromAssetCommand(id, context, assetType, assetId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        // -- Zones --
        iot.MapGet("/zones", async (IMediator mediator, CancellationToken ct) => 
            Results.Ok(await mediator.Send(new GetAllZonesQuery(), ct)));

        iot.MapGet("/zones/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) => 
        {
            var result = await mediator.Send(new GetZoneDetailQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        iot.MapPost("/zones", async (CreateZoneCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(new { Id = result.Value }) : Results.BadRequest(result.Error);
        });

        iot.MapPut("/zones/{id:guid}", async (Guid id, UpdateZoneCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            if (id != cmd.ZoneId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        iot.MapPost("/zones/{id:guid}/archive", async (Guid id, ArchiveZoneCommand cmd, IMediator mediator, CancellationToken ct) => 
        {
            if (id != cmd.ZoneId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return app;
    }
}
