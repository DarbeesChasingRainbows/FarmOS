using FarmOS.Flora.Application;
using FarmOS.SharedKernel.EventStore;
using Microsoft.Extensions.Logging;

namespace FarmOS.Flora.Infrastructure;

/// <summary>
/// Publishes Flora integration events to the RabbitMQ "farm.events" exchange.
/// Called by Flora command handlers after domain events are persisted,
/// converting domain events into integration events for cross-context communication.
///
/// Routing key conventions follow: flora.{aggregate}.{action}
///   - flora.harvest.recorded
///   - flora.batch.ready
///   - flora.bouquet.made
///   - flora.revenue.{channel}
/// </summary>
public sealed class FloraIntegrationPublisher(
    IEventBus eventBus,
    ILogger<FloraIntegrationPublisher> logger) : IFloraIntegrationPublisher
{
    public async Task PublishHarvestRecordedAsync(
        Guid bedId, Guid successionId, string species, string cultivar,
        int stemCount, DateOnly harvestDate, CancellationToken ct)
    {
        var @event = new FlowerHarvestIntegrationEvent(
            bedId, successionId, species, cultivar, stemCount, harvestDate, DateTimeOffset.UtcNow);

        await eventBus.PublishAsync(@event, "flora.harvest.recorded", ct);
        logger.LogInformation("Published flora.harvest.recorded: {Species} {Cultivar} x{Stems}", species, cultivar, stemCount);
    }

    public async Task PublishBatchReadyAsync(
        Guid batchId, string species, string cultivar,
        int stemsAvailable, int premiumStems, int standardStems,
        decimal coolerTempF, string? coolerSlot, CancellationToken ct)
    {
        var @event = new BatchReadyForSaleIntegrationEvent(
            batchId, species, cultivar, stemsAvailable, premiumStems, standardStems,
            coolerTempF, coolerSlot, DateTimeOffset.UtcNow);

        await eventBus.PublishAsync(@event, "flora.batch.ready", ct);
        logger.LogInformation("Published flora.batch.ready: {Species} {Cultivar} — {Stems} stems available", species, cultivar, stemsAvailable);
    }

    public async Task PublishBouquetsMadeAsync(
        Guid recipeId, string recipeName, string category,
        int quantity, int stemsPerBouquet, DateOnly date, CancellationToken ct)
    {
        var @event = new BouquetsMadeIntegrationEvent(
            recipeId, recipeName, category, quantity, stemsPerBouquet, date, DateTimeOffset.UtcNow);

        await eventBus.PublishAsync(@event, "flora.bouquet.made", ct);
        logger.LogInformation("Published flora.bouquet.made: {Qty}x {Recipe}", quantity, recipeName);
    }

    public async Task PublishRevenueRecordedAsync(
        Guid planId, string channel, decimal amount, DateOnly date, string? notes, CancellationToken ct)
    {
        var @event = new CropRevenueIntegrationEvent(
            planId, channel, amount, date, notes, DateTimeOffset.UtcNow);

        await eventBus.PublishAsync(@event, $"flora.revenue.{channel.ToLowerInvariant()}", ct);
        logger.LogInformation("Published flora.revenue.{Channel}: ${Amount}", channel, amount);
    }
}
