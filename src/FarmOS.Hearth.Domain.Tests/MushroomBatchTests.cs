using System;
using System.Linq;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class MushroomBatchTests
{
    [Fact]
    public void Start_ShouldCreateBatchAndRaiseEvent()
    {
        // Arrange
        var id = new BatchId(Guid.NewGuid());
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var batch = MushroomBatch.Start(id, "MB-01", "Lions Mane", "LM-01", "Hardwood", null, timestamp);

        // Assert
        batch.Id.Should().Be(id);
        batch.Phase.Should().Be(MushroomPhase.Inoculation);
        batch.BatchCode.Should().Be("MB-01");
        
        var @event = batch.UncommittedEvents.OfType<MushroomBatchStarted>().Single();
        @event.Id.Should().Be(id);
        @event.BatchCode.Should().Be("MB-01");
    }

    [Fact]
    public void AdvancePhase_ShouldSucceed_WhenValid()
    {
        // Arrange
        var batch = MushroomBatch.Start(new BatchId(Guid.NewGuid()), "MB", "Sp", "Cv", "Sub", null, DateTimeOffset.UtcNow);
        batch.ClearEvents();

        // Act
        var result = batch.AdvancePhase(MushroomPhase.Colonization, DateTimeOffset.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.Phase.Should().Be(MushroomPhase.Colonization);
        batch.UncommittedEvents.Should().ContainSingle(e => e is MushroomPhaseAdvanced);
    }

    [Fact]
    public void AdvancePhase_ShouldFail_WhenContaminated()
    {
        // Arrange
        var batch = MushroomBatch.Start(new BatchId(Guid.NewGuid()), "MB", "Sp", "Cv", "Sub", null, DateTimeOffset.UtcNow);
        batch.MarkContaminated("Mold", DateTimeOffset.UtcNow);
        batch.ClearEvents();

        // Act
        var result = batch.AdvancePhase(MushroomPhase.Fruiting, DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.Phase.Should().Be(MushroomPhase.Contaminated);
        batch.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordFlush_ShouldFail_WhenNotInFruitingOrResting()
    {
        // Arrange
        var batch = MushroomBatch.Start(new BatchId(Guid.NewGuid()), "MB", "Sp", "Cv", "Sub", null, DateTimeOffset.UtcNow);
        // Currently in Inoculation

        // Act
        var result = batch.RecordFlush(new Quantity(5, "lbs", "weight"), DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.FlushCount.Should().Be(0);
    }

    [Fact]
    public void RecordFlush_ShouldSucceed_AndRaiseHarvestAvailableEvent()
    {
        // Arrange
        var batch = MushroomBatch.Start(new BatchId(Guid.NewGuid()), "MB", "Sp", "Cv", "Sub", null, DateTimeOffset.UtcNow);
        batch.AdvancePhase(MushroomPhase.Fruiting, DateTimeOffset.UtcNow);
        batch.ClearEvents();

        var yield = new Quantity(5.5m, "lbs", "weight");
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = batch.RecordFlush(yield, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.FlushCount.Should().Be(1);
        batch.Yield.Should().Be(yield);
        
        var @events = batch.UncommittedEvents.ToList();
        @events.Should().HaveCount(2);
        @events.Should().ContainSingle(e => e is MushroomFlushRecorded);
        @events.Should().ContainSingle(e => e is MushroomHarvestAvailable);
    }
}
