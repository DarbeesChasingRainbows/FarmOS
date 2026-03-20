using System;
using System.Collections.Generic;
using System.Linq;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class SourdoughCCPEnforcementTests
{
    private static SourdoughBatch CreateBatch()
    {
        var ingredients = new List<Ingredient>
        {
            new("Bread Flour", new Quantity(1000m, "g", "weight"), "LOT-2026-001", "Central Milling"),
            new("Water", new Quantity(700m, "g", "weight"), null, null),
            new("Salt", new Quantity(20m, "g", "weight"), "SALT-100", "Redmond")
        };

        return SourdoughBatch.Start("SD-001", new LivingCultureId(Guid.NewGuid()), ingredients);
    }

    private static HACCPReading WithinLimitsReading() =>
        new(DateTimeOffset.UtcNow, "Baking Temperature", 210m, null, WithinLimits: true, CorrectiveAction: null);

    private static HACCPReading OutOfLimitsWithAction() =>
        new(DateTimeOffset.UtcNow, "Baking Temperature", 180m, null, WithinLimits: false, CorrectiveAction: "Extended bake time by 10 minutes");

    private static HACCPReading OutOfLimitsWithoutAction() =>
        new(DateTimeOffset.UtcNow, "Baking Temperature", 180m, null, WithinLimits: false, CorrectiveAction: null);

    private static HACCPReading OutOfLimitsWithEmptyAction() =>
        new(DateTimeOffset.UtcNow, "Baking Temperature", 180m, null, WithinLimits: false, CorrectiveAction: "   ");

    [Fact]
    public void RecordCCP_WithinLimits_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();

        // Act
        var result = batch.RecordCCP(WithinLimitsReading());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecordCCP_OutOfLimits_WithCorrectiveAction_ShouldSucceed()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();

        // Act
        var result = batch.RecordCCP(OutOfLimitsWithAction());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecordCCP_OutOfLimits_WithoutCorrectiveAction_ShouldFail()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();

        // Act
        var result = batch.RecordCCP(OutOfLimitsWithoutAction());

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordCCP_OutOfLimits_WithEmptyCorrectiveAction_ShouldFail()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();

        // Act
        var result = batch.RecordCCP(OutOfLimitsWithEmptyAction());

        // Assert
        result.IsFailure.Should().BeTrue();
        batch.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordCCP_ShouldRaiseCCPReadingRecordedEvent()
    {
        // Arrange
        var batch = CreateBatch();
        batch.ClearEvents();
        var reading = WithinLimitsReading();

        // Act
        var result = batch.RecordCCP(reading);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.UncommittedEvents.Should().ContainSingle(e => e is CCPReadingRecorded);
        var @event = batch.UncommittedEvents.OfType<CCPReadingRecorded>().Single();
        @event.Reading.Should().Be(reading);
    }
}
