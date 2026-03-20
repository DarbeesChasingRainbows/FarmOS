# Domain Models — FarmOS (Complete)

> All aggregate roots, value objects, domain events, and F# rule modules across all bounded contexts.
> Includes cross-cutting types: `Quantity`, `GeoJsonGeometry`, `FileAttachment`, `Observation`.

---

## Cross-Cutting Value Objects (SharedKernel)

These types are used across all bounded contexts.

```csharp
// === Identity ===
public record TenantId(Guid Value);     // Always present. Sovereign mode = single hardcoded GUID.

// === Universal measurement ===
public record Quantity(decimal Value, string Unit, string Measure, string? Label = null);
// Examples:
//   new Quantity(42.7m, "percent", "ratio", "Soil Moisture")
//   new Quantity(350m, "lbs", "weight", "Hanging Weight")
//   new Quantity(24, "loaves", "count", "Sourdough Production")

// === GIS ===
public record GeoJsonGeometry(string Type, double[][] Coordinates);
// Type: "Point", "Polygon", "LineString"
// Point: [[lng, lat]]   Polygon: [[lng,lat], [lng,lat], ...]

public record GeoPosition(double Latitude, double Longitude, double? Elevation = null);

// === Files & Photos ===
public record FileAttachment(Guid Id, string FileName, string ContentType, long SizeBytes,
    string StoragePath, DateTimeOffset UploadedAt);
public record ImageAttachment(Guid Id, string FileName, string ContentType, long SizeBytes,
    string StoragePath, int Width, int Height, DateTimeOffset UploadedAt);

// === ID Tags (multiple identification schemes) ===
public record IdTag(string Type, string Value);
// Examples: ("ear_tag", "047"), ("eid", "840003123456789"), ("leg_band", "B-12")

// === Temporal ===
public record DateRange(DateOnly Start, DateOnly End);
public record Season(int Year, string Name);  // e.g., (2026, "Spring")
```

---

## 1. Pasture Context

### Aggregate Roots

#### `Paddock`

```csharp
public sealed class Paddock : AggregateRoot<PaddockId>
{
    public string Name { get; private set; }
    public Acreage Size { get; private set; }
    public GrazingStatus Status { get; private set; }         // Resting | ActiveGrazing | Recovering
    public string LandType { get; private set; }              // Pasture | Silvopasture | Sacrifice | Riparian
    public GeoJsonGeometry? Boundary { get; private set; }    // GIS polygon
    public DateOnly? LastGrazedDate { get; private set; }
    public int RestDaysElapsed { get; private set; }
    public BiomassEstimate? CurrentBiomass { get; private set; }
    public SoilProfile? Soil { get; private set; }
    public IReadOnlyList<ImageAttachment> Images { get; }

    public Result<Unit, DomainError> BeginGrazing(HerdId herdId, DateOnly date);
    public void EndGrazing(DateOnly date);
    public void UpdateBiomass(BiomassEstimate estimate);
    public void RecordSoilTest(SoilProfile profile);
    public void UpdateBoundary(GeoJsonGeometry boundary);
}
```

#### `Animal`

```csharp
public sealed class Animal : AggregateRoot<AnimalId>
{
    public IReadOnlyList<IdTag> Tags { get; }                 // Multiple ID schemes
    public Species Species { get; private set; }              // Cattle | Sheep | Broiler | LayingHen | GooseDog
    public string? Breed { get; private set; }
    public AnimalStatus Status { get; private set; }          // Active | Isolated | Sold | Butchered | Deceased
    public Sex Sex { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public DateOnly DateAcquired { get; private set; }
    public bool IsSterile { get; private set; }
    public string? Nickname { get; private set; }
    public AnimalId? DamId { get; private set; }
    public AnimalId? SireId { get; private set; }
    public HerdId? CurrentHerdId { get; private set; }
    public GeoPosition? LastKnownPosition { get; private set; }
    public IReadOnlyList<MedicalRecord> MedicalHistory { get; }
    public PregnancyStatus? Pregnancy { get; private set; }
    public IReadOnlyList<ImageAttachment> Images { get; }

    public void Isolate(string reason, DateOnly date);
    public void RecordTreatment(Treatment treatment);
    public void RecordPregnancy(DateOnly expectedDue, AnimalId? sireId);
    public void RecordBirth(AnimalId offspringId, DateOnly date);
    public void Butcher(ButcherRecord record);
    public void Sell(SaleRecord record);
    public void RecordWeight(Quantity weight, DateOnly date);
}
```

