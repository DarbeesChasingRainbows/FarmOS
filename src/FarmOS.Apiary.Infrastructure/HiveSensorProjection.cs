using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Apiary.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

public record HiveSensorReading(string HiveId, string SensorType, decimal Value, string Unit, DateTimeOffset Timestamp);
public record HiveSensorSummary(string HiveId, decimal? WeightLbs, decimal? TempF, decimal? Humidity, DateTimeOffset? LastReadingAt);
public record HiveWeightTrend(DateOnly Date, decimal WeightLbs);

// ─── Projection ────────────────────────────────────────────────────────────

/// <summary>
/// Cross-context read projection that queries the IoT event store
/// for devices assigned to hives. Provides sensor data for the apiary module.
/// </summary>
/// <remarks>
/// This projection will be fully implemented when IoT devices are linked
/// to hives via AssetRef(Context: "Apiary", AssetType: "Hive", AssetId).
/// Currently returns placeholder data to establish the API contract.
/// </remarks>
public sealed class HiveSensorProjection
{
    public Task<IReadOnlyList<HiveSensorReading>> GetReadingsAsync(string hiveId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HiveSensorReading>>([]);

    public Task<HiveSensorSummary?> GetSummaryAsync(string hiveId, CancellationToken ct) =>
        Task.FromResult<HiveSensorSummary?>(null);

    public Task<IReadOnlyList<HiveWeightTrend>> GetWeightTrendAsync(string hiveId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HiveWeightTrend>>([]);
}
