using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel.CQRS;
using FsAnalytics = FarmOS.Hearth.Rules.FermentationAnalytics;

namespace FarmOS.Hearth.Application.Queries.Handlers;

/// <summary>
/// Handles fermentation analytics queries by loading batches from the event store
/// and evaluating pH trajectories using the F# FermentationAnalytics rules engine.
/// </summary>
public sealed class FermentationAnalyticsHandlers(IHearthEventStore store) :
    IQueryHandler<GetFermentationPHTimelineQuery, FermentationAnalyticsDto>,
    IQueryHandler<GetActiveFermentationMonitoringQuery, List<ActiveFermentationMonitorDto>>
{
    public async Task<FermentationAnalyticsDto?> Handle(
        GetFermentationPHTimelineQuery request, CancellationToken ct)
    {
        // Try loading as kombucha first (most common fermentation with pH tracking)
        try
        {
            var batch = await store.LoadKombuchaAsync(request.BatchId.ToString(), ct);
            if (batch.Version > 0)
                return BuildKombuchaAnalytics(batch);
        }
        catch { /* Not a kombucha batch */ }

        return null;
    }

    public async Task<List<ActiveFermentationMonitorDto>?> Handle(
        GetActiveFermentationMonitoringQuery request, CancellationToken ct)
    {
        // This would ideally query a projection of active batches.
        // For now, return an empty list — the projection worker will populate this.
        return new List<ActiveFermentationMonitorDto>();
    }

    private static FermentationAnalyticsDto BuildKombuchaAnalytics(KombuchaBatch batch)
    {
        var productType = batch.Type == KombuchaType.Jun
            ? FsAnalytics.ProductType.Jun
            : FsAnalytics.ProductType.Kombucha;

        // Convert C# PHReading list → F# PHDataPoint list
        var fsPoints = batch.PHLog
            .Select(r => new FsAnalytics.PHDataPoint(r.Timestamp, r.pH))
            .ToList();

        var fsList = Microsoft.FSharp.Collections.ListModule.OfSeq(fsPoints);

        // Evaluate trajectory via F# rules engine
        var safetyResult = FsAnalytics.evaluateTrajectory(productType, fsList);

        // Build timeline with delta per hour
        var timeline = new List<PHTimelinePointDto>();
        for (int i = 0; i < batch.PHLog.Count; i++)
        {
            var reading = batch.PHLog[i];
            decimal? delta = null;
            if (i > 0)
            {
                var prev = batch.PHLog[i - 1];
                var hours = (decimal)(reading.Timestamp - prev.Timestamp).TotalHours;
                if (hours > 0)
                    delta = (prev.pH - reading.pH) / hours;
            }
            timeline.Add(new PHTimelinePointDto(reading.Timestamp, reading.pH, delta));
        }

        var safetyDto = new FermentationSafetyStatusDto(
            safetyResult.IsSafe,
            safetyResult.CurrentPH,
            safetyResult.TargetPH,
            Microsoft.FSharp.Core.FSharpOption<decimal>.get_IsNone(safetyResult.DropRatePerHour)
                ? null : safetyResult.DropRatePerHour.Value,
            Microsoft.FSharp.Core.FSharpOption<decimal>.get_IsNone(safetyResult.EstimatedHoursToSafe)
                ? null : safetyResult.EstimatedHoursToSafe.Value,
            safetyResult.Confidence,
            safetyResult.Message);

        return new FermentationAnalyticsDto(
            batch.Id.Value,
            batch.BatchCode,
            batch.Type.ToString(),
            batch.Phase.ToString(),
            batch.CurrentPH,
            safetyDto,
            timeline);
    }
}
