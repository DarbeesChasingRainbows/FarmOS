using FarmOS.SharedKernel.EventStore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FarmOS.SharedKernel.Infrastructure;

/// <summary>
/// RabbitMQ implementation of <see cref="IEventBus"/> using topic exchanges.
/// Uses RabbitMQ.Client v7 fully-async API.
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName;

    private IConnection? _connection;
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqEventBus(string hostName, int port, string exchangeName = "farm.events",
        string userName = "guest", string password = "guest")
    {
        _factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
        };
        _exchangeName = exchangeName;
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _publishChannel is { IsOpen: true })
            return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                _connection = await _factory.CreateConnectionAsync("FarmOS.EventBus", ct);
            }

            if (_publishChannel is not { IsOpen: true })
            {
                _publishChannel = await _connection.CreateChannelAsync(cancellationToken: ct);
                await _publishChannel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: ct);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct) where T : IDomainEvent
    {
        await EnsureConnectedAsync(ct);

        var body = MsgPackOptions.SerializeToBytes(@event);
        var props = new BasicProperties
        {
            ContentType = "application/x-msgpack",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = @event.GetType().Name,
        };

        await _publishChannel!.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    public async Task SubscribeAsync<T>(
        string queueName,
        string bindingKey,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct) where T : class
    {
        await EnsureConnectedAsync(ct);

        var channel = await _connection!.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: bindingKey,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = MsgPackOptions.DeserializeFromBytes<T>(ea.Body);
                if (message is not null)
                {
                    await handler(message, ct);
                }
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            }
            catch
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_publishChannel is not null)
        {
            await _publishChannel.CloseAsync();
            _publishChannel.Dispose();
        }
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        _initLock.Dispose();
    }
}
