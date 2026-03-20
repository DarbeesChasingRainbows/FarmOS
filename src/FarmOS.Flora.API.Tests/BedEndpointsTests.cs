using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using Xunit;

namespace FarmOS.Flora.API.Tests;

public class BedEndpointsTests : IClassFixture<FloraWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FloraWebApplicationFactory _factory;

    public BedEndpointsTests(FloraWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateBed_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/flora/beds", new
        {
            name = "Bed A-1",
            block = "Block A",
            dimensions = new { lengthFeet = 100m, widthFeet = 4m }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_PlanSuccession_ShouldReturnSuccessionId()
    {
        var bed = FlowerBed.Create("Bed B-1", "Block B", new BedDimensions(80, 4));
        bed.ClearEvents();

        _factory.MockFloraEventStore
            .LoadBedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(bed));

        var bedId = bed.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/beds/{bedId}/successions", new
        {
            bedId,
            variety = new { species = "Zinnia", cultivar = "Benary Giant Lime", daysToMaturity = 75, color = "Lime" },
            sowDate = "2026-03-01",
            transplantDate = "2026-04-01",
            harvestWindowStart = "2026-06-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_RecordHarvest_ShouldReturn204()
    {
        var bed = FlowerBed.Create("Bed C-1", "Block C", new BedDimensions(100, 4));
        var succId = bed.PlanSuccession(
            new CropVariety("Sunflower", "ProCut Orange", 60),
            new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 1));
        bed.ClearEvents();

        _factory.MockFloraEventStore
            .LoadBedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(bed));

        var bedId = bed.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/beds/{bedId}/successions/{succId.Value}/harvest", new
        {
            bedId,
            successionId = succId.Value,
            stems = new { value = 120m, unit = "stems", measure = "count" },
            date = "2026-07-10"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
