using FarmOS.IoT.Domain;

namespace FarmOS.IoT.Application;

/// <summary>
/// Lightweight read-only lookup for resolving device info during telemetry ingestion.
/// Separate from the full IIoTProjection to keep command handler dependencies minimal.
/// </summary>
public interface IIoTProjectionLookup
{
    Task<DeviceLookupDto?> GetDeviceByCodeAsync(string deviceCode, CancellationToken ct);
}

public record DeviceLookupDto(
    Guid Id,
    string DeviceCode,
    string Name,
    SensorType SensorType,
    Guid? ZoneId,
    ZoneType? ZoneType);
