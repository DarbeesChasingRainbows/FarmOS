using FarmOS.Assets.Domain;
using FarmOS.Assets.Domain.Events;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Assets.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

/// <summary>Summary card data for the compost batch grid view.</summary>
public record CompostBatchSummary(
    string Id,
    string BatchCode,
    string Method,
    string Phase,
    string? CnRatioDisplay,
    decimal? CarbonRatio,
    decimal? NitrogenRatio,
    double? LastTempF,
    DateTimeOffset? LastTempAt,
    string? TempZone,          // Optimal | TooHot | TooLow | Fermentation | VermiculiteRange
    int TurnCount,
    int InoculationCount,
    int NoteCount,
    decimal? LatestPH,
    DateOnly? LatestPhDate,
    DateTimeOffset StartedAt,
    int DaysElapsed,
    double Latitude,
    double Longitude,
    string? YieldCuYd);

/// <summary>Full batch detail with all logs for the slide-out sidebar.</summary>
public record CompostBatchDetail(
    string Id,
    string BatchCode,
    string Method,
    string Phase,
    string? CnRatioDisplay,
    decimal? CarbonRatio,
    decimal? NitrogenRatio,
    string? StartNotes,
    DateTimeOffset StartedAt,
    int DaysElapsed,
    double Latitude,
    double Longitude,
    string? YieldCuYd,
    IReadOnlyList<CompostInputDto> Inputs,
    IReadOnlyList<TempReadingDto> TemperatureLog,
    IReadOnlyList<TurnEntryDto> TurnLog,
    IReadOnlyList<KnfInputDto> Inoculations,
    IReadOnlyList<PhEntryDto> PhLog,
    IReadOnlyList<CompostNoteDto> Notes);

public record CompostInputDto(string Material, string Amount, string Unit, string Type, decimal? CnRatio);
public record TempReadingDto(DateTimeOffset Timestamp, decimal TemperatureF, string Zone);
public record TurnEntryDto(string Date, string? Notes, int DaysSincePrev);
public record KnfInputDto(string InputType, string Description, string PreparedDate, string Amount, string Unit);
public record PhEntryDto(string Date, decimal PH, string? Notes, string Status);
public record CompostNoteDto(string Date, string Category, string Body);

// ─── Projection ────────────────────────────────────────────────────────────

