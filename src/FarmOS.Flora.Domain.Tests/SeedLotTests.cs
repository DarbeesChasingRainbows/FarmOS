using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class SeedLotTests
{
    private static SeedLot CreateLot(decimal qty = 500) =>
        SeedLot.Create(
            new CropVariety("Zinnia", "Queen Red Lime", 75),
            "Johnny's Selected Seeds",
            new Quantity(qty, "seeds", "count"),
            germPct: 92,
            year: 2025,
            organic: true);

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var lot = CreateLot();

        lot.Variety.Species.Should().Be("Zinnia");
        lot.Variety.Cultivar.Should().Be("Queen Red Lime");
        lot.Supplier.Should().Be("Johnny's Selected Seeds");
        lot.QuantityOnHand.Value.Should().Be(500);
        lot.GerminationPct.Should().Be(92);
        lot.HarvestYear.Should().Be(2025);
        lot.IsOrganic.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseSeedLotCreatedEvent()
    {
        var lot = CreateLot();

        lot.UncommittedEvents.Should().ContainSingle(e => e is SeedLotCreated);
    }

    [Fact]
    public void Withdraw_WithSufficientStock_ShouldSucceed()
    {
        var lot = CreateLot(500);
        lot.ClearEvents();
        var destBed = FlowerBedId.New();

        var result = lot.Withdraw(new Quantity(200, "seeds", "count"), destBed);

        result.IsSuccess.Should().BeTrue();
        lot.QuantityOnHand.Value.Should().Be(300);
    }

    [Fact]
    public void Withdraw_WithInsufficientStock_ShouldFail()
    {
        var lot = CreateLot(50);
        lot.ClearEvents();

        var result = lot.Withdraw(new Quantity(100, "seeds", "count"), FlowerBedId.New());

        result.IsFailure.Should().BeTrue();
        lot.QuantityOnHand.Value.Should().Be(50); // unchanged
    }

    [Fact]
    public void Withdraw_ShouldRaiseSeedWithdrawnEvent()
    {
        var lot = CreateLot(500);
        lot.ClearEvents();

        lot.Withdraw(new Quantity(100, "seeds", "count"), FlowerBedId.New());

        lot.UncommittedEvents.Should().ContainSingle(e => e is SeedWithdrawn);
    }

    [Fact]
    public void Restock_ShouldIncreaseQuantity()
    {
        var lot = CreateLot(200);
        lot.ClearEvents();

        lot.Restock(new Quantity(300, "seeds", "count"), "LOT-2026-001");

        lot.QuantityOnHand.Value.Should().Be(500);
    }

    [Fact]
    public void Restock_ShouldRaiseSeedRestockedEvent()
    {
        var lot = CreateLot(200);
        lot.ClearEvents();

        lot.Restock(new Quantity(100, "seeds", "count"), null);

        lot.UncommittedEvents.Should().ContainSingle(e => e is SeedRestocked);
    }

    [Fact]
    public void MultipleWithdrawals_ShouldAccumulateCorrectly()
    {
        var lot = CreateLot(1000);
        lot.ClearEvents();

        lot.Withdraw(new Quantity(200, "seeds", "count"), FlowerBedId.New());
        lot.Withdraw(new Quantity(300, "seeds", "count"), FlowerBedId.New());

        lot.QuantityOnHand.Value.Should().Be(500);
    }
}
