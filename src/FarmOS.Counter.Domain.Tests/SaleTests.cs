using FarmOS.Counter.Domain;
using FarmOS.Counter.Domain.Aggregates;
using FarmOS.Counter.Domain.Events;
using FluentAssertions;

namespace FarmOS.Counter.Domain.Tests;

public class SaleTests
{
    private static readonly RegisterId TestRegisterId = RegisterId.New();

    private static IReadOnlyList<SaleLineItem> SimpleItems(TaxCategory taxCat = TaxCategory.NonTaxable) =>
        [new SaleLineItem("Tomatoes", "TOM-001", 2, 5.00m, taxCat, null)];

    private static IReadOnlyList<PaymentRecord> CashPayment(decimal amount) =>
        [new PaymentRecord(PaymentMethod.Cash, amount, null)];

    [Fact]
    public void Complete_ShouldSucceed_WithValidPayment()
    {
        // Arrange
        var items = SimpleItems();
        var payments = CashPayment(10.00m);

        // Act
        var result = Sale.Complete(TestRegisterId, items, payments, "John");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sale = result.Value;
        sale.Status.Should().Be(SaleStatus.Completed);
        sale.Total.Should().Be(10.00m);
        sale.TaxAmount.Should().Be(0m);
        sale.CustomerName.Should().Be("John");
        sale.RegisterId.Should().Be(TestRegisterId);
    }

    [Fact]
    public void Complete_ShouldFail_WithInsufficientPayment()
    {
        // Arrange
        var items = SimpleItems();
        var payments = CashPayment(5.00m);

        // Act
        var result = Sale.Complete(TestRegisterId, items, payments, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public void Complete_ShouldFail_WithEbtOnPreparedFood()
    {
        // Arrange
        var items = new List<SaleLineItem>
        {
            new("Hot Soup", null, 1, 8.00m, TaxCategory.PreparedFood, null)
        };
        var payments = new List<PaymentRecord>
        {
            new(PaymentMethod.EBT, 20.00m, null)
        };

        // Act
        var result = Sale.Complete(TestRegisterId, items, payments, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public void Complete_ShouldCalculateTaxCorrectly_ForPreparedFoodAndNonFood()
    {
        // Arrange — PreparedFood and NonFood get 8.25% tax
        var items = new List<SaleLineItem>
        {
            new("Hot Dog", null, 1, 10.00m, TaxCategory.PreparedFood, null),
            new("T-Shirt", null, 1, 20.00m, TaxCategory.NonFood, null),
            new("Apples", null, 1, 5.00m, TaxCategory.NonTaxable, null)
        };
        // Tax = (10.00 + 20.00) * 0.0825 = 2.475 => rounded to 2.48
        // Total = 35.00 + 2.48 = 37.48
        var payments = CashPayment(37.48m);

        // Act
        var result = Sale.Complete(TestRegisterId, items, payments, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sale = result.Value;
        sale.TaxAmount.Should().Be(2.48m);
        sale.Total.Should().Be(37.48m);
    }

    [Fact]
    public void Void_ShouldSucceed_WhenCompleted()
    {
        // Arrange
        var result = Sale.Complete(TestRegisterId, SimpleItems(), CashPayment(10.00m), null);
        var sale = result.Value;
        sale.ClearEvents();

        // Act
        var voidResult = sale.Void("Customer changed mind");

        // Assert
        voidResult.IsSuccess.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Voided);
        sale.UncommittedEvents.Should().ContainSingle(e => e is SaleVoided);
    }

    [Fact]
    public void Void_ShouldFail_WhenAlreadyVoided()
    {
        // Arrange
        var result = Sale.Complete(TestRegisterId, SimpleItems(), CashPayment(10.00m), null);
        var sale = result.Value;
        sale.Void("First void");
        sale.ClearEvents();

        // Act
        var voidResult = sale.Void("Second void");

        // Assert
        voidResult.IsFailure.Should().BeTrue();
        sale.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Refund_ShouldSucceed_WithValidAmount()
    {
        // Arrange
        var result = Sale.Complete(TestRegisterId, SimpleItems(), CashPayment(10.00m), null);
        var sale = result.Value;
        sale.ClearEvents();

        // Act
        var refundResult = sale.Refund(5.00m, "Partial refund");

        // Assert
        refundResult.IsSuccess.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Refunded);
        sale.UncommittedEvents.Should().ContainSingle(e => e is SaleRefunded);
    }

    [Fact]
    public void Refund_ShouldFail_WhenAmountExceedsTotal()
    {
        // Arrange
        var result = Sale.Complete(TestRegisterId, SimpleItems(), CashPayment(10.00m), null);
        var sale = result.Value;
        sale.ClearEvents();

        // Act
        var refundResult = sale.Refund(20.00m, "Too much");

        // Assert
        refundResult.IsFailure.Should().BeTrue();
        refundResult.Error.Code.Should().Be("VALIDATION_ERROR");
        sale.UncommittedEvents.Should().BeEmpty();
    }
}
