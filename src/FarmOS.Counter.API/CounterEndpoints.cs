using MediatR;
using FarmOS.Counter.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Counter.API;

public static class CounterEndpoints
{
    public static void MapCounterEndpoints(this WebApplication app)
    {
        var registers = app.MapGroup("/api/counter/registers");
        var sales = app.MapGroup("/api/counter/sales");
        var drawers = app.MapGroup("/api/counter/drawers");

        // --- Registers ---------------------------------------------------

        registers.MapPost("/open", async (OpenRegisterCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/counter/registers/{id}", new { id }), err => Results.BadRequest(err));
        });

        registers.MapPost("/{id:guid}/close", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CloseRegisterCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Sales -------------------------------------------------------

        sales.MapPost("/", async (CompleteSaleCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/counter/sales/{id}", new { id }), err => Results.BadRequest(err));
        });

        sales.MapPost("/{id:guid}/void", async (Guid id, VoidSaleCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SaleId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        sales.MapPost("/{id:guid}/refund", async (Guid id, RefundSaleCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { SaleId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // --- Cash Drawers ------------------------------------------------

        drawers.MapPost("/open", async (OpenCashDrawerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/counter/drawers/{id}", new { id }), err => Results.BadRequest(err));
        });

        drawers.MapPost("/{id:guid}/count", async (Guid id, CountCashDrawerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { DrawerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        drawers.MapPost("/{id:guid}/reconcile", async (Guid id, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ReconcileCashDrawerCommand(id), ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
