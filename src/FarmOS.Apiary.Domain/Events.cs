using FarmOS.SharedKernel;

namespace FarmOS.Apiary.Domain.Events;

public record HiveCreated(HiveId Id, string Name, HiveType Type, GeoPosition Position, DateOnly Established, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveInspected(HiveId Id, InspectionId InspectionId, InspectionData Data, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record HoneyHarvested(HiveId Id, HarvestData Data, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveTreated(HiveId Id, TreatmentData Data, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveStatusChanged(HiveId Id, HiveStatus Previous, HiveStatus Next, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveSwarmed(HiveId OriginalId, HiveId? NewHiveId, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 1: Apiary ──────────────────────────────────────────────
public record ApiaryCreated(ApiaryId Id, string Name, GeoPosition Position, int MaxCapacity, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveMovedToApiary(HiveId HiveId, ApiaryId? PreviousApiaryId, ApiaryId NewApiaryId, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveRemovedFromApiary(HiveId HiveId, ApiaryId ApiaryId, DateTimeOffset OccurredAt) : IDomainEvent;
public record ApiaryRetired(ApiaryId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 2: Queen Tracking ──────────────────────────────────────
public record QueenIntroduced(HiveId HiveId, QueenRecord Queen, DateTimeOffset OccurredAt) : IDomainEvent;
public record QueenLost(HiveId HiveId, string Reason, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record QueenReplaced(HiveId HiveId, QueenRecord NewQueen, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 3: Feeding ─────────────────────────────────────────────
public record HiveFed(HiveId Id, FeedingData Data, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 4: Colony Splitting & Merging ──────────────────────────
public record ColonySplit(HiveId OriginalId, HiveId NewHiveId, string NewHiveName, HiveType NewHiveType, GeoPosition NewPosition, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;
public record ColoniesMerged(HiveId SurvivingId, HiveId AbsorbedId, DateOnly Date, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 6: Multi-Product Harvest ───────────────────────────────
public record ProductHarvested(HiveId Id, ProductHarvestData Data, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 5: Equipment/Super Tracking ────────────────────────────
public record SuperAdded(HiveId Id, int NewSuperCount, DateTimeOffset OccurredAt) : IDomainEvent;
public record SuperRemoved(HiveId Id, int NewSuperCount, DateTimeOffset OccurredAt) : IDomainEvent;
public record HiveConfigurationChanged(HiveId Id, HiveConfiguration Config, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Feature 11: Weather ────────────────────────────────────────────
public record WeatherRecordedWithInspection(HiveId HiveId, InspectionId InspectionId, WeatherSnapshot Weather, DateTimeOffset OccurredAt) : IDomainEvent;
