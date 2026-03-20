using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.API.Tests;

public class TraceabilityEndpointsTests : IClassFixture<HearthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TraceabilityEndpointsTests(HearthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_LogReceiving_ShouldReturnOk()
    {
        var command = new LogReceivingEventCommand(
            ProductCategory.Wheat, "Heritage Red Fife", "WHT-20260316-01",
            new Quantity(100m, "lbs", "weight"), "Local Mill Co.", System.DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/hearth/compliance/traceability/receiving", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_LogTransformation_ShouldReturnOk()
    {
        var command = new LogTransformationEventCommand(
            ProductCategory.Sourdough, "Starter Batch", "SOUR-20260316-01",
            new Quantity(5m, "lbs", "weight"), "WHT-20260316-01", System.DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/hearth/compliance/traceability/transformation", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_LogShipping_ShouldReturnOk()
    {
        var command = new LogShippingEventCommand(
            ProductCategory.Sourdough, "Baked Loaves", "SOUR-SHP-001",
            new Quantity(10m, "loaves", "count"), "B2C EdgePortal", System.DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/hearth/compliance/traceability/shipping", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AuditReport_ShouldReturnCsv_WithFSMA204Headers()
    {
        var response = await _client.GetAsync("/api/hearth/compliance/traceability/audit-report");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();

        // Verify FSMA 204 Key Data Element column headers
        csv.Should().Contain("EventType");
        csv.Should().Contain("Category");
        csv.Should().Contain("ProductDescription");
        csv.Should().Contain("LotId");
        csv.Should().Contain("Amount");
        csv.Should().Contain("Unit");
        csv.Should().Contain("SourceLocation");
        csv.Should().Contain("DestinationLocation");
        csv.Should().Contain("SourceLotId");
        csv.Should().Contain("RecordedAt");
    }
}