#### `Herd`

```csharp
public sealed class Herd : AggregateRoot<HerdId>
{
    public string Name { get; private set; }
    public HerdType Type { get; private set; }                // Cattle | Sheep | BroilerTractor | Eggmobile
    public PaddockId? CurrentPaddockId { get; private set; }
    public IReadOnlyList<AnimalId> Members { get; }

    public void MoveToPaddock(PaddockId target, DateOnly date);
    public void AddAnimal(AnimalId animalId);
    public void RemoveAnimal(AnimalId animalId);
}
```

### Value Objects (Pasture)

```csharp
public record Acreage(decimal Value);
public record BiomassEstimate(decimal TonsPerAcre, DateOnly MeasuredOn, string Method);
public record SoilProfile(decimal pH, decimal OrganicMatterPct, decimal CarbonPct, DateOnly TestedOn, string? Lab);
public record CowDaysPerAcre(decimal Value, DateOnly CalculatedOn);
public record Treatment(string Name, string Dosage, string Route, DateOnly Date, string Notes, string? WithdrawalPeriodDays);
public record MedicalRecord(DateOnly Date, string Diagnosis, Treatment Treatment, string? Vet);
public record PregnancyStatus(DateOnly Confirmed, DateOnly ExpectedDue, AnimalId? SireId);
public record ButcherRecord(DateOnly Date, string Processor, Quantity HangingWeight, string? CutSheet);
public record SaleRecord(DateOnly Date, decimal Price, string Buyer, string? Notes);
```

### Domain Events (Pasture)

```csharp
public record PaddockCreated(PaddockId Id, string Name, Acreage Size, string LandType);
public record GrazingStarted(PaddockId PaddockId, HerdId HerdId, DateOnly Date);
public record GrazingEnded(PaddockId PaddockId, HerdId HerdId, DateOnly Date, int DaysGrazed);
public record AnimalRegistered(AnimalId Id, IReadOnlyList<IdTag> Tags, Species Species, Sex Sex, DateOnly Acquired);
public record AnimalIsolated(AnimalId Id, string Reason, DateOnly Date);
public record TreatmentRecorded(AnimalId Id, Treatment Treatment);
public record AnimalButchered(AnimalId Id, ButcherRecord Record);
public record AnimalSold(AnimalId Id, SaleRecord Record);
public record HerdMoved(HerdId Id, PaddockId? From, PaddockId To, DateOnly Date);
public record PregnancyRecorded(AnimalId Id, PregnancyStatus Status);
public record BirthRecorded(AnimalId DamId, AnimalId OffspringId, DateOnly Date);
public record WeightRecorded(AnimalId Id, Quantity Weight, DateOnly Date);
public record BiomassUpdated(PaddockId Id, BiomassEstimate Estimate);
public record SoilTestRecorded(PaddockId Id, SoilProfile Profile);
// Cross-context (published to RabbitMQ)
public record MeatAvailable(string AnimalType, string CutSheet, Quantity HangingWeight, DateOnly Date);
```

### F# Rules (Pasture)

