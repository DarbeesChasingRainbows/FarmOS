namespace FarmOS.SharedKernel.EventStore;

/// <summary>
/// Abstraction over the message bus (RabbitMQ) for cross-context event integration.
/// Each bounded context publishes its integration events here; other contexts subscribe.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to the "farm.events" topic exchange with the given routing key.
    /// Example routing keys: "pasture.grazing.started", "hearth.batch.completed"
    /// </summary>
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct) where T : IDomainEvent;

    /// <summary>
    /// Subscribe to events matching the binding key pattern.
    /// Example: "hearth.batch.*" to receive all batch events from the Hearth context.
    /// </summary>
    Task SubscribeAsync<T>(
        string queueName,
        string bindingKey,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct) where T : class;
}
