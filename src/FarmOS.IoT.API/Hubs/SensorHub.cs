using Microsoft.AspNetCore.SignalR;

namespace FarmOS.IoT.API.Hubs;

/// <summary>
/// SignalR hub for real-time IoT sensor data streaming.
/// Clients (iot-os tablet UI) connect and receive live telemetry readings,
/// excursion alerts, and device status changes.
/// 
/// Client methods:
///   - SensorReading(reading)     — new telemetry data point
///   - ExcursionAlert(alert)      — excursion threshold breach
///   - ExcursionResolved(info)    — excursion returned to normal
///   - DeviceStatusChanged(info)  — device online/offline/maintenance
/// </summary>
public sealed class SensorHub : Hub
{
    /// <summary>
    /// Client subscribes to a specific zone's sensor feed.
    /// </summary>
    public async Task SubscribeToZone(string zoneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"zone:{zoneId}");
    }

    /// <summary>
    /// Client unsubscribes from a zone's sensor feed.
    /// </summary>
    public async Task UnsubscribeFromZone(string zoneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"zone:{zoneId}");
    }

    /// <summary>
    /// Client subscribes to all sensor data (global feed).
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-sensors");
    }

    public override async Task OnConnectedAsync()
    {
        // Auto-subscribe to global feed on connect
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-sensors");
        await base.OnConnectedAsync();
    }
}
