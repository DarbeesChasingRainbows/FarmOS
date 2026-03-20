using FarmOS.SharedKernel;

namespace FarmOS.Pasture.Domain;

// ─── Typed IDs ───────────────────────────────────────────────────────

public record PaddockId(Guid Value)
{
    public static PaddockId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record AnimalId(Guid Value)
{
    public static AnimalId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record HerdId(Guid Value)
{
    public static HerdId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

// ─── Enums ───────────────────────────────────────────────────────────

public enum GrazingStatus { Resting, ActiveGrazing, Recovering }
public enum Species { Cattle, Sheep, Broiler, LayingHen, Goat, GuardDog }
public enum Sex { Male, Female }
public enum AnimalStatus { Active, Isolated, Sold, Butchered, Deceased }
public enum HerdType { Cattle, Sheep, BroilerTractor, Eggmobile, Mixed }

// ─── Pasture Value Objects ───────────────────────────────────────────

public record Acreage(decimal Value);

public record BiomassEstimate(decimal TonsPerAcre, DateOnly MeasuredOn, string Method);

public record SoilProfile(
    decimal pH, decimal OrganicMatterPct, decimal CarbonPct,
    DateOnly TestedOn, string? Lab = null);

public record CowDaysPerAcre(decimal Value, DateOnly CalculatedOn);

public record Treatment(
    string Name, string Dosage, string Route, DateOnly Date,
    string Notes, string? WithdrawalPeriodDays = null);

public record MedicalRecord(
    DateOnly Date, string Diagnosis, Treatment Treatment, string? Vet = null);

public record PregnancyStatus(DateOnly Confirmed, DateOnly ExpectedDue, AnimalId? SireId = null);

public record ButcherRecord(
    DateOnly Date, string Processor, Quantity HangingWeight, string? CutSheet = null);

public record SaleRecord(DateOnly Date, decimal Price, string Buyer, string? Notes = null);
