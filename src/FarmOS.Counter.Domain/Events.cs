using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain.Events;

// ─── Register ───────────────────────────────────────────────────────
public record RegisterOpened(RegisterId Id, RegisterLocation Location, string OperatorName, DateTimeOffset OccurredAt) : IDomainEvent;
public record RegisterClosed(RegisterId Id, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Sale ───────────────────────────────────────────────────────────
public record SaleCompleted(SaleId Id, RegisterId RegisterId, IReadOnlyList<SaleLineItem> Items, IReadOnlyList<PaymentRecord> Payments, decimal Total, decimal TaxAmount, string? CustomerName, DateTimeOffset OccurredAt) : IDomainEvent;
public record SaleVoided(SaleId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record SaleRefunded(SaleId Id, decimal RefundAmount, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── CashDrawer ─────────────────────────────────────────────────────
public record CashDrawerOpened(CashDrawerId Id, RegisterId RegisterId, decimal StartingCash, DateTimeOffset OccurredAt) : IDomainEvent;
public record CashDrawerCounted(CashDrawerId Id, DrawerCount Count, DateTimeOffset OccurredAt) : IDomainEvent;
public record CashDrawerReconciled(CashDrawerId Id, decimal Discrepancy, DateTimeOffset OccurredAt) : IDomainEvent;
