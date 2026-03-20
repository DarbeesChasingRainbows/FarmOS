using FarmOS.Pasture.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Domain.Aggregates;

/// <summary>
/// Paddock aggregate root. Governs a physical land unit with grazing state,
/// rest period enforcement (45-day minimum), biomass estimates, and soil profiles.
/// </summary>
public sealed class Paddock : AggregateRoot<PaddockId>
{
    public string Name { get; private set; } = string.Empty;
    public Acreage Size { get; private set; } = new(0);
    public GrazingStatus Status { get; private set; } = GrazingStatus.Resting;
    public string LandType { get; private set; } = "Pasture";
    public GeoJsonGeometry? Boundary { get; private set; }
    public DateOnly? LastGrazedDate { get; private set; }
    public HerdId? CurrentHerdId { get; private set; }
    public BiomassEstimate? CurrentBiomass { get; private set; }
    public SoilProfile? LatestSoilProfile { get; private set; }

    public int RestDaysElapsed => LastGrazedDate.HasValue
        ? DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - LastGrazedDate.Value.DayNumber
        : int.MaxValue; // Never grazed = infinite rest

    // ─── Commands ────────────────────────────────────────────────

    public static Paddock Create(string name, Acreage size, string landType)
    {
        var paddock = new Paddock();
        paddock.RaiseEvent(new PaddockCreated(
            PaddockId.New(), name, size, landType, DateTimeOffset.UtcNow));
        return paddock;
    }

    public void UpdateBoundary(GeoJsonGeometry boundary)
    {
        RaiseEvent(new PaddockBoundaryUpdated(Id, boundary, DateTimeOffset.UtcNow));
    }

    public Result<PaddockId, DomainError> BeginGrazing(HerdId herdId, DateOnly date)
    {
        if (Status == GrazingStatus.ActiveGrazing)
            return DomainError.Conflict("Paddock is already being grazed.");

        const int minimumRestDays = 45;
        if (RestDaysElapsed < minimumRestDays)
            return DomainError.BusinessRule(
                $"Paddock needs {minimumRestDays - RestDaysElapsed} more rest days (minimum {minimumRestDays}).");

        RaiseEvent(new GrazingStarted(Id, herdId, date, DateTimeOffset.UtcNow));
        return Id;
    }

    public void EndGrazing(DateOnly date)
    {
        if (Status != GrazingStatus.ActiveGrazing)
            return;

        var daysGrazed = LastGrazedDate.HasValue
            ? date.DayNumber - LastGrazedDate.Value.DayNumber
            : 0;

        RaiseEvent(new GrazingEnded(Id, CurrentHerdId!, date, daysGrazed, DateTimeOffset.UtcNow));
    }

    public void UpdateBiomass(BiomassEstimate estimate)
    {
        RaiseEvent(new BiomassUpdated(Id, estimate, DateTimeOffset.UtcNow));
    }

    public void RecordSoilTest(SoilProfile profile)
    {
        RaiseEvent(new SoilTestRecorded(Id, profile, DateTimeOffset.UtcNow));
    }

    // ─── Event Application ───────────────────────────────────────

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaddockCreated e:
                Id = e.PaddockId;
                Name = e.Name;
                Size = e.Size;
                LandType = e.LandType;
                Status = GrazingStatus.Resting;
                break;

            case PaddockBoundaryUpdated e:
                Boundary = e.Boundary;
                break;

            case GrazingStarted e:
                Status = GrazingStatus.ActiveGrazing;
                CurrentHerdId = e.HerdId;
                LastGrazedDate = e.Date;
                break;

            case GrazingEnded e:
                Status = GrazingStatus.Resting;
                CurrentHerdId = null;
                LastGrazedDate = e.Date;
                break;

            case BiomassUpdated e:
                CurrentBiomass = e.Estimate;
                break;

            case SoilTestRecorded e:
                LatestSoilProfile = e.Profile;
                break;
        }
    }
}
