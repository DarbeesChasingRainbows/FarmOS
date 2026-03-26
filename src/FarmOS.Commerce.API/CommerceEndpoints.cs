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

        // ─── A La Carte CSA ─────────────────────────────────────

        seasons.MapPost("/{id:guid}/selection-mode", async (Guid id, SetSelectionModeCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeasonId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        seasons.MapPost("/{id:guid}/selection-window/open", async (Guid id, OpenSelectionWindowCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SeasonId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        seasons.MapPost("/{id:guid}/selection-window/close", async (Guid id, CloseSelectionWindowCommand cmd, IMediator m, CancellationToken ct) =>
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

        members.MapPost("/{id:guid}/select-items", async (Guid id, SelectItemsCommand cmd, IMediator m, CancellationToken ct) =>
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

        // ─── Customers (CRM) ─────────────────────────────────────────

        var customers = app.MapGroup("/api/commerce/customers");

        customers.MapPost("/", async (CreateCustomerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/customers/{id}", new { id }), err => Results.BadRequest(err));
        });

        customers.MapPut("/{id:guid}/profile", async (Guid id, UpdateCustomerProfileCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CustomerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        customers.MapPost("/{id:guid}/notes", async (Guid id, AddCustomerNoteCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CustomerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        customers.MapPost("/merge", async (MergeCustomersCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Ok(new { id }), err => Results.BadRequest(err));
        });

        customers.MapPost("/{id:guid}/dismiss-duplicate", async (Guid id, DismissDuplicateCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { CustomerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Buying Clubs ─────────────────────────────────────────

        var buyingClubs = app.MapGroup("/api/commerce/buying-clubs");

        buyingClubs.MapPost("/", async (CreateBuyingClubCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/buying-clubs/{id}", new { id }), err => Results.BadRequest(err));
        });

        buyingClubs.MapPost("/{id:guid}/drop-sites", async (Guid id, AddDropSiteCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ClubId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        buyingClubs.MapDelete("/{id:guid}/drop-sites/{name}", async (Guid id, string name, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new RemoveDropSiteCommand(id, name), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        buyingClubs.MapPost("/{id:guid}/cycle/open", async (Guid id, OpenOrderCycleCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ClubId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        buyingClubs.MapPost("/{id:guid}/cycle/close", async (Guid id, CloseOrderCycleCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ClubId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        buyingClubs.MapPost("/{id:guid}/pause", async (Guid id, PauseBuyingClubCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ClubId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        buyingClubs.MapPost("/{id:guid}/close", async (Guid id, CloseBuyingClubCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ClubId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Wholesale ────────────────────────────────────────────

        var wholesale = app.MapGroup("/api/commerce/wholesale");

        wholesale.MapPost("/", async (OpenWholesaleAccountCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/commerce/wholesale/{id}", new { id }), err => Results.BadRequest(err));
        });

        wholesale.MapPost("/{id:guid}/standing-order", async (Guid id, SetStandingOrderCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AccountId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        wholesale.MapDelete("/{id:guid}/standing-order/{productName}", async (Guid id, string productName, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CancelStandingOrderCommand(id, productName), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        wholesale.MapPost("/{id:guid}/route", async (Guid id, AssignDeliveryRouteCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AccountId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        wholesale.MapPost("/{id:guid}/close", async (Guid id, CloseWholesaleAccountCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { AccountId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
