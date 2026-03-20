using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FarmOS.Hearth.Application.Tests;

public class HACCPCommandHandlerTests
{
    private readonly IHearthEventStore _store = Substitute.For<IHearthEventStore>();
    private readonly IKitchenHubNotifier _notifier = Substitute.For<IKitchenHubNotifier>();

    [Fact]
    public async Task CreateHACCPPlan_ShouldSaveAndReturnId()
    {
        // Arrange
        var handler = new HACCPPlanCommandHandlers(_store);
        var command = new CreateHACCPPlanCommand(
            PlanName: "Freeze-Dried Fruit HACCP Plan",
            FacilityName: "Building A Kitchen");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _store.Received(1).SaveHACCPPlanAsync(
            Arg.Any<HACCPPlan>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenCAPA_ShouldAppendEventAndReturnId()
    {
        // Arrange
        var handler = new CAPACommandHandlers(_store);
        var command = new OpenCAPACommand(
            Description: "Temperature deviation during primary drying",
            DeviationSource: "FreezeDryer-02",
            RelatedCTE: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _store.Received(1).AppendCAPAEventAsync(
            Arg.Any<IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogEquipmentTemperature_ShouldPersistAndBroadcast()
    {
        // Arrange
        var handler = new EquipmentMonitoringCommandHandlers(_store, _notifier);
        var command = new LogEquipmentTemperatureCommand(
            EquipmentId: Guid.NewGuid(),
            TemperatureF: 38.5m,
            LoggedBy: "sensor-agent");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _store.Received(1).AppendEquipmentTempEventAsync(
            Arg.Any<EquipmentTemperatureLogged>(),
            Arg.Any<CancellationToken>());

        await _notifier.Received(1).BroadcastAsync(
            Arg.Any<SensorReading>(),
            Arg.Any<IoTAlert>(),
            Arg.Any<CancellationToken>());
    }
}
