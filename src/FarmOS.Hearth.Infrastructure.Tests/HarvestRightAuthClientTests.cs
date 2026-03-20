using System.Net;
using System.Text.Json;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Infrastructure.HarvestRight;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FarmOS.Hearth.Infrastructure.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => _handler(request);
}

public class HarvestRightAuthClientTests
{
    private readonly IOptions<HarvestRightOptions> _options;
    private readonly ILogger<HarvestRightAuthClient> _logger;

    public HarvestRightAuthClientTests()
    {
        var opts = new HarvestRightOptions
        {
            Email = "test@example.com",
            Password = "password123",
            ApiBase = "https://prod.harvestrightapp.com"
        };
        _options = Options.Create(opts);
        _logger = Substitute.For<ILogger<HarvestRightAuthClient>>();
    }

    private HarvestRightAuthClient CreateClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var httpClient = new HttpClient(mockHandler);
        return new HarvestRightAuthClient(httpClient, _options, _logger);
    }

    [Fact]
    public async Task LoginAsync_ReturnsSession_OnSuccess()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            accessToken = "tok",
            refreshToken = "rtk",
            refreshAfter = 1234567890L,
            customerId = "cust1",
            userId = "usr1"
        });

        var client = CreateClient(req =>
        {
            req.RequestUri!.AbsolutePath.Should().Be("/auth/v1");

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        // Act
        var session = await client.LoginAsync(CancellationToken.None);

        // Assert
        session.AccessToken.Should().Be("tok");
        session.RefreshToken.Should().Be("rtk");
        session.RefreshAfter.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1234567890));
        session.CustomerId.Should().Be("cust1");
        session.UserId.Should().Be("usr1");
    }

    [Fact]
    public async Task RefreshTokenAsync_FallsBackToLogin_On401()
    {
        // Arrange
        var loginResponseJson = JsonSerializer.Serialize(new
        {
            accessToken = "new-tok",
            refreshToken = "new-rtk",
            refreshAfter = 9999999999L,
            customerId = "cust1",
            userId = "usr1"
        });

        var callCount = 0;
        var client = CreateClient(req =>
        {
            callCount++;
            if (req.RequestUri!.AbsolutePath.Contains("refresh-token"))
            {
                // Return 401 for refresh attempt
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            // Return 200 for login fallback
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(loginResponseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var currentSession = new HarvestRightSession(
            AccessToken: "old-tok",
            RefreshToken: "old-rtk",
            RefreshAfter: DateTimeOffset.UtcNow.AddHours(1),
            CustomerId: "cust1",
            UserId: "usr1");

        // Act
        var session = await client.RefreshTokenAsync(currentSession, CancellationToken.None);

        // Assert
        callCount.Should().Be(2, "one refresh attempt + one login fallback");
        session.AccessToken.Should().Be("new-tok");
        session.RefreshToken.Should().Be("new-rtk");
    }

    [Fact]
    public async Task GetFreezeDryersAsync_ParsesDryerList()
    {
        // Arrange
        var dryersJson = JsonSerializer.Serialize(new[]
        {
            new { id = 1, serial = "HR-001", name = "Kitchen Dryer", model = "Medium", firmwareVersion = "5.2.1" },
            new { id = 2, serial = "HR-002", name = "Lab Dryer", model = "Large", firmwareVersion = "5.3.0" }
        });

        var client = CreateClient(req =>
        {
            req.RequestUri!.AbsolutePath.Should().Be("/freeze-dryer/v1");
            req.Headers.Authorization!.Scheme.Should().Be("Bearer");
            req.Headers.Authorization!.Parameter.Should().Be("session-token");

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(dryersJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var session = new HarvestRightSession(
            AccessToken: "session-token",
            RefreshToken: "rtk",
            RefreshAfter: DateTimeOffset.UtcNow.AddHours(1),
            CustomerId: "cust1",
            UserId: "usr1");

        // Act
        var dryers = await client.GetFreezeDryersAsync(session, CancellationToken.None);

        // Assert
        dryers.Should().HaveCount(2);

        dryers[0].DryerId.Should().Be(1);
        dryers[0].Serial.Should().Be("HR-001");
        dryers[0].Name.Should().Be("Kitchen Dryer");
        dryers[0].Model.Should().Be("Medium");
        dryers[0].FirmwareVersion.Should().Be("5.2.1");

        dryers[1].DryerId.Should().Be(2);
        dryers[1].Serial.Should().Be("HR-002");
        dryers[1].Name.Should().Be("Lab Dryer");
        dryers[1].Model.Should().Be("Large");
        dryers[1].FirmwareVersion.Should().Be("5.3.0");
    }
}
