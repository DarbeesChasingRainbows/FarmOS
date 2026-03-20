using System;
using System.Linq;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class FreezeDryerBatchTests
{
    private static FreezeDryerBatch CreateBatch() =>
        FreezeDryerBatch.Start("FD-001", FreezeDryerId.New(), "Strawberry Slices", 12.5m);

    private static FreezeDryerBatch CreateBatchInPhase(FreezeDryerPhase target)
    {
        var batch = CreateBatch();
        if (target == FreezeDryerPhase.Loading) return batch;

        batch.AdvancePhase(FreezeDryerPhase.Freezing);
        if (target == FreezeDryerPhase.Freezing) return batch;

        batch.AdvancePhase(FreezeDryerPhase.PrimaryDrying);
        if (target == FreezeDryerPhase.PrimaryDrying) return batch;

        batch.AdvancePhase(FreezeDryerPhase.SecondaryDrying);
        return batch;
    }

    private static FreezeDryerReading SampleReading() =>
        new(DateTimeOffset.UtcNow, -40m, 100m, -35m, null);

    [Fact]
    public void Start_ShouldCreateBatch_InLoadingPhase()
    {
        // Arrange & Act
        var batch = FreezeDryerBatch.Start("FD-001", FreezeDryerId.New(), "Strawberry Slices", 12.5m);

        // Assert
        batch.Phase.Should().Be(FreezeDryerPhase.Loading);
        batch.Id.Should().NotBeNull();
    }

    [Fact]
    public void Start_ShouldRaiseFreezeDryerBatchStartedEvent()
    {
        // Arrange & Act
        var batch = FreezeDryerBatch.Start("FD-001", FreezeDryerId.New(), "Strawberry Slices", 12.5m);

        // Assert
        batch.UncommittedEvents.Should().ContainSingle(e => e is FreezeDryerBatchStarted);
        var @event = batch.UncommittedEvents.OfType<FreezeDryerBatchStarted>().Single();
        @event.BatchCode.Should().Be("FD-001");
    }

    [Fact]
    public void AdvancePhase_Loading_To_Freezing_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();

        // Act
        var result = batch.AdvancePhase(FreezeDryerPhase.Freezing);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Freezing);
    }

    [Fact]
    public void AdvancePhase_Skip_ShouldFail()
    {
        // Arrange
        var batch = CreateBatch();

        // Act — skip Freezing, go straight to PrimaryDrying
        var result = batch.AdvancePhase(FreezeDryerPhase.PrimaryDrying);

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Loading);
    }

    [Fact]
    public void AdvancePhase_Complete_ShouldFail()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.SecondaryDrying);
        batch.Complete(3.2m);
        batch.ClearEvents();

        // Act
        var result = batch.AdvancePhase(FreezeDryerPhase.Freezing);

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Complete);
    }

    [Fact]
    public void RecordReading_InFreezingPhase_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.Freezing);
        batch.ClearEvents();

        // Act
        var result = batch.RecordReading(SampleReading());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecordReading_InLoadingPhase_ShouldFail()
    {
        // Arrange
        var batch = CreateBatch();

        // Act
        var result = batch.RecordReading(SampleReading());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordReading_InCompletedBatch_ShouldFail()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.SecondaryDrying);
        batch.Complete(3.2m);
        batch.ClearEvents();

        // Act
        var result = batch.RecordReading(SampleReading());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Complete_FromSecondaryDrying_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.SecondaryDrying);
        batch.ClearEvents();

        // Act
        var result = batch.Complete(3.2m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Complete);
    }

    [Fact]
    public void Complete_FromWrongPhase_ShouldFail()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.Freezing);

        // Act
        var result = batch.Complete(3.2m);

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Freezing);
    }

    [Fact]
    public void Abort_ActiveBatch_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.PrimaryDrying);
        batch.ClearEvents();

        // Act
        var result = batch.Abort("Equipment malfunction");

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Aborted);
    }

    [Fact]
    public void Abort_CompletedBatch_ShouldFail()
    {
        // Arrange
        var batch = CreateBatchInPhase(FreezeDryerPhase.SecondaryDrying);
        batch.Complete(3.2m);
        batch.ClearEvents();

        // Act
        var result = batch.Abort("Too late");

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.Phase.Should().Be(FreezeDryerPhase.Complete);
    }
}