```fsharp
module FarmOS.Pasture.Rules.GrazingRules

let minimumRestDays = 45

let canBeginGrazing (restDaysElapsed: int) (status: GrazingStatus) =
    match status with
    | ActiveGrazing -> Error "Paddock is already being grazed"
    | _ when restDaysElapsed < minimumRestDays ->
        Error $"Needs {minimumRestDays - restDaysElapsed} more rest days"
    | _ -> Ok ()

let calculateCowDaysPerAcre (count: int) (days: int) (acres: decimal) =
    (decimal count * decimal days) / acres
```

---

## 2. Flora Context

### Aggregate Roots

#### `OrchardGuild`

```csharp
public sealed class OrchardGuild : AggregateRoot<OrchardGuildId>
{
    public string Name { get; private set; }
    public GuildType Type { get; private set; }               // NAP | Trio | Custom
    public GeoPosition Position { get; private set; }
    public GeoJsonGeometry? Boundary { get; private set; }
    public IReadOnlyList<GuildMember> Members { get; }
    public DateOnly Planted { get; private set; }
    public IReadOnlyList<ImageAttachment> Images { get; }
}

public record GuildMember(PlantId PlantId, string Species, string Cultivar, GuildRole Role);
public enum GuildRole { NitrogenFixer, PrimaryFruit, SecondaryFruit, DynamicAccumulator, Pollinator, PestRepellent, GroundCover }
```

#### `FlowerBed`

```csharp
public sealed class FlowerBed : AggregateRoot<FlowerBedId>
{
    public string Name { get; private set; }                  // "Bed A3-Row 2"
    public string Block { get; private set; }                 // "Block A"
    public BedDimensions Dimensions { get; private set; }
    public GeoJsonGeometry? Geometry { get; private set; }
    public IReadOnlyList<Succession> Successions { get; }

    public void PlanSuccession(CropVariety variety, DateOnly sowDate, DateOnly transplantDate, DateOnly harvestStart);
    public void RecordSeeding(SuccessionId id, DateOnly date, SeedLotId seedLot, Quantity quantity);
    public void RecordTransplant(SuccessionId id, DateOnly date, Quantity quantity);
    public void RecordHarvest(SuccessionId id, Quantity stems, DateOnly date);
}

public record BedDimensions(decimal LengthFeet, decimal WidthFeet);
public record CropVariety(string Species, string Cultivar, int DaysToMaturity, string? Color);
public record Succession(SuccessionId Id, CropVariety Variety, DateOnly SowDate,
    DateOnly TransplantDate, DateOnly HarvestWindowStart, DateOnly? HarvestWindowEnd,
    IReadOnlyList<Quantity> Harvests);
```

#### `SeedLot`

```csharp
public sealed class SeedLot : AggregateRoot<SeedLotId>
{
    public CropVariety Variety { get; private set; }
    public string Supplier { get; private set; }
    public Quantity QuantityOnHand { get; private set; }
    public decimal GerminationRatePct { get; private set; }
    public int HarvestYear { get; private set; }
    public bool IsOrganic { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public string? LotNumber { get; private set; }

    public void Withdraw(Quantity quantity, FlowerBedId destination);
    public void Restock(Quantity quantity, string? newLotNumber);
}
```

### Domain Events (Flora)

```csharp
public record GuildPlanted(OrchardGuildId Id, string Name, GuildType Type, DateOnly Date);
public record GuildMemberAdded(OrchardGuildId GuildId, GuildMember Member);
public record SuccessionPlanned(FlowerBedId BedId, SuccessionId SuccId, CropVariety Variety, DateOnly SowDate);
public record SeedingRecorded(FlowerBedId BedId, SuccessionId SuccId, SeedLotId SeedLot, Quantity Qty, DateOnly Date);
public record TransplantRecorded(FlowerBedId BedId, SuccessionId SuccId, Quantity Qty, DateOnly Date);
public record FlowerHarvestRecorded(FlowerBedId BedId, SuccessionId SuccId, Quantity Stems, DateOnly Date);
public record SeedWithdrawn(SeedLotId LotId, Quantity Qty, FlowerBedId Destination);
// Cross-context
public record StemsAvailable(string Variety, int StemCount, DateOnly Date);
```

