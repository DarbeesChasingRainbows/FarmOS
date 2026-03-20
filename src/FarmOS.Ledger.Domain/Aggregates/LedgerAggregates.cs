using FarmOS.Ledger.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Ledger.Domain.Aggregates;

public sealed class Expense : AggregateRoot<ExpenseId>
{
    public string Description { get; private set; } = "";
    public ExpenseCategory Category { get; private set; }
    public LedgerContext Context { get; private set; }
    private readonly List<LineItem> _items = [];
    public IReadOnlyList<LineItem> Items => _items;
    public decimal Total { get; private set; }
    public string? Vendor { get; private set; }
    public DateOnly Date { get; private set; }
    public string? ReceiptPath { get; private set; }
    public bool IsVoided { get; private set; }

    public static Expense Record(string description, ExpenseCategory category, LedgerContext context,
        IReadOnlyList<LineItem> items, decimal total, string? vendor, DateOnly date, string? receiptPath)
    {
        var exp = new Expense();
        exp.RaiseEvent(new ExpenseRecorded(ExpenseId.New(), description, category, context, items, total, vendor, date, receiptPath, DateTimeOffset.UtcNow));
        return exp;
    }

    public Result<ExpenseId, DomainError> Void(string reason)
    {
        if (IsVoided) return DomainError.Conflict("Expense is already voided.");
        RaiseEvent(new ExpenseVoided(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ExpenseRecorded e:
                Id = e.Id; Description = e.Description; Category = e.Category; Context = e.Context;
                _items.AddRange(e.Items); Total = e.Total; Vendor = e.Vendor; Date = e.Date; ReceiptPath = e.ReceiptPath; break;
            case ExpenseVoided: IsVoided = true; break;
        }
    }
}

public sealed class Revenue : AggregateRoot<RevenueId>
{
    public string Description { get; private set; } = "";
    public RevenueCategory Category { get; private set; }
    public LedgerContext Context { get; private set; }
    private readonly List<LineItem> _items = [];
    public IReadOnlyList<LineItem> Items => _items;
    public decimal Total { get; private set; }
    public string? CustomerName { get; private set; }
    public DateOnly Date { get; private set; }
    public bool IsVoided { get; private set; }

    public static Revenue Record(string description, RevenueCategory category, LedgerContext context,
        IReadOnlyList<LineItem> items, decimal total, string? customerName, DateOnly date)
    {
        var rev = new Revenue();
        rev.RaiseEvent(new RevenueRecorded(RevenueId.New(), description, category, context, items, total, customerName, date, DateTimeOffset.UtcNow));
        return rev;
    }

    public Result<RevenueId, DomainError> Void(string reason)
    {
        if (IsVoided) return DomainError.Conflict("Revenue is already voided.");
        RaiseEvent(new RevenueVoided(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case RevenueRecorded e:
                Id = e.Id; Description = e.Description; Category = e.Category; Context = e.Context;
                _items.AddRange(e.Items); Total = e.Total; CustomerName = e.CustomerName; Date = e.Date; break;
            case RevenueVoided: IsVoided = true; break;
        }
    }
}
