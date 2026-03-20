using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class PostHarvestBatchTests
{
    private static PostHarvestBatch CreateBatch(int stems = 200) =>
        PostHarvestBatch.Create(
            FlowerBedId.New(), SuccessionId.New(),
            "Dahlia", "Café au Lait", stems, new DateOnly(2026, 7, 15));

    [Fact]
    public void Create_ShouldSetPropertiesAndComputeStemsRemaining()
    {
        var batch = CreateBatch(200);

        batch.Species.Should().Be("Dahlia");
        batch.Cultivar.Should().Be("Café au Lait");
        batch.TotalStems.Should().Be(200);
        batch.StemsUsed.Should().Be(0);
        batch.StemsRemaining.Should().Be(200);
        batch.HarvestDate.Should().Be(new DateOnly(2026, 7, 15));
    }

    [Fact]
    public void Create_ShouldRaisePostHarvestBatchCreatedEvent()
    {
        var batch = CreateBatch();

        batch.UncommittedEvents.Should().ContainSingle(e => e is PostHarvestBatchCreated);
    }

    [Fact]
    public void GradeStems_ShouldAddGrade()
    {
        // ASCFG grading: Premium stems are 18"+ with perfect heads
        var batch = CreateBatch();
        batch.ClearEvents();

        batch.GradeStems(new StemGrade(HarvestGrade.Premium, 80, 24));
        batch.GradeStems(new StemGrade(HarvestGrade.Standard, 70, 18));
        batch.GradeStems(new StemGrade(HarvestGrade.Seconds, 30, 12));
        batch.GradeStems(new StemGrade(HarvestGrade.Cull, 20, 8));

        batch.Grades.Should().HaveCount(4);
        batch.Grades.Sum(g => g.StemCount).Should().Be(200);
    }

    [Fact]
    public void Condition_ShouldSetConditioningProperties()
    {
        // UMass Extension: condition in 100°F water with biocide
        var batch = CreateBatch();
        batch.ClearEvents();

        batch.Condition("Chrysal Professional #2", 100m);

        batch.IsConditioned.Should().BeTrue();
        batch.ConditioningSolution.Should().Be("Chrysal Professional #2");
        batch.WaterTempF.Should().Be(100m);
    }

    [Fact]
    public void MoveToCooler_ShouldSetCoolerProperties()
    {
        // Target: 32-35°F for most cut flowers
        var batch = CreateBatch();
        batch.ClearEvents();

        batch.MoveToCooler(34m, "Shelf-B2");

        batch.InCooler.Should().BeTrue();
        batch.CoolerTempF.Should().Be(34m);
        batch.CoolerSlot.Should().Be("Shelf-B2");
    }

    [Fact]
    public void UseStems_WithSufficientStock_ShouldSucceed()
    {
        var batch = CreateBatch(200);
        batch.ClearEvents();

        var result = batch.UseStems(50, "Wedding order #WED-042");

        result.IsSuccess.Should().BeTrue();
        batch.StemsRemaining.Should().Be(150);
        batch.StemsUsed.Should().Be(50);
    }

    [Fact]
    public void UseStems_WithInsufficientStock_ShouldFail()
    {
        var batch = CreateBatch(100);
        batch.ClearEvents();

        var result = batch.UseStems(150, "Farmers market bouquets");

        result.IsFailure.Should().BeTrue();
        batch.StemsRemaining.Should().Be(100); // unchanged
    }

    [Fact]
    public void UseStems_MultipleUses_ShouldAccumulate()
    {
        var batch = CreateBatch(200);
        batch.ClearEvents();

        batch.UseStems(60, "Market bouquets");
        batch.UseStems(40, "CSA shares");

        batch.StemsUsed.Should().Be(100);
        batch.StemsRemaining.Should().Be(100);
    }

    [Fact]
    public void FullLifecycle_Harvest_Grade_Condition_Cool_Use()
    {
        // Complete post-harvest pipeline per ASCFG/ATTRA standards
        var batch = CreateBatch(150);

        // Grade
        batch.GradeStems(new StemGrade(HarvestGrade.Premium, 60, 22));
        batch.GradeStems(new StemGrade(HarvestGrade.Standard, 50, 16));
        batch.GradeStems(new StemGrade(HarvestGrade.Seconds, 25, 10));
        batch.GradeStems(new StemGrade(HarvestGrade.Cull, 15, 6));

        // Condition
        batch.Condition("Floralife Clear 200", 100m);

        // Cool
        batch.MoveToCooler(34m, "Cooler-1/Row-A");

        // Use stems for orders
        var result = batch.UseStems(110, "Saturday market prep");

        result.IsSuccess.Should().BeTrue();
        batch.IsConditioned.Should().BeTrue();
        batch.InCooler.Should().BeTrue();
        batch.StemsRemaining.Should().Be(40);
    }
}