---

## 3. Hearth Context

### Aggregate Roots

#### `SourdoughBatch`

```csharp
public sealed class SourdoughBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; }
    public LivingCultureId StarterId { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients { get; }
    public IReadOnlyList<HACCPReading> CCPReadings { get; }
    public BatchPhase Phase { get; private set; }             // Mixing | BulkFerment | Shaping | Proofing | Baking | Cooling | Complete
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Quantity? Yield { get; private set; }              // e.g., (24, "loaves", "count")
    public AssetId? OvenId { get; private set; }              // Equipment reference
    public IReadOnlyList<FileAttachment> Documents { get; }   // HACCP printouts

    public void RecordCCPReading(HACCPReading reading);
    public void AdvancePhase(BatchPhase next, DateTimeOffset timestamp);
    public void Complete(Quantity yield);
}

public record HACCPReading(DateTimeOffset Timestamp, string CriticalControlPoint,
    decimal TemperatureF, decimal? pH, bool WithinLimits, string? CorrectiveAction);
public record Ingredient(string Name, Quantity Amount, string? LotNumber, string? Supplier);
public enum BatchPhase { Mixing, BulkFerment, Shaping, Proofing, Baking, Cooling, Complete, Discarded }
```

#### `KombuchaBatch`

```csharp
public sealed class KombuchaBatch : AggregateRoot<BatchId>
{
    public string BatchCode { get; private set; }
    public KombuchaType Type { get; private set; }            // Jun | Standard
    public LivingCultureId SCOBYId { get; private set; }
    public FermentationPhase Phase { get; private set; }      // Primary | Secondary | Bottled | Complete | Discarded
    public decimal StartingPH { get; private set; }
    public decimal? CurrentPH { get; private set; }
    public IReadOnlyList<PHReading> PHLog { get; }
    public decimal? AlcoholContentPct { get; private set; }
    public string TeaType { get; private set; }
    public string Sweetener { get; private set; }
    public Quantity Volume { get; private set; }              // e.g., (5, "gallons", "volume")
    public IReadOnlyList<Flavoring> SecondaryFlavorings { get; }
    public DateTimeOffset StartedAt { get; private set; }

    public void RecordPHReading(decimal pH, DateTimeOffset timestamp);
    public void AddFlavoring(Flavoring flavoring);
    public void Bottle(Quantity bottleCount);
    public void DiscardBatch(string reason);
}

public record PHReading(DateTimeOffset Timestamp, decimal pH, string? Notes);
public record Flavoring(string Ingredient, Quantity Amount);
public enum KombuchaType { Jun, Standard }
public enum FermentationPhase { Primary, Secondary, Bottled, Complete, Discarded }
```

#### `LivingCulture`

```csharp
public sealed class LivingCulture : AggregateRoot<LivingCultureId>
{
    public string Name { get; private set; }
    public CultureType Type { get; private set; }             // SourdoughStarter | JunSCOBY | StandardSCOBY
    public DateOnly BirthDate { get; private set; }
    public LivingCultureId? ParentId { get; private set; }
    public IReadOnlyList<FeedingRecord> FeedingLog { get; }
    public CultureHealth Health { get; private set; }         // Thriving | NeedsFeed | Dormant | Retired

    public void Feed(FeedingRecord feeding);
    public LivingCultureId Split(string newName, DateOnly date);
}

public record FeedingRecord(DateTimeOffset Timestamp, string Flour, Quantity FlourAmount,
    Quantity WaterAmount, decimal? pH);
```

### F# Rules (Hearth)

