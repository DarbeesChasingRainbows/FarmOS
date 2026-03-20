using FarmOS.Apiary.Domain;
using FarmOS.Apiary.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Apiary.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

public record HiveSummary(
    string Id,
    string Name,
    string Type,
    string Status,
    string? ApiaryName,
    string? QueenStatus,
    string? QueenColor,
    int InspectionCount,
    decimal? LastMiteCount,
    DateOnly? LastInspectionDate,
    decimal TotalHoneyHarvestedLbs,
    int HarvestCount,
    int FeedingCount,
    int SuperCount,
    DateTimeOffset EstablishedAt);

public record ApiaryOverview(
    string Id,
    string Name,
    int HiveCount,
    int MaxCapacity,
    double Latitude,
    double Longitude,
    string Status);

public record MiteTrendPoint(DateOnly Date, decimal MiteCount, string HiveId, string HiveName);

public record YieldReport(string HiveId, string HiveName, int Year, decimal HoneyLbs, decimal? WaxLbs, int HarvestCount);

public record ColonySurvivalReport(int TotalHives, int ActiveHives, int DeadHives, decimal LossRate);

// ─── Projection ────────────────────────────────────────────────────────────

public sealed class ApiaryProjection(IEventStore store)
{
    private const string CollectionName = "apiary_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(HiveCreated)] = typeof(HiveCreated),
        [nameof(HiveInspected)] = typeof(HiveInspected),
        [nameof(HoneyHarvested)] = typeof(HoneyHarvested),
        [nameof(HiveTreated)] = typeof(HiveTreated),
        [nameof(HiveStatusChanged)] = typeof(HiveStatusChanged),
        [nameof(HiveSwarmed)] = typeof(HiveSwarmed),
        [nameof(ApiaryCreated)] = typeof(ApiaryCreated),
        [nameof(HiveMovedToApiary)] = typeof(HiveMovedToApiary),
        [nameof(HiveRemovedFromApiary)] = typeof(HiveRemovedFromApiary),
        [nameof(ApiaryRetired)] = typeof(ApiaryRetired),
        [nameof(QueenIntroduced)] = typeof(QueenIntroduced),
        [nameof(QueenLost)] = typeof(QueenLost),
        [nameof(QueenReplaced)] = typeof(QueenReplaced),
        [nameof(HiveFed)] = typeof(HiveFed),
        [nameof(ProductHarvested)] = typeof(ProductHarvested),
        [nameof(ColonySplit)] = typeof(ColonySplit),
        [nameof(ColoniesMerged)] = typeof(ColoniesMerged),
        [nameof(SuperAdded)] = typeof(SuperAdded),
        [nameof(SuperRemoved)] = typeof(SuperRemoved),
        [nameof(HiveConfigurationChanged)] = typeof(HiveConfigurationChanged),
    };

    private async Task<(Dictionary<string, HiveState> Hives, Dictionary<string, ApiaryState> Apiaries)> LoadAllStatesAsync(CancellationToken ct)
    {
        var hives = new Dictionary<string, HiveState>();
        var apiaries = new Dictionary<string, ApiaryState>();
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
                ApplyToState(hives, apiaries, evt);
            }

            position += docs.Count;
            if (docs.Count < 500) break;
        }

        return (hives, apiaries);
    }

    public async Task<IReadOnlyList<HiveSummary>> GetAllHivesAsync(CancellationToken ct)
    {
        var (hives, apiaries) = await LoadAllStatesAsync(ct);
        return hives.Values.Select(h => ToSummary(h, apiaries)).OrderBy(h => h.Name).ToList();
    }

    public async Task<IReadOnlyList<ApiaryOverview>> GetAllApiariesAsync(CancellationToken ct)
    {
        var (_, apiaries) = await LoadAllStatesAsync(ct);
        return apiaries.Values.Select(ToOverview).OrderBy(a => a.Name).ToList();
    }

    public async Task<IReadOnlyList<MiteTrendPoint>> GetMiteTrendsAsync(Guid? hiveId, CancellationToken ct)
    {
        var (hives, _) = await LoadAllStatesAsync(ct);
        var points = new List<MiteTrendPoint>();

        foreach (var h in hives.Values)
        {
            if (hiveId.HasValue && h.Id != hiveId.Value.ToString()) continue;
            points.AddRange(h.MiteReadings.Select(r => new MiteTrendPoint(r.Date, r.Count, h.Id, h.Name)));
        }

        return points.OrderBy(p => p.Date).ToList();
    }

    public async Task<IReadOnlyList<YieldReport>> GetYieldReportAsync(int? year, CancellationToken ct)
    {
        var (hives, _) = await LoadAllStatesAsync(ct);
        return hives.Values
            .Where(h => h.HoneyHarvestLbs > 0)
            .Select(h => new YieldReport(h.Id, h.Name, year ?? DateTime.UtcNow.Year, h.HoneyHarvestLbs, h.WaxHarvestLbs, h.HarvestCount))
            .OrderByDescending(y => y.HoneyLbs)
            .ToList();
    }

    public async Task<ColonySurvivalReport> GetSurvivalReportAsync(CancellationToken ct)
    {
        var (hives, _) = await LoadAllStatesAsync(ct);
        var total = hives.Count;
        var dead = hives.Values.Count(h => h.Status == "Dead");
        var active = hives.Values.Count(h => h.Status == "Active");
        var lossRate = total > 0 ? (decimal)dead / total * 100 : 0;
        return new ColonySurvivalReport(total, active, dead, Math.Round(lossRate, 1));
    }

    // ─── State builder ──────────────────────────────────────────────────────

    private static void ApplyToState(Dictionary<string, HiveState> hives, Dictionary<string, ApiaryState> apiaries, IDomainEvent evt)
    {
        switch (evt)
        {
            case HiveCreated e:
                hives[e.Id.ToString()] = new HiveState
                {
                    Id = e.Id.ToString(), Name = e.Name, Type = e.Type.ToString(),
                    Status = "Active", EstablishedAt = e.OccurredAt
                };
                break;

            case HiveInspected e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.InspectionCount++;
                h.LastInspectionDate = e.Date;
                if (e.Data.MiteCount.HasValue)
                    h.MiteReadings.Add((e.Date, e.Data.MiteCount.Value));
                h.LastMiteCount = e.Data.MiteCount;
                break;

            case HoneyHarvested e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.HarvestCount++;
                h.HoneyHarvestLbs += e.Data.HoneyWeight.Value;
                if (e.Data.WaxWeight is not null) h.WaxHarvestLbs = (h.WaxHarvestLbs ?? 0) + e.Data.WaxWeight.Value;
                break;

            case HiveStatusChanged e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.Status = e.Next.ToString();
                break;

            case HiveSwarmed e when hives.TryGetValue(e.OriginalId.ToString(), out var h):
                h.Status = "Swarmed";
                break;

            case ApiaryCreated e:
                apiaries[e.Id.ToString()] = new ApiaryState
                {
                    Id = e.Id.ToString(), Name = e.Name, MaxCapacity = e.MaxCapacity,
                    Latitude = e.Position.Latitude, Longitude = e.Position.Longitude, Status = "Active"
                };
                break;

            case HiveMovedToApiary e:
                if (hives.TryGetValue(e.HiveId.ToString(), out var movedHive))
                    movedHive.ApiaryId = e.NewApiaryId.ToString();
                if (apiaries.TryGetValue(e.NewApiaryId.ToString(), out var targetApiary))
                    targetApiary.HiveCount++;
                if (e.PreviousApiaryId is not null && apiaries.TryGetValue(e.PreviousApiaryId.ToString(), out var prevApiary))
                    prevApiary.HiveCount = Math.Max(0, prevApiary.HiveCount - 1);
                break;

            case HiveRemovedFromApiary e:
                if (hives.TryGetValue(e.HiveId.ToString(), out var removedHive))
                    removedHive.ApiaryId = null;
                if (apiaries.TryGetValue(e.ApiaryId.ToString(), out var fromApiary))
                    fromApiary.HiveCount = Math.Max(0, fromApiary.HiveCount - 1);
                break;

            case ApiaryRetired e when apiaries.TryGetValue(e.Id.ToString(), out var a):
                a.Status = "Retired";
                break;

            case QueenIntroduced e when hives.TryGetValue(e.HiveId.ToString(), out var h):
                h.QueenStatus = "Present";
                h.QueenColor = e.Queen.Color?.ToString();
                break;

            case QueenLost e when hives.TryGetValue(e.HiveId.ToString(), out var h):
                h.QueenStatus = "Lost";
                h.QueenColor = null;
                break;

            case QueenReplaced e when hives.TryGetValue(e.HiveId.ToString(), out var h):
                h.QueenStatus = "Present";
                h.QueenColor = e.NewQueen.Color?.ToString();
                break;

            case HiveFed e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.FeedingCount++;
                break;

            case SuperAdded e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.SuperCount = e.NewSuperCount;
                break;

            case SuperRemoved e when hives.TryGetValue(e.Id.ToString(), out var h):
                h.SuperCount = e.NewSuperCount;
                break;
        }
    }

    // ─── Mapping ────────────────────────────────────────────────────────────

    private static HiveSummary ToSummary(HiveState h, Dictionary<string, ApiaryState> apiaries)
    {
        var apiaryName = h.ApiaryId is not null && apiaries.TryGetValue(h.ApiaryId, out var a) ? a.Name : null;
        return new HiveSummary(h.Id, h.Name, h.Type, h.Status, apiaryName, h.QueenStatus, h.QueenColor,
            h.InspectionCount, h.LastMiteCount, h.LastInspectionDate, h.HoneyHarvestLbs, h.HarvestCount,
            h.FeedingCount, h.SuperCount, h.EstablishedAt);
    }

    private static ApiaryOverview ToOverview(ApiaryState a) =>
        new(a.Id, a.Name, a.HiveCount, a.MaxCapacity, a.Latitude, a.Longitude, a.Status);

    // ─── Mutable state helpers ──────────────────────────────────────────────

    private sealed class HiveState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string? ApiaryId { get; set; }
        public string? QueenStatus { get; set; }
        public string? QueenColor { get; set; }
        public int InspectionCount { get; set; }
        public decimal? LastMiteCount { get; set; }
        public DateOnly? LastInspectionDate { get; set; }
        public decimal HoneyHarvestLbs { get; set; }
        public decimal? WaxHarvestLbs { get; set; }
        public int HarvestCount { get; set; }
        public int FeedingCount { get; set; }
        public int SuperCount { get; set; }
        public DateTimeOffset EstablishedAt { get; set; }
        public List<(DateOnly Date, decimal Count)> MiteReadings { get; set; } = [];
    }

    private sealed class ApiaryState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int HiveCount { get; set; }
        public int MaxCapacity { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = "";
    }
}
