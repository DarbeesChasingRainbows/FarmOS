using System;
using System.Linq;
using FarmOS.Campus.Domain;
using FarmOS.Campus.Domain.Aggregates;
using FarmOS.Campus.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Campus.Domain.Tests;

public class FarmEventTests
{
    private static EventSchedule CreateSchedule(int capacity = 20) =>
        new(DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0), new TimeOnly(12, 0),
            "Main Barn", capacity, 25.00m, 200.00m);

    [Fact]
    public void Create_ShouldSetDraftStatus()
    {
        // Arrange & Act
        var farmEvent = FarmEvent.Create(EventType.FarmTour, "Spring Tour", "A tour of the farm", CreateSchedule());

        // Assert
        farmEvent.Status.Should().Be(EventStatus.Draft);
        farmEvent.Type.Should().Be(EventType.FarmTour);
        farmEvent.Title.Should().Be("Spring Tour");
        farmEvent.Description.Should().Be("A tour of the farm");
        farmEvent.BookedCount.Should().Be(0);

        var @event = farmEvent.UncommittedEvents.OfType<FarmEventCreated>().Single();
        @event.Id.Should().Be(farmEvent.Id);
    }

    [Fact]
    public void Publish_ShouldSucceed_WhenDraft()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.Workshop, "Composting 101", null, CreateSchedule());
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        farmEvent.Status.Should().Be(EventStatus.Published);
        farmEvent.UncommittedEvents.Should().ContainSingle(e => e is FarmEventPublished);
    }

    [Fact]
    public void Publish_ShouldFail_WhenCancelled()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.FarmTour, "Tour", null, CreateSchedule());
        farmEvent.Cancel("Weather");
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        farmEvent.Status.Should().Be(EventStatus.Cancelled);
        farmEvent.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_ShouldSucceed()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.FieldDay, "Field Day", null, CreateSchedule());
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.Cancel("Rain forecast");

        // Assert
        result.IsSuccess.Should().BeTrue();
        farmEvent.Status.Should().Be(EventStatus.Cancelled);
        farmEvent.UncommittedEvents.Should().ContainSingle(e => e is FarmEventCancelled);
    }

    [Fact]
    public void Cancel_ShouldFail_WhenCompleted()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.FarmDinner, "Dinner", null, CreateSchedule());
        farmEvent.Publish();
        farmEvent.Complete(15, 375.00m);
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.Cancel("Changed mind");

        // Assert
        result.IsFailure.Should().BeTrue();
        farmEvent.Status.Should().Be(EventStatus.Completed);
        farmEvent.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Complete_ShouldSucceed_WhenPublished()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.Workshop, "Workshop", null, CreateSchedule());
        farmEvent.Publish();
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.Complete(18, 450.00m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        farmEvent.Status.Should().Be(EventStatus.Completed);
        farmEvent.UncommittedEvents.Should().ContainSingle(e => e is FarmEventCompleted);
    }

    [Fact]
    public void ReserveSpot_ShouldSucceed_WithCapacity()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.FarmTour, "Tour", null, CreateSchedule(capacity: 20));
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.ReserveSpot(4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        farmEvent.BookedCount.Should().Be(4);
        farmEvent.UncommittedEvents.Should().ContainSingle(e => e is SpotReserved);
    }

    [Fact]
    public void ReserveSpot_ShouldFail_WhenFull()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.FarmTour, "Tour", null, CreateSchedule(capacity: 5));
        farmEvent.ReserveSpot(5);
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.ReserveSpot(1);

        // Assert
        result.IsFailure.Should().BeTrue();
        farmEvent.BookedCount.Should().Be(5);
        farmEvent.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReserveSpot_ShouldSetFullStatus_WhenExactlyFilled()
    {
        // Arrange
        var farmEvent = FarmEvent.Create(EventType.PrivateTour, "Private Tour", null, CreateSchedule(capacity: 10));
        farmEvent.ReserveSpot(6);
        farmEvent.ClearEvents();

        // Act
        var result = farmEvent.ReserveSpot(4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        farmEvent.BookedCount.Should().Be(10);
        farmEvent.Status.Should().Be(EventStatus.Full);
    }
}
