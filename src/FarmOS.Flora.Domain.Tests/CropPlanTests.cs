using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class CropPlanTests
{
    private static CropPlan CreatePlan() =>
        CropPlan.Create(2026, "Summer", "Summer Dahlia Program");

    [Fact]
    public void Create_ShouldSetSeasonAndPlanName()
    {
        var plan = CreatePlan();

        plan.SeasonYear.Should().Be(2026);
        plan.SeasonName.Should().Be("Summer");
        plan.PlanName.Should().Be("Summer Dahlia Program");
    }

    [Fact]
    public void Create_ShouldRaiseCropPlanCreatedEvent()
    {
        var plan = CreatePlan();

        plan.UncommittedEvents.Should().ContainSingle(e => e is CropPlanCreated);
    }

    [Fact]
    public void AssignBed_ShouldTrackBedAssignment()
    {
        var plan = CreatePlan();
        plan.ClearEvents();
        var bedId = FlowerBedId.New();

        plan.AssignBed(new BedAssignment(bedId, new CropVariety("Dahlia", "Café au Lait", 90), 3));

        plan.BedAssignments.Should().HaveCount(1);
        plan.BedAssignments[0].PlannedSuccessions.Should().Be(3);
    }

    [Fact]
    public void RecordYield_ShouldTrackStemsPerLinearFoot()
    {
        // Yield tracking: stems per linear foot is the key metric (Flourish/Artemis pattern)
        var plan = CreatePlan();
        plan.ClearEvents();
        var bedId = FlowerBedId.New();
        var succId = SuccessionId.New();

        plan.RecordYield(bedId, succId, stemsHarvested: 480, stemsPerLinearFoot: 4.8m, new DateOnly(2026, 7, 20));

        plan.TotalStemsHarvested.Should().Be(480);
    }

    [Fact]
    public void RecordCost_ShouldAccumulate()
    {
        var plan = CreatePlan();
        plan.ClearEvents();

        plan.RecordCost(new CostEntry("seed", 125.50m, "Dahlia tubers from Swan Island"));
        plan.RecordCost(new CostEntry("soil", 85.00m, "Compost delivery"));
        plan.RecordCost(new CostEntry("labor", 200.00m, "Planting day crew"));

        plan.TotalCosts.Should().Be(410.50m);
        plan.Costs.Should().HaveCount(3);
    }

    [Fact]
    public void RecordRevenue_ShouldTrackByChannel()
    {
        var plan = CreatePlan();
        plan.ClearEvents();

        plan.RecordRevenue(SalesChannel.FarmersMarket, 850m, new DateOnly(2026, 7, 25), "Saturday market");
        plan.RecordRevenue(SalesChannel.Wedding, 1200m, new DateOnly(2026, 7, 28), "Smith-Jones wedding");
        plan.RecordRevenue(SalesChannel.CSA, 400m, new DateOnly(2026, 7, 25));

        plan.TotalRevenue.Should().Be(2450m);
    }

    [Fact]
    public void ProfitabilityAnalysis_RevenueMinusCosts()
    {
        var plan = CreatePlan();
        plan.ClearEvents();

        // Costs
        plan.RecordCost(new CostEntry("seed", 200m, null));
        plan.RecordCost(new CostEntry("labor", 500m, null));
        plan.RecordCost(new CostEntry("supplies", 100m, null));

        // Revenue
        plan.RecordRevenue(SalesChannel.FarmersMarket, 600m, new DateOnly(2026, 8, 1));
        plan.RecordRevenue(SalesChannel.Wholesale, 400m, new DateOnly(2026, 8, 5));
        plan.RecordRevenue(SalesChannel.Wedding, 800m, new DateOnly(2026, 8, 10));

        var profit = plan.TotalRevenue - plan.TotalCosts;
        profit.Should().Be(1000m);
    }

    [Fact]
    public void MultipleBedAssignments_WithYields_ShouldAccumulate()
    {
        var plan = CreatePlan();
        var bed1 = FlowerBedId.New();
        var bed2 = FlowerBedId.New();
        var succ1 = SuccessionId.New();
        var succ2 = SuccessionId.New();

        plan.AssignBed(new BedAssignment(bed1, new CropVariety("Dahlia", "Café au Lait", 90), 2));
        plan.AssignBed(new BedAssignment(bed2, new CropVariety("Zinnia", "Benary Giant", 75), 3));

        plan.RecordYield(bed1, succ1, 240, 4.8m, new DateOnly(2026, 7, 20));
        plan.RecordYield(bed2, succ2, 360, 7.2m, new DateOnly(2026, 7, 25));

        plan.BedAssignments.Should().HaveCount(2);
        plan.TotalStemsHarvested.Should().Be(600);
    }
}