```fsharp
module FarmOS.Hearth.Rules.KombuchaRules

let safePHThreshold = 4.2m
let maxFermentationDays = 7

type PHResult = Safe | NeedsMoreTime | MustDiscard of string

let validatePH (startDate: DateOnly) (today: DateOnly) (pH: decimal) =
    let days = today.DayNumber - startDate.DayNumber
    match pH, days with
    | ph, _ when ph <= safePHThreshold -> Safe
    | _, d when d >= maxFermentationDays -> MustDiscard $"pH {pH} after {d} days"
    | _ -> NeedsMoreTime

module SourdoughRules

let minBakingTempF = 190m       // Internal temp for kill step
let maxCoolingTempF = 70m       // Must cool below before packaging
let maxProofingHours = 18       // Long proof safety limit

let validateBakingTemp (internalTemp: decimal) =
    if internalTemp >= minBakingTempF then Ok ()
    else Error $"Internal temp {internalTemp}°F below {minBakingTempF}°F kill step"
```

### Domain Events (Hearth)

```csharp
public record BatchStarted(BatchId Id, string BatchCode, string BatchType, LivingCultureId CultureId, DateTimeOffset StartedAt);
public record CCPReadingRecorded(BatchId Id, HACCPReading Reading);
public record PHReadingRecorded(BatchId Id, PHReading Reading);
public record BatchPhaseAdvanced(BatchId Id, string PreviousPhase, string NewPhase);
public record BatchCompleted(BatchId Id, string BatchType, Quantity Yield, DateTimeOffset CompletedAt);
public record BatchDiscarded(BatchId Id, string Reason, DateTimeOffset DiscardedAt);
public record CultureFed(LivingCultureId Id, FeedingRecord Feeding);
public record CultureSplit(LivingCultureId ParentId, LivingCultureId OffspringId, string Name, DateOnly Date);
// Cross-context
public record ProductionCompleted(string ProductType, Quantity Yield, DateOnly Date);
```

---

## 4. Apiary Context

### Aggregate Roots

#### `Hive`

```csharp
public sealed class Hive : AggregateRoot<HiveId>
{
    public string Name { get; private set; }
    public HiveType Type { get; private set; }                // Langstroth | TopBar | Warre
    public GeoPosition Position { get; private set; }
    public PaddockId? LocationId { get; private set; }        // Which area it's in
    public QueenId? CurrentQueenId { get; private set; }
    public HiveStatus Status { get; private set; }            // Active | Queenless | Swarmed | Dead | Winterized
    public int BoxCount { get; private set; }
    public IReadOnlyList<InspectionId> InspectionHistory { get; }
    public IReadOnlyList<ImageAttachment> Images { get; }

    public void RecordInspection(Inspection inspection);
    public void InstallQueen(QueenId queenId, DateOnly date);
    public void HarvestHoney(HoneyHarvest harvest);
    public void Winterize(DateOnly date);
    public void MarkDead(string cause, DateOnly date);
}
```

#### `Inspection`

```csharp
public sealed class Inspection : AggregateRoot<InspectionId>
{
    public HiveId HiveId { get; private set; }
    public DateOnly Date { get; private set; }
    public bool QueenSeen { get; private set; }
    public string BroodPattern { get; private set; }          // Solid | Spotty | None
    public int EstimatedFramesOfBees { get; private set; }
    public MiteCount? MiteCount { get; private set; }
    public string Temperament { get; private set; }           // Calm | Nervous | Aggressive
    public IReadOnlyList<string> Observations { get; }
    public bool QueenCellsSeen { get; private set; }
    public Quantity? HiveWeight { get; private set; }
    public IReadOnlyList<ImageAttachment> Photos { get; }
}

public record MiteCount(int Mites, int Bees, decimal InfestationPct, string Method);
public record HoneyHarvest(HiveId HiveId, DateOnly Date, Quantity Extracted,
    string? FloralSource, decimal? MoisturePct, AssetId? StorageLocation);
```

### Domain Events (Apiary)

