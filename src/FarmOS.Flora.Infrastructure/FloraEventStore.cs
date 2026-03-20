using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FarmOS.Flora.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Flora.Infrastructure;

public sealed class FloraEventStore(IEventStore store) : IFloraEventStore
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

    public Task<OrchardGuild> LoadGuildAsync(string id, CancellationToken ct) =>
        store.LoadAsync<OrchardGuild, OrchardGuildId>(CollectionName, id, () => new OrchardGuild(), DeserializeEvent, ct);

    public Task SaveGuildAsync(OrchardGuild guild, string userId, CancellationToken ct) =>
        SaveAsync(guild, guild.Id.ToString(), "OrchardGuild", userId, ct);

    public Task<FlowerBed> LoadBedAsync(string id, CancellationToken ct) =>
        store.LoadAsync<FlowerBed, FlowerBedId>(CollectionName, id, () => new FlowerBed(), DeserializeEvent, ct);

    public Task SaveBedAsync(FlowerBed bed, string userId, CancellationToken ct) =>
        SaveAsync(bed, bed.Id.ToString(), "FlowerBed", userId, ct);

    public Task<SeedLot> LoadSeedLotAsync(string id, CancellationToken ct) =>
        store.LoadAsync<SeedLot, SeedLotId>(CollectionName, id, () => new SeedLot(), DeserializeEvent, ct);

    public Task SaveSeedLotAsync(SeedLot lot, string userId, CancellationToken ct) =>
        SaveAsync(lot, lot.Id.ToString(), "SeedLot", userId, ct);

    public Task<PostHarvestBatch> LoadPostHarvestBatchAsync(string id, CancellationToken ct) =>
        store.LoadAsync<PostHarvestBatch, PostHarvestBatchId>(CollectionName, id, () => new PostHarvestBatch(), DeserializeEvent, ct);

    public Task SavePostHarvestBatchAsync(PostHarvestBatch batch, string userId, CancellationToken ct) =>
        SaveAsync(batch, batch.Id.ToString(), "PostHarvestBatch", userId, ct);

    public Task<BouquetRecipe> LoadBouquetRecipeAsync(string id, CancellationToken ct) =>
        store.LoadAsync<BouquetRecipe, BouquetRecipeId>(CollectionName, id, () => new BouquetRecipe(), DeserializeEvent, ct);

    public Task SaveBouquetRecipeAsync(BouquetRecipe recipe, string userId, CancellationToken ct) =>
        SaveAsync(recipe, recipe.Id.ToString(), "BouquetRecipe", userId, ct);

    public Task<CropPlan> LoadCropPlanAsync(string id, CancellationToken ct) =>
        store.LoadAsync<CropPlan, CropPlanId>(CollectionName, id, () => new CropPlan(), DeserializeEvent, ct);

    public Task SaveCropPlanAsync(CropPlan plan, string userId, CancellationToken ct) =>
        SaveAsync(plan, plan.Id.ToString(), "CropPlan", userId, ct);

    private async Task SaveAsync<TId>(AggregateRoot<TId> aggregate, string aggregateId, string aggregateType, string userId, CancellationToken ct) where TId : notnull
    {
        if (aggregate.UncommittedEvents.Count == 0) return;
        var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count;

        await store.AppendAsync(CollectionName, aggregateId, aggregateType, expectedVersion,
            aggregate.UncommittedEvents, userId, Guid.NewGuid().ToString(), TenantId.Sovereign.Value.ToString(), SerializeEvent, ct);

        aggregate.ClearEvents();
    }

    private static string SerializeEvent(IDomainEvent @event) => MsgPackOptions.SerializeToBase64(@event, @event.GetType());

    private static IDomainEvent? DeserializeEvent(string eventType, string payload) =>
        EventTypeMap.TryGetValue(eventType, out var type) ? MsgPackOptions.DeserializeFromBase64(payload, type) as IDomainEvent : null;
}
