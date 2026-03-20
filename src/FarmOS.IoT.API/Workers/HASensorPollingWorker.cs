using System.Net.Http.Headers;
using System.Text.Json;
using FarmOS.IoT.Application.Commands;
using FarmOS.IoT.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FarmOS.IoT.API.Workers;

/// <summary>
/// Background worker that polls the Home Assistant REST API every N seconds
/// and dispatches RecordTelemetryReadingCommand for each sensor entity.
/// HA is a dumb sensor gateway — this worker is the bridge to the IoT domain.
/// </summary>
public sealed class HASensorPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HASensorPollingWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _pollInterval;
    private readonly string _haUrl;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    // Maps HA device_class → IoT SensorType
    private static readonly Dictionary<string, SensorType> DeviceClassMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = SensorType.Temperature,
        ["humidity"] = SensorType.Humidity,
        ["moisture"] = SensorType.Moisture,
        ["ph"] = SensorType.Ph,
        ["carbon_dioxide"] = SensorType.Co2,
        ["co2"] = SensorType.Co2,
        ["illuminance"] = SensorType.Light,
        ["weight"] = SensorType.Weight,
    };

    public HASensorPollingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HASensorPollingWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _haUrl = configuration.GetValue<string>("HA_URL") ?? "http://homeassistant:8123";
        var haToken = configuration.GetValue<string>("HA_TOKEN") ?? "";
        var pollSeconds = configuration.GetValue<int>("HA_POLL_INTERVAL_SECONDS");
        _pollInterval = TimeSpan.FromSeconds(pollSeconds > 0 ? pollSeconds : 900);

        _httpClient = new HttpClient { BaseAddress = new Uri(_haUrl) };
        if (!string.IsNullOrWhiteSpace(haToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", haToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "HASensorPollingWorker started. Polling {HaUrl} every {Interval}s",
            _haUrl, _pollInterval.TotalSeconds);

        // Initial delay to let HA and device registrations settle
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollSensorsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "HASensorPollingWorker: error during poll cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task PollSensorsAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("/api/states", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HA returned {StatusCode} from /api/states", response.StatusCode);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var states = JsonSerializer.Deserialize<List<HAState>>(json, JsonOpts);
        if (states is null) return;

        var sensorStates = states
            .Where(s => s.EntityId.StartsWith("sensor.", StringComparison.OrdinalIgnoreCase))
            .Where(s => !string.IsNullOrWhiteSpace(s.State) && s.State != "unavailable" && s.State != "unknown")
            .Where(s => s.Attributes?.DeviceClass is not null && DeviceClassMap.ContainsKey(s.Attributes.DeviceClass))
            .ToList();

        if (sensorStates.Count == 0)
        {
            _logger.LogDebug("HASensorPollingWorker: no relevant sensor states found");
            return;
        }

        _logger.LogInformation("HASensorPollingWorker: processing {Count} sensor readings", sensorStates.Count);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        foreach (var state in sensorStates)
        {
            if (!decimal.TryParse(state.State, out var value)) continue;
            if (!DeviceClassMap.TryGetValue(state.Attributes!.DeviceClass!, out var sensorType)) continue;

            var deviceCode = NormalizeEntityId(state.EntityId);
            var unit = state.Attributes.UnitOfMeasurement ?? "";

            try
            {
                var cmd = new RecordTelemetryReadingCommand(deviceCode, sensorType, value, unit);
                var result = await mediator.Send(cmd, ct);

                if (result.IsFailure)
                {
                    _logger.LogDebug(
                        "Telemetry skipped for {DeviceCode}: {Error}",
                        deviceCode, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ingest telemetry for HA entity {EntityId}", state.EntityId);
            }
        }
    }

    /// <summary>
    /// Converts HA entity IDs like "sensor.freezer_temperature" to device codes like "freezer_temperature".
    /// </summary>
    private static string NormalizeEntityId(string entityId)
    {
        var dotIndex = entityId.IndexOf('.');
        return dotIndex >= 0 ? entityId[(dotIndex + 1)..] : entityId;
    }

    // ─── HA State JSON model ────────────────────────────────────────────────

    private sealed class HAState
    {
        public string EntityId { get; set; } = "";
        public string? State { get; set; }
        public HAAttributes? Attributes { get; set; }
    }

    private sealed class HAAttributes
    {
        public string? DeviceClass { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public string? FriendlyName { get; set; }
    }
}
