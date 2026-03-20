using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmOS.Hearth.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FarmOS.Hearth.Infrastructure.HarvestRight;

/// <summary>
/// REST client for the Harvest Right cloud API (prod.harvestrightapp.com).
/// Handles authentication, token refresh, and device discovery.
/// </summary>
public sealed class HarvestRightAuthClient(
    HttpClient http,
    IOptions<HarvestRightOptions> options,
    ILogger<HarvestRightAuthClient> logger) : IHarvestRightAuthClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HarvestRightOptions _opts = options.Value;

    public async Task<HarvestRightSession> LoginAsync(CancellationToken ct)
    {
        logger.LogInformation("Authenticating with Harvest Right cloud API");

        var payload = new { username = _opts.Email, password = _opts.Password, rememberme = true };
        var response = await http.PostAsJsonAsync($"{_opts.ApiBase}/auth/v1", payload, JsonOpts, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Harvest Right login failed: {Status} {Body}", response.StatusCode, body);
            throw new HarvestRightAuthException($"Login failed with status {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts, ct)
            ?? throw new HarvestRightAuthException("Null response from auth endpoint");

        logger.LogInformation("Harvest Right login succeeded for customer {CustomerId}", result.CustomerId);

        return new HarvestRightSession(
            AccessToken: result.AccessToken,
            RefreshToken: result.RefreshToken,
            RefreshAfter: DateTimeOffset.FromUnixTimeSeconds(result.RefreshAfter),
            CustomerId: result.CustomerId,
            UserId: result.UserId);
    }

    public async Task<HarvestRightSession> RefreshTokenAsync(HarvestRightSession current, CancellationToken ct)
    {
        logger.LogDebug("Refreshing Harvest Right access token");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_opts.ApiBase}/auth/v1/refresh-token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.RefreshToken);

            var response = await http.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Refresh token rejected (401), falling back to full re-login");
                return await LoginAsync(ct);
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts, ct)
                ?? throw new HarvestRightAuthException("Null response from refresh endpoint");

            return new HarvestRightSession(
                AccessToken: result.AccessToken,
                RefreshToken: result.RefreshToken,
                RefreshAfter: DateTimeOffset.FromUnixTimeSeconds(result.RefreshAfter),
                CustomerId: result.CustomerId,
                UserId: result.UserId);
        }
        catch (Exception ex) when (ex is not HarvestRightAuthException)
        {
            logger.LogWarning(ex, "Token refresh failed, falling back to full re-login");
            return await LoginAsync(ct);
        }
    }

    public async Task<HarvestRightDryer[]> GetFreezeDryersAsync(HarvestRightSession session, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_opts.ApiBase}/freeze-dryer/v1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var dryers = await response.Content.ReadFromJsonAsync<DryerResponse[]>(JsonOpts, ct)
            ?? Array.Empty<DryerResponse>();

        return dryers.Select(d => new HarvestRightDryer(
            DryerId: d.Id,
            Serial: d.Serial ?? string.Empty,
            Name: d.Name ?? $"Dryer-{d.Id}",
            Model: d.Model ?? "Unknown",
            FirmwareVersion: d.FirmwareVersion ?? "Unknown"
        )).ToArray();
    }

    // ─── Internal DTO types ─────────────────────────────────────────────────

    private sealed record AuthResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("refreshToken")] string RefreshToken,
        [property: JsonPropertyName("refreshAfter")] long RefreshAfter,
        [property: JsonPropertyName("customerId")] string CustomerId,
        [property: JsonPropertyName("userId")] string UserId);

    private sealed record DryerResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("serial")] string? Serial,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("firmwareVersion")] string? FirmwareVersion);
}

/// <summary>Thrown when Harvest Right authentication fails.</summary>
public sealed class HarvestRightAuthException(string message) : Exception(message);
