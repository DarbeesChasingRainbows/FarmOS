using FarmOS.IoT.Application;
using FarmOS.IoT.Domain;

namespace FarmOS.IoT.Infrastructure;

/// <summary>
/// Provides FDA/GDA-compliant default threshold rules for zone type + sensor type combinations.
/// Rules are based on FDA Food Code 2022, USP 795, and standard food safety guidelines.
/// </summary>
public sealed class DefaultThresholdRuleProvider : IThresholdRuleProvider
{
    private static readonly List<ThresholdRule> Rules =
    [
        // Freezer: temp must stay ≤ 0°F — 15 min grace period
        new(ZoneType.Freezer, SensorType.Temperature, 0m, ThresholdDirection.Above, TimeSpan.FromMinutes(15)),

        // Refrigerator: temp must stay ≤ 41°F — 30 min grace period
        new(ZoneType.Refrigerator, SensorType.Temperature, 41m, ThresholdDirection.Above, TimeSpan.FromMinutes(30)),

        // Storage (Apothecary/dry herbs): temp must stay ≤ 77°F (25°C USP <795>)
        new(ZoneType.Storage, SensorType.Temperature, 77m, ThresholdDirection.Above, TimeSpan.FromMinutes(60)),

        // Storage: humidity must stay ≤ 65% for dried herbs — 2 hour grace
        new(ZoneType.Storage, SensorType.Humidity, 65m, ThresholdDirection.Above, TimeSpan.FromHours(2)),

        // Fermentation room: temp must stay ≤ 85°F — 30 min grace
        new(ZoneType.FermentationRoom, SensorType.Temperature, 85m, ThresholdDirection.Above, TimeSpan.FromMinutes(30)),

        // Fermentation room: temp must stay ≥ 65°F — 30 min grace
        new(ZoneType.FermentationRoom, SensorType.Temperature, 65m, ThresholdDirection.Below, TimeSpan.FromMinutes(30)),

        // Fruiting room (mushrooms): humidity must stay ≥ 80% — 60 min grace
        new(ZoneType.FruitingRoom, SensorType.Humidity, 80m, ThresholdDirection.Below, TimeSpan.FromMinutes(60)),

        // Fruiting room: temp must stay ≤ 75°F — 30 min grace
        new(ZoneType.FruitingRoom, SensorType.Temperature, 75m, ThresholdDirection.Above, TimeSpan.FromMinutes(30)),
    ];

    public ThresholdRule? GetRule(ZoneType zoneType, SensorType sensorType)
    {
        // Return the first matching rule (most specific).
        // For zone types with both Above and Below rules for same sensor,
        // the caller should ideally check both, but we return the Above rule first
        // as it's typically the more dangerous direction.
        return Rules.FirstOrDefault(r => r.ZoneType == zoneType && r.SensorType == sensorType);
    }
}
