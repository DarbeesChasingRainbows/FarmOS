using FarmOS.IoT.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.IoT.Application.Queries;

// ─── DTOs ──────────────────────────────────────────────────────────────────

public record ClimateLogEntryDto(
    DateTimeOffset Timestamp,
    string DeviceCode,
    SensorType SensorType,
    decimal Value,
    string Unit);

public record ZoneClimateLogDto(
    Guid ZoneId,
    string ZoneName,
    ZoneType ZoneType,
    DateTimeOffset From,
    DateTimeOffset To,
    List<ClimateLogEntryDto> Readings);

public record ComplianceViolationDto(
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    TimeSpan? Duration,
    string DeviceCode,
    SensorType SensorType,
    decimal Value,
    decimal ThresholdLimit,
    string Severity,
    string AlertMessage);

public record ZoneComplianceReportDto(
    Guid ZoneId,
    string ZoneName,
    ZoneType ZoneType,
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalReadings,
    int ViolationCount,
    bool IsCompliant,
    List<ComplianceViolationDto> Violations);

public record ActiveExcursionDto(
    Guid ExcursionId,
    Guid DeviceId,
    string DeviceCode,
    Guid ZoneId,
    SensorType SensorType,
    decimal LastValue,
    decimal ThresholdLimit,
    string ThresholdDirection,
    DateTimeOffset StartedAt,
    TimeSpan Duration);

// ─── Queries ───────────────────────────────────────────────────────────────

public record GetZoneClimateLogQuery(
    Guid ZoneId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IQuery<ZoneClimateLogDto>;

public record GetZoneComplianceReportQuery(
    Guid ZoneId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IQuery<ZoneComplianceReportDto>;

public record GetActiveExcursionsQuery() : IQuery<List<ActiveExcursionDto>>;
