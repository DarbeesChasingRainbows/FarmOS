using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Domain.Events;

// ─── Paddock Events ──────────────────────────────────────────────────

public record PaddockCreated(
    PaddockId PaddockId, string Name, Acreage Size, string LandType,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PaddockBoundaryUpdated(
    PaddockId PaddockId, GeoJsonGeometry Boundary,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record GrazingStarted(
    PaddockId PaddockId, HerdId HerdId, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record GrazingEnded(
    PaddockId PaddockId, HerdId HerdId, DateOnly Date, int DaysGrazed,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record BiomassUpdated(
    PaddockId PaddockId, BiomassEstimate Estimate,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record SoilTestRecorded(
    PaddockId PaddockId, SoilProfile Profile,
    DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Animal Events ───────────────────────────────────────────────────

public record AnimalRegistered(
    AnimalId AnimalId, IReadOnlyList<IdTag> Tags, Species Species, string? Breed,
    Sex Sex, DateOnly DateAcquired, string? Nickname,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalIsolated(
    AnimalId AnimalId, string Reason, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalReturnedToHerd(
    AnimalId AnimalId, HerdId HerdId, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TreatmentRecorded(
    AnimalId AnimalId, Treatment Treatment,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PregnancyRecorded(
    AnimalId AnimalId, PregnancyStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record BirthRecorded(
    AnimalId DamId, AnimalId OffspringId, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalButchered(
    AnimalId AnimalId, ButcherRecord Record,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalSold(
    AnimalId AnimalId, SaleRecord Record,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record WeightRecorded(
    AnimalId AnimalId, Quantity Weight, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalDeceased(
    AnimalId AnimalId, string Cause, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Herd Events ─────────────────────────────────────────────────────

public record HerdCreated(
    HerdId HerdId, string Name, HerdType Type,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record HerdMoved(
    HerdId HerdId, PaddockId? FromPaddockId, PaddockId ToPaddockId, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalAddedToHerd(
    HerdId HerdId, AnimalId AnimalId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AnimalRemovedFromHerd(
    HerdId HerdId, AnimalId AnimalId,
    DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Cross-Context Integration Events (published to RabbitMQ) ────────

public record MeatAvailable(
    string AnimalType, string? CutSheet, Quantity HangingWeight, DateOnly Date,
    DateTimeOffset OccurredAt) : IDomainEvent;
