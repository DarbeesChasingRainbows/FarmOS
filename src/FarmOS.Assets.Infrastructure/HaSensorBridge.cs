using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmOS.Assets.Infrastructure;

/// <summary>
/// Proxies the Home Assistant REST API to surface sensor data.
/// Reads HA_URL and HA_TOKEN from environment variables.
/// </summary>
public sealed class HaSensorBridge
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _token;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public HaSensorBridge(HttpClient http)
    {
        _http = http;
        _baseUrl = (Environment.GetEnvironmentVariable("HA_URL") ?? "http://homeassistant:8123").TrimEnd('/');
        _token = Environment.GetEnvironmentVariable("HA_TOKEN") ?? "";
    }

    // ─── DTOs ────────────────────────────────────────────────────────────

    public record HaSensorSummary(
        string EntityId,
        string State,
        string? FriendlyName,
        string? UnitOfMeasurement,
        string? DeviceClass,
        string? Icon,
        string LastChanged,
        string LastUpdated);

    public record HaSensorDetail(
        string EntityId,
        string State,
        Dictionary<string, JsonElement> Attributes,
        string LastChanged,
        string LastUpdated);

    public record HaHistoryEntry(
        string State,
        string LastChanged);

    // ─── Internal HA response shape ─────────────────────────────────────

    private record HaStateResponse(
        [property: JsonPropertyName("entity_id")] string EntityId,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("attributes")] Dictionary<string, JsonElement> Attributes,
        [property: JsonPropertyName("last_changed")] string LastChanged,
        [property: JsonPropertyName("last_updated")] string LastUpdated);

    // ─── Public methods ─────────────────────────────────────────────────

    /// <summary>Returns all sensor.* entities from Home Assistant.</summary>
    public async Task<IReadOnlyList<HaSensorSummary>> GetAllSensorsAsync(CancellationToken ct)
    {
        var states = await GetAsync<List<HaStateResponse>>("/api/states", ct);
        if (states is null) return [];

        return states
            .Where(s => s.EntityId.StartsWith("sensor."))
            .Select(s => new HaSensorSummary(
                s.EntityId,
                s.State,
                GetAttr(s.Attributes, "friendly_name"),
                GetAttr(s.Attributes, "unit_of_measurement"),
                GetAttr(s.Attributes, "device_class"),
                GetAttr(s.Attributes, "icon"),
                s.LastChanged,
                s.LastUpdated))
            .ToList();
    }

    /// <summary>Returns full detail for a single HA entity.</summary>
    public async Task<HaSensorDetail?> GetSensorDetailAsync(string entityId, CancellationToken ct)
    {
        var s = await GetAsync<HaStateResponse>($"/api/states/{entityId}", ct);
        if (s is null) return null;
        return new HaSensorDetail(s.EntityId, s.State, s.Attributes, s.LastChanged, s.LastUpdated);
    }

    /// <summary>Returns time-series history for an entity over the last N hours.</summary>
    public async Task<IReadOnlyList<HaHistoryEntry>> GetSensorHistoryAsync(string entityId, int hours, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-hours).ToString("o");
        var path = $"/api/history/period/{since}?filter_entity_id={entityId}&minimal_response&no_attributes";
        var outer = await GetAsync<List<List<HaStateResponse>>>(path, ct);

        if (outer is null || outer.Count == 0) return [];

        return outer[0]
            .Select(s => new HaHistoryEntry(s.State, s.LastChanged))
            .ToList();
    }

    /// <summary>Checks whether the HA instance is reachable.</summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var resp = await SendAsync(HttpMethod.Get, "/api/", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ─── Private helpers ────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        var resp = await SendAsync(HttpMethod.Get, path, ct);
        if (!resp.IsSuccessStatusCode) return default;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        if (!string.IsNullOrEmpty(_token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await _http.SendAsync(request, ct);
    }

    private static string? GetAttr(Dictionary<string, JsonElement> attrs, string key) =>
        attrs.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
