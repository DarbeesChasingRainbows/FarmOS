using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using Xunit;

namespace FarmOS.Flora.API.Tests;

public class PostHarvestEndpointsTests : IClassFixture<FloraWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FloraWebApplicationFactory _factory;

    public PostHarvestEndpointsTests(FloraWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateBatch_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/flora/batches", new
        {
            sourceBed = Guid.NewGuid(),
            successionId = Guid.NewGuid(),
            species = "Dahlia",
            cultivar = "Café au Lait",
            totalStems = 200,
            harvestDate = "2026-07-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_GradeStems_ShouldReturn204()
    {
        var batch = PostHarvestBatch.Create(
            FlowerBedId.New(), SuccessionId.New(),
            "Dahlia", "Café au Lait", 200, new DateOnly(2026, 7, 15));
        batch.ClearEvents();

        _factory.MockFloraEventStore
            .LoadPostHarvestBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(batch));

        var batchId = batch.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/batches/{batchId}/grade", new
        {
            batchId,
            grade = new { grade = 0, stemCount = 80, stemLengthInches = 24m } // Premium
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_UseStems_Insufficient_ShouldReturn400()
    {
        var batch = PostHarvestBatch.Create(
            FlowerBedId.New(), SuccessionId.New(),
            "Zinnia", "Queen Red Lime", 50, new DateOnly(2026, 7, 20));
        batch.ClearEvents();

        _factory.MockFloraEventStore
            .LoadPostHarvestBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(batch));

        var batchId = batch.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/batches/{batchId}/use-stems", new
        {
            batchId,
            stemsUsed = 100, // more than the 50 available
            purpose = "Market bouquets"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MoveToCooler_ShouldReturn204()
    {
        var batch = PostHarvestBatch.Create(
            FlowerBedId.New(), SuccessionId.New(),
            "Lisianthus", "ABC Purple", 120, new DateOnly(2026, 7, 18));
        batch.ClearEvents();

        _factory.MockFloraEventStore
            .LoadPostHarvestBatchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(batch));

        var batchId = batch.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/batches/{batchId}/cooler", new
        {
            batchId,
            temperatureF = 34m,
            slotLabel = "Cooler-1/Shelf-A3"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
