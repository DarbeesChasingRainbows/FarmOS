using FarmOS.Flora.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Flora.Domain.Aggregates;

public sealed class OrchardGuild : AggregateRoot<OrchardGuildId>
{
    public string Name { get; private set; } = "";
    public GuildType Type { get; private set; }
    public GeoPosition Position { get; private set; } = new(0, 0);
    public GeoJsonGeometry? Boundary { get; private set; }
    public DateOnly Planted { get; private set; }
    private readonly List<GuildMember> _members = [];
    public IReadOnlyList<GuildMember> Members => _members;

    public static OrchardGuild Create(string name, GuildType type, GeoPosition position, DateOnly planted)
    {
        var guild = new OrchardGuild();
        guild.RaiseEvent(new GuildCreated(OrchardGuildId.New(), name, type, position, planted, DateTimeOffset.UtcNow));
        return guild;
    }

    public void AddMember(GuildMember member) =>
        RaiseEvent(new GuildMemberAdded(Id, member, DateTimeOffset.UtcNow));

    public void UpdateBoundary(GeoJsonGeometry boundary) =>
        RaiseEvent(new GuildBoundaryUpdated(Id, boundary, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case GuildCreated e: Id = e.Id; Name = e.Name; Type = e.Type; Position = e.Position; Planted = e.Planted; break;
            case GuildMemberAdded e: _members.Add(e.Member); break;
            case GuildBoundaryUpdated e: Boundary = e.Boundary; break;
        }
    }
}

public sealed class FlowerBed : AggregateRoot<FlowerBedId>
{
    public string Name { get; private set; } = "";
    public string Block { get; private set; } = "";
    public BedDimensions Dimensions { get; private set; } = new(0, 0);
    private readonly List<Succession> _successions = [];
    public IReadOnlyList<Succession> Successions => _successions;

    public static FlowerBed Create(string name, string block, BedDimensions dims)
    {
        var bed = new FlowerBed();
        bed.RaiseEvent(new FlowerBedCreated(FlowerBedId.New(), name, block, dims, DateTimeOffset.UtcNow));
        return bed;
    }

    public SuccessionId PlanSuccession(CropVariety variety, DateOnly sow, DateOnly transplant, DateOnly harvestStart)
    {
        var id = SuccessionId.New();
        RaiseEvent(new SuccessionPlanned(Id, id, variety, sow, transplant, harvestStart, DateTimeOffset.UtcNow));
        return id;
    }

    public void RecordSeeding(SuccessionId succId, SeedLotId seedLot, Quantity qty, DateOnly date) =>
        RaiseEvent(new SeedingRecorded(Id, succId, seedLot, qty, date, DateTimeOffset.UtcNow));

    public void RecordTransplant(SuccessionId succId, Quantity qty, DateOnly date) =>
        RaiseEvent(new TransplantRecorded(Id, succId, qty, date, DateTimeOffset.UtcNow));

    public void RecordHarvest(SuccessionId succId, Quantity stems, DateOnly date) =>
        RaiseEvent(new FlowerHarvestRecorded(Id, succId, stems, date, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case FlowerBedCreated e: Id = e.Id; Name = e.Name; Block = e.Block; Dimensions = e.Dimensions; break;
            case SuccessionPlanned e: _successions.Add(new Succession(e.SuccId, e.Variety, e.SowDate, e.TransplantDate, e.HarvestStart, null)); break;
        }
    }
}

public sealed class SeedLot : AggregateRoot<SeedLotId>
{
    public CropVariety Variety { get; private set; } = new("", "", 0);
    public string Supplier { get; private set; } = "";
    public Quantity QuantityOnHand { get; private set; } = new(0, "packet", "count");
    public decimal GerminationPct { get; private set; }
    public int HarvestYear { get; private set; }
    public bool IsOrganic { get; private set; }

    public static SeedLot Create(CropVariety variety, string supplier, Quantity qty, decimal germPct, int year, bool organic)
    {
        var lot = new SeedLot();
        lot.RaiseEvent(new SeedLotCreated(SeedLotId.New(), variety, supplier, qty, germPct, year, organic, DateTimeOffset.UtcNow));
        return lot;
    }