```csharp
public record HiveCreated(HiveId Id, string Name, HiveType Type, GeoPosition Position);
public record InspectionRecorded(InspectionId Id, HiveId HiveId, DateOnly Date, bool QueenSeen, MiteCount? Mites);
public record HoneyHarvested(HiveId Id, HoneyHarvest Harvest);
public record QueenInstalled(HiveId Id, QueenId QueenId, DateOnly Date);
public record HiveDied(HiveId Id, string Cause, DateOnly Date);
// Cross-context
public record HarvestAvailable(string ProductType, Quantity Amount, DateOnly Date);
```

---

## 5. Commerce Context

### Aggregate Roots

#### `Subscription`

```csharp
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    public CustomerId CustomerId { get; private set; }
    public string Tier { get; private set; }                  // Weekly | BiWeekly | Monthly
    public string ShareSize { get; private set; }             // Half | Full | Family
    public Season Season { get; private set; }
    public PickupLocationId PickupLocationId { get; private set; }
    public string Status { get; private set; }                // Active | Paused | Cancelled
    public IReadOnlyList<AddOn> AddOns { get; }

    public void Pause(DateOnly until);
    public void Resume();
    public void Cancel(string reason);
    public void AddAddOn(AddOn addOn);
    public void RemoveAddOn(string productType);
}

public record AddOn(string ProductType, int Quantity, decimal PricePerUnit);
```

#### `Order`

```csharp
public sealed class Order : AggregateRoot<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    public SubscriptionId? SubscriptionId { get; private set; }
    public string Type { get; private set; }                  // CSAPickup | BakeryOrder | MarketOrder
    public IReadOnlyList<OrderLine> Lines { get; }
    public PickupSlot PickupSlot { get; private set; }
    public string Status { get; private set; }                // Pending | Packed | ReadyForPickup | Completed | Cancelled

    public void Pack();
    public void MarkReady();
    public void Complete();
    public void Cancel(string reason);
}

public record OrderLine(string ProductType, Quantity Amount, decimal UnitPrice);
public record PickupSlot(DateOnly Date, TimeOnly WindowStart, TimeOnly WindowEnd, PickupLocationId LocationId);
```

#### `Customer`

```csharp
public sealed class Customer : AggregateRoot<CustomerId>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string? DeliveryZone { get; private set; }         // Atlanta | Chattanooga | Rome-Local
    public IReadOnlyList<SubscriptionId> Subscriptions { get; }
}
```

### Domain Events (Commerce)

```csharp
public record SubscriptionCreated(SubscriptionId Id, CustomerId CustomerId, string Tier, string ShareSize, Season Season);
public record OrderPlaced(OrderId Id, CustomerId CustomerId, string Type, IReadOnlyList<OrderLine> Lines);
public record OrderPacked(OrderId Id);
public record OrderCompleted(OrderId Id, DateOnly Date);
// Cross-context (listens)
public record InventoryUpdated(string ProductType, Quantity Available, DateOnly Date);
// Cross-context (publishes)
public record ProductionRequested(string ProductType, Quantity Needed, DateOnly NeededBy);
```

---

## 6. Assets Context (NEW)

Cross-cutting physical asset management.

### Aggregate Roots

#### `Equipment`

```csharp
public sealed class Equipment : AggregateRoot<AssetId>
{
    public string Name { get; private set; }                  // "Kubota BX2380"
    public string EquipmentType { get; private set; }         // Tractor | Tiller | Irrigation | Fencing | Processing
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public decimal? PurchasePrice { get; private set; }
    public GeoPosition? CurrentLocation { get; private set; }
    public string Status { get; private set; }                // Active | NeedsMaintenance | OutOfService | Retired
    public IReadOnlyList<MaintenanceRecord> MaintenanceLog { get; }
    public IReadOnlyList<ImageAttachment> Images { get; }

    public void RecordMaintenance(MaintenanceRecord record);
    public void UpdateLocation(GeoPosition position);
    public void Retire(DateOnly date, string reason);
}

public record MaintenanceRecord(DateOnly Date, string Description, string? Mechanic,
    decimal? Cost, int? HoursAtService);
```

