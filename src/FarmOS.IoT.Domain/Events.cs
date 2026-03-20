using FarmOS.SharedKernel;
using FarmOS.IoT.Domain;

namespace FarmOS.IoT.Domain.Events;

// ─── IoT Device Events ─────────────────────────────────────────────────────

public record DeviceRegistered(
    IoTDeviceId Id,
    string DeviceCode,
    string Name,
    SensorType SensorType,
    DeviceStatus Status,
    ZoneId? ZoneId,
    GridPosition? GridPos,
    GeoPosition? GeoPos,
    Dictionary<string, string>? Metadata,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceUpdated(
    IoTDeviceId Id,
    string Name,
    DeviceStatus Status,
    Dictionary<string, string>? Metadata,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceDecommissioned(
    IoTDeviceId Id,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceAssignedToZone(
    IoTDeviceId DeviceId,
    ZoneId ZoneId,
    GridPosition? GridPos,
    GeoPosition? GeoPos,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceUnassignedFromZone(
    IoTDeviceId DeviceId,
    ZoneId PreviousZoneId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceAssignedToAsset(
    IoTDeviceId DeviceId,
    DeviceAssignment Assignment,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record DeviceUnassignedFromAsset(
    IoTDeviceId DeviceId,
    string Context,
    string AssetType,
    Guid AssetId,
    DateTimeOffset OccurredAt) : IDomainEvent;


// ─── Zone Events ───────────────────────────────────────────────────────────

public record ZoneCreated(
    ZoneId Id,
    string Name,
    ZoneType ZoneType,
    string? Description,
    GridPosition? GridPos,
    GeoPosition? GeoPos,
    ZoneId? ParentZoneId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record ZoneUpdated(
    ZoneId Id,
    string Name,
    string? Description,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record ZoneArchived(
    ZoneId Id,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;


// ─── Telemetry Events ─────────────────────────────────────────────────────

public record TelemetryReadingRecorded(
    IoTDeviceId DeviceId,
    string DeviceCode,
    ZoneId? ZoneId,
    ZoneType? ZoneType,
    SensorType SensorType,
    decimal Value,
    string Unit,
    DateTimeOffset Timestamp) : IDomainEvent
{
    public DateTimeOffset OccurredAt => Timestamp;
}

public record ExcursionStarted(
    ExcursionId Id,
    IoTDeviceId DeviceId,
    ZoneId ZoneId,
    SensorType SensorType,
    decimal Value,
    decimal ThresholdLimit,
    ThresholdDirection ThresholdDirection,
    DateTimeOffset StartedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => StartedAt;
}

public record ExcursionEnded(
    ExcursionId Id,
    IoTDeviceId DeviceId,
    ZoneId ZoneId,
    SensorType SensorType,
    DateTimeOffset EndedAt,
    TimeSpan Duration) : IDomainEvent
{
    public DateTimeOffset OccurredAt => EndedAt;
}

public record ExcursionAlertFired(
    ExcursionId ExcursionId,
    IoTDeviceId DeviceId,
    ZoneId ZoneId,
    SensorType SensorType,
    string Severity,
    string AlertMessage,
    string? CorrectiveAction,
    DateTimeOffset FiredAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => FiredAt;
}
