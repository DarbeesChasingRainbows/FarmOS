using System;
using System.Threading;
using System.Threading.Tasks;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.API.Workers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FarmOS.Hearth.API.Tests;

public class TraceabilityRecordPurgeWorkerTests
{
    private readonly IHearthEventStore _eventStore;
    private readonly TraceabilityRecordPurgeWorker _worker;

    public TraceabilityRecordPurgeWorkerTests()
    {
        _eventStore = Substitute.For<IHearthEventStore>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => _eventStore);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _worker = new TraceabilityRecordPurgeWorker(
            scopeFactory,
            NullLogger<TraceabilityRecordPurgeWorker>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallPurge_WithCorrectCutoff()
    {
        _eventStore.PurgeExpiredTraceabilityAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(5));

        // Use a CancellationTokenSource that cancels after the first cycle
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        try
        {
            await _worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _worker.StopAsync(CancellationToken.None);
        }

        // Verify the event store was called with a cutoff roughly 730 days in the past
        await _eventStore.Received().PurgeExpiredTraceabilityAsync(
            Arg.Is<DateTimeOffset>(cutoff =>
                cutoff < DateTimeOffset.UtcNow.AddDays(-729) &&
                cutoff > DateTimeOffset.UtcNow.AddDays(-731)),
            Arg.Any<CancellationToken>());
    }
}
