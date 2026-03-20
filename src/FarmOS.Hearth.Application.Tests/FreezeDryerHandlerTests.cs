using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Application.Commands.Handlers;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FarmOS.Hearth.Application.Tests;

public class FreezeDryerHandlerTests
{
    private readonly IHearthEventStore _store = Substitute.For<IHearthEventStore>();
    private readonly FreezeDryerCommandHandlers _sut;

    public FreezeDryerHandlerTests()
    {
        _sut = new FreezeDryerCommandHandlers(_store);
    }

    [Fact]
    public async Task StartBatch_ShouldSaveAndReturnId()
    {
        // Arrange
        var command = new StartFreezeDryerBatchCommand(
            BatchCode: "FD-2026-001",
            DryerId: Guid.NewGuid(),
            ProductDescription: "Strawberries",
            PreDryWeight: 25.5m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _store.Received(1).SaveFreezeDryerAsync(
            Arg.Any<FreezeDryerBatch>(),
            "steward",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordReading_ShouldLoadRecordSave()
    {
        // Arrange — build a batch that has been started and advanced to Freezing
        var batch = FreezeDryerBatch.Start(
            "FD-2026-002",
            new FreezeDryerId(Guid.NewGuid()),
            "Blueberries",
            30.0m);

        batch.AdvancePhase(FreezeDryerPhase.Freezing);
        batch.ClearEvents(); // simulate rehydration from event store

        _store.LoadFreezeDryerAsync(batch.Id.Value.ToString(), Arg.Any<CancellationToken>())
            .Returns(batch);

        var reading = new FreezeDryerReading(
            Timestamp: DateTimeOffset.UtcNow,
            ShelfTempF: -40.0m,
            VacuumMTorr: 200m,
            ProductTempF: -35.0m,
            Notes: "Initial freeze reading");

        var command = new RecordFreezeDryerReadingCommand(
            BatchId: batch.Id.Value,
            Reading: reading);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _store.Received(1).LoadFreezeDryerAsync(
            batch.Id.Value.ToString(),
            Arg.Any<CancellationToken>());

        await _store.Received(1).SaveFreezeDryerAsync(
            Arg.Any<FreezeDryerBatch>(),
            "steward",
            Arg.Any<CancellationToken>());
    }
}
