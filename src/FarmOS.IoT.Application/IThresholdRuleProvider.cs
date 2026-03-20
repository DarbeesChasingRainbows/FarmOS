using FarmOS.IoT.Domain;

namespace FarmOS.IoT.Application;

/// <summary>
/// Provides threshold rules for zone type + sensor type combinations.
/// Allows rules to be configurable per-zone or use system defaults.
/// </summary>
public interface IThresholdRuleProvider
{
    ThresholdRule? GetRule(ZoneType zoneType, SensorType sensorType);
}
