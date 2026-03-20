using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;

namespace FarmOS.IoT.Application.Queries;

// ─── DTOs ──────────────────────────────────────────────────────────────────

public record DeviceSummaryDto(
    Guid Id,
    string DeviceCode,
    string Name,
    SensorType SensorType,
    DeviceStatus Status,
    Guid? ZoneId);

public record DeviceDetailDto(
    Guid Id,
    string DeviceCode,
    string Name,
    SensorType SensorType,
    DeviceStatus Status,
    Guid? ZoneId,
    GridPosition? GridPos,
    GeoPosition? GeoPos,
    IReadOnlyList<DeviceAssignment> Assignments,
    IReadOnlyDictionary<string, string> Metadata);

public record ZoneSummaryDto(
    Guid Id,
    string Name,
    ZoneType ZoneType,
    Guid? ParentZoneId);

public record ZoneDetailDto(
    Guid Id,
    string Name,
    ZoneType ZoneType,
    string? Description,
    GridPosition? GridPos,
    GeoPosition? GeoPos,
    Guid? ParentZoneId,
    bool IsArchived,
    List<DeviceSummaryDto> Devices);

// ─── Queries ───────────────────────────────────────────────────────────────

public record GetAllDevicesQuery() : IQuery<List<DeviceSummaryDto>>;

public record GetDeviceDetailQuery(Guid DeviceId) : IQuery<DeviceDetailDto>;

public record GetDevicesByZoneQuery(Guid ZoneId) : IQuery<List<DeviceSummaryDto>>;

public record GetAllZonesQuery() : IQuery<List<ZoneSummaryDto>>;

public record GetZoneDetailQuery(Guid ZoneId) : IQuery<ZoneDetailDto>;

// ─── Projection Interface ──────────────────────────────────────────────────

/// <summary>
/// Infrastructure projection responsible for building read models from event streams.
/// </summary>
public interface IIoTProjection
{
    Task<List<DeviceSummaryDto>> GetAllDevicesAsync(CancellationToken ct);
    Task<DeviceDetailDto?> GetDeviceDetailAsync(Guid deviceId, CancellationToken ct);
    Task<List<DeviceSummaryDto>> GetDevicesByZoneAsync(Guid zoneId, CancellationToken ct);
    
    Task<List<ZoneSummaryDto>> GetAllZonesAsync(CancellationToken ct);
    Task<ZoneDetailDto?> GetZoneDetailAsync(Guid zoneId, CancellationToken ct);
}
