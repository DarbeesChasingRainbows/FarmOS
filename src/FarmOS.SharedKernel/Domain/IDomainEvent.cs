namespace FarmOS.SharedKernel;

/// <summary>
/// Marker interface for all domain events.
/// All events are immutable records with an OccurredAt timestamp.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
