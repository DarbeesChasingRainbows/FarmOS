namespace FarmOS.Flora.Application;

/// <summary>
/// Publishes Flora domain events as cross-context integration events via RabbitMQ.
/// Injected into command handlers that produce events interesting to other bounded contexts.
/// </summary>
public interface IFloraIntegrationPublisher
{
    /// <summary>
    /// Notify Commerce/Ledger that stems were harvested and are entering post-harvest pipeline.
    /// </summary>
    Task PublishHarvestRecordedAsync(
        Guid bedId, Guid successionId, string species, string cultivar,
        int stemCount, DateOnly harvestDate, CancellationToken ct);

    /// <summary>
    /// Notify Commerce that a graded+conditioned+cooled batch is available for sale.
    /// </summary>
    Task PublishBatchReadyAsync(
        Guid batchId, string species, string cultivar,
        int stemsAvailable, int premiumStems, int standardStems,
        decimal coolerTempF, string? coolerSlot, CancellationToken ct);

    /// <summary>
    /// Notify Commerce/Ledger that bouquets were assembled from a recipe.
    /// </summary>
    Task PublishBouquetsMadeAsync(
        Guid recipeId, string recipeName, string category,
        int quantity, int stemsPerBouquet, DateOnly date, CancellationToken ct);

    /// <summary>
    /// Notify Ledger/Commerce that crop revenue was recorded against a plan.
    /// </summary>
    Task PublishRevenueRecordedAsync(
        Guid planId, string channel, decimal amount, DateOnly date, string? notes, CancellationToken ct);
}
