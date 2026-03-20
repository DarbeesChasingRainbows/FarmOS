using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Events;
using FarmOS.Flora.Application.Queries;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Flora.Infrastructure;

public sealed class FloraProjection(IEventStore store) : IFloraProjection
{
    private const string CollectionName = "flora_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(GuildCreated)] = typeof(GuildCreated),
        [nameof(GuildMemberAdded)] = typeof(GuildMemberAdded),
        [nameof(GuildBoundaryUpdated)] = typeof(GuildBoundaryUpdated),
        [nameof(FlowerBedCreated)] = typeof(FlowerBedCreated),
        [nameof(SuccessionPlanned)] = typeof(SuccessionPlanned),
        [nameof(SeedingRecorded)] = typeof(SeedingRecorded),
        [nameof(TransplantRecorded)] = typeof(TransplantRecorded),
        [nameof(FlowerHarvestRecorded)] = typeof(FlowerHarvestRecorded),
        [nameof(SeedLotCreated)] = typeof(SeedLotCreated),
        [nameof(SeedWithdrawn)] = typeof(SeedWithdrawn),
        [nameof(SeedRestocked)] = typeof(SeedRestocked),
        // PostHarvestBatch
        [nameof(PostHarvestBatchCreated)] = typeof(PostHarvestBatchCreated),
        [nameof(StemsGraded)] = typeof(StemsGraded),
        [nameof(StemsConditioned)] = typeof(StemsConditioned),
        [nameof(BatchMovedToCooler)] = typeof(BatchMovedToCooler),
        [nameof(BatchStemsUsed)] = typeof(BatchStemsUsed),
        // BouquetRecipe
        [nameof(BouquetRecipeCreated)] = typeof(BouquetRecipeCreated),
        [nameof(RecipeItemAdded)] = typeof(RecipeItemAdded),
        [nameof(RecipeItemRemoved)] = typeof(RecipeItemRemoved),
        [nameof(BouquetMade)] = typeof(BouquetMade),
        // CropPlan
        [nameof(CropPlanCreated)] = typeof(CropPlanCreated),
        [nameof(BedAssignedToPlan)] = typeof(BedAssignedToPlan),
        [nameof(YieldRecorded)] = typeof(YieldRecorded),
        [nameof(CropCostRecorded)] = typeof(CropCostRecorded),
        [nameof(CropRevenueRecorded)] = typeof(CropRevenueRecorded)
    };

    private async Task<AllStates> LoadAllStatesAsync(int batchSize, CancellationToken ct)
    {
        var state = new AllStates();
        long position = 0;

        while (true)
        {
            var docs = await store.GetAllEventsAsync(CollectionName, position, batchSize, ct);
            if (docs.Count == 0) break;

            foreach (var doc in docs)
            {
                if (!EventTypeMap.TryGetValue(doc.EventType, out var type)) continue;
                var evt = MsgPackOptions.DeserializeFromBase64(doc.Payload, type) as IDomainEvent;
                if (evt is null) continue;

                ApplyToState(state, evt);
            }

            position += docs.Count;
            if (docs.Count < batchSize) break;
        }

        return state;
    }

    private sealed class AllStates
    {
        public Dictionary<string, BedState> Beds { get; } = [];
        public Dictionary<string, SeedLotState> Seeds { get; } = [];
        public Dictionary<string, GuildState> Guilds { get; } = [];
        public Dictionary<string, BatchState> Batches { get; } = [];
        public Dictionary<string, RecipeState> Recipes { get; } = [];
        public Dictionary<string, PlanState> Plans { get; } = [];
    }

    // ─── Public query methods ──────────────────────────────────────────────

    public async Task<List<FlowerBedSummaryDto>> GetAllBedsAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Beds.Values.Select(ToBedSummary).ToList();
    }

    public async Task<FlowerBedDetailDto?> GetBedDetailAsync(Guid bedId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Beds.TryGetValue(bedId.ToString(), out var state) ? ToBedDetail(state) : null;
    }

    public async Task<List<SeedLotSummaryDto>> GetAllSeedLotsAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Seeds.Values.Select(ToSeedLotSummary).ToList();
    }

    public async Task<SeedLotDetailDto?> GetSeedLotDetailAsync(Guid lotId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Seeds.TryGetValue(lotId.ToString(), out var state) ? ToSeedLotDetail(state) : null;
    }

    public async Task<List<GuildSummaryDto>> GetAllGuildsAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Guilds.Values.Select(ToGuildSummary).ToList();
    }

    public async Task<GuildDetailDto?> GetGuildDetailAsync(Guid guildId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Guilds.TryGetValue(guildId.ToString(), out var state) ? ToGuildDetail(state) : null;
    }

    public async Task<List<PostHarvestBatchSummaryDto>> GetAllBatchesAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Batches.Values.Select(ToBatchSummary).ToList();
    }

    public async Task<PostHarvestBatchDetailDto?> GetBatchDetailAsync(Guid batchId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Batches.TryGetValue(batchId.ToString(), out var state) ? ToBatchDetail(state) : null;
    }

    public async Task<List<BouquetRecipeSummaryDto>> GetAllRecipesAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Recipes.Values.Select(ToRecipeSummary).ToList();
    }

    public async Task<BouquetRecipeDetailDto?> GetRecipeDetailAsync(Guid recipeId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Recipes.TryGetValue(recipeId.ToString(), out var state) ? ToRecipeDetail(state) : null;
    }

    public async Task<List<CropPlanSummaryDto>> GetAllCropPlansAsync(CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Plans.Values.Select(ToPlanSummary).ToList();
    }

    public async Task<CropPlanDetailDto?> GetCropPlanDetailAsync(Guid planId, CancellationToken ct)
    {
        var s = await LoadAllStatesAsync(500, ct);
        return s.Plans.TryGetValue(planId.ToString(), out var state) ? ToPlanDetail(state) : null;
    }

    // ─── State builder ──────────────────────────────────────────────────────

    private static void ApplyToState(AllStates s, IDomainEvent evt)
    {
        switch (evt)
        {
            // Guild events
            case GuildCreated gc:
                s.Guilds[gc.Id.ToString()] = new GuildState
                {
                    Id = gc.Id.ToString(), Name = gc.Name, Type = gc.Type,
                    Latitude = gc.Position.Latitude, Longitude = gc.Position.Longitude, Planted = gc.Planted
                };
                break;
            case GuildMemberAdded gma when s.Guilds.TryGetValue(gma.GuildId.ToString(), out var guild):
                guild.Members.Add(new GuildMemberState
                {
                    PlantId = gma.Member.PlantId.Value.ToString(), Species = gma.Member.Species,
                    Cultivar = gma.Member.Cultivar, Role = gma.Member.Role
                });
                break;
            case GuildBoundaryUpdated gbu when s.Guilds.TryGetValue(gbu.Id.ToString(), out var guild):
                guild.Boundary = gbu.Boundary;
                break;

            // Bed events
            case FlowerBedCreated fbc:
                s.Beds[fbc.Id.ToString()] = new BedState
                {
                    Id = fbc.Id.ToString(), Name = fbc.Name, Block = fbc.Block,
                    LengthFeet = fbc.Dimensions.LengthFeet, WidthFeet = fbc.Dimensions.WidthFeet
                };
                break;
            case SuccessionPlanned sp when s.Beds.TryGetValue(sp.BedId.ToString(), out var bed):
                bed.Successions.Add(new SuccessionState
                {
                    Id = sp.SuccId.ToString(), Species = sp.Variety.Species, Cultivar = sp.Variety.Cultivar,
                    DaysToMaturity = sp.Variety.DaysToMaturity, Color = sp.Variety.Color,
                    SowDate = sp.SowDate, TransplantDate = sp.TransplantDate, HarvestStart = sp.HarvestStart
                });
                break;
            case SeedingRecorded sr when s.Beds.TryGetValue(sr.BedId.ToString(), out var bed):
                var seedSucc = bed.Successions.Find(x => x.Id == sr.SuccId.ToString());
                if (seedSucc != null) seedSucc.IsSeeded = true;
                break;
            case TransplantRecorded tr when s.Beds.TryGetValue(tr.BedId.ToString(), out var bed):
                var transSucc = bed.Successions.Find(x => x.Id == tr.SuccId.ToString());
                if (transSucc != null) transSucc.IsTransplanted = true;
                break;
            case FlowerHarvestRecorded fhr when s.Beds.TryGetValue(fhr.BedId.ToString(), out var bed):
                var harvSucc = bed.Successions.Find(x => x.Id == fhr.SuccId.ToString());
                if (harvSucc != null)
                {
                    harvSucc.Harvests.Add(new HarvestEntryState { StemCount = fhr.Stems.Value, Unit = fhr.Stems.Unit, Date = fhr.Date });
                    harvSucc.HarvestEnd = fhr.Date;
                }
                break;

            // Seed lot events
            case SeedLotCreated slc:
                s.Seeds[slc.Id.ToString()] = new SeedLotState
                {
                    Id = slc.Id.ToString(), Species = slc.Variety.Species, Cultivar = slc.Variety.Cultivar,
                    DaysToMaturity = slc.Variety.DaysToMaturity, Color = slc.Variety.Color,
                    Supplier = slc.Supplier, QtyOnHand = slc.Quantity.Value, Unit = slc.Quantity.Unit,
                    GerminationPct = slc.GerminationPct, HarvestYear = slc.HarvestYear, IsOrganic = slc.IsOrganic
                };
                break;
            case SeedWithdrawn sw when s.Seeds.TryGetValue(sw.Id.ToString(), out var lot):
                lot.QtyOnHand -= sw.Qty.Value;
                break;
            case SeedRestocked srs when s.Seeds.TryGetValue(srs.Id.ToString(), out var lot):
                lot.QtyOnHand += srs.Qty.Value;
                if (srs.LotNumber != null) lot.LotNumber = srs.LotNumber;
                break;

            // PostHarvestBatch events
            case PostHarvestBatchCreated phbc:
                s.Batches[phbc.Id.ToString()] = new BatchState
                {
                    Id = phbc.Id.ToString(), SourceBedId = phbc.SourceBed.Value.ToString(),
                    SuccessionId = phbc.SuccessionId.Value.ToString(), Species = phbc.Species,
                    Cultivar = phbc.Cultivar, TotalStems = phbc.TotalStems, HarvestDate = phbc.HarvestDate
                };
                break;
            case StemsGraded sg when s.Batches.TryGetValue(sg.BatchId.ToString(), out var batch):
                batch.Grades.Add(new StemGradeState { Grade = sg.Grade.Grade, StemCount = sg.Grade.StemCount, StemLengthInches = sg.Grade.StemLengthInches });
                break;
            case StemsConditioned sc when s.Batches.TryGetValue(sc.BatchId.ToString(), out var batch):
                batch.IsConditioned = true; batch.ConditioningSolution = sc.Solution; batch.WaterTempF = sc.WaterTempF;
                break;
            case BatchMovedToCooler bmc when s.Batches.TryGetValue(bmc.BatchId.ToString(), out var batch):
                batch.InCooler = true; batch.CoolerTempF = bmc.TemperatureF; batch.CoolerSlot = bmc.SlotLabel;
                break;
            case BatchStemsUsed bsu when s.Batches.TryGetValue(bsu.BatchId.ToString(), out var batch):
                batch.StemsUsed += bsu.StemsUsed;
                break;

            // BouquetRecipe events
            case BouquetRecipeCreated brc:
                s.Recipes[brc.Id.ToString()] = new RecipeState { Id = brc.Id.ToString(), Name = brc.Name, Category = brc.Category };
                break;
            case RecipeItemAdded ria when s.Recipes.TryGetValue(ria.RecipeId.ToString(), out var recipe):
                recipe.Items.Add(new RecipeItemState
                {
                    Species = ria.Item.Species, Cultivar = ria.Item.Cultivar,
                    StemCount = ria.Item.StemCount, Color = ria.Item.Color, Role = ria.Item.Role
                });
                break;
            case RecipeItemRemoved rir when s.Recipes.TryGetValue(rir.RecipeId.ToString(), out var recipe):
                recipe.Items.RemoveAll(i => i.Species == rir.Species && i.Cultivar == rir.Cultivar);
                break;
            case BouquetMade bm when s.Recipes.TryGetValue(bm.RecipeId.ToString(), out var recipe):
                recipe.TotalBouquetsMade += bm.Quantity;
                break;

            // CropPlan events
            case CropPlanCreated cpc:
                s.Plans[cpc.Id.ToString()] = new PlanState
                {
                    Id = cpc.Id.ToString(), SeasonYear = cpc.SeasonYear,
                    SeasonName = cpc.SeasonName, PlanName = cpc.PlanName
                };
                break;
            case BedAssignedToPlan bap when s.Plans.TryGetValue(bap.PlanId.ToString(), out var plan):
                plan.BedAssignments.Add(new BedAssignmentState
                {
                    BedId = bap.Assignment.BedId.Value.ToString(), Species = bap.Assignment.Variety.Species,
                    Cultivar = bap.Assignment.Variety.Cultivar, PlannedSuccessions = bap.Assignment.PlannedSuccessions
                });
                break;
            case YieldRecorded yr when s.Plans.TryGetValue(yr.PlanId.ToString(), out var plan):
                plan.TotalStemsHarvested += yr.StemsHarvested;
                break;
            case CropCostRecorded ccr when s.Plans.TryGetValue(ccr.PlanId.ToString(), out var plan):
                plan.Costs.Add(new CostEntryState { Category = ccr.Cost.Category, Amount = ccr.Cost.Amount, Notes = ccr.Cost.Notes });
                plan.TotalCosts += ccr.Cost.Amount;
                break;
            case CropRevenueRecorded crr when s.Plans.TryGetValue(crr.PlanId.ToString(), out var plan):
                plan.Revenues.Add(new RevenueEntryState { Channel = crr.Channel, Amount = crr.Amount, Date = crr.Date, Notes = crr.Notes });
                plan.TotalRevenue += crr.Amount;
                break;
        }
    }

    // ─── Mapping ────────────────────────────────────────────────────────────

    private static FlowerBedSummaryDto ToBedSummary(BedState b) => new(
        Guid.Parse(b.Id), b.Name, b.Block, b.LengthFeet, b.WidthFeet, b.Successions.Count);

    private static FlowerBedDetailDto ToBedDetail(BedState b) => new(
        Guid.Parse(b.Id), b.Name, b.Block, b.LengthFeet, b.WidthFeet,
        b.Successions.Select(s => new SuccessionDto(
            Guid.Parse(s.Id), s.Species, s.Cultivar, s.DaysToMaturity, s.Color,
            s.SowDate, s.TransplantDate, s.HarvestStart, s.HarvestEnd,
            s.Harvests.Select(h => new HarvestEntryDto(h.StemCount, h.Unit, h.Date)).ToList()
        )).ToList());

    private static SeedLotSummaryDto ToSeedLotSummary(SeedLotState s) => new(
        Guid.Parse(s.Id), s.Species, s.Cultivar, s.Supplier,
        s.QtyOnHand, s.Unit, s.GerminationPct, s.HarvestYear, s.IsOrganic);

    private static SeedLotDetailDto ToSeedLotDetail(SeedLotState s) => new(
        Guid.Parse(s.Id), s.Species, s.Cultivar, s.DaysToMaturity, s.Color,
        s.Supplier, s.QtyOnHand, s.Unit, s.GerminationPct,
        s.HarvestYear, s.IsOrganic, s.LotNumber, s.PurchaseDate);

    private static GuildSummaryDto ToGuildSummary(GuildState g) => new(
        Guid.Parse(g.Id), g.Name, g.Type, g.Latitude, g.Longitude,
        g.Planted, g.Members.Count);

    private static GuildDetailDto ToGuildDetail(GuildState g) => new(
        Guid.Parse(g.Id), g.Name, g.Type, g.Latitude, g.Longitude,
        g.Planted, g.Boundary,
        g.Members.Select(m => new GuildMemberDto(
            Guid.Parse(m.PlantId), m.Species, m.Cultivar, m.Role
        )).ToList());

    private static PostHarvestBatchSummaryDto ToBatchSummary(BatchState b) => new(
        Guid.Parse(b.Id), b.Species, b.Cultivar, b.TotalStems,
        b.TotalStems - b.StemsUsed, b.HarvestDate, b.IsConditioned, b.InCooler);

    private static PostHarvestBatchDetailDto ToBatchDetail(BatchState b) => new(
        Guid.Parse(b.Id), Guid.Parse(b.SourceBedId), Guid.Parse(b.SuccessionId),
        b.Species, b.Cultivar, b.TotalStems, b.StemsUsed,
        b.TotalStems - b.StemsUsed, b.HarvestDate,
        b.Grades.Select(g => new StemGradeDto(g.Grade, g.StemCount, g.StemLengthInches)).ToList(),
        b.IsConditioned, b.ConditioningSolution, b.WaterTempF,
        b.InCooler, b.CoolerTempF, b.CoolerSlot);

    private static BouquetRecipeSummaryDto ToRecipeSummary(RecipeState r) => new(
        Guid.Parse(r.Id), r.Name, r.Category, r.Items.Count,
        r.Items.Sum(i => i.StemCount));

    private static BouquetRecipeDetailDto ToRecipeDetail(RecipeState r) => new(
        Guid.Parse(r.Id), r.Name, r.Category,
        r.Items.Select(i => new RecipeItemDto(i.Species, i.Cultivar, i.StemCount, i.Color, i.Role)).ToList(),
        r.Items.Sum(i => i.StemCount), r.TotalBouquetsMade);

    private static CropPlanSummaryDto ToPlanSummary(PlanState p) => new(
        Guid.Parse(p.Id), p.SeasonYear, p.SeasonName, p.PlanName,
        p.BedAssignments.Count, p.TotalStemsHarvested, p.TotalRevenue, p.TotalCosts);

    private static CropPlanDetailDto ToPlanDetail(PlanState p) => new(
        Guid.Parse(p.Id), p.SeasonYear, p.SeasonName, p.PlanName,
        p.BedAssignments.Select(a => new BedAssignmentDto(Guid.Parse(a.BedId), a.Species, a.Cultivar, a.PlannedSuccessions)).ToList(),
        p.TotalStemsHarvested, p.TotalRevenue, p.TotalCosts,
        p.Costs.Select(c => new CostEntryDto(c.Category, c.Amount, c.Notes)).ToList(),
        p.Revenues.Select(r => new RevenueEntryDto(r.Channel, r.Amount, r.Date, r.Notes)).ToList());

    // ─── Mutable state helpers ──────────────────────────────────────────────

    private sealed class BedState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Block { get; set; } = "";
        public decimal LengthFeet { get; set; }
        public decimal WidthFeet { get; set; }
        public List<SuccessionState> Successions { get; set; } = [];
    }

    private sealed class SuccessionState
    {
        public string Id { get; set; } = "";
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public int DaysToMaturity { get; set; }
        public string? Color { get; set; }
        public DateOnly SowDate { get; set; }
        public DateOnly TransplantDate { get; set; }
        public DateOnly HarvestStart { get; set; }
        public DateOnly? HarvestEnd { get; set; }
        public bool IsSeeded { get; set; }
        public bool IsTransplanted { get; set; }
        public List<HarvestEntryState> Harvests { get; set; } = [];
    }

    private sealed class HarvestEntryState
    {
        public decimal StemCount { get; set; }
        public string Unit { get; set; } = "";
        public DateOnly Date { get; set; }
    }

    private sealed class SeedLotState
    {
        public string Id { get; set; } = "";
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public int DaysToMaturity { get; set; }
        public string? Color { get; set; }
        public string Supplier { get; set; } = "";
        public decimal QtyOnHand { get; set; }
        public string Unit { get; set; } = "";
        public decimal GerminationPct { get; set; }
        public int HarvestYear { get; set; }
        public bool IsOrganic { get; set; }
        public string? LotNumber { get; set; }
        public DateOnly? PurchaseDate { get; set; }
    }

    private sealed class GuildState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public GuildType Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateOnly Planted { get; set; }
        public GeoJsonGeometry? Boundary { get; set; }
        public List<GuildMemberState> Members { get; set; } = [];
    }

    private sealed class GuildMemberState
    {
        public string PlantId { get; set; } = "";
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public GuildRole Role { get; set; }
    }

    // ─── PostHarvestBatch state ──────────────────────────────────────────

    private sealed class BatchState
    {
        public string Id { get; set; } = "";
        public string SourceBedId { get; set; } = "";
        public string SuccessionId { get; set; } = "";
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public int TotalStems { get; set; }
        public int StemsUsed { get; set; }
        public DateOnly HarvestDate { get; set; }
        public List<StemGradeState> Grades { get; set; } = [];
        public bool IsConditioned { get; set; }
        public string? ConditioningSolution { get; set; }
        public decimal? WaterTempF { get; set; }
        public bool InCooler { get; set; }
        public decimal? CoolerTempF { get; set; }
        public string? CoolerSlot { get; set; }
    }

    private sealed class StemGradeState
    {
        public HarvestGrade Grade { get; set; }
        public int StemCount { get; set; }
        public decimal StemLengthInches { get; set; }
    }

    // ─── BouquetRecipe state ─────────────────────────────────────────────

    private sealed class RecipeState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public List<RecipeItemState> Items { get; set; } = [];
        public int TotalBouquetsMade { get; set; }
    }

    private sealed class RecipeItemState
    {
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public int StemCount { get; set; }
        public string? Color { get; set; }
        public string Role { get; set; } = "";
    }

    // ─── CropPlan state ──────────────────────────────────────────────────

    private sealed class PlanState
    {
        public string Id { get; set; } = "";
        public int SeasonYear { get; set; }
        public string SeasonName { get; set; } = "";
        public string PlanName { get; set; } = "";
        public List<BedAssignmentState> BedAssignments { get; set; } = [];
        public int TotalStemsHarvested { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public List<CostEntryState> Costs { get; set; } = [];
        public List<RevenueEntryState> Revenues { get; set; } = [];
    }

    private sealed class BedAssignmentState
    {
        public string BedId { get; set; } = "";
        public string Species { get; set; } = "";
        public string Cultivar { get; set; } = "";
        public int PlannedSuccessions { get; set; }
    }

    private sealed class CostEntryState
    {
        public string Category { get; set; } = "";
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class RevenueEntryState
    {
        public SalesChannel Channel { get; set; }
        public decimal Amount { get; set; }
        public DateOnly Date { get; set; }
        public string? Notes { get; set; }
    }
}
