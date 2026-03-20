# FarmOS Implementation Guide

> Detailed build guide for the sovereign C# backend. Covers solution creation, SharedKernel, all bounded contexts, API endpoints (for Deno frontend consumption), and deployment.

## Target Runtime

- **.NET 9** (C# 13, F# 9)
- **ArangoDB 3.12+** (single instance, LXC container)
- **RabbitMQ 4.x** (single instance, K3s pod)
- **Auth**: PIN-based (no external identity provider)

---

## Step 1: Create Solution Structure

```powershell
cd c:\Work\FarmOS

# Create solution
dotnet new sln -n FarmOS

# === SharedKernel ===
dotnet new classlib -n FarmOS.SharedKernel -f net10.0 -o src/FarmOS.SharedKernel
dotnet new classlib -n FarmOS.SharedKernel.FSharp -lang F# -f net10.0 -o src/FarmOS.SharedKernel.FSharp

# === Pasture ===
dotnet new classlib -n FarmOS.Pasture.Domain -f net10.0 -o src/FarmOS.Pasture.Domain
dotnet new classlib -n FarmOS.Pasture.Domain.FSharp -lang F# -f net10.0 -o src/FarmOS.Pasture.Domain.FSharp
dotnet new classlib -n FarmOS.Pasture.Application -f net10.0 -o src/FarmOS.Pasture.Application
dotnet new classlib -n FarmOS.Pasture.Infrastructure -f net10.0 -o src/FarmOS.Pasture.Infrastructure
dotnet new webapi -n FarmOS.Pasture.API -f net10.0 -o src/FarmOS.Pasture.API --no-openapi

# === Flora ===
dotnet new classlib -n FarmOS.Flora.Domain -f net10.0 -o src/FarmOS.Flora.Domain
dotnet new classlib -n FarmOS.Flora.Domain.FSharp -lang F# -f net10.0 -o src/FarmOS.Flora.Domain.FSharp
dotnet new classlib -n FarmOS.Flora.Application -f net10.0 -o src/FarmOS.Flora.Application
dotnet new classlib -n FarmOS.Flora.Infrastructure -f net10.0 -o src/FarmOS.Flora.Infrastructure
dotnet new webapi -n FarmOS.Flora.API -f net10.0 -o src/FarmOS.Flora.API --no-openapi

# === Hearth ===
dotnet new classlib -n FarmOS.Hearth.Domain -f net10.0 -o src/FarmOS.Hearth.Domain
dotnet new classlib -n FarmOS.Hearth.Domain.FSharp -lang F# -f net10.0 -o src/FarmOS.Hearth.Domain.FSharp
dotnet new classlib -n FarmOS.Hearth.Application -f net10.0 -o src/FarmOS.Hearth.Application
dotnet new classlib -n FarmOS.Hearth.Infrastructure -f net10.0 -o src/FarmOS.Hearth.Infrastructure
dotnet new webapi -n FarmOS.Hearth.API -f net10.0 -o src/FarmOS.Hearth.API --no-openapi

# === Apiary ===
dotnet new classlib -n FarmOS.Apiary.Domain -f net10.0 -o src/FarmOS.Apiary.Domain
dotnet new classlib -n FarmOS.Apiary.Domain.FSharp -lang F# -f net10.0 -o src/FarmOS.Apiary.Domain.FSharp
dotnet new classlib -n FarmOS.Apiary.Application -f net10.0 -o src/FarmOS.Apiary.Application
dotnet new classlib -n FarmOS.Apiary.Infrastructure -f net10.0 -o src/FarmOS.Apiary.Infrastructure
dotnet new webapi -n FarmOS.Apiary.API -f net10.0 -o src/FarmOS.Apiary.API --no-openapi

# === Commerce ===
dotnet new classlib -n FarmOS.Commerce.Domain -f net10.0 -o src/FarmOS.Commerce.Domain
dotnet new classlib -n FarmOS.Commerce.Application -f net10.0 -o src/FarmOS.Commerce.Application
dotnet new classlib -n FarmOS.Commerce.Infrastructure -f net10.0 -o src/FarmOS.Commerce.Infrastructure
dotnet new webapi -n FarmOS.Commerce.API -f net10.0 -o src/FarmOS.Commerce.API --no-openapi

# === Assets ===
dotnet new classlib -n FarmOS.Assets.Domain -f net10.0 -o src/FarmOS.Assets.Domain
dotnet new classlib -n FarmOS.Assets.Application -f net10.0 -o src/FarmOS.Assets.Application
dotnet new classlib -n FarmOS.Assets.Infrastructure -f net10.0 -o src/FarmOS.Assets.Infrastructure
dotnet new webapi -n FarmOS.Assets.API -f net10.0 -o src/FarmOS.Assets.API --no-openapi

# === Ledger ===
dotnet new classlib -n FarmOS.Ledger.Domain -f net10.0 -o src/FarmOS.Ledger.Domain
dotnet new classlib -n FarmOS.Ledger.Application -f net10.0 -o src/FarmOS.Ledger.Application
dotnet new classlib -n FarmOS.Ledger.Infrastructure -f net10.0 -o src/FarmOS.Ledger.Infrastructure
dotnet new webapi -n FarmOS.Ledger.API -f net10.0 -o src/FarmOS.Ledger.API --no-openapi

# === AI Adapter ===
dotnet new classlib -n FarmOS.AI -f net10.0 -o src/FarmOS.AI

# === API Gateway (single entry point) ===
dotnet new webapi -n FarmOS.Gateway -f net10.0 -o src/FarmOS.Gateway --no-openapi

# === Tests ===
dotnet new xunit -n FarmOS.Pasture.Tests -f net10.0 -o tests/FarmOS.Pasture.Tests
dotnet new xunit -n FarmOS.Flora.Tests -f net10.0 -o tests/FarmOS.Flora.Tests
dotnet new xunit -n FarmOS.Hearth.Tests -f net10.0 -o tests/FarmOS.Hearth.Tests
dotnet new xunit -n FarmOS.Apiary.Tests -f net10.0 -o tests/FarmOS.Apiary.Tests
dotnet new xunit -n FarmOS.Commerce.Tests -f net10.0 -o tests/FarmOS.Commerce.Tests
dotnet new xunit -n FarmOS.Assets.Tests -f net10.0 -o tests/FarmOS.Assets.Tests
dotnet new xunit -n FarmOS.Ledger.Tests -f net10.0 -o tests/FarmOS.Ledger.Tests
dotnet new xunit -n FarmOS.SharedKernel.Tests -f net10.0 -o tests/FarmOS.SharedKernel.Tests

# Add all projects to solution
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet sln add $_.FullName }
Get-ChildItem -Recurse -Filter *.fsproj | ForEach-Object { dotnet sln add $_.FullName }
```

### Project Reference Graph

```
SharedKernel ◄── Every .Domain project
SharedKernel.FSharp ◄── Each context's .Domain.FSharp

*.Domain ◄── *.Application
*.Application ◄── *.Infrastructure
*.Application ◄── *.API
*.Infrastructure ◄── *.API

SharedKernel ◄── FarmOS.AI
SharedKernel ◄── FarmOS.Gateway
```

Each `*.API` references its own `*.Application` and `*.Infrastructure`. **No API project references another context's projects.** Cross-context = RabbitMQ events only.

---

## Step 2: NuGet Packages

### SharedKernel

```xml
<PackageReference Include="ArangoDBNetStandard" Version="3.1.0" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.0" />
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="FluentValidation" Version="11.11.0" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

### Each *.API project

```xml
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.11.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.0.0" />  <!-- OpenAPI docs -->
```

### Gateway

The Gateway is now just an Auth API and does not need reverse proxy packages.

```xml
<!-- No YARP package needed -->
```

### Test projects

```xml
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="FluentAssertions" Version="7.1.0" />
<PackageReference Include="NSubstitute" Version="5.3.0" />
```

---

## Step 3: SharedKernel Implementation

### `AggregateRoot<TId>`

```csharp
namespace FarmOS.SharedKernel;

public abstract class AggregateRoot<TId> where TId : notnull
{
    public TenantId TenantId { get; protected set; } = TenantId.Sovereign;
    public TId Id { get; protected set; } = default!;
    public int Version { get; protected set; }

    private readonly List<IDomainEvent> _uncommittedEvents = [];
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    protected void RaiseEvent(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    public void ClearEvents() => _uncommittedEvents.Clear();

    protected abstract void Apply(IDomainEvent @event);

    public void Rehydrate(IEnumerable<IDomainEvent> history)
    {
        foreach (var e in history)
        {
            Apply(e);
            Version++;
        }
    }
}

public record TenantId(Guid Value)
{
    public static readonly TenantId Sovereign = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
}
```

### `IDomainEvent` & `EventEnvelope`

```csharp
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public record EventEnvelope
{
    public required string _key { get; init; }
    public required string AggregateId { get; init; }
    public required string AggregateType { get; init; }
    public required string EventType { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required DateTimeOffset StoredAt { get; init; }
    public required string Payload { get; init; }           // Serialized JSON
    public required string CorrelationId { get; init; }
    public required string UserId { get; init; }
    public required string TenantId { get; init; }
}
```

### `IEventStore`

```csharp
public interface IEventStore
{
    Task<T> LoadAsync<T>(string aggregateId, CancellationToken ct)
        where T : AggregateRoot<???>, new();   // See generic approach below

    Task AppendAsync<TId>(AggregateRoot<TId> aggregate, string userId,
        string correlationId, CancellationToken ct) where TId : notnull;

    Task<IReadOnlyList<EventEnvelope>> GetEventsAsync(string aggregateId,
        int fromVersion, CancellationToken ct);

    Task<IReadOnlyList<EventEnvelope>> GetAllEventsAsync(
        long fromPosition, int batchSize, CancellationToken ct);
}
```

### `Result<T, E>`

```csharp
public readonly struct Result<T, E>
{
    private readonly T? _value;
    private readonly E? _error;
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value) { _value = value; IsSuccess = true; _error = default; }
    private Result(E error) { _error = error; IsSuccess = false; _value = default; }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on failure");
    public E Error => IsFailure ? _error! : throw new InvalidOperationException("No error on success");

    public static Result<T, E> Success(T value) => new(value);
    public static Result<T, E> Failure(E error) => new(error);

    public static implicit operator Result<T, E>(T value) => Success(value);
    public static implicit operator Result<T, E>(E error) => Failure(error);
}

public record DomainError(string Code, string Message);
```

### `ICommand<T>` / `IQuery<T>` (MediatR contracts)

```csharp
public interface ICommand<TResponse> : IRequest<Result<TResponse, DomainError>>;
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse, DomainError>>
    where TCommand : ICommand<TResponse>;

public interface IQuery<TResponse> : IRequest<TResponse?>;
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse?>
    where TQuery : IQuery<TResponse>;
```

### `IEventBus` (RabbitMQ abstraction)

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct) where T : IDomainEvent;
    Task SubscribeAsync<T>(string queueName, string bindingKey,
        Func<T, CancellationToken, Task> handler, CancellationToken ct) where T : IDomainEvent;
}
```

### PIN-Based Auth

```csharp
public record FarmUser(string Id, string Name, string Role, string PinHash);
// Roles: "steward", "partner", "apprentice", "helper"

public interface IAuthService
{
    Task<FarmUser?> AuthenticateAsync(string pin, CancellationToken ct);
    string HashPin(string pin);
}

// Middleware: reads X-Farm-Pin header or cookie, resolves to FarmUser
// Sets HttpContext.User with claims: sub, name, role
```

---

## Step 4: ArangoDB Setup

### Collections to create (run once)

```javascript
// === Event Store collections (append-only) ===
db._create("pasture_events");
db._create("flora_events");
db._create("hearth_events");
db._create("apiary_events");
db._create("commerce_events");
db._create("assets_events");
db._create("ledger_events");
db._create("shared_events");        // Observations, InputLogs, LabTests

// === Read Projection collections ===
db._create("pasture_paddock_view");
db._create("pasture_animal_view");
db._create("pasture_herd_view");
db._create("flora_guild_view");
db._create("flora_flowerbed_view");
db._create("flora_seedlot_view");
db._create("hearth_batch_view");
db._create("hearth_culture_view");
db._create("apiary_hive_view");
db._create("apiary_inspection_view");
db._create("commerce_subscription_view");
db._create("commerce_order_view");
db._create("commerce_customer_view");
db._create("commerce_inventory_view");
db._create("assets_equipment_view");
db._create("assets_structure_view");
db._create("assets_water_view");
db._create("assets_compost_view");
db._create("assets_sensor_view");
db._create("assets_material_view");
db._create("ledger_expense_view");
db._create("ledger_revenue_view");
db._create("shared_observation_view");
db._create("shared_labtest_view");

// === Graph ===
db._createEdgeCollection("grazed");
db._createEdgeCollection("follows_in_rotation");
db._createEdgeCollection("member_of_herd");
db._createEdgeCollection("born_from");
db._createEdgeCollection("sired_by");
db._createEdgeCollection("member_of_guild");
db._createEdgeCollection("fixes_nitrogen_for");
db._createEdgeCollection("pollinates");
db._createEdgeCollection("planted_in");
db._createEdgeCollection("descended_from");
db._createEdgeCollection("produced_from");
db._createEdgeCollection("harvested_from");
db._createEdgeCollection("located_in");
db._createEdgeCollection("monitors");            // Sensor → Asset
db._createEdgeCollection("applied_to");          // CompostBatch → Paddock

var graph = db._createGraph("farm_graph", [
    { collection: "grazed", from: ["pasture_herd_view"], to: ["pasture_paddock_view"] },
    { collection: "member_of_herd", from: ["pasture_animal_view"], to: ["pasture_herd_view"] },
    { collection: "born_from", from: ["pasture_animal_view"], to: ["pasture_animal_view"] },
    { collection: "member_of_guild", from: ["flora_guild_view"], to: ["flora_guild_view"] },
    { collection: "descended_from", from: ["hearth_culture_view"], to: ["hearth_culture_view"] },
    { collection: "pollinates", from: ["apiary_hive_view"], to: ["flora_guild_view"] },
    { collection: "located_in", from: ["apiary_hive_view"], to: ["pasture_paddock_view"] },
    { collection: "monitors", from: ["assets_sensor_view"], to: ["pasture_paddock_view"] },
    { collection: "applied_to", from: ["assets_compost_view"], to: ["pasture_paddock_view"] }
]);

// Indexes
db.pasture_events.ensureIndex({ type: "persistent", fields: ["AggregateId", "Version"] });
// ... repeat for all event collections
db.pasture_paddock_view.ensureIndex({ type: "persistent", fields: ["Status"] });
db.pasture_animal_view.ensureIndex({ type: "persistent", fields: ["Species", "Status"] });
db.hearth_batch_view.ensureIndex({ type: "persistent", fields: ["Phase", "BatchType"] });
```

---

## Step 5: API Gateway (Single Entry Point)

The Gateway uses Caddy to route requests to the correct context API. All Deno frontends call **one base URL**.

### Caddyfile

```caddyfile
farmos.local:5000 {
	# Route context APIs
	reverse_proxy /api/pasture/* localhost:5101
	reverse_proxy /api/flora/* localhost:5102
	reverse_proxy /api/hearth/* localhost:5103
	reverse_proxy /api/apiary/* localhost:5104
	reverse_proxy /api/commerce/* localhost:5105
	reverse_proxy /api/assets/* localhost:5106
	reverse_proxy /api/ledger/* localhost:5107

	# Auth API (The Gateway C# project)
	reverse_proxy /api/auth/* localhost:5050
}
```

**Gateway URL**: `http://farmos.local:5000` (or `https://farmos.local` with self-signed cert)

---

## Step 6: API Endpoint Catalog (For Deno Frontends)

> This is what your Deno apps call. All requests go through the gateway at `http://farmos.local:5000`.

### Auth

| Method | Path | Body | Response | Notes |
|--------|------|------|----------|-------|
| `POST` | `/api/auth/login` | `{ "pin": "1234" }` | `{ "token": "...", "user": { "name", "role" } }` | Returns JWT-like session token |
| `GET` | `/api/auth/me` | — | `{ "name", "role" }` | Current user from token |

### Pasture

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/pasture/paddocks` | — | `PaddockStatusDto[]` |
| `GET` | `/api/pasture/paddocks/{id}` | — | `PaddockDetailDto` |
| `POST` | `/api/pasture/paddocks` | `CreatePaddockCmd` | `{ "id": "guid" }` |
| `PUT` | `/api/pasture/paddocks/{id}/boundary` | `{ "geometry": GeoJson }` | `204` |
| `POST` | `/api/pasture/paddocks/{id}/begin-grazing` | `{ "herdId", "date" }` | `{ "id": "guid" }` |
| `POST` | `/api/pasture/paddocks/{id}/end-grazing` | `{ "date" }` | `204` |
| `POST` | `/api/pasture/paddocks/{id}/biomass` | `BiomassEstimate` | `204` |
| `POST` | `/api/pasture/paddocks/{id}/soil-test` | `SoilProfile` | `204` |
| `GET` | `/api/pasture/animals` | `?species=&status=` | `AnimalSummaryDto[]` |
| `GET` | `/api/pasture/animals/{id}` | — | `AnimalDetailDto` |
| `POST` | `/api/pasture/animals` | `RegisterAnimalCmd` | `{ "id": "guid" }` |
| `POST` | `/api/pasture/animals/{id}/isolate` | `{ "reason", "date" }` | `204` |
| `POST` | `/api/pasture/animals/{id}/treatment` | `Treatment` | `204` |
| `POST` | `/api/pasture/animals/{id}/pregnancy` | `{ "expectedDue", "sireId?" }` | `204` |
| `POST` | `/api/pasture/animals/{id}/birth` | `{ "offspringId", "date" }` | `204` |
| `POST` | `/api/pasture/animals/{id}/butcher` | `ButcherRecord` | `204` |
| `POST` | `/api/pasture/animals/{id}/sell` | `SaleRecord` | `204` |
| `POST` | `/api/pasture/animals/{id}/weight` | `{ "weight": Quantity }` | `204` |
| `GET` | `/api/pasture/herds` | — | `HerdSummaryDto[]` |
| `GET` | `/api/pasture/herds/{id}` | — | `HerdDetailDto` |
| `POST` | `/api/pasture/herds` | `CreateHerdCmd` | `{ "id": "guid" }` |
| `POST` | `/api/pasture/herds/{id}/move` | `{ "paddockId", "date" }` | `204` |
| `POST` | `/api/pasture/herds/{id}/add-animal` | `{ "animalId" }` | `204` |
| `POST` | `/api/pasture/herds/{id}/remove-animal` | `{ "animalId" }` | `204` |

### Flora

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/flora/guilds` | — | `GuildSummaryDto[]` |
| `GET` | `/api/flora/guilds/{id}` | — | `GuildDetailDto` |
| `POST` | `/api/flora/guilds` | `CreateGuildCmd` | `{ "id" }` |
| `POST` | `/api/flora/guilds/{id}/members` | `GuildMember` | `204` |
| `GET` | `/api/flora/beds` | `?block=` | `FlowerBedDto[]` |
| `GET` | `/api/flora/beds/{id}` | — | `FlowerBedDetailDto` |
| `POST` | `/api/flora/beds` | `CreateBedCmd` | `{ "id" }` |
| `POST` | `/api/flora/beds/{id}/successions` | `PlanSuccessionCmd` | `{ "id" }` |
| `POST` | `/api/flora/beds/{id}/successions/{sid}/seed` | `RecordSeedingCmd` | `204` |
| `POST` | `/api/flora/beds/{id}/successions/{sid}/transplant` | `RecordTransplantCmd` | `204` |
| `POST` | `/api/flora/beds/{id}/successions/{sid}/harvest` | `RecordHarvestCmd` | `204` |
| `GET` | `/api/flora/seeds` | — | `SeedLotDto[]` |
| `POST` | `/api/flora/seeds` | `CreateSeedLotCmd` | `{ "id" }` |
| `POST` | `/api/flora/seeds/{id}/withdraw` | `WithdrawCmd` | `204` |

### Hearth

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/hearth/batches` | `?type=&phase=` | `BatchSummaryDto[]` |
| `GET` | `/api/hearth/batches/{id}` | — | `BatchDetailDto` (includes CCP/pH log) |
| `POST` | `/api/hearth/batches/sourdough` | `StartSourdoughCmd` | `{ "id" }` |
| `POST` | `/api/hearth/batches/kombucha` | `StartKombuchaCmd` | `{ "id" }` |
| `POST` | `/api/hearth/batches/{id}/ccp-reading` | `HACCPReading` | `204` |
| `POST` | `/api/hearth/batches/{id}/ph-reading` | `PHReading` | `204` |
| `POST` | `/api/hearth/batches/{id}/advance-phase` | `{ "phase" }` | `204` |
| `POST` | `/api/hearth/batches/{id}/complete` | `{ "yield": Quantity }` | `204` |
| `POST` | `/api/hearth/batches/{id}/discard` | `{ "reason" }` | `204` |
| `GET` | `/api/hearth/cultures` | — | `CultureSummaryDto[]` |
| `GET` | `/api/hearth/cultures/{id}` | — | `CultureDetailDto` (includes lineage) |
| `POST` | `/api/hearth/cultures` | `CreateCultureCmd` | `{ "id" }` |
| `POST` | `/api/hearth/cultures/{id}/feed` | `FeedingRecord` | `204` |
| `POST` | `/api/hearth/cultures/{id}/split` | `{ "newName" }` | `{ "offspringId" }` |

### Apiary

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/apiary/hives` | — | `HiveSummaryDto[]` |
| `GET` | `/api/apiary/hives/{id}` | — | `HiveDetailDto` |
| `POST` | `/api/apiary/hives` | `CreateHiveCmd` | `{ "id" }` |
| `POST` | `/api/apiary/hives/{id}/inspections` | `Inspection` | `{ "id" }` |
| `POST` | `/api/apiary/hives/{id}/harvest` | `HoneyHarvest` | `204` |
| `POST` | `/api/apiary/hives/{id}/install-queen` | `{ "queenId", "date" }` | `204` |
| `GET` | `/api/apiary/hives/{id}/inspections` | — | `InspectionSummaryDto[]` |

### Commerce

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/commerce/customers` | — | `CustomerDto[]` |
| `POST` | `/api/commerce/customers` | `CreateCustomerCmd` | `{ "id" }` |
| `GET` | `/api/commerce/subscriptions` | — | `SubscriptionDto[]` |
| `POST` | `/api/commerce/subscriptions` | `CreateSubscriptionCmd` | `{ "id" }` |
| `POST` | `/api/commerce/subscriptions/{id}/pause` | `{ "until" }` | `204` |
| `POST` | `/api/commerce/subscriptions/{id}/cancel` | `{ "reason" }` | `204` |
| `GET` | `/api/commerce/orders` | `?status=` | `OrderSummaryDto[]` |
| `GET` | `/api/commerce/orders/{id}` | — | `OrderDetailDto` |
| `POST` | `/api/commerce/orders` | `PlaceOrderCmd` | `{ "id" }` |
| `POST` | `/api/commerce/orders/{id}/pack` | — | `204` |
| `POST` | `/api/commerce/orders/{id}/ready` | — | `204` |
| `POST` | `/api/commerce/orders/{id}/complete` | — | `204` |
| `GET` | `/api/commerce/inventory` | — | `InventoryProjectionDto[]` |

### Assets

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/assets/equipment` | — | `EquipmentDto[]` |
| `POST` | `/api/assets/equipment` | `CreateEquipmentCmd` | `{ "id" }` |
| `POST` | `/api/assets/equipment/{id}/maintenance` | `MaintenanceRecord` | `204` |
| `GET` | `/api/assets/structures` | — | `StructureDto[]` |
| `POST` | `/api/assets/structures` | `CreateStructureCmd` | `{ "id" }` |
| `GET` | `/api/assets/water` | — | `WaterSourceDto[]` |
| `POST` | `/api/assets/water` | `CreateWaterCmd` | `{ "id" }` |
| `GET` | `/api/assets/compost` | — | `CompostBatchDto[]` |
| `POST` | `/api/assets/compost` | `StartCompostCmd` | `{ "id" }` |
| `POST` | `/api/assets/compost/{id}/temperature` | `CompostReading` | `204` |
| `POST` | `/api/assets/compost/{id}/turn` | `{ "date", "notes?" }` | `204` |
| `POST` | `/api/assets/compost/{id}/apply` | `{ "paddockId", "amount" }` | `204` |
| `GET` | `/api/assets/sensors` | — | `SensorDto[]` |
| `POST` | `/api/assets/sensors` | `RegisterSensorCmd` | `{ "id" }` |
| `GET` | `/api/assets/materials` | — | `MaterialDto[]` |
| `POST` | `/api/assets/materials` | `CreateMaterialCmd` | `{ "id" }` |
| `POST` | `/api/assets/materials/{id}/withdraw` | `{ "amount", "purpose" }` | `204` |
| `POST` | `/api/assets/materials/{id}/restock` | `{ "amount", "supplier?" }` | `204` |

### Ledger

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/ledger/expenses` | `?from=&to=&category=` | `ExpenseDto[]` |
| `POST` | `/api/ledger/expenses` | `RecordExpenseCmd` | `{ "id" }` |
| `GET` | `/api/ledger/revenue` | `?from=&to=&source=` | `RevenueDto[]` |
| `POST` | `/api/ledger/revenue` | `RecordRevenueCmd` | `{ "id" }` |
| `GET` | `/api/ledger/summary` | `?from=&to=` | `ProfitLossDto` |

### Shared (Observations, Inputs, Lab Tests)

| Method | Path | Body | Response |
|--------|------|------|----------|
| `GET` | `/api/shared/observations` | `?assetId=&tag=` | `ObservationDto[]` |
| `POST` | `/api/shared/observations` | `CreateObservationCmd` | `{ "id" }` |
| `GET` | `/api/shared/inputs` | `?assetId=` | `InputLogDto[]` |
| `POST` | `/api/shared/inputs` | `RecordInputCmd` | `{ "id" }` |
| `GET` | `/api/shared/lab-tests` | `?assetId=&type=` | `LabTestDto[]` |
| `POST` | `/api/shared/lab-tests` | `RecordLabTestCmd` | `{ "id" }` |

### Graph Queries

| Method | Path | Response |
|--------|------|----------|
| `GET` | `/api/graph/paddock/{id}/grazing-history` | `GrazingHistoryDto[]` |
| `GET` | `/api/graph/paddock/next-to-graze` | `PaddockRecommendationDto[]` |
| `GET` | `/api/graph/guild/{id}/composition` | `GuildCompositionDto` |
| `GET` | `/api/graph/culture/{id}/lineage` | `CultureLineageDto` |
| `GET` | `/api/graph/animal/{id}/lineage` | `AnimalLineageDto` |
| `GET` | `/api/graph/hive/{id}/pollination-map` | `PollinationMapDto` |

---

## Step 7: Deno Frontend Conventions

Your three Deno Fresh apps go here:

```
FarmOS/
├── frontends/
│   ├── field-ops/          ← Deno Fresh app (port 3001)
│   │   ├── deno.json
│   │   ├── fresh.config.ts
│   │   ├── routes/
│   │   │   ├── _app.tsx
│   │   │   ├── index.tsx          → /
│   │   │   ├── paddocks/
│   │   │   │   ├── index.tsx      → /paddocks
│   │   │   │   └── [id].tsx       → /paddocks/:id
│   │   │   ├── animals/
│   │   │   ├── herds/
│   │   │   ├── flowers/
│   │   │   ├── guilds/
│   │   │   ├── tasks/
│   │   │   └── api/               → BFF proxy if needed
│   │   ├── islands/               → Interactive components
│   │   │   ├── PaddockMap.tsx
│   │   │   ├── TaskBoard.tsx
│   │   │   └── AnimalFilter.tsx
│   │   └── lib/
│   │       └── api.ts             → fetch wrapper for gateway
│   │
│   ├── hearth-os/          ← Deno Fresh app (port 3002)
│   │   ├── routes/
│   │   │   ├── batches/
│   │   │   ├── cultures/
│   │   │   ├── hives/
│   │   │   ├── haccp/
│   │   │   └── compost/
│   │   └── islands/
│   │       ├── PHChart.tsx
│   │       ├── BatchStatusCards.tsx
│   │       └── FeedingTimer.tsx
│   │
│   └── edge-portal/        ← Deno Fresh app (port 3003, deployed externally)
│       ├── routes/
│       │   ├── index.tsx          → Available inventory
│       │   ├── subscribe.tsx
│       │   ├── orders/
│       │   └── bakery.tsx
│       └── islands/
│           ├── SubscriptionForm.tsx
│           └── BakeryOrderForm.tsx
```

### API Client (`lib/api.ts`)

```typescript
const GATEWAY = Deno.env.get("FARMOS_GATEWAY") ?? "http://farmos.local:5000";

export async function farmFetch<T>(
  path: string,
  options?: RequestInit,
): Promise<T> {
  const res = await fetch(`${GATEWAY}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Farm-Pin": getCookiePin(),  // Or from session
      ...options?.headers,
    },
  });
  if (!res.ok) throw new Error(`${res.status}: ${await res.text()}`);
  return res.json();
}

// Examples:
// const paddocks = await farmFetch<PaddockDto[]>("/api/pasture/paddocks");
// await farmFetch("/api/pasture/herds/xxx/move", {
//   method: "POST",
//   body: JSON.stringify({ paddockId: "yyy", date: "2026-03-15" }),
// });
```

---

## Step 8: Docker & K3s Deployment

### Dockerfile (shared pattern for all APIs)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FarmOS.Pasture.API -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FarmOS.Pasture.API.dll"]
```

### K3s Manifests (`deploy/k3s/`)

```yaml
# deploy/k3s/pasture.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pasture-api
spec:
  replicas: 1
  selector:
    matchLabels: { app: pasture-api }
  template:
    metadata:
      labels: { app: pasture-api }
    spec:
      containers:
        - name: pasture-api
          image: farmos/pasture-api:latest
          ports: [{ containerPort: 8080 }]
          env:
            - name: ArangoDB__Url
              value: "http://arango.local:8529"
            - name: RabbitMQ__Host
              value: "rabbitmq.default.svc.cluster.local"
---
apiVersion: v1
kind: Service
metadata:
  name: pasture-api
spec:
  selector: { app: pasture-api }
  ports: [{ port: 80, targetPort: 8080 }]
```

Repeat for each context API. The gateway routes to `pasture-api.default.svc.cluster.local:80` etc.

---

## Verification Plan

### Automated Tests

```powershell
# Run all unit tests
dotnet test tests/ --verbosity normal

# Run specific context
dotnet test tests/FarmOS.Pasture.Tests
```

### Manual Testing (Phase 1)

1. Start ArangoDB, RabbitMQ, and Pasture.API locally
2. Create a paddock: `POST /api/pasture/paddocks`
3. Register an animal: `POST /api/pasture/animals`
4. Create a herd and add the animal
5. Move herd to paddock (should succeed, no grazing history)
6. End grazing after 2 days
7. Attempt to re-graze immediately → **expect 400 error** (45-day rule)
8. Query paddock status → verify rest days counting
