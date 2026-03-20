using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using Xunit;

namespace FarmOS.Flora.API.Tests;

public class CropPlanEndpointsTests : IClassFixture<FloraWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FloraWebApplicationFactory _factory;

    public CropPlanEndpointsTests(FloraWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreatePlan_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/flora/plans", new
        {
            seasonYear = 2026,
            seasonName = "Summer",
            planName = "Dahlia Program 2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_AssignBed_ShouldReturn204()
    {
        var plan = CropPlan.Create(2026, "Summer", "Test Plan");
        plan.ClearEvents();

        _factory.MockFloraEventStore
            .LoadCropPlanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(plan));

        var planId = plan.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/plans/{planId}/beds", new
        {
            planId,
            assignment = new
            {
                bedId = new { value = Guid.NewGuid() },
                variety = new { species = "Dahlia", cultivar = "Café au Lait", daysToMaturity = 90 },
                plannedSuccessions = 3
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_RecordRevenue_ShouldReturn204()
    {
        var plan = CropPlan.Create(2026, "Summer", "Revenue Test");
        plan.ClearEvents();

        _factory.MockFloraEventStore
            .LoadCropPlanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(plan));

        var planId = plan.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/plans/{planId}/revenue", new
        {
            planId,
            channel = 0, // FarmersMarket
            amount = 850m,
            date = "2026-07-25",
            notes = "Saturday market"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_RecordCost_ShouldReturn204()
    {
        var plan = CropPlan.Create(2026, "Summer", "Cost Test");
        plan.ClearEvents();

        _factory.MockFloraEventStore
            .LoadCropPlanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(plan));

        var planId = plan.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/plans/{planId}/costs", new
        {
            planId,
            cost = new { category = "seed", amount = 125.50m, notes = "Dahlia tubers from Swan Island" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
