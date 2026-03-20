namespace FarmOS.Apiary.Infrastructure;

// ─── Read models ───────────────────────────────────────────────────────────

public record SeasonalTask(string Name, string Description, string Category, int Priority, int Month);

// ─── Calendar ──────────────────────────────────────────────────────────────

/// <summary>
/// Configuration-driven seasonal beekeeping task calendar.
/// Returns recommended tasks by month, based on temperate climate zone.
/// </summary>
public sealed class SeasonalTaskCalendar
{
    private static readonly List<SeasonalTask> Tasks =
    [
        // January
        new("Check Winter Stores", "Heft hives to estimate remaining honey stores. Emergency feed if light.", "Stores", 1, 1),
        new("Review Records", "Analyze last season's mite counts, yields, and losses. Order supplies.", "Planning", 2, 1),

        // February
        new("Order Packages/Queens", "Place orders early for spring package bees or replacement queens.", "Planning", 1, 2),
        new("Check Ventilation", "Ensure upper entrances are clear. Remove any dead bees blocking lower entrance.", "Maintenance", 2, 2),
        new("Emergency Feeding", "If stores are low, add fondant or dry sugar above inner cover.", "Stores", 1, 2),

        // March
        new("First Spring Inspection", "Quick peek: alive? Queen present? Brood? Stores sufficient?", "Inspection", 1, 3),
        new("Clean Bottom Boards", "Remove debris from winter. Check for mold or excessive moisture.", "Maintenance", 2, 3),
        new("Begin Feeding", "Start 1:1 sugar syrup to stimulate brood buildup if stores are low.", "Feeding", 2, 3),

        // April
        new("Full Spring Inspection", "Thorough frame-by-frame inspection. Assess queen, brood pattern, disease.", "Inspection", 1, 4),
        new("Swarm Prevention", "Check for swarm cells every 7 days. Split strong hives proactively.", "Management", 1, 4),
        new("Add Supers", "Once 7-8 frames are drawn out, add honey supers with excluder.", "Equipment", 2, 4),
        new("Pollen Patty", "Supplement protein if natural pollen is scarce.", "Feeding", 3, 4),

        // May
        new("Weekly Swarm Checks", "Inspect every 7-10 days for swarm cells. Cut or split as needed.", "Management", 1, 5),
        new("Make Splits", "Split strong colonies to grow operation and reduce swarm pressure.", "Management", 1, 5),
        new("Monitor Mites", "Do first mite wash or alcohol roll. Establish baseline count.", "Health", 2, 5),

        // June
        new("Harvest Spring Honey", "Pull full/capped supers. Extract and bottle.", "Harvest", 1, 6),
        new("Add Supers as Needed", "Keep ahead of nectar flow. Don't let bees run out of storage space.", "Equipment", 2, 6),
        new("Mite Check", "Mid-season mite count. Treat if above 3 mites/100 bees.", "Health", 1, 6),

        // July
        new("Mid-Summer Mite Treatment", "Post-harvest: apply approved mite treatment (MAQS, OAV, ApiGuard).", "Health", 1, 7),
        new("Requeen if Needed", "Replace failing or aggressive queens during strong population period.", "Management", 2, 7),
        new("Check Water Source", "Ensure bees have reliable water source during summer heat.", "Maintenance", 3, 7),

        // August
        new("Fall Flow Assessment", "Evaluate goldenrod/aster flow. Monitor colony weight gain.", "Inspection", 1, 8),
        new("Begin Fall Feeding", "Start 2:1 sugar syrup for colonies that need stores built up.", "Feeding", 1, 8),
        new("Combine Weak Colonies", "Merge weak or queenless colonies with strong ones for winter.", "Management", 2, 8),

        // September
        new("Final Mite Treatment", "Last treatment window before winter. Check mite levels first.", "Health", 1, 9),
        new("Reduce Entrances", "Install entrance reducers to prevent robbing during dearth.", "Equipment", 2, 9),
        new("Fall Feeding Continues", "Heavy 2:1 syrup until colonies reach target winter weight.", "Feeding", 1, 9),

        // October
        new("Winterize Hives", "Add insulation, moisture boards, or quilt boxes as needed for climate.", "Maintenance", 1, 10),
        new("Install Mouse Guards", "Prevent mice from nesting inside hives during winter.", "Equipment", 1, 10),
        new("Final Inspection", "Last look inside. Verify queen, stores, cluster position.", "Inspection", 2, 10),

        // November
        new("Apply Wind Protection", "Set up windbreaks or wrap hives if in exposed locations.", "Maintenance", 2, 11),
        new("Check Emergency Stores", "Heft hives. Light hives may need fondant placed on top bars.", "Stores", 1, 11),
        new("Clean Equipment", "Scrape, sterilize, and store extracted supers and equipment.", "Maintenance", 3, 11),

        // December
        new("Equipment Maintenance", "Repair, build, and paint equipment for next season.", "Maintenance", 1, 12),
        new("Order Supplies", "Order foundation, frames, treatments, and queen cups early.", "Planning", 2, 12),
        new("Review Bee Journal", "Summarize the year's data. Identify trends and plan improvements.", "Planning", 3, 12),
    ];

    public IReadOnlyList<SeasonalTask> GetTasksForMonth(int month)
    {
        if (month < 1 || month > 12) return [];
        return Tasks.Where(t => t.Month == month).OrderBy(t => t.Priority).ToList();
    }

    public IReadOnlyList<SeasonalTask> GetAllTasks() => Tasks;
}
