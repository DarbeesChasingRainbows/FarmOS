using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Apiary.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

public record ApiaryExpenseEntry(string Id, string Category, decimal Amount, string Currency, DateOnly Date, string? HiveRef, string? Notes);
public record ApiaryRevenueEntry(string Id, string Category, decimal Amount, string Currency, DateOnly Date, string? ProductType, string? Notes);
public record ApiaryProfitLoss(decimal TotalRevenue, decimal TotalExpenses, decimal NetProfit, int ExpenseCount, int RevenueCount);

// ─── Projection ────────────────────────────────────────────────────────────

/// <summary>
/// Cross-context read projection that queries the Ledger event store
/// filtered by apiary-related entries. Provides financial summaries
/// for the apiary module without owning any financial aggregates.
/// </summary>
/// <remarks>
/// This projection will be fully implemented when the Ledger bounded context
/// adds support for context-tagged entries (LedgerContext.Apiary).
/// Currently returns placeholder data to establish the API contract.
/// </remarks>
public sealed class ApiaryFinancialProjection
{
    public Task<ApiaryProfitLoss> GetSummaryAsync(CancellationToken ct) =>
        Task.FromResult(new ApiaryProfitLoss(0, 0, 0, 0, 0));

    public Task<IReadOnlyList<ApiaryExpenseEntry>> GetExpensesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ApiaryExpenseEntry>>([]);

    public Task<IReadOnlyList<ApiaryRevenueEntry>> GetRevenueAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ApiaryRevenueEntry>>([]);
}
