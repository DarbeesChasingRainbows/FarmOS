using System;
using System.Threading;
using System.Threading.Tasks;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.Hearth.Application.Queries;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FarmOS.Hearth.Application.Tests;

public class TraceabilityHandlerTests
{
    private readonly IHearthEventStore _eventStore;

    public TraceabilityHandlerTests()
    {
        _eventStore = Substitute.For<IHearthEventStore>();
    }

    [Fact]
    public async Task LogReceivingHandler_ShouldSaveRecord_AndReturnId()
    {
        var handler = new LogReceivingEventHandler(_eventStore);
        var command = new LogReceivingEventCommand(
            ProductCategory.Wheat, "Heritage Red Fife", "WHT-20260316-01",
            new Quantity(100m, "lbs", "weight"), "Local Mill Co.", DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _eventStore.Received(1).SaveTraceabilityAsync(
            Arg.Is<TraceabilityRecord>(r =>
                r.EventType == CriticalTrackingEvent.Receiving &&
                r.Category == ProductCategory.Wheat &&
                r.LotId == "WHT-20260316-01"),
            Arg.Is("system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogTransformationHandler_ShouldSaveRecord_WithSourceLot()
    {
        var handler = new LogTransformationEventHandler(_eventStore);
        var command = new LogTransformationEventCommand(
            ProductCategory.Sourdough, "Starter Batch", "SOUR-20260316-01",
            new Quantity(5m, "lbs", "weight"), "WHT-20260316-01", DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _eventStore.Received(1).SaveTraceabilityAsync(
            Arg.Is<TraceabilityRecord>(r =>
                r.EventType == CriticalTrackingEvent.Transformation &&
                r.SourceLotId == "WHT-20260316-01" &&
                r.LotId == "SOUR-20260316-01"),
            Arg.Is("system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogShippingHandler_ShouldSaveRecord_WithDestination()
    {
        var handler = new LogShippingEventHandler(_eventStore);
        var command = new LogShippingEventCommand(
            ProductCategory.Sourdough, "Baked Loaves", "SOUR-SHP-001",
            new Quantity(10m, "loaves", "count"), "B2C EdgePortal", DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _eventStore.Received(1).SaveTraceabilityAsync(
            Arg.Is<TraceabilityRecord>(r =>
                r.EventType == CriticalTrackingEvent.Shipping &&
                r.DestinationLocation == "B2C EdgePortal"),
            Arg.Is("system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditReportQuery_ShouldReturnDtos()
    {
        var handler = new Get24HourAuditReportQueryHandler();
        var query = new Get24HourAuditReportQuery(DateTimeOffset.UtcNow);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result!.Should().AllSatisfy(dto =>
        {
            dto.EventType.Should().NotBeNullOrWhiteSpace();
            dto.Category.Should().NotBeNullOrWhiteSpace();
            dto.LotId.Should().NotBeNullOrWhiteSpace();
        });
    }
}
