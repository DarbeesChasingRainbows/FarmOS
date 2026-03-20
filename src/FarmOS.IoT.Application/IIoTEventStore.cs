using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Aggregates;
using FarmOS.SharedKernel;

namespace FarmOS.IoT.Application;

public interface IIoTEventStore
{
    Task<IoTDevice> LoadDeviceAsync(string id, CancellationToken ct);
    Task SaveDeviceAsync(IoTDevice device, string userId, CancellationToken ct);
    
    Task<Zone> LoadZoneAsync(string id, CancellationToken ct);
    Task SaveZoneAsync(Zone zone, string userId, CancellationToken ct);

    Task SaveTelemetryStreamAsync(TelemetryStream stream, string userId, CancellationToken ct);
}
