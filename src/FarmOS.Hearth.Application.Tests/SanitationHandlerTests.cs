using System;
using System.Threading;
using System.Threading.Tasks;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Application.Commands.Handlers;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FarmOS.Hearth.Application.Tests;

public class SanitationHandlerTests
{
    private readonly IHearthEventStore _eventStoreMock;
    private readonly RecordSanitationHandler _handler;

    public SanitationHandlerTests()
    {
        _eventStoreMock = Substitute.For<IHearthEventStore>();
        _handler = new RecordSanitationHandler(_eventStoreMock);
    }

    [Fact]
    public async Task Handle_ShouldSaveSanitationRecord_AndReturnId()
    {
        // Arrange
        var command = new RecordSanitationCommand(
            SanitationSurfaceType.PrepTable,
            "Main Kitchen",
            "Spray and Wipe",
            SanitizerType.Quat,
            200m,
            "John Doe"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _eventStoreMock.Received(1).SaveSanitationAsync(
            Arg.Is<SanitationRecord>(r =>
                r.SurfaceType == SanitationSurfaceType.PrepTable &&
                r.CleanedBy == "John Doe" &&
                r.Sanitizer == SanitizerType.Quat &&
                r.SanitizerPpm == 200m
            ),
            Arg.Is("steward"),
            Arg.Any<CancellationToken>()
        );
    }
}
