using MediatR;
using FarmOS.Commerce.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.API;

public static class CommerceEndpoints
{
    public static void MapCommerceEndpoints(this WebApplication app)
    {
        var seasons = app.MapGroup("/api/commerce/seasons");
        var members = app.MapGroup("/api/commerce/members");
        var orders = app.MapGroup("/api/commerce/orders");

        // ─── CSA Seasons ─────────────────────────────────────────

        seasons.MapPost("/", async (CreateSeasonCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/seasons/{id}", new { id }), err => Results.BadRequest(err));
        });

        seasons.MapPost("/{id:guid}/pickups", async (Guid id, SchedulePickupCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeasonId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        seasons.MapPost("/{id:guid}/close", async (Guid id, CloseSeasonCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeasonId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── CSA Members ─────────────────────────────────────────

        members.MapPost("/", async (RegisterMemberCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/members/{id}", new { id }), err => Results.BadRequest(err));
        });

        members.MapPost("/{id:guid}/payments", async (Guid id, RecordPaymentCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { MemberId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        members.MapPost("/{id:guid}/pickups", async (Guid id, RecordPickupCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { MemberId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Direct Orders ───────────────────────────────────────

        orders.MapPost("/", async (CreateOrderCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/orders/{id}", new { id }), err => Results.BadRequest(err));
        });

        orders.MapPost("/{id:guid}/pack", async (Guid id, PackOrderCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { OrderId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        orders.MapPost("/{id:guid}/fulfill", async (Guid id, FulfillOrderCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { OrderId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        orders.MapPost("/{id:guid}/cancel", async (Guid id, CancelOrderCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { OrderId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
