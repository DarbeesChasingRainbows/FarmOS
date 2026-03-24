using FarmOS.Counter.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain.Aggregates;

public sealed class Sale : AggregateRoot<SaleId>
{
    private const decimal TaxRate = 0.0825m;

    public RegisterId RegisterId { get; private set; } = new(Guid.Empty);
    public IReadOnlyList<SaleLineItem> Items { get; private set; } = [];
    public IReadOnlyList<PaymentRecord> Payments { get; private set; } = [];
    public decimal Total { get; private set; }
    public decimal TaxAmount { get; private set; }
    public SaleStatus Status { get; private set; }
    public string? CustomerName { get; private set; }

    public static Result<Sale, DomainError> Complete(
        RegisterId registerId,
        IReadOnlyList<SaleLineItem> items,
        IReadOnlyList<PaymentRecord> payments,
        string? customerName)
    {
        // Validate EBT payments — EBT can only pay for NonTaxable or StandardFood items
        var hasEbtPayment = payments.Any(p => p.Method == PaymentMethod.EBT);
        if (hasEbtPayment)
        {
            var hasIneligibleItems = items.Any(i =>
                i.TaxCat != TaxCategory.NonTaxable && i.TaxCat != TaxCategory.StandardFood);
            if (hasIneligibleItems)
                return DomainError.Validation("EBT payments are only allowed for NonTaxable and StandardFood items.");
        }

        // Calculate subtotal and tax
        var subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = items
            .Where(i => i.TaxCat is TaxCategory.PreparedFood or TaxCategory.NonFood)
            .Sum(i => i.Quantity * i.UnitPrice * TaxRate);
        taxAmount = Math.Round(taxAmount, 2);
        var total = subtotal + taxAmount;

        // Validate payment covers total
        var totalPayment = payments.Sum(p => p.Amount);
        if (totalPayment < total)
            return DomainError.Validation("Total payment is less than the sale total including tax.");

        var sale = new Sale();
        sale.RaiseEvent(new SaleCompleted(
            SaleId.New(), registerId, items, payments, total, taxAmount, customerName, DateTimeOffset.UtcNow));
        return sale;
    }

    public Result<SaleId, DomainError> Void(string reason)
    {
        if (Status is SaleStatus.Voided or SaleStatus.Refunded)
            return DomainError.Conflict("Sale has already been voided or refunded.");
        RaiseEvent(new SaleVoided(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<SaleId, DomainError> Refund(decimal refundAmount, string reason)
    {
        if (Status == SaleStatus.Voided)
            return DomainError.Conflict("Cannot refund a voided sale.");
        if (refundAmount > Total)
            return DomainError.Validation("Refund amount cannot exceed the sale total.");
        RaiseEvent(new SaleRefunded(Id, refundAmount, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SaleCompleted e:
                Id = e.Id; RegisterId = e.RegisterId; Items = e.Items; Payments = e.Payments;
                Total = e.Total; TaxAmount = e.TaxAmount; CustomerName = e.CustomerName;
                Status = SaleStatus.Completed; break;
            case SaleVoided: Status = SaleStatus.Voided; break;
            case SaleRefunded: Status = SaleStatus.Refunded; break;
        }
    }
}
