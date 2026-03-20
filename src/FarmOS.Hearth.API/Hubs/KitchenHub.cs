using FarmOS.Hearth.Application;
using FarmOS.Hearth.Domain;
using Microsoft.AspNetCore.SignalR;

namespace FarmOS.Hearth.API.Hubs;

/// <summary>
/// SignalR hub for real-time kitchen and IoT sensor data.
/// Clients can subscribe to specific device groups for targeted updates.
/// </summary>
public sealed class KitchenHub : Hub
{
    /// <summary>Subscribe to updates for a specific device ID.</summary>
    public async Task Subscribe(string deviceId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device:{deviceId}");

    /// <summary>Subscribe to alert-level filter (Safe, Warning, Critical).</summary>
    public async Task SubscribeToAlertLevel(string level) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alert:{level}");
}

// ─── Notifier Abstraction ─────────────────────────────────────────────────────

public sealed class KitchenHubNotifier(IHubContext<KitchenHub> hub) : IKitchenHubNotifier
{
    public async Task BroadcastAsync(SensorReading reading, IoTAlert alert, CancellationToken ct)
    {
        var payload = new
        {
            reading = new
            {
                deviceId = reading.DeviceId,
                sensorType = reading.SensorType.ToString(),
                value = reading.Value,
                unit = reading.Unit,
                timestamp = reading.Timestamp,
            },
            alert = new
            {
                level = alert.Level.ToString(),
                message = alert.Message,
                correctiveAction = alert.CorrectiveAction,
            }
        };

        // Broadcast to all connected clients
        await hub.Clients.All.SendAsync("SensorReading", payload, ct);

        // Also broadcast to alert-level group
        await hub.Clients.Group($"alert:{alert.Level}").SendAsync("SensorReading", payload, ct);

        // If it's a Critical alert, broadcast to warnings group too
        if (alert.Level == AlertLevel.Critical)
            await hub.Clients.Group("alert:Warning").SendAsync("SensorReading", payload, ct);
    }

    public async Task BroadcastFreezeDryerTelemetryAsync(FreezeDryerTelemetrySnapshot snapshot, CancellationToken ct)
    {
        await hub.Clients.All.SendAsync("FreezeDryerTelemetry", snapshot, ct);
        await hub.Clients.Group($"device:{snapshot.DryerSerial}").SendAsync("FreezeDryerTelemetry", snapshot, ct);
    }
}