#### `Structure`

```csharp
public sealed class Structure : AggregateRoot<AssetId>
{
    public string Name { get; private set; }                  // "Main Barn", "Walk-in Cooler A"
    public string StructureType { get; private set; }         // Barn | Greenhouse | HoopHouse | Cooler | ProcessingRoom | ChickenCoop
    public GeoJsonGeometry? Footprint { get; private set; }
    public Quantity? Capacity { get; private set; }           // e.g., (200, "sq_ft", "area")
    public bool IsHACCPZone { get; private set; }             // Critical for food safety
    public string Status { get; private set; }                // Active | UnderRepair | Decommissioned

    public void RecordMaintenance(MaintenanceRecord record);
}
```

#### `WaterSource`

```csharp
public sealed class WaterSource : AggregateRoot<AssetId>
{
    public string Name { get; private set; }
    public string WaterType { get; private set; }             // Well | Pond | RainwaterCatchment | Creek | IrrigationLine
    public GeoPosition Position { get; private set; }
    public Quantity? Capacity { get; private set; }           // (500, "gallons", "volume")
    public Quantity? FlowRate { get; private set; }           // (5, "gpm", "flow")
    public string Status { get; private set; }                // Active | Dry | Frozen | NeedsRepair
}
```

#### `CompostBatch`

```csharp
public sealed class CompostBatch : AggregateRoot<AssetId>
{
    public string Name { get; private set; }                  // "Windrow 2026-03"
    public string Method { get; private set; }                // Windrow | StaticPile | Vermicompost | BokashiBin
    public DateOnly StartDate { get; private set; }
    public IReadOnlyList<Ingredient> Ingredients { get; }
    public decimal? CarbonNitrogenRatio { get; private set; }
    public IReadOnlyList<CompostReading> TemperatureLog { get; }
    public IReadOnlyList<TurningRecord> TurningLog { get; }
    public string Phase { get; private set; }                 // Active | Curing | Finished | Applied
    public GeoPosition? Location { get; private set; }

    public void RecordTemperature(CompostReading reading);
    public void RecordTurning(DateOnly date, string? notes);
    public void MarkFinished(DateOnly date);
    public void Apply(PaddockId destination, Quantity amount, DateOnly date);
}

public record CompostReading(DateTimeOffset Timestamp, decimal TemperatureF, decimal? MoisturePct);
public record TurningRecord(DateOnly Date, string? Notes);
```

#### `Sensor`

```csharp
public sealed class Sensor : AggregateRoot<AssetId>
{
    public string Name { get; private set; }                  // "Soil Probe Paddock 3"
    public string SensorType { get; private set; }            // SoilMoisture | Temperature | pH | Weight | Voltage | Weather
    public string? Manufacturer { get; private set; }
    public string? DeviceId { get; private set; }             // LoRaWAN device EUI
    public GeoPosition? Position { get; private set; }
    public AssetId? AttachedToAssetId { get; private set; }   // Which asset this sensor monitors
    public string Status { get; private set; }                // Online | Offline | LowBattery | Decommissioned
    public DateTimeOffset? LastReadingAt { get; private set; }
}
```

#### `Material`

```csharp
public sealed class Material : AggregateRoot<AssetId>
{
    public string Name { get; private set; }                  // "Azomite", "Row Cover", "Canning Lids"
    public string MaterialType { get; private set; }          // Amendment | Mulch | Packaging | Supply | Feed
    public Quantity QuantityOnHand { get; private set; }
    public string? Supplier { get; private set; }
    public bool IsOrganic { get; private set; }

    public void Withdraw(Quantity amount, string purpose);
    public void Restock(Quantity amount, string? supplier, decimal? cost);
}
```

### Domain Events (Assets)

