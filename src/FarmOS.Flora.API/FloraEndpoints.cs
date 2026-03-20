using MediatR;
using FarmOS.Flora.Application.Commands;
using FarmOS.Flora.Application.Queries;
using FarmOS.Flora.Domain;
using FarmOS.SharedKernel;

namespace FarmOS.Flora.API;

public static class FloraEndpoints
{
    public static void MapFloraEndpoints(this WebApplication app)
    {
        var guilds = app.MapGroup("/api/flora/guilds")
            //.RequireAuthorization()
            ;

        var beds = app.MapGroup("/api/flora/beds")
            //.RequireAuthorization()
            ;

        var seeds = app.MapGroup("/api/flora/seeds")
            //.RequireAuthorization()
            ;

        // ─── Guilds ──────────────────────────────────────────

        guilds.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllGuildsQuery(), ct);
            return Results.Ok(result);
        });

        guilds.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetGuildByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        guilds.MapPost("/", async (CreateGuildCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/guilds/{id}", new { id }), err => Results.BadRequest(err));
        });

        guilds.MapPost("/{id:guid}/members", async (Guid id, AddGuildMemberCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { GuildId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Beds ────────────────────────────────────────────

        beds.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllBedsQuery(), ct);
            return Results.Ok(result);
        });

        beds.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetBedByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        beds.MapPost("/", async (CreateFlowerBedCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/beds/{id}", new { id }), err => Results.BadRequest(err));
        });

        beds.MapPost("/{id:guid}/successions", async (Guid id, PlanSuccessionCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BedId = id }, ct);
            return result.Match(succId => Results.Ok(new { id = succId }), err => Results.BadRequest(err));
        });

        beds.MapPost("/{bedId:guid}/successions/{succId:guid}/seed", async (Guid bedId, Guid succId, RecordSeedingCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BedId = bedId, SuccessionId = succId }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        beds.MapPost("/{bedId:guid}/successions/{succId:guid}/transplant", async (Guid bedId, Guid succId, RecordTransplantCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BedId = bedId, SuccessionId = succId }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        beds.MapPost("/{bedId:guid}/successions/{succId:guid}/harvest", async (Guid bedId, Guid succId, RecordHarvestCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BedId = bedId, SuccessionId = succId }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Seeds ───────────────────────────────────────────

        seeds.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllSeedLotsQuery(), ct);
            return Results.Ok(result);
        });

        seeds.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetSeedLotByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        seeds.MapPost("/", async (CreateSeedLotCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/seeds/{id}", new { id }), err => Results.BadRequest(err));
        });

        seeds.MapPost("/{id:guid}/withdraw", async (Guid id, WithdrawSeedCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeedLotId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        seeds.MapPost("/{id:guid}/restock", async (Guid id, RestockSeedCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeedLotId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── PostHarvestBatches ────────────────────────────────

        var batches = app.MapGroup("/api/flora/batches");

        batches.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllPostHarvestBatchesQuery(), ct);
            return Results.Ok(result);
        });

        batches.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetPostHarvestBatchByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        batches.MapPost("/", async (CreatePostHarvestBatchCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/batches/{id}", new { id }), err => Results.BadRequest(err));
        });

        batches.MapPost("/{id:guid}/grade", async (Guid id, GradeStemsCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        batches.MapPost("/{id:guid}/condition", async (Guid id, ConditionStemsCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        batches.MapPost("/{id:guid}/cooler", async (Guid id, MoveBatchToCoolerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        batches.MapPost("/{id:guid}/use-stems", async (Guid id, UseBatchStemsCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { BatchId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── BouquetRecipes ───────────────────────────────────

        var recipes = app.MapGroup("/api/flora/recipes");

        recipes.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllBouquetRecipesQuery(), ct);
            return Results.Ok(result);
        });

        recipes.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetBouquetRecipeByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        recipes.MapPost("/", async (CreateBouquetRecipeCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/recipes/{id}", new { id }), err => Results.BadRequest(err));
        });

        recipes.MapPost("/{id:guid}/items", async (Guid id, AddRecipeItemCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { RecipeId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        recipes.MapDelete("/{id:guid}/items/{species}/{cultivar}", async (Guid id, string species, string cultivar, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new RemoveRecipeItemCommand(id, species, cultivar), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        recipes.MapPost("/{id:guid}/make", async (Guid id, MakeBouquetCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { RecipeId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── CropPlans ────────────────────────────────────────

        var plans = app.MapGroup("/api/flora/plans");

        plans.MapGet("/", async (IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAllCropPlansQuery(), ct);
            return Results.Ok(result);
        });

        plans.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetCropPlanByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        plans.MapPost("/", async (CreateCropPlanCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/flora/plans/{id}", new { id }), err => Results.BadRequest(err));
        });

        plans.MapPost("/{id:guid}/beds", async (Guid id, AssignBedToPlanCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlanId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        plans.MapPost("/{id:guid}/yields", async (Guid id, RecordYieldCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlanId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        plans.MapPost("/{id:guid}/costs", async (Guid id, RecordCropCostCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlanId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        plans.MapPost("/{id:guid}/revenue", async (Guid id, RecordCropRevenueCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { PlanId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
