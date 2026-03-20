using FarmOS.Apiary.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Apiary.Application.Commands;

public record CreateHiveCommand(string Name, HiveType Type, GeoPosition Position, DateOnly Established) : ICommand<Guid>;
public record InspectHiveCommand(Guid HiveId, InspectionData Data, DateOnly Date) : ICommand<Guid>;
public record HarvestHoneyCommand(Guid HiveId, HarvestData Data, DateOnly Date) : ICommand<Guid>;
public record TreatHiveCommand(Guid HiveId, TreatmentData Data) : ICommand<Guid>;
public record ChangeHiveStatusCommand(Guid HiveId, HiveStatus Status, string Reason) : ICommand<Guid>;

// ─── Feature 1: Apiary ──────────────────────────────────────────────
public record CreateApiaryCommand(string Name, GeoPosition Position, int MaxCapacity, string? Notes) : ICommand<Guid>;
public record MoveHiveToApiaryCommand(Guid HiveId, Guid ApiaryId) : ICommand<Guid>;
public record RetireApiaryCommand(Guid ApiaryId, string Reason) : ICommand<Guid>;

// ─── Feature 2: Queen Tracking ──────────────────────────────────────
public record IntroduceQueenCommand(Guid HiveId, QueenRecord Queen) : ICommand<Guid>;
public record MarkQueenLostCommand(Guid HiveId, string Reason, DateOnly Date) : ICommand<Guid>;
public record ReplaceQueenCommand(Guid HiveId, QueenRecord NewQueen, string Reason) : ICommand<Guid>;

// ─── Feature 3: Feeding ─────────────────────────────────────────────
public record FeedHiveCommand(Guid HiveId, FeedingData Data) : ICommand<Guid>;

// ─── Feature 6: Multi-Product Harvest ───────────────────────────────
public record HarvestProductCommand(Guid HiveId, ProductHarvestData Data) : ICommand<Guid>;

// ─── Feature 4: Colony Splitting & Merging ──────────────────────────
public record SplitColonyCommand(Guid OriginalHiveId, string NewHiveName, HiveType NewHiveType, GeoPosition NewPosition, DateOnly Date) : ICommand<Guid>;
public record MergeColoniesCommand(Guid SurvivingHiveId, Guid AbsorbedHiveId, DateOnly Date) : ICommand<Guid>;

// ─── Feature 5: Equipment/Super Tracking ────────────────────────────
public record AddSuperCommand(Guid HiveId) : ICommand<Guid>;
public record RemoveSuperCommand(Guid HiveId) : ICommand<Guid>;
public record UpdateHiveConfigurationCommand(Guid HiveId, HiveConfiguration Config) : ICommand<Guid>;
