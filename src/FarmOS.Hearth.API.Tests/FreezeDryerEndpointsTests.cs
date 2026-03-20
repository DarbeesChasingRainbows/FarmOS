using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using Xunit;

namespace FarmOS.Hearth.API.Tests;

public class FreezeDryerEndpointsTests : IClassFixture<HearthWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HearthWebApplicationFactory _factory;

    public FreezeDryerEndpointsTests(HearthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_StartBatch_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/hearth/freeze-dryer", new
        {
            batchCode = "FD-TEST-001",
            dryerId = Guid.NewGuid(),
            productDescription = "Beef jerky strips",
            preDryWeight = 5.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_RecordReading_OnNewBatch_ShouldReturn400()
    {
        // A new batch is in Loading phase — recording readings should fail
        var batch = FreezeDryerBatch.Start("FD-TEST-002", FreezeDryerId.New(), "Test product", 3.0m);
        batch.ClearEvents();

        _factory.MockHearthEventStore
            .LoadFreezeDryerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(batch));

        var batchId = batch.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/hearth/freeze-dryer/{batchId}/readings", new
        {
            batchId,
            reading = new
            {
                timestamp = DateTimeOffset.UtcNow,
                shelfTempF = -25.0m,
                vacuumMTorr = 300m,
                productTempF = (decimal?)null,
                notes = (string?)null
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_AdvancePhase_ShouldReturn204()
    {
        var batch = FreezeDryerBatch.Start("FD-TEST-003", FreezeDryerId.New(), "Mushroom slices", 2.0m);
        batch.ClearEvents();

        _factory.MockHearthEventStore
            .LoadFreezeDryerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(batch));

        var batchId = batch.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/hearth/freeze-dryer/{batchId}/advance", new
        {
            batchId,
            nextPhase = 1 // Freezing
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
