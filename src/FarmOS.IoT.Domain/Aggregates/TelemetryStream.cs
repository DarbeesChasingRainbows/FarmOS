using FarmOS.SharedKernel;
using FarmOS.IoT.Domain.Events;

namespace FarmOS.IoT.Domain.Aggregates;

/// <summary>
/// Aggregate root tracking a per-device telemetry stream with excursion state machine.
/// Each device has one TelemetryStream that accumulates readings and manages excursion detection.
/// The aggregate ID is the IoTDeviceId.
/// </summary>
public class TelemetryStream : AggregateRoot<IoTDeviceId>
{
    public string DeviceCode { get; private set; } = string.Empty;
    public ZoneId? ZoneId { get; private set; }
    public ZoneType? ZoneType { get; private set; }

    // Excursion state machine
    public ExcursionState CurrentExcursionState { get; private set; } = ExcursionState.Normal;
    public ExcursionId? ActiveExcursionId { get; private set; }
    public DateTimeOffset? ExcursionStartedAt { get; private set; }
    public SensorType? ExcursionSensorType { get; private set; }
    public decimal? ExcursionThresholdLimit { get; private set; }
    public ThresholdDirection? ExcursionThresholdDirection { get; private set; }

    // Last reading cache
    public decimal? LastValue { get; private set; }
    public SensorType? LastSensorType { get; private set; }
    public DateTimeOffset? LastReadingAt { get; private set; }

    private TelemetryStream() { }

    public static TelemetryStream Initialize(IoTDeviceId deviceId, string deviceCode, ZoneId? zoneId, ZoneType? zoneType)
    {
        var stream = new TelemetryStream
        {
            Id = deviceId,
            DeviceCode = deviceCode,
            ZoneId = zoneId,
            ZoneType = zoneType
        };
        return stream;
    }

    public void UpdateZoneInfo(ZoneId? zoneId, ZoneType? zoneType)
    {
        ZoneId = zoneId;
        ZoneType = zoneType;
    }

    /// <summary>
    /// Record a telemetry reading and evaluate excursion state.
    /// </summary>
    public void RecordReading(
        SensorType sensorType,
        decimal value,
        string unit,
        DateTimeOffset timestamp,
        ThresholdRule? applicableRule)
    {
        RaiseEvent(new TelemetryReadingRecorded(
            Id, DeviceCode, ZoneId, ZoneType,
            sensorType, value, unit, timestamp));

        if (applicableRule is null || ZoneId is null)
            return;

        var isViolation = applicableRule.Direction == ThresholdDirection.Above
            ? value > applicableRule.Limit
            : value < applicableRule.Limit;

        if (isViolation && CurrentExcursionState == ExcursionState.Normal)
        {
            // Start a new excursion
            var excursionId = ExcursionId.New();
            RaiseEvent(new ExcursionStarted(
                excursionId, Id, ZoneId, sensorType,
                value, applicableRule.Limit, applicableRule.Direction,
                timestamp));
        }
        else if (isViolation && CurrentExcursionState == ExcursionState.InExcursion)
        {
            // Check if grace period has elapsed → fire alert
            if (ExcursionStartedAt.HasValue &&
                (timestamp - ExcursionStartedAt.Value) >= applicableRule.GracePeriod)
            {
                var severity = DetermineSeverity(sensorType, value, applicableRule);
                var message = BuildAlertMessage(sensorType, value, applicableRule);
                var corrective = BuildCorrectiveAction(sensorType, applicableRule);

                RaiseEvent(new ExcursionAlertFired(
                    ActiveExcursionId!, Id, ZoneId, sensorType,
                    severity, message, corrective, timestamp));
            }
        }
        else if (!isViolation && CurrentExcursionState == ExcursionState.InExcursion)
        {
            // Return to normal — end excursion
            var duration = timestamp - (ExcursionStartedAt ?? timestamp);
            RaiseEvent(new ExcursionEnded(
                ActiveExcursionId!, Id, ZoneId, sensorType,
                timestamp, duration));
        }
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TelemetryReadingRecorded e:
                LastValue = e.Value;
                LastSensorType = e.SensorType;
                LastReadingAt = e.Timestamp;
                break;

            case ExcursionStarted e:
                CurrentExcursionState = ExcursionState.InExcursion;
                ActiveExcursionId = e.Id;
                ExcursionStartedAt = e.StartedAt;
                ExcursionSensorType = e.SensorType;
                ExcursionThresholdLimit = e.ThresholdLimit;
                ExcursionThresholdDirection = e.ThresholdDirection;
                break;

            case ExcursionEnded:
                CurrentExcursionState = ExcursionState.Normal;
                ActiveExcursionId = null;
                ExcursionStartedAt = null;
                ExcursionSensorType = null;
                ExcursionThresholdLimit = null;
                ExcursionThresholdDirection = null;
                break;

            case ExcursionAlertFired:
                // Alert fired does not change excursion state — excursion continues
                break;
        }
    }

    // ─── Alert helpers ─────────────────────────────────────────────────────

    private static string DetermineSeverity(SensorType sensor, decimal value, ThresholdRule rule)
    {
        var overshoot = Math.Abs(value - rule.Limit);
        return sensor switch
        {
            SensorType.Temperature when overshoot > 10m => "Critical",
            SensorType.Temperature => "Warning",
            SensorType.Humidity when overshoot > 20m => "Critical",
            SensorType.Humidity => "Warning",
            SensorType.Ph when overshoot > 1.0m => "Critical",
            SensorType.Ph => "Warning",
            _ => "Warning"
        };
    }

    private static string BuildAlertMessage(SensorType sensor, decimal value, ThresholdRule rule)
    {
        var direction = rule.Direction == ThresholdDirection.Above ? "above" : "below";
        return $"{sensor} reading {value}{GetUnit(sensor)} is {direction} the {rule.Limit}{GetUnit(sensor)} threshold for {rule.ZoneType} zone (exceeded grace period of {rule.GracePeriod.TotalMinutes:F0} min).";
    }

    private static string BuildCorrectiveAction(SensorType sensor, ThresholdRule rule) =>
        (rule.ZoneType, sensor) switch
        {
            (IoT.Domain.ZoneType.Freezer, SensorType.Temperature) =>
                "Check freezer door seal, compressor, and power supply. Move perishables to backup unit if temp cannot be restored within 30 min.",
            (IoT.Domain.ZoneType.Refrigerator, SensorType.Temperature) =>
                "Check refrigerator door seal and compressor. Verify food temps with probe thermometer. Discard if held >2h above 41°F.",
            (IoT.Domain.ZoneType.Storage, SensorType.Humidity) =>
                "Check dehumidifier and ventilation. Inspect dried herbs for moisture. Move product to climate-controlled area if needed.",
            (IoT.Domain.ZoneType.Storage, SensorType.Temperature) =>
                "Increase ventilation or move product to cooler storage. Dried herbs degrade above 77°F.",
            (IoT.Domain.ZoneType.FermentationRoom, SensorType.Temperature) =>
                "Adjust fermentation chamber climate control. Check heater/cooler thermostat.",
            _ => $"Investigate {sensor} reading for {rule.ZoneType} zone. Contact farm steward."
        };

    private static string GetUnit(SensorType sensor) =>
        sensor switch
        {
            SensorType.Temperature => "°F",
            SensorType.Humidity => "%",
            SensorType.Ph => "",
            SensorType.Co2 => "ppm",
            _ => ""
        };
}
