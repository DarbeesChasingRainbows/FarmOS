using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using FarmOS.Hearth.Application;
using FarmOS.Hearth.Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace FarmOS.Hearth.Infrastructure.HarvestRight;

/// <summary>
/// Background service that connects to the Harvest Right MQTT cloud broker,
/// subscribes to telemetry topics for each registered dryer, and dispatches
/// <see cref="IngestHarvestRightTelemetryCommand"/> through MediatR.
///
/// Key behaviors:
/// - Publishes "on" keepalive every 30s (dryers only send telemetry while a client is listening)
/// - Auto-refreshes auth token before expiry
/// - Reconnects on silence (no messages for 15 min)
/// </summary>
public sealed class HarvestRightMqttWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHarvestRightAuthClient _authClient;
    private readonly HarvestRightOptions _opts;
    private readonly ILogger<HarvestRightMqttWorker> _logger;

    private IMqttClient? _mqttClient;
    private HarvestRightSession? _session;
    private HarvestRightDryer[] _dryers = [];
    private DateTimeOffset _lastMessageTime = DateTimeOffset.UtcNow;

    // Public status properties for the /harvest-right/status endpoint
    public bool IsConnected => _mqttClient?.IsConnected ?? false;
    public IReadOnlyList<HarvestRightDryer> RegisteredDryers => _dryers;
    public DateTimeOffset? LastTelemetryAt => _lastMessageTime;

    public HarvestRightMqttWorker(
        IServiceScopeFactory scopeFactory,
        IHarvestRightAuthClient authClient,
        IOptions<HarvestRightOptions> options,
        ILogger<HarvestRightMqttWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _authClient = authClient;
        _opts = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Skip if no credentials configured
        if (string.IsNullOrWhiteSpace(_opts.Email) || string.IsNullOrWhiteSpace(_opts.Password))
        {
            _logger.LogWarning(
                "Harvest Right MQTT worker disabled — no credentials configured. " +
                "Set HarvestRight:Email and HarvestRight:Password in appsettings.json");
            return;
        }

        _logger.LogInformation("Harvest Right MQTT worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndRunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Harvest Right MQTT worker shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Harvest Right MQTT worker error — reconnecting in 60s");
                await SafeDisconnectAsync();
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        await SafeDisconnectAsync();
    }

    private async Task ConnectAndRunAsync(CancellationToken ct)
    {
        // ── Authenticate ─────────────────────────────────────────────────
        _session = await _authClient.LoginAsync(ct);
        _dryers = await _authClient.GetFreezeDryersAsync(_session, ct);

        _logger.LogInformation(
            "Authenticated as customer {CustomerId}. Found {Count} dryer(s): {Dryers}",
            _session.CustomerId, _dryers.Length,
            string.Join(", ", _dryers.Select(d => $"{d.Name} ({d.Serial})")));

        if (_dryers.Length == 0)
        {
            _logger.LogWarning("No dryers registered — waiting 5 min before retry");
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            return;
        }

        // ── Connect MQTT ─────────────────────────────────────────────────
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var clientId = $"{_session.CustomerId}-farmos.{Guid.NewGuid().ToString("N")[..6]}";

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_opts.MqttBroker, _opts.MqttPort)
            .WithTlsOptions(o => o.WithSslProtocols(SslProtocols.Tls12))
            .WithCredentials(_opts.Email, _session.AccessToken)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(clientId)
            .WithSessionExpiryInterval(60)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(20))
            .Build();

        // Setup message handler before connecting
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await _mqttClient.ConnectAsync(mqttOptions, ct);
        _logger.LogInformation("Connected to Harvest Right MQTT broker as {ClientId}", clientId);

        // ── Subscribe to dryer topics ────────────────────────────────────
        var subscribeBuilder = factory.CreateSubscribeOptionsBuilder();
        foreach (var dryer in _dryers)
        {
            var telemetryTopic = $"act/{_session.CustomerId}/ed/{dryer.DryerId}/m/telemetry";
            var systemTopic = $"act/{_session.CustomerId}/ed/{dryer.DryerId}/m/system";

            subscribeBuilder
                .WithTopicFilter(t => t.WithTopic(telemetryTopic).WithAtMostOnceQoS())
                .WithTopicFilter(t => t.WithTopic(systemTopic).WithAtMostOnceQoS());

            _logger.LogDebug("Subscribing to {Topic}", telemetryTopic);
        }

        await _mqttClient.SubscribeAsync(subscribeBuilder.Build(), ct);
        _lastMessageTime = DateTimeOffset.UtcNow;

        // ── Run watchdog + keepalive + token refresh loops ───────────────
        var keepaliveTopic = $"act/{_session.CustomerId}/on";
        var watchdogInterval = TimeSpan.FromSeconds(_opts.WatchdogIntervalSeconds);
        var silenceThreshold = TimeSpan.FromSeconds(_opts.SilenceThresholdSeconds);

        while (!ct.IsCancellationRequested && _mqttClient.IsConnected)
        {
            // Publish keepalive "on" so dryers know we're listening
            await PublishAsync(keepaliveTopic, "on", ct);

            // Check token refresh
            if (_session is not null && DateTimeOffset.UtcNow >= _session.RefreshAfter.AddMinutes(-1))
            {
                _logger.LogDebug("Refreshing Harvest Right token");
                _session = await _authClient.RefreshTokenAsync(_session, ct);
            }

            // Check silence threshold
            if (DateTimeOffset.UtcNow - _lastMessageTime > silenceThreshold)
            {
                _logger.LogWarning(
                    "No MQTT messages for {Seconds}s — forcing reconnect",
                    (DateTimeOffset.UtcNow - _lastMessageTime).TotalSeconds);
                break; // Exit loop to trigger reconnection
            }

            await Task.Delay(watchdogInterval, ct);
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            _lastMessageTime = DateTimeOffset.UtcNow;

            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            // Parse topic: act/{customerId}/ed/{dryerId}/m/{messageType}
            var parts = topic.Split('/');
            if (parts.Length < 6) return;

            var dryerIdStr = parts[3];
            var messageType = parts[5];

            // We only process telemetry messages for now
            if (messageType != "telemetry") return;

            if (!int.TryParse(dryerIdStr, out var dryerCloudId)) return;

            var dryer = _dryers.FirstOrDefault(d => d.DryerId == dryerCloudId);
            if (dryer is null)
            {
                _logger.LogDebug("Received telemetry for unknown dryer {DryerId}", dryerCloudId);
                return;
            }

            // Parse telemetry JSON
            var telemetry = ParseTelemetry(payload);
            if (telemetry is null) return;

            // Dispatch through MediatR
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new IngestHarvestRightTelemetryCommand(
                HarvestRightDryerId: dryerCloudId,
                DryerSerial: dryer.Serial,
                TemperatureF: telemetry.Temperature,
                VacuumMTorr: telemetry.Vacuum,
                ProgressPercent: telemetry.Progress,
                ScreenNumber: telemetry.Screen,
                BatchName: telemetry.BatchName,
                BatchElapsedSeconds: telemetry.BatchElapsed,
                PhaseElapsedSeconds: telemetry.PhaseElapsed,
                Timestamp: DateTimeOffset.UtcNow);

            var result = await mediator.Send(command);

            if (result.IsSuccess)
            {
                var r = result.Value;
                _logger.LogDebug(
                    "Telemetry processed: dryer={Serial} screen={Screen} phase={Phase} alert={Alert} autoCreated={Created} autoAdvanced={Advanced}",
                    dryer.Serial, telemetry.Screen, r.MappedPhase, r.Alert?.Level, r.BatchAutoCreated, r.BatchAutoAdvanced);
            }
            else
            {
                _logger.LogWarning("Failed to process telemetry for dryer {Serial}: {Error}", dryer.Serial, result.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message on topic {Topic}", e.ApplicationMessage.Topic);
        }
    }

    private async Task PublishAsync(string topic, string payload, CancellationToken ct)
    {
        if (_mqttClient is null || !_mqttClient.IsConnected) return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        await _mqttClient.PublishAsync(message, ct);
    }

    private async Task SafeDisconnectAsync()
    {
        try
        {
            if (_mqttClient is { IsConnected: true })
                await _mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error during MQTT disconnect");
        }
        finally
        {
            _mqttClient?.Dispose();
            _mqttClient = null;
        }
    }

    // ─── Telemetry JSON Parsing ──────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private TelemetryPayload? ParseTelemetry(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<TelemetryPayload>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse telemetry JSON: {Payload}", json[..Math.Min(200, json.Length)]);
            return null;
        }
    }

    /// <summary>
    /// Telemetry payload structure based on ha-harvest-right sensor entities.
    /// Field names match the JSON keys from the Harvest Right cloud MQTT broker.
    /// </summary>
    private sealed record TelemetryPayload
    {
        public decimal Temperature { get; init; }
        public decimal Vacuum { get; init; }
        public decimal Progress { get; init; }
        public int Screen { get; init; }
        public string? BatchName { get; init; }
        public decimal? BatchElapsed { get; init; }
        public decimal? PhaseElapsed { get; init; }
    }
}
