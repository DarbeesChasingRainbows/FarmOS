using FarmOS.SharedKernel;

namespace FarmOS.Apiary.Domain;

public record HiveId(Guid Value) { public static HiveId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record InspectionId(Guid Value) { public static InspectionId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record ApiaryId(Guid Value) { public static ApiaryId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum HiveType { Langstroth, TopBar, Warre }
public enum ApiaryStatus { Active, Retired }
public enum MarkedColor { White, Yellow, Red, Green, Blue }
public enum QueenOrigin { Purchased, Raised, Swarm }
public enum FeedType { SugarSyrup, Fondant, PollenPatty, Other }

public record QueenRecord(MarkedColor? Color, QueenOrigin Origin, DateOnly IntroducedDate, string? Breed, string? Notes);

public record FeedingData(FeedType FeedType, Quantity Amount, string? Concentration, DateOnly Date, string? Notes);

// ─── Feature 5: Equipment/Super Tracking ────────────────────────────
public enum FrameType { LangstrothDeep, LangstrothMedium, LangstrothShallow, TopBar, Warre }
public record HiveConfiguration(int BroodBoxes, int HoneySupers, FrameType FrameType, bool ExcluderInstalled);

// ─── Feature 6: Multi-Product Harvest ───────────────────────────────
public enum ProductType { Honey, Wax, Pollen, Propolis, RoyalJelly, NucSale }
public record ProductHarvestData(ProductType Product, Quantity Yield, DateOnly Date, string? Method, string? Notes, decimal? MoisturePercent);

// ─── Feature 11: Weather ────────────────────────────────────────────
public record WeatherSnapshot(decimal TempF, decimal Humidity, decimal? WindMph, string? Conditions, string? Source);
public enum HiveStatus { Active, Queenless, Weak, Swarmed, Dead, Wintering }
public enum QueenStatus { Present, Queenless, QueenCells, NewQueen, Uncertain }
public enum MiteLevel { None, Low, Medium, High, Critical }

public record InspectionData(
    QueenStatus QueenStatus, int BroodFrames, int HoneyFrames,
    string Temperament, MiteLevel MiteLevel, decimal? MiteCount,
    string? Notes);

public record HarvestData(
    Quantity HoneyWeight, Quantity? WaxWeight, string ExtractionMethod,
    decimal? MoisturePercent);

public record TreatmentData(
    string ProductName, string TargetPest, DateOnly StartDate,
    DateOnly? EndDate, string Method, string? Notes);