```csharp
public record EquipmentRegistered(AssetId Id, string Name, string EquipmentType);
public record MaintenanceRecorded(AssetId Id, MaintenanceRecord Record);
public record StructureCreated(AssetId Id, string Name, string StructureType, bool IsHACCPZone);
public record WaterSourceCreated(AssetId Id, string Name, string WaterType);
public record CompostBatchStarted(AssetId Id, string Name, string Method, DateOnly StartDate);
public record CompostTemperatureRecorded(AssetId Id, CompostReading Reading);
public record CompostApplied(AssetId CompostId, PaddockId Destination, Quantity Amount, DateOnly Date);
public record SensorRegistered(AssetId Id, string Name, string SensorType, string? DeviceId);
public record MaterialRestocked(AssetId Id, Quantity Amount);
public record MaterialWithdrawn(AssetId Id, Quantity Amount, string Purpose);
```

---

## 7. Ledger Context (NEW)

Financial tracking tied to farm operations.

### Aggregate Roots

#### `Expense`

```csharp
public sealed class Expense : AggregateRoot<ExpenseId>
{
    public string Category { get; private set; }              // Feed | Seed | Equipment | Fuel | Vet | Packaging | Labor | Land
    public decimal Amount { get; private set; }
    public string Vendor { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Description { get; private set; }
    public string? ReceiptPath { get; private set; }          // File attachment
    public IReadOnlyList<AssetId> RelatedAssets { get; }
    public string? BoundedContext { get; private set; }       // Pasture | Flora | Hearth | Apiary
}
```

#### `Revenue`

```csharp
public sealed class Revenue : AggregateRoot<RevenueId>
{
    public string Source { get; private set; }                // CSA | FarmersMarket | Wholesale | DirectSale | Online
    public decimal Amount { get; private set; }
    public OrderId? OrderId { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Description { get; private set; }
    public string? BoundedContext { get; private set; }
}
```

### Domain Events (Ledger)

```csharp
public record ExpenseRecorded(ExpenseId Id, string Category, decimal Amount, string Vendor, DateOnly Date);
public record RevenueRecorded(RevenueId Id, string Source, decimal Amount, DateOnly Date);
```

---

## 8. Observations & General Logs (SharedKernel)

These log types can be attached to ANY asset across ANY context.

```csharp
public sealed class Observation : AggregateRoot<ObservationId>
{
    public string Description { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public GeoPosition? Location { get; private set; }
    public IReadOnlyList<AssetId> RelatedAssets { get; }       // What was observed
    public IReadOnlyList<ImageAttachment> Photos { get; }
    public IReadOnlyList<string> Tags { get; }                 // Free-form categorization
    public string? Observer { get; private set; }              // Who recorded it
}

public sealed class InputLog : AggregateRoot<InputLogId>
{
    public string Name { get; private set; }                   // "Compost Tea Application"
    public string Method { get; private set; }                 // Spray | Drench | Broadcast | SideDress
    public Quantity Amount { get; private set; }
    public IReadOnlyList<AssetId> AppliedToAssets { get; }     // Paddocks, beds, guilds
    public DateOnly Date { get; private set; }
    public string? Source { get; private set; }                // Where the input came from
    public string? LotNumber { get; private set; }
    public bool IsOrganic { get; private set; }
}

public sealed class LabTest : AggregateRoot<LabTestId>
{
    public string TestType { get; private set; }               // Soil | Water | Tissue | Pathogen
    public string? Laboratory { get; private set; }
    public DateOnly SampleDate { get; private set; }
    public DateOnly? ResultDate { get; private set; }
    public IReadOnlyList<AssetId> SampledAssets { get; }        // What was tested
    public IReadOnlyList<Quantity> Results { get; }             // Structured results
    public IReadOnlyList<FileAttachment> Reports { get; }       // PDF lab reports
    public string? SoilTexture { get; private set; }           // If soil test
}
```
