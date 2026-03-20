using FarmOS.IoT.Domain;
using FarmOS.IoT.Domain.Events;
using FarmOS.IoT.Application.Queries;
using FarmOS.IoT.Application.Queries.Handlers;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.IoT.Infrastructure;

/// <summary>
/// Builds telemetry read models from the iot_events stream.
/// Handles climate logs, compliance reports, and active excursion tracking.
/// </summary>
public sealed class TelemetryProjection(IEventStore store) : ITelemetryProjection
{
    private const string CollectionName = "iot_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(TelemetryReadingRecorded)] = typeof(TelemetryReadingRecorded),
        [nameof(ExcursionStarted)] = typeof(ExcursionStarted),
        [nameof(ExcursionEnded)] = typeof(ExcursionEnded),
        [nameof(ExcursionAlertFired)] = typeof(ExcursionAlertFired),
        [nameof(ZoneCreated)] = typeof(ZoneCreated),
        [nameof(DeviceRegistered)] = typeof(DeviceRegistered),
    };

    public async Task<ZoneClimateLogDto?> GetZoneClimateLogAsync(
        Guid zoneId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var (readings, zones, _) = await LoadTelemetryStateAsync(ct);

        var zidStr = zoneId.ToString();
        if (!zones.TryGetValue(zidStr, out var zoneInfo))
            return null;

        var fromDt = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var toDt = to ?? DateTimeOffset.UtcNow;

        var filtered = readings
            .Where(r => r.ZoneId == zidStr && r.Timestamp >= fromDt && r.Timestamp <= toDt)
            .OrderBy(r => r.Timestamp)
            .Select(r => new ClimateLogEntryDto(r.Timestamp, r.DeviceCode, r.SensorType, r.Value, r.Unit))
            .ToList();

        return new ZoneClimateLogDto(zoneId, zoneInfo.Name, zoneInfo.ZoneType, fromDt, toDt, filtered);
    }

    public async Task<ZoneComplianceReportDto?> GetZoneComplianceReportAsync(
        Guid zoneId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var (readings, zones, excursions) = await LoadTelemetryStateAsync(ct);

        var zidStr = zoneId.ToString();
        if (!zones.TryGetValue(zidStr, out var zoneInfo))
            return null;

        var fromDt = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var toDt = to ?? DateTimeOffset.UtcNow;

        var zoneReadings = readings
            .Where(r => r.ZoneId == zidStr && r.Timestamp >= fromDt && r.Timestamp <= toDt)
            .ToList();

        var violations = excursions
            .Where(e => e.ZoneId == zidStr && e.StartedAt >= fromDt && e.StartedAt <= toDt && e.AlertFired)
            .Select(e => new ComplianceViolationDto(
                e.StartedAt, e.EndedAt, e.Duration,
                e.DeviceCode, e.SensorType, e.LastValue,
                e.ThresholdLimit, e.Severity ?? "Warning", e.AlertMessage ?? ""))
            .ToList();

        return new ZoneComplianceReportDto(
            zoneId, zoneInfo.Name, zoneInfo.ZoneType, fromDt, toDt,
            zoneReadings.Count, violations.Count, violations.Count == 0, violations);
    }

    public async Task<List<ActiveExcursionDto>> GetActiveExcursionsAsync(CancellationToken ct)
    {
        var (_, _, excursions) = await LoadTelemetryStateAsync(ct);

        return excursions
            .Where(e => e.IsActive)
            .Select(e => new ActiveExcursionDto(
                e.ExcursionId, e.DeviceId, e.DeviceCode, Guid.Parse(e.ZoneId),
                e.SensorType, e.LastValue, e.ThresholdLimit,
                e.ThresholdDirection, e.StartedAt,
                DateTimeOffset.UtcNow - e.StartedAt))
            .ToList();
    }

    // ─── State loading ───────────────────────────────────────────────────

    private async Task<(List<ReadingState>, Dictionary<string, ZoneInfo>, List<ExcursionState>)>
        LoadTelemetryStateAsync(CancellationToken ct)
    {
        var readings = new List<ReadingState>();
        var zones = new Dictionary<string, ZoneInfo>();
        var devices = new Dictionary<string, string>(); // deviceId → deviceCode
        var excursions = new Dictionary<string, ExcursionState>(); // excursionId → state
        long position = 0;

        while (true)
        {
            var docs = await store.GetAllEventsAsync(CollectionName, position, 500, ct);
            if (docs.Count == 0) break;

            foreach (var doc in docs)
            {
                if (!EventTypeMap.TryGetValue(doc.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(doc.Payload, type) as IDomainEvent;
                if (evt is null) continue;

                switch (evt)
                {
                    case DeviceRegistered dreg:
                        devices[dreg.Id.Value.ToString()] = dreg.DeviceCode;
                        break;

                    case ZoneCreated zc:
                        zones[zc.Id.Value.ToString()] = new ZoneInfo(zc.Name, zc.ZoneType);
                        break;

                    case TelemetryReadingRecorded r:
                        readings.Add(new ReadingState
                        {
                            DeviceCode = r.DeviceCode,
                            ZoneId = r.ZoneId?.Value.ToString() ?? "",
                            SensorType = r.SensorType,
                            Value = r.Value,
                            Unit = r.Unit,
                            Timestamp = r.Timestamp,
                        });
                        break;

                    case ExcursionStarted es:
                        var devCode = devices.GetValueOrDefault(es.DeviceId.Value.ToString(), "");
                        excursions[es.Id.Value.ToString()] = new ExcursionState
                        {
                            ExcursionId = es.Id.Value,
                            DeviceId = es.DeviceId.Value,
                            DeviceCode = devCode,
                            ZoneId = es.ZoneId.Value.ToString(),
                            SensorType = es.SensorType,
                            ThresholdLimit = es.ThresholdLimit,
                            ThresholdDirection = es.ThresholdDirection.ToString(),
                            LastValue = es.Value,
                            StartedAt = es.StartedAt,
                            IsActive = true,
                        };
                        break;

                    case ExcursionAlertFired eaf:
                        if (excursions.TryGetValue(eaf.ExcursionId.Value.ToString(), out var exc1))
                        {
                            exc1.AlertFired = true;
                            exc1.Severity = eaf.Severity;
                            exc1.AlertMessage = eaf.AlertMessage;
                        }
                        break;

                    case ExcursionEnded ee:
                        if (excursions.TryGetValue(ee.Id.Value.ToString(), out var exc2))
                        {
                            exc2.IsActive = false;
                            exc2.EndedAt = ee.EndedAt;
                            exc2.Duration = ee.Duration;
                        }
                        break;
                }
            }

            position += docs.Count;
            if (docs.Count < 500) break;
        }

        return (readings, zones, excursions.Values.ToList());
    }

    // ─── State classes ───────────────────────────────────────────────────

    private record ZoneInfo(string Name, ZoneType ZoneType);

    private sealed class ReadingState
    {
        public string DeviceCode { get; set; } = "";
        public string ZoneId { get; set; } = "";
        public SensorType SensorType { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; } = "";
        public DateTimeOffset Timestamp { get; set; }
    }

    private sealed class ExcursionState
    {
        public Guid ExcursionId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceCode { get; set; } = "";
        public string ZoneId { get; set; } = "";
        public SensorType SensorType { get; set; }
        public decimal ThresholdLimit { get; set; }
        public string ThresholdDirection { get; set; } = "";
        public decimal LastValue { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public bool IsActive { get; set; }
        public bool AlertFired { get; set; }
        public string? Severity { get; set; }
        public string? AlertMessage { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
