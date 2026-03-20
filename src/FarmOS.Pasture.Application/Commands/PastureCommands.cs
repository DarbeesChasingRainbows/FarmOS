using FarmOS.Pasture.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Pasture.Application.Commands;

// ─── Paddock Commands ────────────────────────────────────────────────

public record CreatePaddockCommand(
    string Name, decimal Acreage, string LandType) : ICommand<Guid>;

public record UpdatePaddockBoundaryCommand(
    Guid PaddockId, GeoJsonGeometry Boundary) : ICommand<Guid>;

public record BeginGrazingCommand(
    Guid PaddockId, Guid HerdId, DateOnly Date) : ICommand<Guid>;

public record EndGrazingCommand(
    Guid PaddockId, DateOnly Date) : ICommand<Guid>;

public record UpdateBiomassCommand(
    Guid PaddockId, decimal TonsPerAcre, DateOnly MeasuredOn, string Method) : ICommand<Guid>;

public record RecordSoilTestCommand(
    Guid PaddockId, decimal pH, decimal OrganicMatterPct, decimal CarbonPct,
    DateOnly TestedOn, string? Lab) : ICommand<Guid>;

// ─── Animal Commands ─────────────────────────────────────────────────

public record RegisterAnimalCommand(
    IReadOnlyList<IdTag> Tags, Species Species, string? Breed,
    Sex Sex, DateOnly DateAcquired, string? Nickname) : ICommand<Guid>;

public record IsolateAnimalCommand(
    Guid AnimalId, string Reason, DateOnly Date) : ICommand<Guid>;

public record ReturnAnimalToHerdCommand(
    Guid AnimalId, Guid HerdId, DateOnly Date) : ICommand<Guid>;

public record RecordTreatmentCommand(
    Guid AnimalId, string Name, string Dosage, string Route,
    DateOnly Date, string Notes, string? WithdrawalPeriodDays) : ICommand<Guid>;

public record RecordPregnancyCommand(
    Guid AnimalId, DateOnly ExpectedDue, Guid? SireId) : ICommand<Guid>;

public record RecordBirthCommand(
    Guid DamId, Guid OffspringId, DateOnly Date) : ICommand<Guid>;

public record ButcherAnimalCommand(
    Guid AnimalId, DateOnly Date, string Processor,
    Quantity HangingWeight, string? CutSheet) : ICommand<Guid>;

public record SellAnimalCommand(
    Guid AnimalId, DateOnly Date, decimal Price,
    string Buyer, string? Notes) : ICommand<Guid>;

public record RecordWeightCommand(
    Guid AnimalId, Quantity Weight, DateOnly Date) : ICommand<Guid>;

// ─── Herd Commands ───────────────────────────────────────────────────

public record CreateHerdCommand(
    string Name, HerdType Type) : ICommand<Guid>;

public record MoveHerdCommand(
    Guid HerdId, Guid PaddockId, DateOnly Date) : ICommand<Guid>;

public record AddAnimalToHerdCommand(
    Guid HerdId, Guid AnimalId) : ICommand<Guid>;

public record RemoveAnimalFromHerdCommand(
    Guid HerdId, Guid AnimalId) : ICommand<Guid>;