/// <summary>
/// Replays assets_events to build CompostBatch read models.
/// Understands all 8 compost event types and produces summary + detail views.
/// </summary>
public sealed class CompostProjection(IEventStore store)
{
    private const string CollectionName = "assets_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(CompostBatchStarted)] = typeof(CompostBatchStarted),
        [nameof(CompostTempRecorded)] = typeof(CompostTempRecorded),
        [nameof(CompostTurned)] = typeof(CompostTurned),
        [nameof(CompostPhaseChanged)] = typeof(CompostPhaseChanged),
        [nameof(CompostInoculated)] = typeof(CompostInoculated),
        [nameof(CompostPhMeasured)] = typeof(CompostPhMeasured),
        [nameof(CompostNoteAdded)] = typeof(CompostNoteAdded),
        [nameof(CompostBatchCompleted)] = typeof(CompostBatchCompleted),
    };

    private async Task<Dictionary<string, BatchState>> LoadAllBatchStatesAsync(int batchSize, CancellationToken ct)
    {
        var batches = new Dictionary<string, BatchState>();
        long position = 0;

        while (true)
        {
            var docs = await store.GetAllEventsAsync(CollectionName, position, batchSize, ct);
            if (docs.Count == 0) break;

            foreach (var doc in docs)
            {
                // Only process CompostBatch aggregate events
                if (!EventTypeMap.TryGetValue(doc.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(doc.Payload, type) as IDomainEvent;
                if (evt is null) continue;
                ApplyToState(batches, evt);
            }

            position += docs.Count;
            if (docs.Count < batchSize) break;
        }

        return batches;
    }

    public async Task<IReadOnlyList<CompostBatchSummary>> GetAllBatchesAsync(CancellationToken ct)
    {
        var batches = await LoadAllBatchStatesAsync(200, ct);
        return batches.Values.Select(ToSummary).OrderByDescending(b => b.StartedAt).ToList();
    }

    public async Task<CompostBatchDetail?> GetBatchDetailAsync(string id, CancellationToken ct)
    {
        var batches = await LoadAllBatchStatesAsync(500, ct);
        return batches.TryGetValue(id, out var state) ? ToDetail(state) : null;
    }

    // ─── State builder ──────────────────────────────────────────────────────

    private static void ApplyToState(Dictionary<string, BatchState> batches, IDomainEvent evt)
    {
        switch (evt)
        {
            case CompostBatchStarted e:
                batches[e.Id.ToString()] = new BatchState
                {
                    Id = e.Id.ToString(),
                    BatchCode = e.BatchCode,
                    Method = e.Method.ToString(),
                    Phase = CompostPhase.Active.ToString(),
                    CarbonRatio = e.CarbonRatio,
                    NitrogenRatio = e.NitrogenRatio,
                    StartNotes = e.Notes,
                    StartedAt = e.OccurredAt,
                    Latitude = e.Location.Latitude,
                    Longitude = e.Location.Longitude,
                    Inputs = e.Inputs.Select(i => new CompostInputDto(
                        i.Material,
                        i.Amount.Value.ToString("F2"),
                        i.Amount.Unit,
                        i.Type,
                        i.CnRatio)).ToList()
                };
                break;

            case CompostTempRecorded e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.TemperatureLog.Add(new TempReadingDto(
                    e.Reading.Timestamp,
                    e.Reading.TemperatureF,
                    GetTempZone(e.Reading.TemperatureF, b.Method)));
                break;

            case CompostTurned e when batches.TryGetValue(e.Id.ToString(), out var b):
                var prevTurnDate = b.TurnLog.Count > 0 ? DateOnly.Parse(b.TurnLog[^1].Date) : DateOnly.FromDateTime(b.StartedAt.UtcDateTime);
                var daysSince = e.Date.DayNumber - prevTurnDate.DayNumber;
                b.TurnLog.Add(new TurnEntryDto(e.Date.ToString("yyyy-MM-dd"), e.Notes, daysSince));
                b.Phase = CompostPhase.Turning.ToString();
                break;

            case CompostPhaseChanged e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.Phase = e.NewPhase.ToString();
                break;

            case CompostInoculated e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.Inoculations.Add(new KnfInputDto(
                    e.Input.InputType,
                    e.Input.Description,
                    e.Input.PreparedDate.ToString("yyyy-MM-dd"),
                    e.Input.Amount.Value.ToString("F2"),
                    e.Input.Amount.Unit));
                break;

            case CompostPhMeasured e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.PhLog.Add(new PhEntryDto(
                    e.Measurement.Date.ToString("yyyy-MM-dd"),
                    e.Measurement.PH,
                    e.Measurement.Notes,
                    GetPhStatus(e.Measurement.PH, b.Method)));
                break;

            case CompostNoteAdded e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.NoteLog.Add(new CompostNoteDto(
                    e.Note.Date.ToString("yyyy-MM-dd"),
                    e.Note.Category,
                    e.Note.Body));
                break;

            case CompostBatchCompleted e when batches.TryGetValue(e.Id.ToString(), out var b):
                b.Phase = CompostPhase.Finished.ToString();
                b.YieldCuYd = $"{e.YieldCuYd.Value} {e.YieldCuYd.Unit}";
                break;
        }
    }

    // ─── Mapping ────────────────────────────────────────────────────────────

    private static CompostBatchSummary ToSummary(BatchState b)
    {
        var lastTemp = b.TemperatureLog.Count > 0 ? b.TemperatureLog[^1] : null;
        var lastPh = b.PhLog.Count > 0 ? b.PhLog[^1] : null;
        return new CompostBatchSummary(
            b.Id, b.BatchCode, b.Method, b.Phase,
            CnRatioDisplay(b.CarbonRatio, b.NitrogenRatio),
            b.CarbonRatio, b.NitrogenRatio,
            lastTemp is null ? null : (double?)lastTemp.TemperatureF,
            lastTemp?.Timestamp,
            lastTemp?.Zone,
            b.TurnLog.Count,
            b.Inoculations.Count,
            b.NoteLog.Count,
            lastPh is null ? null : (decimal?)lastPh.PH,
            lastPh is null ? null : DateOnly.Parse(lastPh.Date),
            b.StartedAt,
            (int)(DateTimeOffset.UtcNow - b.StartedAt).TotalDays,
            b.Latitude, b.Longitude,
            b.YieldCuYd);
    }

    private static CompostBatchDetail ToDetail(BatchState b) => new(
        b.Id, b.BatchCode, b.Method, b.Phase,
        CnRatioDisplay(b.CarbonRatio, b.NitrogenRatio),
        b.CarbonRatio, b.NitrogenRatio,
        b.StartNotes, b.StartedAt,
        (int)(DateTimeOffset.UtcNow - b.StartedAt).TotalDays,
        b.Latitude, b.Longitude, b.YieldCuYd,
        b.Inputs, b.TemperatureLog, b.TurnLog, b.Inoculations, b.PhLog, b.NoteLog);

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Temperature zone classification per method:
    /// - HotAerobic: Optimal = 131-149°F (55-65°C); TooHot = 159°F+; TooLow = below 104°F
    /// - Vermicompost: Optimal = 65-95°F (18-35°C)
    /// - Bokashi/KNF: Fermentation (ambient, not heat-critical)
    /// </summary>
    private static string GetTempZone(decimal tempF, string method) => method switch
    {
        "HotAerobic" when tempF >= 131 && tempF <= 149 => "Optimal",
        "HotAerobic" when tempF > 149 => "TooHot",
        "HotAerobic" when tempF < 104 => "TooLow",
        "Vermicompost" when tempF >= 65 && tempF <= 95 => "Optimal",
        "Vermicompost" when tempF > 95 => "TooHot",
        "Vermicompost" when tempF < 50 => "TooLow",
        "Bokashi" or "KoreanNaturalFarming" => "Fermentation",
        _ => "Ambient"
    };

    /// <summary>
    /// pH status:
    /// - Bokashi: Optimal = 3.5-4.5 (successful fermentation)
    /// - General: 6.5-8.0 is neutral-acceptable range
    /// </summary>
    private static string GetPhStatus(decimal ph, string method) => method switch
    {
        "Bokashi" when ph >= 3.5m && ph <= 4.5m => "Optimal",
        "Bokashi" when ph > 4.5m => "TooHigh",
        "Bokashi" when ph < 3.5m => "TooLow",
        _ when ph >= 6.5m && ph <= 8.0m => "Neutral",
        _ when ph < 6.5m => "Acidic",
        _ => "Alkaline"
    };

    private static string? CnRatioDisplay(decimal? c, decimal? n) =>
        c.HasValue && n.HasValue && n > 0 ? $"{c / n:F0}:1" : null;

    // ─── Mutable state helper ───────────────────────────────────────────────

    private sealed class BatchState
    {
        public string Id { get; set; } = "";
        public string BatchCode { get; set; } = "";
        public string Method { get; set; } = "";
        public string Phase { get; set; } = "";
        public decimal? CarbonRatio { get; set; }
        public decimal? NitrogenRatio { get; set; }
        public string? StartNotes { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? YieldCuYd { get; set; }
        public List<CompostInputDto> Inputs { get; set; } = [];
        public List<TempReadingDto> TemperatureLog { get; set; } = [];
        public List<TurnEntryDto> TurnLog { get; set; } = [];
        public List<KnfInputDto> Inoculations { get; set; } = [];
        public List<PhEntryDto> PhLog { get; set; } = [];
        public List<CompostNoteDto> NoteLog { get; set; } = [];
    }
}
