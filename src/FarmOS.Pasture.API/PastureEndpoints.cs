using MediatR;
using FarmOS.Pasture.Application.Commands;
using FarmOS.Pasture.Application.Queries;
using FarmOS.SharedKernel;

namespace FarmOS.Pasture.API;

public static class PastureEndpoints
{
    public static void MapPastureEndpoints(this WebApplication app)
    {
        var paddocks = app.MapGroup("/api/pasture/paddocks");
        var animals = app.MapGroup("/api/pasture/animals");
        var herds = app.MapGroup("/api/pasture/herds");

        // ─── Paddock Endpoints ───────────────────────────────────

        paddocks.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetPaddocksQuery(), ct);
            return Results.Ok(result);
        });

        paddocks.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetPaddockByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        paddocks.MapPost("/", async (CreatePaddockCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(
                id => Results.Created($"/api/pasture/paddocks/{id}", new { id }),
                err => Results.BadRequest(err));
        });

        paddocks.MapPut("/{id:guid}/boundary", async (
            Guid id, UpdateBoundaryRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new UpdatePaddockBoundaryCommand(id, req.Geometry), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        paddocks.MapPost("/{id:guid}/begin-grazing", async (
            Guid id, BeginGrazingRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new BeginGrazingCommand(id, req.HerdId, req.Date), ct);
            return result.Match(
                paddockId => Results.Ok(new { id = paddockId }),
                err => Results.BadRequest(err));
        });

        paddocks.MapPost("/{id:guid}/end-grazing", async (
            Guid id, EndGrazingRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new EndGrazingCommand(id, req.Date), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        paddocks.MapPost("/{id:guid}/biomass", async (
            Guid id, UpdateBiomassCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PaddockId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        paddocks.MapPost("/{id:guid}/soil-test", async (
            Guid id, RecordSoilTestCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PaddockId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Animal Endpoints ────────────────────────────────────

        animals.MapGet("/", async (
            string? species, string? status, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAnimalsQuery(species, status), ct);
            return Results.Ok(result);
        });

        animals.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAnimalByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        animals.MapPost("/", async (RegisterAnimalCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(
                id => Results.Created($"/api/pasture/animals/{id}", new { id }),
                err => Results.BadRequest(err));
        });

        animals.MapPost("/{id:guid}/isolate", async (
            Guid id, IsolateAnimalRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new IsolateAnimalCommand(id, req.Reason, req.Date), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        animals.MapPost("/{id:guid}/treatment", async (
            Guid id, RecordTreatmentCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AnimalId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        animals.MapPost("/{id:guid}/weight", async (
            Guid id, RecordWeightCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AnimalId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        animals.MapPost("/{id:guid}/butcher", async (
            Guid id, ButcherAnimalCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AnimalId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        animals.MapPost("/{id:guid}/sell", async (
            Guid id, SellAnimalCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AnimalId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Herd Endpoints ─────────────────────────────────────

        herds.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetHerdsQuery(), ct);
            return Results.Ok(result);
        });

        herds.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetHerdByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        herds.MapPost("/", async (CreateHerdCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(
                id => Results.Created($"/api/pasture/herds/{id}", new { id }),
                err => Results.BadRequest(err));
        });

        herds.MapPost("/{id:guid}/move", async (
            Guid id, MoveHerdRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new MoveHerdCommand(id, req.PaddockId, req.Date), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        herds.MapPost("/{id:guid}/add-animal", async (
            Guid id, AddAnimalRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new AddAnimalToHerdCommand(id, req.AnimalId), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        herds.MapPost("/{id:guid}/remove-animal", async (
            Guid id, RemoveAnimalRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new RemoveAnimalFromHerdCommand(id, req.AnimalId), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}

// ─── Request DTOs (for complex bodies where command requires route params) ───

public record UpdateBoundaryRequest(GeoJsonGeometry Geometry);
public record BeginGrazingRequest(Guid HerdId, DateOnly Date);
public record EndGrazingRequest(DateOnly Date);
public record IsolateAnimalRequest(string Reason, DateOnly Date);
public record MoveHerdRequest(Guid PaddockId, DateOnly Date);
public record AddAnimalRequest(Guid AnimalId);
public record RemoveAnimalRequest(Guid AnimalId);
