using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class FlowerBedTests
{
    private static FlowerBed CreateBed() =>
        FlowerBed.Create("Bed A-1", "Block A", new BedDimensions(100, 4));

    [Fact]
    public void Create_ShouldSetNameBlockAndDimensions()
    {
        var bed = CreateBed();

        bed.Name.Should().Be("Bed A-1");
        bed.Block.Should().Be("Block A");
        bed.Dimensions.LengthFeet.Should().Be(100);
        bed.Dimensions.WidthFeet.Should().Be(4);
    }

    [Fact]
    public void Create_ShouldRaiseFlowerBedCreatedEvent()
    {
        var bed = CreateBed();

        bed.UncommittedEvents.Should().ContainSingle(e => e is FlowerBedCreated);
        var @event = bed.UncommittedEvents.OfType<FlowerBedCreated>().Single();
        @event.Name.Should().Be("Bed A-1");
        @event.Block.Should().Be("Block A");
    }

    [Fact]
    public void PlanSuccession_ShouldAddSuccession()
    {
        var bed = CreateBed();
        var variety = new CropVariety("Zinnia", "Benary Giant Lime", 75, "Lime");

        var succId = bed.PlanSuccession(
            variety,
            sow: new DateOnly(2026, 3, 1),
            transplant: new DateOnly(2026, 4, 1),
            harvestStart: new DateOnly(2026, 6, 15));

        succId.Should().NotBeNull();
        bed.Successions.Should().HaveCount(1);
        bed.Successions[0].Variety.Species.Should().Be("Zinnia");
        bed.Successions[0].Variety.Cultivar.Should().Be("Benary Giant Lime");
    }

    [Fact]
    public void PlanSuccession_ShouldRaiseSuccessionPlannedEvent()
    {
        var bed = CreateBed();
        bed.ClearEvents();

        bed.PlanSuccession(
            new CropVariety("Dahlia", "Café au Lait", 90),
            new DateOnly(2026, 2, 15),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 7, 1));

        bed.UncommittedEvents.Should().ContainSingle(e => e is SuccessionPlanned);
    }

    [Fact]
    public void RecordHarvest_ShouldRaiseFlowerHarvestRecordedEvent()
    {
        var bed = CreateBed();
        var succId = bed.PlanSuccession(
            new CropVariety("Sunflower", "ProCut Orange", 60),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 7, 1));
        bed.ClearEvents();

        bed.RecordHarvest(succId, new Quantity(120, "stems", "count"), new DateOnly(2026, 7, 10));

        bed.UncommittedEvents.Should().ContainSingle(e => e is FlowerHarvestRecorded);
        var harvest = bed.UncommittedEvents.OfType<FlowerHarvestRecorded>().Single();
        harvest.Stems.Value.Should().Be(120);
    }

    [Fact]
    public void MultipleSuccessions_ShouldTrackIndependently()
    {
        // Succession planting: 7-14 day intervals (ASCFG recommendation)
        var bed = CreateBed();
        var variety = new CropVariety("Snapdragon", "Rocket Mix", 85);

        bed.PlanSuccession(variety, new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 15));
        bed.PlanSuccession(variety, new DateOnly(2026, 3, 15), new DateOnly(2026, 4, 15), new DateOnly(2026, 6, 29));
        bed.PlanSuccession(variety, new DateOnly(2026, 3, 29), new DateOnly(2026, 4, 29), new DateOnly(2026, 7, 13));

        bed.Successions.Should().HaveCount(3);
        bed.Successions.Select(s => s.SowDate).Should().BeInAscendingOrder();
    }
}
