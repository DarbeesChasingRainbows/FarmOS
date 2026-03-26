using FarmOS.Counter.Domain;
using FarmOS.Counter.Domain.Aggregates;
using FarmOS.Counter.Domain.Events;
using FluentAssertions;

namespace FarmOS.Counter.Domain.Tests;

public class RegisterTests
{
    [Fact]
    public void Open_ShouldCreateOpenRegisterAndRaiseEvent()
    {
        // Arrange & Act
        var register = Register.Open(RegisterLocation.FarmStore, "Alice");

        // Assert
        register.Status.Should().Be(RegisterStatus.Open);
        register.Location.Should().Be(RegisterLocation.FarmStore);
        register.OperatorName.Should().Be("Alice");

        var @event = register.UncommittedEvents.OfType<RegisterOpened>().Single();
        @event.Location.Should().Be(RegisterLocation.FarmStore);
        @event.OperatorName.Should().Be("Alice");
        @event.Id.Should().Be(register.Id);
    }

    [Fact]
    public void Close_ShouldSucceed_WhenOpen()
    {
        // Arrange
        var register = Register.Open(RegisterLocation.Cafe, "Bob");
        register.ClearEvents();

        // Act
        var result = register.Close();

        // Assert
        result.IsSuccess.Should().BeTrue();
        register.Status.Should().Be(RegisterStatus.Closed);
        register.UncommittedEvents.Should().ContainSingle(e => e is RegisterClosed);
    }

    [Fact]
    public void Close_ShouldFail_WhenAlreadyClosed()
    {
        // Arrange
        var register = Register.Open(RegisterLocation.FarmersMarket, "Carol");
        register.Close();
        register.ClearEvents();

        // Act
        var result = register.Close();

        // Assert
        result.IsFailure.Should().BeTrue();
        register.UncommittedEvents.Should().BeEmpty();
    }
}
