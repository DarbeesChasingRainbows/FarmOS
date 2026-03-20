using FarmOS.SharedKernel;

namespace FarmOS.IoT.Domain;

// ─── Typed IDs ─────────────────────────────────────────────────────────────

public record IoTDeviceId(Guid Value)
{
    public static IoTDeviceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record ZoneId(Guid Value)
{
    public static ZoneId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record TelemetryStreamId(Guid Value)
{
    public static TelemetryStreamId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record ExcursionId(Guid Value)
{
    public static ExcursionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// ─── Enums ─────────────────────────────────────────────────────────────────

public enum DeviceStatus
{
    Active,
    Inactive,
    Maintenance,
    Decommissioned
}

public enum ZoneType
{
    Refrigerator,
    Freezer,
    FruitingRoom,
    FermentationRoom,
    GrowBed,
    Greenhouse,
    Field,
    Apiary,
    Kitchen,
    Storage,
    Custom
}

/// <summary>
/// Mirrors but formally owns the IoT sensor types used in this context.
/// </summary>
public enum SensorType
{
    Temperature,
    Humidity,
    Ph,
    Co2,
    Light,
    Moisture,
    Weight,
    Custom
}

public enum ThresholdDirection { Above, Below }

public enum ExcursionState { Normal, InExcursion }

// ─── Value Objects ─────────────────────────────────────────────────────────

/// <summary>
/// Assignment of an IoT device to a cross-context asset.
/// </summary>
public record DeviceAssignment(AssetRef Asset, DateTimeOffset AssignedAt);

/// <summary>
/// A single telemetry data point recorded from a sensor.
/// </summary>
public record TelemetryReading(
    string DeviceCode,
    SensorType SensorType,
    decimal Value,
    string Unit,
    DateTimeOffset Timestamp);

/// <summary>
/// Configurable threshold rule for a zone type + sensor type combination.
/// GracePeriod defines how long the threshold can be exceeded before an excursion alert fires.
/// </summary>
public record ThresholdRule(
    ZoneType ZoneType,
    SensorType SensorType,
    decimal Limit,
    ThresholdDirection Direction,
    TimeSpan GracePeriod);

/// <summary>
/// Climate specification for Apothecary / herb storage zones with custom thresholds.
/// </summary>
public record ApothecaryClimateSpec(
    decimal MaxTempF,
    decimal MaxHumidityPct,
    decimal MinHumidityPct);
