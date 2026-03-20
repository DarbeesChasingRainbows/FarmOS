using System;
using System.Linq;
using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Crew.Domain.Tests;

public class ShiftTests
{
    private static ShiftEntry CreateEntry() =>
        new(WorkerId.New(), Enterprise.Pasture, DateOnly.FromDateTime(DateTime.UtcNow),
            new TimeOnly(8, 0), new TimeOnly(16, 0), "Fence repair", null);

    [Fact]
    public void Schedule_ShouldCreateShiftAndRaiseEvent()
    {
        // Arrange
        var entry = CreateEntry();

        // Act
        var shift = Shift.Schedule(entry);

        // Assert
        shift.Status.Should().Be(ShiftStatus.Scheduled);
        shift.Entry.Should().Be(entry);

        var @event = shift.UncommittedEvents.OfType<ShiftScheduled>().Single();
        @event.Entry.Should().Be(entry);
        @event.Id.Should().Be(shift.Id);
    }

    [Fact]
    public void Start_ShouldSucceed_WhenScheduled()
    {
        // Arrange
        var shift = Shift.Schedule(CreateEntry());
        shift.ClearEvents();

        // Act
        var result = shift.Start();

        // Assert
        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.InProgress);
        shift.UncommittedEvents.Should().ContainSingle(e => e is ShiftStarted);
    }

    [Fact]
    public void Complete_ShouldSucceed_WhenInProgress()
    {
        // Arrange
        var shift = Shift.Schedule(CreateEntry());
        shift.Start();
        shift.ClearEvents();

        // Act
        var result = shift.Complete("All done");

        // Assert
        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Completed);
        shift.UncommittedEvents.Should().ContainSingle(e => e is ShiftCompleted);
    }

    [Fact]
    public void Complete_ShouldFail_WhenNotInProgress()
    {
        // Arrange
        var shift = Shift.Schedule(CreateEntry());
        shift.ClearEvents();

        // Act
        var result = shift.Complete("Trying to complete");

        // Assert
        result.IsFailure.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Scheduled);
        shift.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_ShouldSucceed_WhenScheduled()
    {
        // Arrange
        var shift = Shift.Schedule(CreateEntry());
        shift.ClearEvents();

        // Act
        var result = shift.Cancel("Weather");

        // Assert
        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Cancelled);
        shift.UncommittedEvents.Should().ContainSingle(e => e is ShiftCancelled);
    }

    [Fact]
    public void Cancel_ShouldFail_WhenCompleted()
    {
        // Arrange
        var shift = Shift.Schedule(CreateEntry());
        shift.Start();
        shift.Complete("Done");
        shift.ClearEvents();

        // Act
        var result = shift.Cancel("Too late");

        // Assert
        result.IsFailure.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Completed);
        shift.UncommittedEvents.Should().BeEmpty();
    }
}
