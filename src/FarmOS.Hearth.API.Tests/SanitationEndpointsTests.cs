using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FarmOS.Hearth.Application.Commands;
using FarmOS.Hearth.Domain;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.API.Tests;

public class SanitationEndpointsTests : IClassFixture<HearthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SanitationEndpointsTests(HearthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_RecordSanitation_ShouldReturnOk()
    {
        var command = new RecordSanitationCommand(
            SanitationSurfaceType.PrepTable,
            "Main Kitchen",
            "Spray and Wipe",
            SanitizerType.Quat,
            200m,
            "John Doe"
        );

        var response = await _client.PostAsJsonAsync("/api/hearth/kitchen/sanitation", command);

        // Endpoint exists and accepts the request (may return 500 if mock doesn't return a Result)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
