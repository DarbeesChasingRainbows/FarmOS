using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Pasture.Application.Queries;

// ─── Query DTOs (Read Models) ────────────────────────────────────────

public record PaddockSummaryDto(
    Guid Id, string Name, decimal Acreage, string LandType,
    string Status, int RestDaysElapsed, Guid? CurrentHerdId);

public record PaddockDetailDto(
    Guid Id, string Name, decimal Acreage, string LandType,
    string Status, int RestDaysElapsed, Guid? CurrentHerdId,
    double[][]? BoundaryCoordinates,
    decimal? BiomassTPAcre, DateOnly? BiomassDate,
    decimal? SoilpH, decimal? SoilOM, DateOnly? SoilTestDate);

public record AnimalSummaryDto(
    Guid Id, string PrimaryTag, string Species, string? Breed,
    string Sex, string Status, string? Nickname, Guid? CurrentHerdId);

public record AnimalDetailDto(
    Guid Id, IReadOnlyList<TagDto> Tags, string Species, string? Breed,
    string Sex, string Status, string? Nickname,
    DateOnly DateAcquired, Guid? CurrentHerdId,
    PregnancyDto? Pregnancy, IReadOnlyList<WeightEntryDto> WeightHistory,
    IReadOnlyList<MedicalEntryDto> MedicalHistory);

public record TagDto(string Type, string Value);
public record PregnancyDto(DateOnly Confirmed, DateOnly ExpectedDue, Guid? SireId);
public record WeightEntryDto(decimal Value, string Unit, DateOnly Date);
public record MedicalEntryDto(DateOnly Date, string Diagnosis, string TreatmentName);

public record HerdSummaryDto(
    Guid Id, string Name, string Type, Guid? CurrentPaddockId, int MemberCount);

public record HerdDetailDto(
    Guid Id, string Name, string Type, Guid? CurrentPaddockId,
    IReadOnlyList<AnimalSummaryDto> Members);

// ─── Queries ─────────────────────────────────────────────────────────

public record GetPaddocksQuery() : IQuery<IReadOnlyList<PaddockSummaryDto>>;

public record GetPaddockByIdQuery(Guid PaddockId) : IQuery<PaddockDetailDto>;

public record GetAnimalsQuery(string? Species = null, string? Status = null)
    : IQuery<IReadOnlyList<AnimalSummaryDto>>;

public record GetAnimalByIdQuery(Guid AnimalId) : IQuery<AnimalDetailDto>;

public record GetHerdsQuery() : IQuery<IReadOnlyList<HerdSummaryDto>>;

public record GetHerdByIdQuery(Guid HerdId) : IQuery<HerdDetailDto>;
