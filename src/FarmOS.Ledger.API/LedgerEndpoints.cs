using MediatR;
using FarmOS.Ledger.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Ledger.API;

public static class LedgerEndpoints
{
    public static void MapLedgerEndpoints(this WebApplication app)
    {
        var expenses = app.MapGroup("/api/ledger/expenses");
        var revenue = app.MapGroup("/api/ledger/revenue");

        // ─── Expenses ────────────────────────────────────────────

        expenses.MapPost("/", async (RecordExpenseCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/ledger/expenses/{id}", new { id }), err => Results.BadRequest(err));
        });

        expenses.MapPost("/{id:guid}/void", async (Guid id, VoidExpenseCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ExpenseId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Revenue ─────────────────────────────────────────────

        revenue.MapPost("/", async (RecordRevenueCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/ledger/revenue/{id}", new { id }), err => Results.BadRequest(err));
        });

        revenue.MapPost("/{id:guid}/void", async (Guid id, VoidRevenueCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { RevenueId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
