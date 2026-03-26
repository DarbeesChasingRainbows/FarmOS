using System;
using System.Linq;
using FarmOS.Campus.Domain;
using FarmOS.Campus.Domain.Aggregates;
using FarmOS.Campus.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Campus.Domain.Tests;

public class BookingTests
{
    private static AttendeeInfo CreateAttendee() =>
        new("John Smith", "john@farm.com", "555-0200", 2, "Vegetarian");

    [Fact]
    public void Create_ShouldSetReservedStatus()
    {
        // Arrange
        var eventId = EventId.New();
        var attendee = CreateAttendee();

        // Act
        var booking = Booking.Create(eventId, attendee);

        // Assert
        booking.Status.Should().Be(BookingStatus.Reserved);
        booking.EventId.Should().Be(eventId);
        booking.Attendee.Should().Be(attendee);

        var @event = booking.UncommittedEvents.OfType<BookingCreated>().Single();
        @event.Id.Should().Be(booking.Id);
    }

    [Fact]
    public void Confirm_ShouldSucceed_WhenReserved()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.ClearEvents();

        // Act
        var result = booking.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UncommittedEvents.Should().ContainSingle(e => e is BookingConfirmed);
    }

    [Fact]
    public void Confirm_ShouldFail_WhenNotReserved()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.Confirm();
        booking.ClearEvents();

        // Act
        var result = booking.Confirm();

        // Assert
        result.IsFailure.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void CheckIn_ShouldSucceed_WhenConfirmed()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.Confirm();
        booking.ClearEvents();

        // Act
        var result = booking.CheckIn();

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.CheckedIn);
        booking.UncommittedEvents.Should().ContainSingle(e => e is BookingCheckedIn);
    }

    [Fact]
    public void Cancel_ShouldSucceed_WhenReserved()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.ClearEvents();

        // Act
        var result = booking.Cancel("Changed plans");

        // Assert
        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.UncommittedEvents.Should().ContainSingle(e => e is BookingCancelled);
    }

    [Fact]
    public void Cancel_ShouldFail_WhenCheckedIn()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.Confirm();
        booking.CheckIn();
        booking.ClearEvents();

        // Act
        var result = booking.Cancel("Too late");

        // Assert
        result.IsFailure.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.CheckedIn);
        booking.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void SignWaiver_ShouldSetWaiver()
    {
        // Arrange
        var booking = Booking.Create(EventId.New(), CreateAttendee());
        booking.ClearEvents();
        var waiver = new WaiverInfo("John Smith", DateTimeOffset.UtcNow, "/waivers/123.pdf");

        // Act
        booking.SignWaiver(waiver);

        // Assert
        booking.Waiver.Should().Be(waiver);
        booking.UncommittedEvents.Should().ContainSingle(e => e is WaiverSigned);
    }
}