    public Result<SeedLotId, DomainError> Withdraw(Quantity qty, FlowerBedId dest)
    {
        if (qty.Value > QuantityOnHand.Value)
            return DomainError.BusinessRule($"Insufficient seed: have {QuantityOnHand.Value}, requested {qty.Value}.");
        RaiseEvent(new SeedWithdrawn(Id, qty, dest, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Restock(Quantity qty, string? lotNumber) =>
        RaiseEvent(new SeedRestocked(Id, qty, lotNumber, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SeedLotCreated e:
                Id = e.Id; Variety = e.Variety; Supplier = e.Supplier; QuantityOnHand = e.Quantity;
                GerminationPct = e.GerminationPct; HarvestYear = e.HarvestYear; IsOrganic = e.IsOrganic; break;
            case SeedWithdrawn e:
                QuantityOnHand = QuantityOnHand with { Value = QuantityOnHand.Value - e.Qty.Value }; break;
            case SeedRestocked e:
                QuantityOnHand = QuantityOnHand with { Value = QuantityOnHand.Value + e.Qty.Value }; break;
        }
    }
}

// ─── PostHarvestBatch ────────────────────────────────────────────────

public sealed class PostHarvestBatch : AggregateRoot<PostHarvestBatchId>
{
    public FlowerBedId SourceBed { get; private set; } = new(Guid.Empty);
    public SuccessionId SuccessionId { get; private set; } = new(Guid.Empty);
    public string Species { get; private set; } = "";
    public string Cultivar { get; private set; } = "";
    public int TotalStems { get; private set; }
    public int StemsUsed { get; private set; }
    public int StemsRemaining => TotalStems - StemsUsed;
    public DateOnly HarvestDate { get; private set; }
    private readonly List<StemGrade> _grades = [];
    public IReadOnlyList<StemGrade> Grades => _grades;
    public bool IsConditioned { get; private set; }
    public string? ConditioningSolution { get; private set; }
    public decimal? WaterTempF { get; private set; }
    public bool InCooler { get; private set; }
    public decimal? CoolerTempF { get; private set; }
    public string? CoolerSlot { get; private set; }

    public static PostHarvestBatch Create(FlowerBedId sourceBed, SuccessionId successionId,
        string species, string cultivar, int totalStems, DateOnly harvestDate)
    {
        var batch = new PostHarvestBatch();
        batch.RaiseEvent(new PostHarvestBatchCreated(
            PostHarvestBatchId.New(), sourceBed, successionId,
            species, cultivar, totalStems, harvestDate, DateTimeOffset.UtcNow));
        return batch;
    }

    public void GradeStems(StemGrade grade) =>
        RaiseEvent(new StemsGraded(Id, grade, DateTimeOffset.UtcNow));

    public void Condition(string solution, decimal waterTempF) =>
        RaiseEvent(new StemsConditioned(Id, solution, waterTempF, DateTimeOffset.UtcNow));

    public void MoveToCooler(decimal temperatureF, string? slotLabel = null) =>
        RaiseEvent(new BatchMovedToCooler(Id, temperatureF, slotLabel, DateTimeOffset.UtcNow));

    public Result<PostHarvestBatchId, DomainError> UseStems(int stemCount, string purpose)
    {
        if (stemCount > StemsRemaining)
            return DomainError.BusinessRule($"Insufficient stems: have {StemsRemaining}, requested {stemCount}.");
        RaiseEvent(new BatchStemsUsed(Id, stemCount, purpose, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PostHarvestBatchCreated e:
                Id = e.Id; SourceBed = e.SourceBed; SuccessionId = e.SuccessionId;
                Species = e.Species; Cultivar = e.Cultivar; TotalStems = e.TotalStems;
                HarvestDate = e.HarvestDate; break;
            case StemsGraded e:
                _grades.Add(e.Grade); break;
            case StemsConditioned e:
                IsConditioned = true; ConditioningSolution = e.Solution; WaterTempF = e.WaterTempF; break;
            case BatchMovedToCooler e:
                InCooler = true; CoolerTempF = e.TemperatureF; CoolerSlot = e.SlotLabel; break;
            case BatchStemsUsed e:
                StemsUsed += e.StemsUsed; break;
        }
    }
}

// ─── BouquetRecipe ───────────────────────────────────────────────────

public sealed class BouquetRecipe : AggregateRoot<BouquetRecipeId>
{
    public string Name { get; private set; } = "";
    public string Category { get; private set; } = ""; // market, wedding, CSA
    private readonly List<RecipeItem> _items = [];
    public IReadOnlyList<RecipeItem> Items => _items;
    public int TotalStemsPerBouquet => _items.Sum(i => i.StemCount);

    public static BouquetRecipe Create(string name, string category)
    {
        var recipe = new BouquetRecipe();
        recipe.RaiseEvent(new BouquetRecipeCreated(BouquetRecipeId.New(), name, category, DateTimeOffset.UtcNow));
        return recipe;
    }

    public void AddItem(RecipeItem item) =>
        RaiseEvent(new RecipeItemAdded(Id, item, DateTimeOffset.UtcNow));

    public void RemoveItem(string species, string cultivar) =>
        RaiseEvent(new RecipeItemRemoved(Id, species, cultivar, DateTimeOffset.UtcNow));

    public void MakeBouquet(int quantity, DateOnly date, string? notes = null) =>
        RaiseEvent(new BouquetMade(Id, quantity, date, notes, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BouquetRecipeCreated e:
                Id = e.Id; Name = e.Name; Category = e.Category; break;
            case RecipeItemAdded e:
                _items.Add(e.Item); break;
            case RecipeItemRemoved e:
                _items.RemoveAll(i => i.Species == e.Species && i.Cultivar == e.Cultivar); break;
        }
    }
}

// ─── CropPlan ────────────────────────────────────────────────────────

public sealed class CropPlan : AggregateRoot<CropPlanId>
{
    public int SeasonYear { get; private set; }
    public string SeasonName { get; private set; } = "";
    public string PlanName { get; private set; } = "";
    private readonly List<BedAssignment> _bedAssignments = [];
    public IReadOnlyList<BedAssignment> BedAssignments => _bedAssignments;
    private readonly List<CostEntry> _costs = [];
    public IReadOnlyList<CostEntry> Costs => _costs;

    // Yield tracking: stems harvested per bed per succession
    private readonly List<(FlowerBedId BedId, SuccessionId SuccessionId, int StemsHarvested, decimal StemsPerLinearFoot, DateOnly Date)> _yields = [];
    public int TotalStemsHarvested => _yields.Sum(y => y.StemsHarvested);

    // Revenue tracking
    private readonly List<(SalesChannel Channel, decimal Amount, DateOnly Date, string? Notes)> _revenues = [];
    public decimal TotalRevenue => _revenues.Sum(r => r.Amount);
    public decimal TotalCosts => _costs.Sum(c => c.Amount);

    public static CropPlan Create(int seasonYear, string seasonName, string planName)
    {
        var plan = new CropPlan();
        plan.RaiseEvent(new CropPlanCreated(CropPlanId.New(), seasonYear, seasonName, planName, DateTimeOffset.UtcNow));
        return plan;
    }

    public void AssignBed(BedAssignment assignment) =>
        RaiseEvent(new BedAssignedToPlan(Id, assignment, DateTimeOffset.UtcNow));

    public void RecordYield(FlowerBedId bedId, SuccessionId successionId, int stemsHarvested, decimal stemsPerLinearFoot, DateOnly date) =>
        RaiseEvent(new YieldRecorded(Id, bedId, successionId, stemsHarvested, stemsPerLinearFoot, date, DateTimeOffset.UtcNow));

    public void RecordCost(CostEntry cost) =>
        RaiseEvent(new CropCostRecorded(Id, cost, DateTimeOffset.UtcNow));

    public void RecordRevenue(SalesChannel channel, decimal amount, DateOnly date, string? notes = null) =>
        RaiseEvent(new CropRevenueRecorded(Id, channel, amount, date, notes, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CropPlanCreated e:
                Id = e.Id; SeasonYear = e.SeasonYear; SeasonName = e.SeasonName; PlanName = e.PlanName; break;
            case BedAssignedToPlan e:
                _bedAssignments.Add(e.Assignment); break;
            case YieldRecorded e:
                _yields.Add((e.BedId, e.SuccessionId, e.StemsHarvested, e.StemsPerLinearFoot, e.Date)); break;
            case CropCostRecorded e:
                _costs.Add(e.Cost); break;
            case CropRevenueRecorded e:
                _revenues.Add((e.Channel, e.Amount, e.Date, e.Notes)); break;
        }
    }
}
