using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using Xunit;

namespace FarmOS.Hearth.API.Tests;

public class HACCPEndpointsTests : IClassFixture<HearthWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HearthWebApplicationFactory _factory;

    public HACCPEndpointsTests(HearthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateHACCPPlan_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/hearth/compliance/haccp/plans", new
        {
            planName = "Hearth Kitchen HACCP",
            facilityName = "Hearth Kitchen"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_AddCCPDefinition_ShouldReturn204()
    {
        var plan = HACCPPlan.Create("Test Plan", "Test Facility");
        plan.ClearEvents();

        _factory.MockHearthEventStore
            .LoadHACCPPlanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(plan));

        var planId = plan.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/hearth/compliance/haccp/plans/{planId}/ccps", new
        {
            planId,
            definition = new
            {
                product = "Sourdough",
                ccpName = "Internal Bake Temperature",
                hazardType = 0,
                criticalLimitExpression = ">= 190°F",
                monitoringProcedure = "Probe thermometer at center of loaf",
                defaultCorrectiveAction = "Return to oven until temp reaches 190°F"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_LogEquipmentTemp_ShouldReturn200()
    {
        var response = await _client.PostAsJsonAsync("/api/hearth/kitchen/temps", new
        {
            equipmentId = Guid.NewGuid(),
            temperatureF = 38.5m,
            loggedBy = "steward"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_OpenCAPA_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/hearth/compliance/capa", new
        {
            description = "Walk-in fridge temp exceeded 41°F for 3 hours",
            deviationSource = "Equipment Temperature Log",
            relatedCTE = (int?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }
}
