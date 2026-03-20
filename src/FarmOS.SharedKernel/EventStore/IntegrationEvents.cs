using FarmOS.SharedKernel;

namespace FarmOS.SharedKernel.EventStore;

/// <summary>
/// Cross-context integration events published via RabbitMQ.
/// These DTOs are shared between bounded contexts — they do NOT reference domain-specific types.
/// </summary>

/// <summary>
/// Published by IoT Service when a sensor excursion alert fires.
/// Consumed by Compliance Service (auto-CAPA) and Notification Service (SMS/email).
/// Routing key: iot.excursion.{severity}
/// </summary>
public record ExcursionAlertIntegrationEvent(
    Guid ExcursionId,
    Guid DeviceId,
    Guid ZoneId,
    string SensorType,
    string Severity,
    string AlertMessage,
    string? CorrectiveAction,
    DateTimeOffset FiredAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => FiredAt;
}

/// <summary>
/// Published by IoT Service when an excursion begins.
/// Routing key: iot.excursion.started
/// </summary>
public record ExcursionStartedIntegrationEvent(
    Guid ExcursionId,
    Guid DeviceId,
    Guid ZoneId,
    string SensorType,
    decimal Value,
    decimal ThresholdLimit,
    string ThresholdDirection,
    DateTimeOffset StartedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => StartedAt;
}

/// <summary>
/// Published by IoT Service when an excursion ends (return to normal).
/// Routing key: iot.excursion.ended
/// </summary>
public record ExcursionEndedIntegrationEvent(
    Guid ExcursionId,
    Guid DeviceId,
    Guid ZoneId,
    string SensorType,
    DateTimeOffset EndedAt,
    double DurationMinutes) : IDomainEvent
{
    public DateTimeOffset OccurredAt => EndedAt;
}

// ─── Flora Context Integration Events ──────────────────────────────

/// <summary>
/// Published by Flora Service when a flower harvest is recorded.
/// Consumed by Commerce (inventory availability) and Ledger (production tracking).
/// Routing key: flora.harvest.recorded
/// </summary>
public record FlowerHarvestIntegrationEvent(
    Guid BedId,
    Guid SuccessionId,
    string Species,
    string Cultivar,
    int StemCount,
    DateOnly HarvestDate,
    DateTimeOffset RecordedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => RecordedAt;
}

/// <summary>
/// Published by Flora Service when a post-harvest batch completes processing
/// (graded, conditioned, and placed in cooler — ready for sale).
/// Consumed by Commerce (saleable inventory) and IoT (cooler monitoring).
/// Routing key: flora.batch.ready
/// </summary>
public record BatchReadyForSaleIntegrationEvent(
    Guid BatchId,
    string Species,
    string Cultivar,
    int StemsAvailable,
    int PremiumStems,
    int StandardStems,
    decimal CoolerTempF,
    string? CoolerSlot,
    DateTimeOffset ReadyAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => ReadyAt;
}

/// <summary>
/// Published by Flora Service when bouquets are made from a recipe.
/// Consumed by Commerce (product created) and Ledger (COGS tracking).
/// Routing key: flora.bouquet.made
/// </summary>
public record BouquetsMadeIntegrationEvent(
    Guid RecipeId,
    string RecipeName,
    string Category,
    int Quantity,
    int StemsPerBouquet,
    DateOnly Date,
    DateTimeOffset MadeAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => MadeAt;
}

/// <summary>
/// Published by Flora Service when crop revenue is recorded.
/// Consumed by Ledger (revenue journals) and Commerce (sales analytics).
/// Routing key: flora.revenue.{channel}
/// </summary>
public record CropRevenueIntegrationEvent(
    Guid PlanId,
    string Channel,
    decimal Amount,
    DateOnly Date,
    string? Notes,
    DateTimeOffset RecordedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => RecordedAt;
}
