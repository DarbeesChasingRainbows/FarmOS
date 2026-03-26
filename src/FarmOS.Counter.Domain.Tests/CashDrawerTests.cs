using FarmOS.Counter.Domain;
using FarmOS.Counter.Domain.Aggregates;
using FarmOS.Counter.Domain.Events;
using FluentAssertions;

namespace FarmOS.Counter.Domain.Tests;

public class CashDrawerTests
{
    private static readonly RegisterId TestRegisterId = RegisterId.New();

    [Fact]
    public void Open_ShouldCreateDrawerAndRaiseEvent()
    {
        // Arrange & Act
        var drawer = CashDrawer.Open(TestRegisterId, 100.00m);

        // Assert
        drawer.RegisterId.Should().Be(TestRegisterId);
        drawer.StartingCash.Should().Be(100.00m);
        drawer.IsReconciled.Should().BeFalse();

        var @event = drawer.UncommittedEvents.OfType<CashDrawerOpened>().Single();
        @event.RegisterId.Should().Be(TestRegisterId);
        @event.StartingCash.Should().Be(100.00m);
    }

    [Fact]
    public void Count_ShouldRecordCountAndRaiseEvent()
    {
        // Arrange
        var drawer = CashDrawer.Open(TestRegisterId, 100.00m);
        drawer.ClearEvents();
        var count = new DrawerCount(150.00m, 148.50m, "Short $1.50");

        // Act
        drawer.Count(count);

        // Assert
        drawer.LastCount.Should().Be(count);
        drawer.UncommittedEvents.Should().ContainSingle(e => e is CashDrawerCounted);
    }

    [Fact]
    public void Reconcile_ShouldSucceed_AfterCount()
    {
        // Arrange
        var drawer = CashDrawer.Open(TestRegisterId, 100.00m);
        drawer.Count(new DrawerCount(150.00m, 148.50m, null));
        drawer.ClearEvents();

        // Act
        var result = drawer.Reconcile();

        // Assert
        result.IsSuccess.Should().BeTrue();
        drawer.IsReconciled.Should().BeTrue();
        drawer.UncommittedEvents.Should().ContainSingle(e => e is CashDrawerReconciled);
    }

    [Fact]
    public void Reconcile_ShouldFail_WithNoCount()
    {
        // Arrange
        var drawer = CashDrawer.Open(TestRegisterId, 100.00m);
        drawer.ClearEvents();

        // Act
        var result = drawer.Reconcile();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDATION_ERROR");
        drawer.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Discrepancy_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var drawer = CashDrawer.Open(TestRegisterId, 100.00m);
        drawer.Count(new DrawerCount(150.00m, 148.50m, null));

        // Act
        var result = drawer.Reconcile();

        // Assert
        result.IsSuccess.Should().BeTrue();
        drawer.Discrepancy.Should().Be(-1.50m); // Actual(148.50) - Expected(150.00) = -1.50
    }
}
