using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Queries;

// ─── DTOs ──────────────────────────────────────────────────────────────────

public record PHTimelinePointDto(
    DateTimeOffset Timestamp,
    decimal PH,
    decimal? DeltaPerHour);

public record FermentationSafetyStatusDto(
    bool IsSafe,
    decimal CurrentPH,
    decimal TargetPH,
    decimal? DropRatePerHour,
    decimal? EstimatedHoursToSafe,
    decimal Confidence,
    string Message);

public record FermentationAnalyticsDto(
    Guid BatchId,
    string BatchCode,
    string ProductType,
    string Phase,
    decimal? CurrentPH,
    FermentationSafetyStatusDto SafetyStatus,
    List<PHTimelinePointDto> Timeline);

public record ActiveFermentationMonitorDto(
    Guid BatchId,
    string BatchCode,
    string ProductType,
    string Phase,
    decimal? CurrentPH,
    decimal? DropRatePerHour,
    bool IsSafe,
    string StatusMessage);

// ─── Queries ───────────────────────────────────────────────────────────────

public record GetFermentationPHTimelineQuery(Guid BatchId) : IQuery<FermentationAnalyticsDto>;

public record GetActiveFermentationMonitoringQuery() : IQuery<List<ActiveFermentationMonitorDto>>;
