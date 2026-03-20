using FarmOS.Ledger.Domain.Aggregates;

namespace FarmOS.Ledger.Application;

public interface ILedgerEventStore
{
    Task<Expense> LoadExpenseAsync(string id, CancellationToken ct);
    Task SaveExpenseAsync(Expense expense, string userId, CancellationToken ct);

    Task<Revenue> LoadRevenueAsync(string id, CancellationToken ct);
    Task SaveRevenueAsync(Revenue revenue, string userId, CancellationToken ct);
}
