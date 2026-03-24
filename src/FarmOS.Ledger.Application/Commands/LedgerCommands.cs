using FarmOS.Ledger.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Ledger.Application.Commands;

// ─── Expense ─────────────────────────────────────────────────────────
public record RecordExpenseCommand(
    string Description, ExpenseCategory Category, LedgerContext Context,
    IReadOnlyList<LineItem> Items, decimal Total, string? Vendor,
    DateOnly Date, string? ReceiptPath) : ICommand<Guid>;

public record VoidExpenseCommand(Guid ExpenseId, string Reason) : ICommand<Guid>;

// ─── Revenue ─────────────────────────────────────────────────────────
public record RecordRevenueCommand(
    string Description, RevenueCategory Category, LedgerContext Context,
    IReadOnlyList<LineItem> Items, decimal Total, string? CustomerName,
    DateOnly Date) : ICommand<Guid>;

public record VoidRevenueCommand(Guid RevenueId, string Reason) : ICommand<Guid>;

// ─── Enterprise Tagging ─────────────────────────────────────────────
public record TagExpenseEnterpriseCommand(Guid ExpenseId, EnterpriseCode Enterprise) : ICommand<Guid>;
public record TagRevenueEnterpriseCommand(Guid RevenueId, EnterpriseCode Enterprise) : ICommand<Guid>;
