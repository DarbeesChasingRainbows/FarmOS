using FarmOS.SharedKernel;

namespace FarmOS.Ledger.Domain.Events;

public record ExpenseRecorded(ExpenseId Id, string Description, ExpenseCategory Category, LedgerContext Context, IReadOnlyList<LineItem> Items, decimal Total, string? Vendor, DateOnly Date, string? ReceiptPath, DateTimeOffset OccurredAt) : IDomainEvent;
public record ExpenseVoided(ExpenseId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public record RevenueRecorded(RevenueId Id, string Description, RevenueCategory Category, LedgerContext Context, IReadOnlyList<LineItem> Items, decimal Total, string? CustomerName, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record RevenueVoided(RevenueId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
