using FarmOS.Pasture.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Domain.Aggregates;

/// <summary>
/// Animal aggregate root. Tracks individual livestock through their full lifecycle:
/// registration, herd membership, medical history, pregnancy, birth, butchering, and sale.
/// </summary>
public sealed class Animal : AggregateRoot<AnimalId>
{
    public List<IdTag> Tags { get; private set; } = [];
    public Species Species { get; private set; }
    public string? Breed { get; private set; }
    public AnimalStatus Status { get; private set; } = AnimalStatus.Active;
    public Sex Sex { get; private set; }
    public DateOnly DateAcquired { get; private set; }
    public string? Nickname { get; private set; }
    public AnimalId? DamId { get; private set; }
    public AnimalId? SireId { get; private set; }
    public HerdId? CurrentHerdId { get; private set; }
    public List<MedicalRecord> MedicalHistory { get; private set; } = [];
    public PregnancyStatus? Pregnancy { get; private set; }
    public List<(Quantity Weight, DateOnly Date)> WeightHistory { get; private set; } = [];

    // ─── Commands ────────────────────────────────────────────────

    public static Animal Register(
        IReadOnlyList<IdTag> tags, Species species, string? breed,
        Sex sex, DateOnly dateAcquired, string? nickname = null)
    {
        var animal = new Animal();
        animal.RaiseEvent(new AnimalRegistered(
            AnimalId.New(), tags, species, breed, sex, dateAcquired, nickname,
            DateTimeOffset.UtcNow));
        return animal;
    }

    public Result<AnimalId, DomainError> Isolate(string reason, DateOnly date)
    {
        if (Status != AnimalStatus.Active)
            return DomainError.Conflict($"Cannot isolate animal with status '{Status}'.");

        RaiseEvent(new AnimalIsolated(Id, reason, date, DateTimeOffset.UtcNow));
        return Id;
    }

    public void ReturnToHerd(HerdId herdId, DateOnly date)
    {
        if (Status == AnimalStatus.Isolated)
            RaiseEvent(new AnimalReturnedToHerd(Id, herdId, date, DateTimeOffset.UtcNow));
    }

    public void RecordTreatment(Treatment treatment)
    {
        RaiseEvent(new TreatmentRecorded(Id, treatment, DateTimeOffset.UtcNow));
    }

    public Result<AnimalId, DomainError> RecordPregnancy(DateOnly expectedDue, AnimalId? sireId = null)
    {
        if (Sex != Sex.Female)
            return DomainError.BusinessRule("Only female animals can be pregnant.");
        if (Pregnancy is not null)
            return DomainError.Conflict("Animal already has an active pregnancy recorded.");

        RaiseEvent(new PregnancyRecorded(Id,
            new PregnancyStatus(DateOnly.FromDateTime(DateTime.UtcNow), expectedDue, sireId),
            DateTimeOffset.UtcNow));
        return Id;
    }

    public void RecordBirth(AnimalId offspringId, DateOnly date)
    {
        RaiseEvent(new BirthRecorded(Id, offspringId, date, DateTimeOffset.UtcNow));
    }

    public Result<AnimalId, DomainError> Butcher(ButcherRecord record)
    {
        if (Status is AnimalStatus.Sold or AnimalStatus.Butchered or AnimalStatus.Deceased)
            return DomainError.Conflict($"Cannot butcher animal with status '{Status}'.");

        RaiseEvent(new AnimalButchered(Id, record, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<AnimalId, DomainError> Sell(SaleRecord record)
    {
        if (Status is AnimalStatus.Sold or AnimalStatus.Butchered or AnimalStatus.Deceased)
            return DomainError.Conflict($"Cannot sell animal with status '{Status}'.");

        RaiseEvent(new AnimalSold(Id, record, DateTimeOffset.UtcNow));
        return Id;
    }

    public void RecordWeight(Quantity weight, DateOnly date)
    {
        RaiseEvent(new WeightRecorded(Id, weight, date, DateTimeOffset.UtcNow));
    }

    public void MarkDeceased(string cause, DateOnly date)
    {
        if (Status is not (AnimalStatus.Sold or AnimalStatus.Butchered or AnimalStatus.Deceased))
            RaiseEvent(new AnimalDeceased(Id, cause, date, DateTimeOffset.UtcNow));
    }

    // ─── Event Application ─────────────────────────────────────

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AnimalRegistered e:
                Id = e.AnimalId;
                Tags = [.. e.Tags];
                Species = e.Species;
                Breed = e.Breed;
                Sex = e.Sex;
                DateAcquired = e.DateAcquired;
                Nickname = e.Nickname;
                Status = AnimalStatus.Active;
                break;

            case AnimalIsolated:
                Status = AnimalStatus.Isolated;
                CurrentHerdId = null;
                break;

            case AnimalReturnedToHerd e:
                Status = AnimalStatus.Active;
                CurrentHerdId = e.HerdId;
                break;

            case TreatmentRecorded e:
                MedicalHistory.Add(new MedicalRecord(e.Treatment.Date, "Treatment", e.Treatment));
                break;

            case PregnancyRecorded e:
                Pregnancy = e.Status;
                break;

            case BirthRecorded:
                Pregnancy = null; // Pregnancy resolved
                break;

            case AnimalButchered:
                Status = AnimalStatus.Butchered;
                CurrentHerdId = null;
                break;

            case AnimalSold:
                Status = AnimalStatus.Sold;
                CurrentHerdId = null;
                break;

            case WeightRecorded e:
                WeightHistory.Add((e.Weight, e.Date));
                break;

            case AnimalDeceased:
                Status = AnimalStatus.Deceased;
                CurrentHerdId = null;
                break;
        }
    }
}
