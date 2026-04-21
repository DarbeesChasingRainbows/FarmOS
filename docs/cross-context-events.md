# Cross-Context Event Flow — FarmOS

> RabbitMQ integration events, routing keys, publishers, subscribers, and the event bus architecture.

---

## Architecture

FarmOS uses **RabbitMQ** as the cross-context event bus. Each bounded context is fully autonomous — contexts communicate asynchronously via **integration events** published to a shared topic exchange.

```
┌────────────┐     ┌──────────────────────┐     ┌────────────────┐
│  IoT API   │────►│  RabbitMQ            │────►│  Hearth API    │
│            │     │  Exchange:           │     │  (CAPA auto-   │
│ Publishes: │     │    farm.events       │     │   creation)    │
│ iot.excur- │     │  Type: topic         │     │                │
│ sion.#     │     │                      │────►│  Flora API     │
│            │     │  Routing key pattern │     │  (Cooler alert)│
└────────────┘     └──────────────────────┘     └────────────────┘

┌────────────┐     ┌──────────────────────┐     ┌────────────────┐
│  Flora API │────►│  RabbitMQ            │────►│  Commerce API  │
│            │     │  Exchange:           │     │  (Inventory)   │
│ Publishes: │     │    farm.events       │     │                │
│ flora.har- │     │                      │────►│  Ledger API    │
│ vest.*     │     │                      │     │  (Revenue)     │
└────────────┘     └──────────────────────┘     └────────────────┘
```

---

## Event Bus Contract

Defined in [IEventBus.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/EventStore/IEventBus.cs):

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct)
        where T : IDomainEvent;

    Task SubscribeAsync<T>(
        string queueName, string bindingKey,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct) where T : class;
}
```

- **Exchange**: `farm.events` (topic type)
- **Routing keys**: Dot-separated context + entity + action (e.g., `iot.excursion.started`)
- **Binding patterns**: Use `#` wildcard for all sub-keys (e.g., `iot.excursion.#`)
- **Implementation**: [RabbitMqEventBus.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/Infrastructure/RabbitMqEventBus.cs)

---

## Integration Events

All integration events are defined in [IntegrationEvents.cs](file:///c:/Work/FarmOS/src/FarmOS.SharedKernel/EventStore/IntegrationEvents.cs). They are thin DTOs with no domain-specific type references.

### IoT Context Events

| Event | Routing Key | Payload |
|-------|------------|---------|
| `ExcursionAlertIntegrationEvent` | `iot.excursion.{severity}` | ExcursionId, DeviceId, ZoneId, SensorType, Severity, AlertMessage, CorrectiveAction |
| `ExcursionStartedIntegrationEvent` | `iot.excursion.started` | ExcursionId, DeviceId, ZoneId, SensorType, Value, ThresholdLimit, ThresholdDirection |
| `ExcursionEndedIntegrationEvent` | `iot.excursion.ended` | ExcursionId, DeviceId, ZoneId, SensorType, DurationMinutes |

### Flora Context Events

| Event | Routing Key | Payload |
|-------|------------|---------|
| `FlowerHarvestIntegrationEvent` | `flora.harvest.recorded` | BedId, SuccessionId, Species, Cultivar, StemCount, HarvestDate |
| `BatchReadyForSaleIntegrationEvent` | `flora.batch.ready` | BatchId, Species, Cultivar, StemsAvailable, PremiumStems, StandardStems, CoolerTempF |
| `BouquetsMadeIntegrationEvent` | `flora.bouquet.made` | RecipeId, RecipeName, Category, Quantity, StemsPerBouquet |
| `CropRevenueIntegrationEvent` | `flora.revenue.{channel}` | PlanId, Channel, Amount, Date |

---

## Active Publishers & Subscribers

### Publishers

| Context | Worker | Events Published | Routing Key |
|---------|--------|-----------------|-------------|
| **IoT** | `NotificationWorker` | `ExcursionAlertIntegrationEvent` | `iot.excursion.{severity}` |
| **IoT** | (inline in command handlers) | `ExcursionStarted/Ended` | `iot.excursion.started/ended` |
| **Flora** | (inline in command handlers) | `FlowerHarvest`, `BatchReady`, `BouquetsMade`, `CropRevenue` | `flora.*` |

### Subscribers

| Context | Worker | Queue | Binding Key | Action |
|---------|--------|-------|-------------|--------|
| **Hearth** | [ComplianceEventSubscriber](file:///c:/Work/FarmOS/src/FarmOS.Hearth.API/Workers/ComplianceEventSubscriber.cs) | `hearth.compliance.excursion` | `iot.excursion.#` | Auto-creates CAPA records for critical IoT excursions |
| **Flora** | [FloraEventPublisher](file:///c:/Work/FarmOS/src/FarmOS.Flora.API/Workers/FloraEventPublisher.cs) | `flora.cooler.excursion` | `iot.excursion.#` | Logs cooler temperature alerts for cut flower preservation |
| **Commerce** | `InventoryProjectorWorker` | *(inline)* | `flora.batch.ready`, `hearth.batch.completed` | Updates saleable inventory projections |

---

## Planned Event Flows (Not Yet Implemented)

Per the [development roadmap](file:///c:/Work/FarmOS/docs/development-roadmap.md):

| Publisher | Event | Subscriber | Action |
|-----------|-------|-----------|--------|
| Hearth | `BatchCompleted` | Commerce | Create inventory entry |
| Commerce | `ProductionRequested` | Hearth | Display demand signal cards on dashboard |
| Pasture | `AnimalButchered` | Commerce | Create meat inventory entry |
| Pasture | `GrazingStarted` | IoT | Auto-assign paddock sensors |
| Commerce | `OrderPlaced` | Ledger | Auto-create revenue entry |

---

## Adding a New Integration Event

1. **Define the event** in `SharedKernel/EventStore/IntegrationEvents.cs`
   - Must implement `IDomainEvent`
   - Use only primitive types (no domain-specific references)

2. **Publish** from the source context's command handler or worker:
   ```csharp
   await _eventBus.PublishAsync(new MyIntegrationEvent(...), "context.entity.action", ct);
   ```

3. **Subscribe** in the consuming context with a `BackgroundService`:
   ```csharp
   await _eventBus.SubscribeAsync<MyIntegrationEvent>(
       "consumer.queue.name",
       "context.entity.*",
       HandleEventAsync,
       stoppingToken);
   ```

4. **Register** the worker in the consuming API's `Program.cs`:
   ```csharp
   builder.Services.AddHostedService<MyEventSubscriber>();
   ```
