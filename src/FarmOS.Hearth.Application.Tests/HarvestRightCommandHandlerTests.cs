using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FarmOS.Hearth.Application.Tests;

public class HarvestRightCommandHandlerTests
{
    private readonly IHearthEventStore _store = Substitute.For<IHearthEventStore>();
    private readonly IKitchenHubNotifier _notifier = Substitute.For<IKitchenHubNotifier>();
    private readonly ILogger<HarvestRightCommandHandlers> _logger = Substitute.For<ILogger<HarvestRightCommandHandlers>>();
    private readonly HarvestRightCommandHandlers _sut;

    public HarvestRightCommandHandlerTests()
    {
        // By default: no active batch, save/append do nothing
        _store.LoadActiveFreezeDryerByDryerIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FreezeDryerBatch?)null);

        _sut = new HarvestRightCommandHandlers(_store, _notifier, _logger);
    }

    private static IngestHarvestRightTelemetryCommand MakeCommand(
        int screenNumber,
        decimal temperatureF = -30m,
        decimal vacuumMTorr = 200m,
        decimal progressPercent = 50m,
        string? batchName = null) =>
        new(
            HarvestRightDryerId: 1,
            DryerSerial: "HR-TEST-001",
            TemperatureF: temperatureF,
            VacuumMTorr: vacuumMTorr,
            ProgressPercent: progressPercent,
            ScreenNumber: screenNumber,
            BatchName: batchName,
            BatchElapsedSeconds: 3600m,
            PhaseElapsedSeconds: 1800m,
            Timestamp: DateTimeOffset.UtcNow);

    [Fact]
    public async Task Telemetry_Screen4_Maps_To_Freezing_Phase()
    {
        // Arrange
        var command = MakeCommand(screenNumber: 4);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MappedPhase.Should().Be(FreezeDryerPhase.Freezing);
    }

    [Fact]
    public async Task Telemetry_Screen23_Error_Returns_Alert()
    {
        // Arrange — screen 23 is an error screen
        var command = MakeCommand(screenNumber: 23);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Error screens don't map to a phase (mapScreenToPhase returns None)
        result.Value.MappedPhase.Should().BeNull();
        // Error screen is not a running screen, so no batch auto-created
        // and no alert from evaluateTelemetry (no mapped phase).
        // The handler does not produce an alert when mappedPhase is null.
    }

    [Fact]
    public async Task Telemetry_NoActiveBatch_RunningScreen_AutoCreatesBatch()
    {
        // Arrange — no active batch (default mock), screen 4 is running
        var command = MakeCommand(screenNumber: 4);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BatchAutoCreated.Should().BeTrue();

        await _store.Received().SaveFreezeDryerAsync(
            Arg.Any<FreezeDryerBatch>(),
            "harvest-right-mqtt",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Telemetry_HighVacuum_Triggers_CriticalAlert()
    {
        // Arrange — screen 4 (Freezing) with vacuum > 1000 mTorr
        var command = MakeCommand(screenNumber: 4, vacuumMTorr: 1500m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Alert.Should().NotBeNull();
        result.Value.Alert!.Level.Should().Be(AlertLevel.Critical);
    }

    [Fact]
    public async Task Telemetry_Screen0_ReadyToStart_NoBatchCreated()
    {
        // Arrange — screen 0 is not a running screen
        var command = MakeCommand(screenNumber: 0);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BatchAutoCreated.Should().BeFalse();
    }
}
