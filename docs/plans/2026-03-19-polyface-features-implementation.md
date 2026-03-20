# Polyface-Style Feature Expansion — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add 4 new bounded contexts (Crew, Campus, Counter, Codex), extend 2 existing ones (Commerce CRM, Ledger enterprise accounting), and add 1 cross-cutting context (Codex) to support a Polyface Farms-style diversified operation.

**Architecture:** Event-sourced DDD with CQRS. Each bounded context gets Domain/Application/Infrastructure/API layers. Commands dispatched via MediatR, events persisted to ArangoDB via MessagePack serialization, read models built via event replay projections. Frontend apps use Deno Fresh Islands architecture.

**Tech Stack:** .NET 10, ArangoDB, MediatR 12.4, MessagePack, xUnit + FluentAssertions + NSubstitute, Deno Fresh/Preact, Tailwind CSS

**Design Doc:** `docs/plans/2026-03-19-polyface-features-design.md`

---

## Conventions Reference

Before implementing any task, study these existing files to match patterns exactly:

| Pattern | Reference File |
|---------|---------------|
| Aggregate base class | `src/FarmOS.SharedKernel/Domain/AggregateRoot.cs` |
| Typed IDs | `src/FarmOS.Commerce.Domain/Types.cs` — `record XId(Guid Value) { static New(); ToString(); }` |
| Events | `src/FarmOS.Commerce.Domain/Events.cs` — `record EventName(..., DateTimeOffset OccurredAt) : IDomainEvent` |
| Aggregate impl | `src/FarmOS.Commerce.Domain/Aggregates/CommerceAggregates.cs` — `static Create()`, `RaiseEvent()`, `Apply()` |
| Commands | `src/FarmOS.Commerce.Application/Commands/CommerceCommands.cs` — `record XCommand(...) : ICommand<Guid>` |
| Handlers | `src/FarmOS.Commerce.Application/Commands/Handlers/CommerceCommandHandlers.cs` — `ICommandHandler<TCmd, Guid>` |
| EventStore interface | `src/FarmOS.Commerce.Application/ICommerceEventStore.cs` |
| EventStore impl | `src/FarmOS.Commerce.Infrastructure/CommerceEventStore.cs` — `EventTypeMap`, `LoadAsync`, `SaveAsync` |
| Endpoints | `src/FarmOS.Commerce.API/CommerceEndpoints.cs` — `MapGroup`, MediatR dispatch, `Result.Match()` |
| Program.cs | `src/FarmOS.Commerce.API/Program.cs` — ArangoDB, DI, CORS, MessagePack, endpoint mapping |
| Domain csproj | `src/FarmOS.Commerce.Domain/FarmOS.Commerce.Domain.csproj` — refs SharedKernel |
| Application csproj | `src/FarmOS.Commerce.Application/FarmOS.Commerce.Application.csproj` — refs Domain + MediatR |
| Infrastructure csproj | `src/FarmOS.Commerce.Infrastructure/FarmOS.Commerce.Infrastructure.csproj` — refs Application |
| API csproj | `src/FarmOS.Commerce.API/FarmOS.Commerce.API.csproj` — Sdk.Web, refs Application + Infrastructure |
| Domain tests | `src/FarmOS.Hearth.Domain.Tests/MushroomBatchTests.cs` — xUnit, FluentAssertions, Arrange/Act/Assert |
| Test csproj | `src/FarmOS.Hearth.Domain.Tests/FarmOS.Hearth.Domain.Tests.csproj` — xunit, FluentAssertions, NSubstitute, coverlet |
| CQRS contracts | `src/FarmOS.SharedKernel/CQRS/Contracts.cs` — `ICommand<T>`, `ICommandHandler<T,R>`, `IQuery<T>`, `IQueryHandler<T,R>` |
| Frontend route | `frontend/apiary-os/routes/index.tsx` — SSR page with island import |
| Frontend island | `frontend/apiary-os/islands/FinancialDashboard.tsx` — `useSignal`, dynamic API import |
| Frontend API client | `frontend/apiary-os/utils/farmos-client.ts` — typed `fetchFarmOS<T>()` |

---

## Phase 1: Foundation (4 contexts, all parallel — no dependencies)

Each context follows this build sequence:
1. Domain layer (Types, Events, Aggregates)
2. Domain tests
3. Application layer (Commands, Handlers, EventStore interface)
4. Infrastructure layer (EventStore impl)
5. API layer (Endpoints, Program.cs, csproj files)
6. Solution integration (add to .sln, verify build)

---

### Task 1: Crew Context — Domain Layer

**Files:**
- Create: `src/FarmOS.Crew.Domain/FarmOS.Crew.Domain.csproj`
- Create: `src/FarmOS.Crew.Domain/Types.cs`
- Create: `src/FarmOS.Crew.Domain/Events.cs`
- Create: `src/FarmOS.Crew.Domain/Aggregates/Worker.cs`
- Create: `src/FarmOS.Crew.Domain/Aggregates/Shift.cs`

**Step 1: Create the Domain csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\FarmOS.SharedKernel\FarmOS.SharedKernel.csproj" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Step 2: Create Types.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain;

// ─── Typed IDs ──────────────────────────────────────────────────────
public record WorkerId(Guid Value) { public static WorkerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record ShiftId(Guid Value) { public static ShiftId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

// ─── Enums ──────────────────────────────────────────────────────────
public enum WorkerRole { Employee, Apprentice, Volunteer, Intern }
public enum WorkerStatus { Active, OnLeave, Completed, Terminated }
public enum CertificationType { FoodHandler, FirstAid, CPR, PesticideApplicator, EquipmentOperator, CDL, OrganicInspector, Custom }
public enum Enterprise { Pasture, Flora, Hearth, Apiary, Commerce, Assets, General }
public enum ShiftStatus { Scheduled, InProgress, Completed, Cancelled }

// ─── Value Objects ──────────────────────────────────────────────────
public record EmergencyContact(string Name, string Relationship, string Phone);
public record WorkerProfile(string Name, string Email, string? Phone, WorkerRole Role, EmergencyContact? Emergency, string? HousingAssignment, DateOnly StartDate);
public record Certification(CertificationType Type, string Name, DateOnly Issued, DateOnly? Expires, string? IssuingBody, string? DocumentPath);
public record ShiftEntry(WorkerId WorkerId, Enterprise Enterprise, DateOnly Date, TimeOnly Start, TimeOnly End, string? TaskDescription, string? Notes);
```

**Step 3: Create Events.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Events;

// ─── Worker ─────────────────────────────────────────────────────────
public record WorkerRegistered(WorkerId Id, WorkerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record WorkerProfileUpdated(WorkerId Id, WorkerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record WorkerDeactivated(WorkerId Id, WorkerStatus NewStatus, string? Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record CertificationAdded(WorkerId Id, Certification Cert, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Shift ──────────────────────────────────────────────────────────
public record ShiftScheduled(ShiftId Id, ShiftEntry Entry, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftStarted(ShiftId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftCompleted(ShiftId Id, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record ShiftCancelled(ShiftId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Step 4: Create Worker aggregate**

```csharp
using FarmOS.Crew.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Aggregates;

public sealed class Worker : AggregateRoot<WorkerId>
{
    public WorkerProfile Profile { get; private set; } = new("", "", null, WorkerRole.Employee, null, null, DateOnly.MinValue);
    public WorkerStatus Status { get; private set; }
    private readonly List<Certification> _certifications = [];
    public IReadOnlyList<Certification> Certifications => _certifications;

    public static Worker Register(WorkerProfile profile)
    {
        var worker = new Worker();
        worker.RaiseEvent(new WorkerRegistered(WorkerId.New(), profile, DateTimeOffset.UtcNow));
        return worker;
    }

    public void UpdateProfile(WorkerProfile profile) =>
        RaiseEvent(new WorkerProfileUpdated(Id, profile, DateTimeOffset.UtcNow));

    public Result<WorkerId, DomainError> Deactivate(WorkerStatus newStatus, string? reason)
    {
        if (Status != WorkerStatus.Active)
            return DomainError.Conflict("Only active workers can be deactivated.");
        if (newStatus == WorkerStatus.Active)
            return DomainError.Validation("Deactivation status cannot be Active.");
        RaiseEvent(new WorkerDeactivated(Id, newStatus, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<WorkerId, DomainError> AddCertification(Certification cert)
    {
        if (Status != WorkerStatus.Active)
            return DomainError.Conflict("Cannot add certifications to inactive workers.");
        RaiseEvent(new CertificationAdded(Id, cert, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WorkerRegistered e: Id = e.Id; Profile = e.Profile; Status = WorkerStatus.Active; break;
            case WorkerProfileUpdated e: Profile = e.Profile; break;
            case WorkerDeactivated e: Status = e.NewStatus; break;
            case CertificationAdded e: _certifications.Add(e.Cert); break;
        }
    }
}
```

**Step 5: Create Shift aggregate**

```csharp
using FarmOS.Crew.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.Domain.Aggregates;

public sealed class Shift : AggregateRoot<ShiftId>
{
    public ShiftEntry Entry { get; private set; } = default!;
    public ShiftStatus Status { get; private set; }

    public static Shift Schedule(ShiftEntry entry)
    {
        var shift = new Shift();
        shift.RaiseEvent(new ShiftScheduled(ShiftId.New(), entry, DateTimeOffset.UtcNow));
        return shift;
    }

    public Result<ShiftId, DomainError> Start()
    {
        if (Status != ShiftStatus.Scheduled)
            return DomainError.Conflict("Only scheduled shifts can be started.");
        RaiseEvent(new ShiftStarted(Id, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ShiftId, DomainError> Complete(string? notes)
    {
        if (Status != ShiftStatus.InProgress)
            return DomainError.Conflict("Only in-progress shifts can be completed.");
        RaiseEvent(new ShiftCompleted(Id, notes, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ShiftId, DomainError> Cancel(string reason)
    {
        if (Status is ShiftStatus.Completed or ShiftStatus.Cancelled)
            return DomainError.Conflict("Cannot cancel a completed or already cancelled shift.");
        RaiseEvent(new ShiftCancelled(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ShiftScheduled e: Id = e.Id; Entry = e.Entry; Status = ShiftStatus.Scheduled; break;
            case ShiftStarted: Status = ShiftStatus.InProgress; break;
            case ShiftCompleted: Status = ShiftStatus.Completed; break;
            case ShiftCancelled: Status = ShiftStatus.Cancelled; break;
        }
    }
}
```

**Step 6: Commit**

```bash
git add src/FarmOS.Crew.Domain/
git commit -m "feat(crew): add domain layer — Worker and Shift aggregates with types and events"
```

---

### Task 2: Crew Context — Domain Tests

**Files:**
- Create: `src/FarmOS.Crew.Domain.Tests/FarmOS.Crew.Domain.Tests.csproj`
- Create: `src/FarmOS.Crew.Domain.Tests/WorkerTests.cs`
- Create: `src/FarmOS.Crew.Domain.Tests/ShiftTests.cs`

**Step 1: Create test csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\FarmOS.Crew.Domain\FarmOS.Crew.Domain.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Write WorkerTests.cs**

```csharp
using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FluentAssertions;

namespace FarmOS.Crew.Domain.Tests;

public class WorkerTests
{
    private static WorkerProfile MakeProfile(string name = "Jane Doe") =>
        new(name, "jane@farm.local", "555-0100", WorkerRole.Employee,
            new EmergencyContact("John Doe", "Spouse", "555-0101"),
            null, new DateOnly(2026, 4, 1));

    [Fact]
    public void Register_ShouldCreateActiveWorkerAndRaiseEvent()
    {
        var profile = MakeProfile();

        var worker = Worker.Register(profile);

        worker.Status.Should().Be(WorkerStatus.Active);
        worker.Profile.Name.Should().Be("Jane Doe");
        worker.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkerRegistered>();
    }

    [Fact]
    public void Deactivate_ShouldSucceed_WhenActive()
    {
        var worker = Worker.Register(MakeProfile());
        worker.ClearEvents();

        var result = worker.Deactivate(WorkerStatus.Terminated, "End of season");

        result.IsSuccess.Should().BeTrue();
        worker.Status.Should().Be(WorkerStatus.Terminated);
        worker.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkerDeactivated>();
    }

    [Fact]
    public void Deactivate_ShouldFail_WhenAlreadyInactive()
    {
        var worker = Worker.Register(MakeProfile());
        worker.Deactivate(WorkerStatus.Terminated, "Done");
        worker.ClearEvents();

        var result = worker.Deactivate(WorkerStatus.Completed, "Nope");

        result.IsFailure.Should().BeTrue();
        worker.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddCertification_ShouldSucceed_WhenActive()
    {
        var worker = Worker.Register(MakeProfile());
        worker.ClearEvents();
        var cert = new Certification(CertificationType.FoodHandler, "ServSafe",
            new DateOnly(2026, 3, 1), new DateOnly(2031, 3, 1), "ServSafe", null);

        var result = worker.AddCertification(cert);

        result.IsSuccess.Should().BeTrue();
        worker.Certifications.Should().ContainSingle();
        worker.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CertificationAdded>();
    }

    [Fact]
    public void AddCertification_ShouldFail_WhenInactive()
    {
        var worker = Worker.Register(MakeProfile());
        worker.Deactivate(WorkerStatus.Terminated, "Gone");
        worker.ClearEvents();

        var result = worker.AddCertification(
            new Certification(CertificationType.FirstAid, "Red Cross",
                DateOnly.MinValue, null, null, null));

        result.IsFailure.Should().BeTrue();
        worker.Certifications.Should().BeEmpty();
    }
}
```

**Step 3: Write ShiftTests.cs**

```csharp
using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FluentAssertions;

namespace FarmOS.Crew.Domain.Tests;

public class ShiftTests
{
    private static ShiftEntry MakeEntry() =>
        new(WorkerId.New(), Enterprise.Hearth, new DateOnly(2026, 4, 1),
            new TimeOnly(8, 0), new TimeOnly(12, 0), "Morning bake", null);

    [Fact]
    public void Schedule_ShouldCreateShiftAndRaiseEvent()
    {
        var shift = Shift.Schedule(MakeEntry());

        shift.Status.Should().Be(ShiftStatus.Scheduled);
        shift.Entry.Enterprise.Should().Be(Enterprise.Hearth);
        shift.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShiftScheduled>();
    }

    [Fact]
    public void Start_ShouldSucceed_WhenScheduled()
    {
        var shift = Shift.Schedule(MakeEntry());
        shift.ClearEvents();

        var result = shift.Start();

        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.InProgress);
    }

    [Fact]
    public void Complete_ShouldSucceed_WhenInProgress()
    {
        var shift = Shift.Schedule(MakeEntry());
        shift.Start();
        shift.ClearEvents();

        var result = shift.Complete("All done");

        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldFail_WhenNotInProgress()
    {
        var shift = Shift.Schedule(MakeEntry());
        // Still Scheduled, not started

        var result = shift.Complete(null);

        result.IsFailure.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Scheduled);
    }

    [Fact]
    public void Cancel_ShouldSucceed_WhenScheduled()
    {
        var shift = Shift.Schedule(MakeEntry());
        shift.ClearEvents();

        var result = shift.Cancel("Rain day");

        result.IsSuccess.Should().BeTrue();
        shift.Status.Should().Be(ShiftStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldFail_WhenCompleted()
    {
        var shift = Shift.Schedule(MakeEntry());
        shift.Start();
        shift.Complete(null);
        shift.ClearEvents();

        var result = shift.Cancel("Oops");

        result.IsFailure.Should().BeTrue();
    }
}
```

**Step 4: Run tests**

```bash
dotnet test src/FarmOS.Crew.Domain.Tests/ -v minimal
```
Expected: All 9 tests pass.

**Step 5: Commit**

```bash
git add src/FarmOS.Crew.Domain.Tests/
git commit -m "test(crew): add Worker and Shift domain tests"
```

---

### Task 3: Crew Context — Application Layer

**Files:**
- Create: `src/FarmOS.Crew.Application/FarmOS.Crew.Application.csproj`
- Create: `src/FarmOS.Crew.Application/ICrewEventStore.cs`
- Create: `src/FarmOS.Crew.Application/Commands/CrewCommands.cs`
- Create: `src/FarmOS.Crew.Application/Commands/Handlers/CrewCommandHandlers.cs`

**Step 1: Create Application csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\FarmOS.SharedKernel\FarmOS.SharedKernel.csproj" />
    <ProjectReference Include="..\FarmOS.Crew.Domain\FarmOS.Crew.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Step 2: Create ICrewEventStore.cs**

```csharp
using FarmOS.Crew.Domain.Aggregates;

namespace FarmOS.Crew.Application;

public interface ICrewEventStore
{
    Task<Worker> LoadWorkerAsync(string workerId, CancellationToken ct);
    Task SaveWorkerAsync(Worker worker, string userId, CancellationToken ct);

    Task<Shift> LoadShiftAsync(string shiftId, CancellationToken ct);
    Task SaveShiftAsync(Shift shift, string userId, CancellationToken ct);
}
```

**Step 3: Create CrewCommands.cs**

```csharp
using FarmOS.Crew.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Crew.Application.Commands;

// ─── Worker ─────────────────────────────────────────────────────────
public record RegisterWorkerCommand(WorkerProfile Profile) : ICommand<Guid>;
public record UpdateWorkerProfileCommand(Guid WorkerId, WorkerProfile Profile) : ICommand<Guid>;
public record DeactivateWorkerCommand(Guid WorkerId, WorkerStatus NewStatus, string? Reason) : ICommand<Guid>;
public record AddCertificationCommand(Guid WorkerId, Certification Cert) : ICommand<Guid>;

// ─── Shift ──────────────────────────────────────────────────────────
public record ScheduleShiftCommand(ShiftEntry Entry) : ICommand<Guid>;
public record StartShiftCommand(Guid ShiftId) : ICommand<Guid>;
public record CompleteShiftCommand(Guid ShiftId, string? Notes) : ICommand<Guid>;
public record CancelShiftCommand(Guid ShiftId, string Reason) : ICommand<Guid>;
```

**Step 4: Create CrewCommandHandlers.cs**

```csharp
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Crew.Application.Commands.Handlers;

public sealed class WorkerCommandHandlers(ICrewEventStore store) :
    ICommandHandler<RegisterWorkerCommand, Guid>,
    ICommandHandler<UpdateWorkerProfileCommand, Guid>,
    ICommandHandler<DeactivateWorkerCommand, Guid>,
    ICommandHandler<AddCertificationCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RegisterWorkerCommand cmd, CancellationToken ct)
    {
        var worker = Worker.Register(cmd.Profile);
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateWorkerProfileCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        worker.UpdateProfile(cmd.Profile);
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(DeactivateWorkerCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        var result = worker.Deactivate(cmd.NewStatus, cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCertificationCommand cmd, CancellationToken ct)
    {
        var worker = await store.LoadWorkerAsync(cmd.WorkerId.ToString(), ct);
        var result = worker.AddCertification(cmd.Cert);
        if (result.IsFailure) return result.Error;
        await store.SaveWorkerAsync(worker, "steward", ct);
        return worker.Id.Value;
    }
}

public sealed class ShiftCommandHandlers(ICrewEventStore store) :
    ICommandHandler<ScheduleShiftCommand, Guid>,
    ICommandHandler<StartShiftCommand, Guid>,
    ICommandHandler<CompleteShiftCommand, Guid>,
    ICommandHandler<CancelShiftCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(ScheduleShiftCommand cmd, CancellationToken ct)
    {
        var shift = Shift.Schedule(cmd.Entry);
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(StartShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Start();
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CompleteShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Complete(cmd.Notes);
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CancelShiftCommand cmd, CancellationToken ct)
    {
        var shift = await store.LoadShiftAsync(cmd.ShiftId.ToString(), ct);
        var result = shift.Cancel(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveShiftAsync(shift, "steward", ct);
        return shift.Id.Value;
    }
}
```

**Step 5: Commit**

```bash
git add src/FarmOS.Crew.Application/
git commit -m "feat(crew): add application layer — commands, handlers, event store interface"
```

---

### Task 4: Crew Context — Infrastructure + API Layers

**Files:**
- Create: `src/FarmOS.Crew.Infrastructure/FarmOS.Crew.Infrastructure.csproj`
- Create: `src/FarmOS.Crew.Infrastructure/CrewEventStore.cs`
- Create: `src/FarmOS.Crew.API/FarmOS.Crew.API.csproj`
- Create: `src/FarmOS.Crew.API/CrewEndpoints.cs`
- Create: `src/FarmOS.Crew.API/Program.cs`

**Step 1: Create Infrastructure csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\FarmOS.Crew.Application\FarmOS.Crew.Application.csproj" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Step 2: Create CrewEventStore.cs**

```csharp
using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FarmOS.Crew.Application;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

namespace FarmOS.Crew.Infrastructure;

public sealed class CrewEventStore(IEventStore store) : ICrewEventStore
{
    private const string CollectionName = "crew_events";

    private static readonly Dictionary<string, Type> EventTypeMap = new()
    {
        [nameof(WorkerRegistered)] = typeof(WorkerRegistered),
        [nameof(WorkerProfileUpdated)] = typeof(WorkerProfileUpdated),
        [nameof(WorkerDeactivated)] = typeof(WorkerDeactivated),
        [nameof(CertificationAdded)] = typeof(CertificationAdded),

        [nameof(ShiftScheduled)] = typeof(ShiftScheduled),
        [nameof(ShiftStarted)] = typeof(ShiftStarted),
        [nameof(ShiftCompleted)] = typeof(ShiftCompleted),
        [nameof(ShiftCancelled)] = typeof(ShiftCancelled),
    };

    public Task<Worker> LoadWorkerAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Worker, WorkerId>(CollectionName, id, () => new Worker(), DeserializeEvent, ct);

    public Task SaveWorkerAsync(Worker worker, string userId, CancellationToken ct) =>
        SaveAsync(worker, worker.Id.ToString(), "Worker", userId, ct);

    public Task<Shift> LoadShiftAsync(string id, CancellationToken ct) =>
        store.LoadAsync<Shift, ShiftId>(CollectionName, id, () => new Shift(), DeserializeEvent, ct);

    public Task SaveShiftAsync(Shift shift, string userId, CancellationToken ct) =>
        SaveAsync(shift, shift.Id.ToString(), "Shift", userId, ct);

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
```

**Step 3: Create API csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\FarmOS.Crew.Application\FarmOS.Crew.Application.csproj" />
    <ProjectReference Include="..\FarmOS.Crew.Infrastructure\FarmOS.Crew.Infrastructure.csproj" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Step 4: Create CrewEndpoints.cs**

```csharp
using MediatR;
using FarmOS.Crew.Application.Commands;
using FarmOS.SharedKernel;

namespace FarmOS.Crew.API;

public static class CrewEndpoints
{
    public static void MapCrewEndpoints(this WebApplication app)
    {
        var workers = app.MapGroup("/api/crew/workers");
        var shifts = app.MapGroup("/api/crew/shifts");

        // ─── Workers ────────────────────────────────────────────────
        workers.MapPost("/", async (RegisterWorkerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/crew/workers/{id}", new { id }), err => Results.BadRequest(err));
        });

        workers.MapPut("/{id:guid}/profile", async (Guid id, UpdateWorkerProfileCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        workers.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateWorkerCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        workers.MapPost("/{id:guid}/certifications", async (Guid id, AddCertificationCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { WorkerId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        // ─── Shifts ─────────────────────────────────────────────────
        shifts.MapPost("/", async (ScheduleShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.Match(id => Results.Created($"/api/crew/shifts/{id}", new { id }), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/start", async (Guid id, StartShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/complete", async (Guid id, CompleteShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });

        shifts.MapPost("/{id:guid}/cancel", async (Guid id, CancelShiftCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd with { ShiftId = id }, ct);
            return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
        });
    }
}
```

**Step 5: Create Program.cs**

```csharp
using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using FarmOS.Crew.API;
using FarmOS.Crew.Application;
using FarmOS.Crew.Application.Commands.Handlers;
using FarmOS.Crew.Infrastructure;
using FarmOS.SharedKernel.EventStore;
using FarmOS.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── ArangoDB ────────────────────────────────────────────────────────
var arangoUrl = builder.Configuration.GetValue<string>("ArangoDB:Url") ?? "http://localhost:8529";
var arangoUser = builder.Configuration.GetValue<string>("ArangoDB:User") ?? "root";
var arangoPass = builder.Configuration.GetValue<string>("ArangoDB:Password") ?? "farmos_dev";
var arangoDb = builder.Configuration.GetValue<string>("ArangoDB:Database") ?? "farmos";

builder.Services.AddSingleton<IArangoDBClient>(_ =>
{
    var transport = HttpApiTransport.UsingBasicAuth(
        new Uri(arangoUrl), arangoDb, arangoUser, arangoPass);
    return new ArangoDBClient(transport);
});

builder.Services.AddSingleton<IEventStore>(sp =>
    new ArangoEventStore(sp.GetRequiredService<IArangoDBClient>(), arangoDb));

// ─── Crew Services ───────────────────────────────────────────────────
builder.Services.AddScoped<ICrewEventStore, CrewEventStore>();

// ─── MediatR ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<WorkerCommandHandlers>();
});

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<MessagePackMiddleware>();
app.MapCrewEndpoints();

app.Run();
```

**Step 6: Add all 4 Crew projects to solution**

```bash
dotnet sln FarmOS.sln add src/FarmOS.Crew.Domain/FarmOS.Crew.Domain.csproj
dotnet sln FarmOS.sln add src/FarmOS.Crew.Application/FarmOS.Crew.Application.csproj
dotnet sln FarmOS.sln add src/FarmOS.Crew.Infrastructure/FarmOS.Crew.Infrastructure.csproj
dotnet sln FarmOS.sln add src/FarmOS.Crew.API/FarmOS.Crew.API.csproj
dotnet sln FarmOS.sln add src/FarmOS.Crew.Domain.Tests/FarmOS.Crew.Domain.Tests.csproj
```

**Step 7: Build and test**

```bash
dotnet build src/FarmOS.Crew.API/FarmOS.Crew.API.csproj
dotnet test src/FarmOS.Crew.Domain.Tests/ -v minimal
```
Expected: Build succeeds. All 9 domain tests pass.

**Step 8: Commit**

```bash
git add src/FarmOS.Crew.Infrastructure/ src/FarmOS.Crew.API/ FarmOS.sln
git commit -m "feat(crew): add infrastructure and API layers — full Crew context"
```

---

### Task 5: Compliance Context — Domain Layer

**Files:**
- Create: `src/FarmOS.Compliance.Domain/FarmOS.Compliance.Domain.csproj`
- Create: `src/FarmOS.Compliance.Domain/Types.cs`
- Create: `src/FarmOS.Compliance.Domain/Events.cs`
- Create: `src/FarmOS.Compliance.Domain/Aggregates/Permit.cs`
- Create: `src/FarmOS.Compliance.Domain/Aggregates/InsurancePolicy.cs`

**Step 1: Create Domain csproj** (same pattern as Task 1 Step 1, ref SharedKernel)

**Step 2: Create Types.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain;

public record PermitId(Guid Value) { public static PermitId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record PolicyId(Guid Value) { public static PolicyId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum PermitType { BusinessLicense, FoodProcessing, RetailFood, SalesTax, ZoningUse, OrganicCertification, GAPCertification, CottageFoodExemption, HealthDepartment, WeightsAndMeasures, Custom }
public enum PermitStatus { Active, PendingRenewal, Expired, Revoked }
public enum PolicyType { GeneralLiability, Property, Equipment, WorkersComp, ProductLiability, CommercialAuto, UmbrellaPolicy }
public enum PolicyStatus { Active, PendingRenewal, Expired, Cancelled }

public record RenewalInfo(DateOnly RenewalDate, decimal? Fee, string? Notes);
public record CoverageDetail(string CoverageType, decimal Limit, decimal Deductible);
```

**Step 3: Create Events.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Events;

// ─── Permit ─────────────────────────────────────────────────────────
public record PermitRegistered(PermitId Id, PermitType Type, string Name, string IssuingAuthority, DateOnly IssueDate, DateOnly ExpiryDate, decimal? Fee, string? Notes, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitRenewed(PermitId Id, RenewalInfo Renewal, DateOnly NewExpiryDate, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitExpired(PermitId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record PermitRevoked(PermitId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Insurance Policy ───────────────────────────────────────────────
public record PolicyRegistered(PolicyId Id, PolicyType Type, string Provider, string PolicyNumber, DateOnly EffectiveDate, DateOnly ExpiryDate, decimal AnnualPremium, IReadOnlyList<CoverageDetail> Coverages, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyRenewed(PolicyId Id, DateOnly NewExpiryDate, decimal NewPremium, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyExpired(PolicyId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record PolicyCoverageUpdated(PolicyId Id, IReadOnlyList<CoverageDetail> Coverages, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Step 4: Create Permit aggregate**

```csharp
using FarmOS.Compliance.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Aggregates;

public sealed class Permit : AggregateRoot<PermitId>
{
    public PermitType Type { get; private set; }
    public string Name { get; private set; } = "";
    public string IssuingAuthority { get; private set; } = "";
    public DateOnly IssueDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public PermitStatus Status { get; private set; }
    public decimal? Fee { get; private set; }
    private readonly List<RenewalInfo> _renewals = [];
    public IReadOnlyList<RenewalInfo> Renewals => _renewals;

    public static Permit Register(PermitType type, string name, string issuingAuthority, DateOnly issueDate, DateOnly expiryDate, decimal? fee, string? notes)
    {
        var permit = new Permit();
        permit.RaiseEvent(new PermitRegistered(PermitId.New(), type, name, issuingAuthority, issueDate, expiryDate, fee, notes, DateTimeOffset.UtcNow));
        return permit;
    }

    public Result<PermitId, DomainError> Renew(RenewalInfo renewal, DateOnly newExpiryDate)
    {
        if (Status == PermitStatus.Revoked)
            return DomainError.Conflict("Cannot renew a revoked permit.");
        RaiseEvent(new PermitRenewed(Id, renewal, newExpiryDate, DateTimeOffset.UtcNow));
        return Id;
    }

    public void MarkExpired() => RaiseEvent(new PermitExpired(Id, DateTimeOffset.UtcNow));

    public Result<PermitId, DomainError> Revoke(string reason)
    {
        if (Status == PermitStatus.Revoked)
            return DomainError.Conflict("Permit already revoked.");
        RaiseEvent(new PermitRevoked(Id, reason, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PermitRegistered e:
                Id = e.Id; Type = e.Type; Name = e.Name; IssuingAuthority = e.IssuingAuthority;
                IssueDate = e.IssueDate; ExpiryDate = e.ExpiryDate; Fee = e.Fee; Status = PermitStatus.Active; break;
            case PermitRenewed e: _renewals.Add(e.Renewal); ExpiryDate = e.NewExpiryDate; Status = PermitStatus.Active; break;
            case PermitExpired: Status = PermitStatus.Expired; break;
            case PermitRevoked: Status = PermitStatus.Revoked; break;
        }
    }
}
```

**Step 5: Create InsurancePolicy aggregate**

```csharp
using FarmOS.Compliance.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Compliance.Domain.Aggregates;

public sealed class InsurancePolicy : AggregateRoot<PolicyId>
{
    public PolicyType Type { get; private set; }
    public string Provider { get; private set; } = "";
    public string PolicyNumber { get; private set; } = "";
    public DateOnly EffectiveDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public decimal AnnualPremium { get; private set; }
    public PolicyStatus Status { get; private set; }
    private readonly List<CoverageDetail> _coverages = [];
    public IReadOnlyList<CoverageDetail> Coverages => _coverages;

    public static InsurancePolicy Register(PolicyType type, string provider, string policyNumber, DateOnly effectiveDate, DateOnly expiryDate, decimal annualPremium, IReadOnlyList<CoverageDetail> coverages)
    {
        var policy = new InsurancePolicy();
        policy.RaiseEvent(new PolicyRegistered(PolicyId.New(), type, provider, policyNumber, effectiveDate, expiryDate, annualPremium, coverages, DateTimeOffset.UtcNow));
        return policy;
    }

    public Result<PolicyId, DomainError> Renew(DateOnly newExpiryDate, decimal newPremium)
    {
        if (Status == PolicyStatus.Cancelled)
            return DomainError.Conflict("Cannot renew a cancelled policy.");
        RaiseEvent(new PolicyRenewed(Id, newExpiryDate, newPremium, DateTimeOffset.UtcNow));
        return Id;
    }

    public void MarkExpired() => RaiseEvent(new PolicyExpired(Id, DateTimeOffset.UtcNow));

    public void UpdateCoverages(IReadOnlyList<CoverageDetail> coverages) =>
        RaiseEvent(new PolicyCoverageUpdated(Id, coverages, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PolicyRegistered e:
                Id = e.Id; Type = e.Type; Provider = e.Provider; PolicyNumber = e.PolicyNumber;
                EffectiveDate = e.EffectiveDate; ExpiryDate = e.ExpiryDate; AnnualPremium = e.AnnualPremium;
                _coverages.AddRange(e.Coverages); Status = PolicyStatus.Active; break;
            case PolicyRenewed e: ExpiryDate = e.NewExpiryDate; AnnualPremium = e.NewPremium; Status = PolicyStatus.Active; break;
            case PolicyExpired: Status = PolicyStatus.Expired; break;
            case PolicyCoverageUpdated e: _coverages.Clear(); _coverages.AddRange(e.Coverages); break;
        }
    }
}
```

**Step 6: Commit**

```bash
git add src/FarmOS.Compliance.Domain/
git commit -m "feat(compliance): add domain layer — Permit and InsurancePolicy aggregates"
```

---

### Task 6: Compliance Context — Tests + Application + Infrastructure + API

Follow the same pattern as Tasks 2-4 but for Compliance.

**Files:**
- Create: `src/FarmOS.Compliance.Domain.Tests/FarmOS.Compliance.Domain.Tests.csproj`
- Create: `src/FarmOS.Compliance.Domain.Tests/PermitTests.cs`
- Create: `src/FarmOS.Compliance.Domain.Tests/InsurancePolicyTests.cs`
- Create: `src/FarmOS.Compliance.Application/FarmOS.Compliance.Application.csproj`
- Create: `src/FarmOS.Compliance.Application/IComplianceEventStore.cs`
- Create: `src/FarmOS.Compliance.Application/Commands/ComplianceCommands.cs`
- Create: `src/FarmOS.Compliance.Application/Commands/Handlers/ComplianceCommandHandlers.cs`
- Create: `src/FarmOS.Compliance.Infrastructure/FarmOS.Compliance.Infrastructure.csproj`
- Create: `src/FarmOS.Compliance.Infrastructure/ComplianceEventStore.cs`
- Create: `src/FarmOS.Compliance.API/FarmOS.Compliance.API.csproj`
- Create: `src/FarmOS.Compliance.API/ComplianceEndpoints.cs`
- Create: `src/FarmOS.Compliance.API/Program.cs`

**Key differences from Crew:**
- Collection name: `"compliance_events"`
- Event store interface: `IComplianceEventStore` with `LoadPermitAsync/SavePermitAsync` + `LoadPolicyAsync/SavePolicyAsync`
- Commands: `RegisterPermitCommand`, `RenewPermitCommand`, `RevokePermitCommand`, `RegisterPolicyCommand`, `RenewPolicyCommand`, `UpdateCoveragesCommand`
- Endpoints group: `/api/compliance/permits` and `/api/compliance/policies`
- MediatR scans `PermitCommandHandlers`

**Test cases to write:**
- Permit: Register creates Active, Renew succeeds, Renew fails when Revoked, MarkExpired works, Revoke fails when already Revoked
- InsurancePolicy: Register creates Active with coverages, Renew succeeds, Renew fails when Cancelled, UpdateCoverages replaces list

**Step N: Add to solution, build, test, commit**

```bash
dotnet sln FarmOS.sln add src/FarmOS.Compliance.Domain/FarmOS.Compliance.Domain.csproj
dotnet sln FarmOS.sln add src/FarmOS.Compliance.Application/FarmOS.Compliance.Application.csproj
dotnet sln FarmOS.sln add src/FarmOS.Compliance.Infrastructure/FarmOS.Compliance.Infrastructure.csproj
dotnet sln FarmOS.sln add src/FarmOS.Compliance.API/FarmOS.Compliance.API.csproj
dotnet sln FarmOS.sln add src/FarmOS.Compliance.Domain.Tests/FarmOS.Compliance.Domain.Tests.csproj
dotnet build src/FarmOS.Compliance.API/FarmOS.Compliance.API.csproj
dotnet test src/FarmOS.Compliance.Domain.Tests/ -v minimal
git add src/FarmOS.Compliance.Domain/ src/FarmOS.Compliance.Domain.Tests/ src/FarmOS.Compliance.Application/ src/FarmOS.Compliance.Infrastructure/ src/FarmOS.Compliance.API/ FarmOS.sln
git commit -m "feat(compliance): add full Compliance context — Permit and InsurancePolicy with tests"
```

---

### Task 7: Codex Context — Domain Layer

**Files:**
- Create: `src/FarmOS.Codex.Domain/FarmOS.Codex.Domain.csproj`
- Create: `src/FarmOS.Codex.Domain/Types.cs`
- Create: `src/FarmOS.Codex.Domain/Events.cs`
- Create: `src/FarmOS.Codex.Domain/Aggregates/Procedure.cs`
- Create: `src/FarmOS.Codex.Domain/Aggregates/Playbook.cs`

**Step 1: Create Types.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain;

public record ProcedureId(Guid Value) { public static ProcedureId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record PlaybookId(Guid Value) { public static PlaybookId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ProcedureCategory { Pasture, Flora, Hearth, Apiary, Commerce, Assets, Safety, Compliance, Onboarding, General }
public enum ProcedureStatus { Draft, Published, Archived }
public enum AudienceRole { Everyone, Employee, Apprentice, Manager }

public record ProcedureStep(int Order, string Title, string Instructions, string? ImagePath, string? WarningNote, int? EstimatedMinutes);
public record PlaybookTask(int Month, string Title, string Description, ProcedureCategory Category, string? LinkedProcedureId, string Priority);
```

**Step 2: Create Events.cs**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Events;

// ─── Procedure ──────────────────────────────────────────────────────
public record ProcedureCreated(ProcedureId Id, string Title, ProcedureCategory Category, AudienceRole Audience, string? Description, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureStepAdded(ProcedureId Id, ProcedureStep Step, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedurePublished(ProcedureId Id, int Revision, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureRevised(ProcedureId Id, int NewRevision, string? ChangeNotes, DateTimeOffset OccurredAt) : IDomainEvent;
public record ProcedureArchived(ProcedureId Id, DateTimeOffset OccurredAt) : IDomainEvent;

// ─── Playbook ───────────────────────────────────────────────────────
public record PlaybookCreated(PlaybookId Id, string Title, string? Description, AudienceRole Audience, DateTimeOffset OccurredAt) : IDomainEvent;
public record PlaybookTaskAdded(PlaybookId Id, PlaybookTask Task, DateTimeOffset OccurredAt) : IDomainEvent;
public record PlaybookTaskRemoved(PlaybookId Id, int Month, string TaskTitle, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Step 3: Create Procedure aggregate**

```csharp
using FarmOS.Codex.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Aggregates;

public sealed class Procedure : AggregateRoot<ProcedureId>
{
    public string Title { get; private set; } = "";
    public ProcedureCategory Category { get; private set; }
    public AudienceRole Audience { get; private set; }
    public string? Description { get; private set; }
    public ProcedureStatus Status { get; private set; }
    public int Revision { get; private set; }
    private readonly List<ProcedureStep> _steps = [];
    public IReadOnlyList<ProcedureStep> Steps => _steps;

    public static Procedure Create(string title, ProcedureCategory category, AudienceRole audience, string? description)
    {
        var proc = new Procedure();
        proc.RaiseEvent(new ProcedureCreated(ProcedureId.New(), title, category, audience, description, DateTimeOffset.UtcNow));
        return proc;
    }

    public void AddStep(ProcedureStep step) =>
        RaiseEvent(new ProcedureStepAdded(Id, step, DateTimeOffset.UtcNow));

    public Result<ProcedureId, DomainError> Publish()
    {
        if (Status == ProcedureStatus.Archived)
            return DomainError.Conflict("Cannot publish an archived procedure.");
        if (_steps.Count == 0)
            return DomainError.Validation("Cannot publish a procedure with no steps.");
        RaiseEvent(new ProcedurePublished(Id, Revision + 1, DateTimeOffset.UtcNow));
        return Id;
    }

    public Result<ProcedureId, DomainError> Revise(string? changeNotes)
    {
        if (Status != ProcedureStatus.Published)
            return DomainError.Conflict("Only published procedures can be revised.");
        RaiseEvent(new ProcedureRevised(Id, Revision + 1, changeNotes, DateTimeOffset.UtcNow));
        return Id;
    }

    public void Archive() => RaiseEvent(new ProcedureArchived(Id, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProcedureCreated e: Id = e.Id; Title = e.Title; Category = e.Category; Audience = e.Audience; Description = e.Description; Status = ProcedureStatus.Draft; break;
            case ProcedureStepAdded e: _steps.Add(e.Step); break;
            case ProcedurePublished e: Status = ProcedureStatus.Published; Revision = e.Revision; break;
            case ProcedureRevised e: Status = ProcedureStatus.Draft; Revision = e.NewRevision; break;
            case ProcedureArchived: Status = ProcedureStatus.Archived; break;
        }
    }
}
```

**Step 4: Create Playbook aggregate**

```csharp
using FarmOS.Codex.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Aggregates;

public sealed class Playbook : AggregateRoot<PlaybookId>
{
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public AudienceRole Audience { get; private set; }
    private readonly List<PlaybookTask> _tasks = [];
    public IReadOnlyList<PlaybookTask> Tasks => _tasks;

    public static Playbook Create(string title, string? description, AudienceRole audience)
    {
        var pb = new Playbook();
        pb.RaiseEvent(new PlaybookCreated(PlaybookId.New(), title, description, audience, DateTimeOffset.UtcNow));
        return pb;
    }

    public void AddTask(PlaybookTask task) =>
        RaiseEvent(new PlaybookTaskAdded(Id, task, DateTimeOffset.UtcNow));

    public Result<PlaybookId, DomainError> RemoveTask(int month, string taskTitle)
    {
        if (!_tasks.Any(t => t.Month == month && t.Title == taskTitle))
            return DomainError.NotFound("Task not found in playbook.");
        RaiseEvent(new PlaybookTaskRemoved(Id, month, taskTitle, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PlaybookCreated e: Id = e.Id; Title = e.Title; Description = e.Description; Audience = e.Audience; break;
            case PlaybookTaskAdded e: _tasks.Add(e.Task); break;
            case PlaybookTaskRemoved e: _tasks.RemoveAll(t => t.Month == e.Month && t.Title == e.TaskTitle); break;
        }
    }
}
```

**Step 5: Commit**

```bash
git add src/FarmOS.Codex.Domain/
git commit -m "feat(codex): add domain layer — Procedure and Playbook aggregates"
```

---

### Task 8: Codex Context — Tests + Application + Infrastructure + API

Follow same pattern as Tasks 2-4/6. Key specifics:

**Collection name:** `"codex_events"`

**Commands:**
- `CreateProcedureCommand`, `AddProcedureStepCommand`, `PublishProcedureCommand`, `ReviseProcedureCommand`, `ArchiveProcedureCommand`
- `CreatePlaybookCommand`, `AddPlaybookTaskCommand`, `RemovePlaybookTaskCommand`

**Endpoints:**
- `/api/codex/procedures` group
- `/api/codex/playbooks` group

**Test cases:**
- Procedure: Create is Draft, AddStep works, Publish succeeds with steps, Publish fails with no steps, Publish fails when Archived, Revise succeeds when Published, Archive works
- Playbook: Create works, AddTask works, RemoveTask succeeds, RemoveTask fails when not found

**Build, test, add to solution, commit.**

---

### Task 9: Commerce CRM Extension — Domain Layer

This extends the existing Commerce context. **Do not create new projects.** Add to existing files.

**Files:**
- Modify: `src/FarmOS.Commerce.Domain/Types.cs` — add Customer types
- Modify: `src/FarmOS.Commerce.Domain/Events.cs` — add Customer events
- Create: `src/FarmOS.Commerce.Domain/Aggregates/Customer.cs`

**Step 1: Add types to existing Types.cs**

Append after existing types:

```csharp
// ─── Customer CRM ───────────────────────────────────────────────────
public record CustomerId(Guid Value) { public static CustomerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum CustomerChannel { CSA, BuyingClub, FarmStore, FarmersMarket, Wholesale, Online, Tour }
public enum AccountTier { Standard, Premium, Wholesale }

public record CustomerProfile(string Name, string Email, string? Phone, string? Address, IReadOnlyList<CustomerChannel> Channels, string? Notes, string? DietaryPrefs);
public record CustomerNote(string Content, DateTimeOffset CreatedAt);
public record MatchCandidate(CustomerId ExistingId, string ExistingName, string? ExistingEmail, decimal ConfidenceScore, string MatchBasis);
```

**Step 2: Add events to existing Events.cs**

Append after existing events:

```csharp
// ─── Customer CRM ───────────────────────────────────────────────────
public record CustomerCreated(CustomerId Id, CustomerProfile Profile, AccountTier Tier, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomerProfileUpdated(CustomerId Id, CustomerProfile Profile, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomerNoteAdded(CustomerId Id, CustomerNote Note, DateTimeOffset OccurredAt) : IDomainEvent;
public record DuplicateSuspected(CustomerId Id, MatchCandidate Candidate, DateTimeOffset OccurredAt) : IDomainEvent;
public record CustomersMerged(CustomerId SurvivingId, CustomerId AbsorbedId, DateTimeOffset OccurredAt) : IDomainEvent;
public record DuplicateDismissed(CustomerId Id, CustomerId DismissedMatchId, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Step 3: Create Customer aggregate**

```csharp
using FarmOS.Commerce.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Commerce.Domain.Aggregates;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerProfile Profile { get; private set; } = new("", "", null, null, [], null, null);
    public AccountTier Tier { get; private set; }
    private readonly List<CustomerNote> _notes = [];
    public IReadOnlyList<CustomerNote> Notes => _notes;
    private readonly List<CustomerId> _mergedFrom = [];
    public IReadOnlyList<CustomerId> MergedFrom => _mergedFrom;
    public bool IsMerged { get; private set; }

    public static Customer Create(CustomerProfile profile, AccountTier tier)
    {
        var customer = new Customer();
        customer.RaiseEvent(new CustomerCreated(CustomerId.New(), profile, tier, DateTimeOffset.UtcNow));
        return customer;
    }

    public void UpdateProfile(CustomerProfile profile) =>
        RaiseEvent(new CustomerProfileUpdated(Id, profile, DateTimeOffset.UtcNow));

    public void AddNote(string content) =>
        RaiseEvent(new CustomerNoteAdded(Id, new CustomerNote(content, DateTimeOffset.UtcNow), DateTimeOffset.UtcNow));

    public void FlagDuplicate(MatchCandidate candidate) =>
        RaiseEvent(new DuplicateSuspected(Id, candidate, DateTimeOffset.UtcNow));

    public Result<CustomerId, DomainError> AbsorbCustomer(CustomerId absorbedId)
    {
        if (absorbedId == Id)
            return DomainError.Validation("Cannot merge customer with self.");
        RaiseEvent(new CustomersMerged(Id, absorbedId, DateTimeOffset.UtcNow));
        return Id;
    }

    public void DismissDuplicate(CustomerId dismissedId) =>
        RaiseEvent(new DuplicateDismissed(Id, dismissedId, DateTimeOffset.UtcNow));

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreated e: Id = e.Id; Profile = e.Profile; Tier = e.Tier; break;
            case CustomerProfileUpdated e: Profile = e.Profile; break;
            case CustomerNoteAdded e: _notes.Add(e.Note); break;
            case CustomersMerged e: _mergedFrom.Add(e.AbsorbedId); break;
            default: break; // DuplicateSuspected/DuplicateDismissed are informational
        }
    }
}
```

**Step 4: Add CustomerId? reference to CSAMember**

In `CommerceAggregates.cs`, add to CSAMember:
- Property: `public CustomerId? CustomerId { get; private set; }`
- This is a backward-compatible addition — no existing events change

**Step 5: Commit**

```bash
git add src/FarmOS.Commerce.Domain/
git commit -m "feat(commerce): add Customer CRM aggregate with dedup support"
```

---

### Task 10: Commerce CRM Extension — Application + Infrastructure + API

**Files:**
- Modify: `src/FarmOS.Commerce.Application/ICommerceEventStore.cs` — add Customer methods
- Create: `src/FarmOS.Commerce.Application/Commands/CustomerCommands.cs`
- Create: `src/FarmOS.Commerce.Application/Commands/Handlers/CustomerCommandHandlers.cs`
- Modify: `src/FarmOS.Commerce.Infrastructure/CommerceEventStore.cs` — add Customer event types + Load/Save
- Modify: `src/FarmOS.Commerce.API/CommerceEndpoints.cs` — add /customers group

**Step 1: Add to ICommerceEventStore.cs**

```csharp
Task<Customer> LoadCustomerAsync(string customerId, CancellationToken ct);
Task SaveCustomerAsync(Customer customer, string userId, CancellationToken ct);
```

**Step 2: Create CustomerCommands.cs**

```csharp
using FarmOS.Commerce.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands;

public record CreateCustomerCommand(CustomerProfile Profile, AccountTier Tier) : ICommand<Guid>;
public record UpdateCustomerProfileCommand(Guid CustomerId, CustomerProfile Profile) : ICommand<Guid>;
public record AddCustomerNoteCommand(Guid CustomerId, string Content) : ICommand<Guid>;
public record MergeCustomersCommand(Guid SurvivingId, Guid AbsorbedId) : ICommand<Guid>;
public record DismissDuplicateCommand(Guid CustomerId, Guid DismissedMatchId) : ICommand<Guid>;
```

**Step 3: Create CustomerCommandHandlers.cs**

```csharp
using FarmOS.Commerce.Domain;
using FarmOS.Commerce.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Commerce.Application.Commands.Handlers;

public sealed class CustomerCommandHandlers(ICommerceEventStore store) :
    ICommandHandler<CreateCustomerCommand, Guid>,
    ICommandHandler<UpdateCustomerProfileCommand, Guid>,
    ICommandHandler<AddCustomerNoteCommand, Guid>,
    ICommandHandler<MergeCustomersCommand, Guid>,
    ICommandHandler<DismissDuplicateCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = Customer.Create(cmd.Profile, cmd.Tier);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(UpdateCustomerProfileCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.UpdateProfile(cmd.Profile);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(AddCustomerNoteCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.AddNote(cmd.Content);
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(MergeCustomersCommand cmd, CancellationToken ct)
    {
        var surviving = await store.LoadCustomerAsync(cmd.SurvivingId.ToString(), ct);
        var result = surviving.AbsorbCustomer(new CustomerId(cmd.AbsorbedId));
        if (result.IsFailure) return result.Error;
        await store.SaveCustomerAsync(surviving, "steward", ct);
        return surviving.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(DismissDuplicateCommand cmd, CancellationToken ct)
    {
        var customer = await store.LoadCustomerAsync(cmd.CustomerId.ToString(), ct);
        customer.DismissDuplicate(new CustomerId(cmd.DismissedMatchId));
        await store.SaveCustomerAsync(customer, "steward", ct);
        return customer.Id.Value;
    }
}
```

**Step 4: Add Customer event types to CommerceEventStore.cs EventTypeMap**

```csharp
[nameof(CustomerCreated)] = typeof(CustomerCreated),
[nameof(CustomerProfileUpdated)] = typeof(CustomerProfileUpdated),
[nameof(CustomerNoteAdded)] = typeof(CustomerNoteAdded),
[nameof(DuplicateSuspected)] = typeof(DuplicateSuspected),
[nameof(CustomersMerged)] = typeof(CustomersMerged),
[nameof(DuplicateDismissed)] = typeof(DuplicateDismissed),
```

Add Load/Save methods:

```csharp
public Task<Customer> LoadCustomerAsync(string id, CancellationToken ct) =>
    store.LoadAsync<Customer, CustomerId>(CollectionName, id, () => new Customer(), DeserializeEvent, ct);

public Task SaveCustomerAsync(Customer customer, string userId, CancellationToken ct) =>
    SaveAsync(customer, customer.Id.ToString(), "Customer", userId, ct);
```

**Step 5: Add customer endpoints to CommerceEndpoints.cs**

```csharp
var customers = app.MapGroup("/api/commerce/customers");

customers.MapPost("/", async (CreateCustomerCommand cmd, IMediator m, CancellationToken ct) =>
{
    var result = await m.Send(cmd, ct);
    return result.Match(id => Results.Created($"/api/commerce/customers/{id}", new { id }), err => Results.BadRequest(err));
});

customers.MapPut("/{id:guid}/profile", async (Guid id, UpdateCustomerProfileCommand cmd, IMediator m, CancellationToken ct) =>
{
    var result = await m.Send(cmd with { CustomerId = id }, ct);
    return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
});

customers.MapPost("/{id:guid}/notes", async (Guid id, AddCustomerNoteCommand cmd, IMediator m, CancellationToken ct) =>
{
    var result = await m.Send(cmd with { CustomerId = id }, ct);
    return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
});

customers.MapPost("/merge", async (MergeCustomersCommand cmd, IMediator m, CancellationToken ct) =>
{
    var result = await m.Send(cmd, ct);
    return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
});

customers.MapPost("/{id:guid}/dismiss-duplicate", async (Guid id, DismissDuplicateCommand cmd, IMediator m, CancellationToken ct) =>
{
    var result = await m.Send(cmd with { CustomerId = id }, ct);
    return result.Match(_ => Results.NoContent(), err => Results.BadRequest(err));
});
```

**Step 6: Build and commit**

```bash
dotnet build src/FarmOS.Commerce.API/FarmOS.Commerce.API.csproj
git add src/FarmOS.Commerce.Application/ src/FarmOS.Commerce.Infrastructure/ src/FarmOS.Commerce.API/
git commit -m "feat(commerce): add Customer CRM commands, handlers, event store, and API endpoints"
```

---

### Task 11: SharedKernel — String Normalization Utility

**Files:**
- Create: `src/FarmOS.SharedKernel/StringNormalization.cs`

For customer dedup fuzzy matching.

```csharp
namespace FarmOS.SharedKernel;

public static class StringNormalization
{
    /// <summary>
    /// Normalizes a name for dedup comparison: lowercase, trim, collapse whitespace,
    /// strip punctuation, handle "Last, First" -> "First Last".
    /// </summary>
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var trimmed = name.Trim().ToLowerInvariant();

        // Handle "Last, First" format
        if (trimmed.Contains(','))
        {
            var parts = trimmed.Split(',', 2);
            if (parts.Length == 2)
                trimmed = $"{parts[1].Trim()} {parts[0].Trim()}";
        }

        // Strip punctuation except spaces
        var cleaned = new string(trimmed.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());

        // Collapse whitespace
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Levenshtein distance for similarity scoring.
    /// Returns a 0.0-1.0 similarity score (1.0 = identical).
    /// </summary>
    public static decimal Similarity(string a, string b)
    {
        if (a == b) return 1.0m;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0m;

        var na = NormalizeName(a);
        var nb = NormalizeName(b);
        if (na == nb) return 1.0m;

        var distance = LevenshteinDistance(na, nb);
        var maxLen = Math.Max(na.Length, nb.Length);
        return maxLen == 0 ? 1.0m : Math.Round(1.0m - (decimal)distance / maxLen, 2);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = s[i - 1] == t[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }

        return d[n, m];
    }
}
```

```bash
git add src/FarmOS.SharedKernel/StringNormalization.cs
git commit -m "feat(shared): add StringNormalization utility for customer dedup"
```

---

### Task 12: Phase 1 Verification

**Step 1: Build all Phase 1 projects**

```bash
dotnet build src/FarmOS.Crew.API/FarmOS.Crew.API.csproj
dotnet build src/FarmOS.Compliance.API/FarmOS.Compliance.API.csproj
dotnet build src/FarmOS.Codex.API/FarmOS.Codex.API.csproj
dotnet build src/FarmOS.Commerce.API/FarmOS.Commerce.API.csproj
```
Expected: All 4 build successfully.

**Step 2: Run all Phase 1 tests**

```bash
dotnet test src/FarmOS.Crew.Domain.Tests/ -v minimal
dotnet test src/FarmOS.Compliance.Domain.Tests/ -v minimal
dotnet test src/FarmOS.Codex.Domain.Tests/ -v minimal
```
Expected: All tests pass.

**Step 3: Commit verification milestone**

```bash
git commit --allow-empty -m "milestone: Phase 1 complete — Crew, Compliance, Codex, Commerce CRM"
```

---

## Phase 2: Revenue Channels (depends on Phase 1 Commerce CRM for Customer linkage)

---

### Task 13: Campus Context — Full Stack

**Files (Domain):**
- Create: `src/FarmOS.Campus.Domain/FarmOS.Campus.Domain.csproj`
- Create: `src/FarmOS.Campus.Domain/Types.cs`
- Create: `src/FarmOS.Campus.Domain/Events.cs`
- Create: `src/FarmOS.Campus.Domain/Aggregates/Event.cs`
- Create: `src/FarmOS.Campus.Domain/Aggregates/Booking.cs`

**Types.cs:**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain;

public record EventId(Guid Value) { public static EventId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record BookingId(Guid Value) { public static BookingId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum EventType { FarmTour, Workshop, FieldDay, ClassroomSession, PrivateTour, FarmDinner }
public enum EventStatus { Draft, Published, Full, InProgress, Completed, Cancelled }
public enum BookingStatus { Reserved, Confirmed, CheckedIn, NoShow, Cancelled }

public record EventSchedule(DateOnly Date, TimeOnly Start, TimeOnly End, string Location, int Capacity, decimal PricePerPerson, decimal? GroupRate);
public record AttendeeInfo(string Name, string Email, string? Phone, int PartySize, string? DietaryNotes);
public record WaiverInfo(string SignedBy, DateTimeOffset SignedAt, string? DocumentPath);
```

**Events.cs:**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Campus.Domain.Events;

public record EventCreated(EventId Id, EventType Type, string Title, string? Description, EventSchedule Schedule, DateTimeOffset OccurredAt) : IDomainEvent;
public record EventPublished(EventId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record EventCancelled(EventId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record EventCompleted(EventId Id, int TotalAttendees, decimal TotalRevenue, DateTimeOffset OccurredAt) : IDomainEvent;

public record BookingCreated(BookingId Id, EventId EventId, AttendeeInfo Attendee, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingConfirmed(BookingId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingCheckedIn(BookingId Id, DateTimeOffset OccurredAt) : IDomainEvent;
public record BookingCancelled(BookingId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record WaiverSigned(BookingId Id, WaiverInfo Waiver, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Aggregates:** Event aggregate tracks bookings count vs capacity. Booking aggregate tracks attendee, waiver, status.

**Business rules:**
- Cannot book beyond capacity (Event aggregate enforces)
- BookingConfirmed requires WaiverSigned for FarmTour type events
- EventCompleted calculates total attendees and revenue

**Application/Infrastructure/API:** Follow Task 3-4 pattern with collection `"campus_events"`, endpoints `/api/campus/events` and `/api/campus/bookings`.

**Tests:** Event capacity enforcement, booking lifecycle, waiver requirement for farm tours.

```bash
dotnet sln FarmOS.sln add src/FarmOS.Campus.Domain/FarmOS.Campus.Domain.csproj src/FarmOS.Campus.Application/FarmOS.Campus.Application.csproj src/FarmOS.Campus.Infrastructure/FarmOS.Campus.Infrastructure.csproj src/FarmOS.Campus.API/FarmOS.Campus.API.csproj src/FarmOS.Campus.Domain.Tests/FarmOS.Campus.Domain.Tests.csproj
dotnet build src/FarmOS.Campus.API/FarmOS.Campus.API.csproj
dotnet test src/FarmOS.Campus.Domain.Tests/ -v minimal
git add src/FarmOS.Campus.Domain/ src/FarmOS.Campus.Domain.Tests/ src/FarmOS.Campus.Application/ src/FarmOS.Campus.Infrastructure/ src/FarmOS.Campus.API/ FarmOS.sln
git commit -m "feat(campus): add full Campus context — Event and Booking aggregates with tests"
```

---

### Task 14: Counter Context — Full Stack

**Files (Domain):**
- Create: `src/FarmOS.Counter.Domain/FarmOS.Counter.Domain.csproj`
- Create: `src/FarmOS.Counter.Domain/Types.cs`
- Create: `src/FarmOS.Counter.Domain/Events.cs`
- Create: `src/FarmOS.Counter.Domain/Aggregates/Register.cs`
- Create: `src/FarmOS.Counter.Domain/Aggregates/Sale.cs`
- Create: `src/FarmOS.Counter.Domain/Aggregates/CashDrawer.cs`

**Types.cs:**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain;

public record RegisterId(Guid Value) { public static RegisterId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record SaleId(Guid Value) { public static SaleId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record CashDrawerId(Guid Value) { public static CashDrawerId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum RegisterLocation { FarmStore, Cafe, FarmersMarket, PopUp }
public enum RegisterStatus { Open, Closed }
public enum PaymentMethod { Cash, Card, Check, EBT, Comped }
public enum TaxCategory { NonTaxable, StandardFood, PreparedFood, NonFood }
public enum SaleStatus { Completed, Voided, Refunded }

public record SaleLineItem(string ProductName, string? SKU, int Quantity, decimal UnitPrice, TaxCategory TaxCat, string? Notes);
public record PaymentRecord(PaymentMethod Method, decimal Amount, string? Reference);
public record DrawerCount(decimal Expected, decimal Actual, string? Notes);
```

**Events.cs:**

```csharp
using FarmOS.SharedKernel;

namespace FarmOS.Counter.Domain.Events;

public record RegisterOpened(RegisterId Id, RegisterLocation Location, string OperatorName, DateTimeOffset OccurredAt) : IDomainEvent;
public record RegisterClosed(RegisterId Id, DateTimeOffset OccurredAt) : IDomainEvent;

public record SaleCompleted(SaleId Id, RegisterId RegisterId, IReadOnlyList<SaleLineItem> Items, IReadOnlyList<PaymentRecord> Payments, decimal Total, decimal TaxAmount, string? CustomerName, DateTimeOffset OccurredAt) : IDomainEvent;
public record SaleVoided(SaleId Id, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public record SaleRefunded(SaleId Id, decimal RefundAmount, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public record CashDrawerOpened(CashDrawerId Id, RegisterId RegisterId, decimal StartingCash, DateTimeOffset OccurredAt) : IDomainEvent;
public record CashDrawerCounted(CashDrawerId Id, DrawerCount Count, DateTimeOffset OccurredAt) : IDomainEvent;
public record CashDrawerReconciled(CashDrawerId Id, decimal Discrepancy, DateTimeOffset OccurredAt) : IDomainEvent;
```

**Business rules:**
- Sale completion validates total payments >= total + tax
- EBT payment only valid for NonTaxable + StandardFood items
- Drawer reconciliation flags discrepancy if abs(actual - expected) > $5.00
- Cannot complete sale on a Closed register

**Application/Infrastructure/API:** Follow pattern with collection `"counter_events"`, endpoints `/api/counter/registers`, `/api/counter/sales`, `/api/counter/drawers`.

```bash
dotnet sln FarmOS.sln add src/FarmOS.Counter.Domain/FarmOS.Counter.Domain.csproj src/FarmOS.Counter.Application/FarmOS.Counter.Application.csproj src/FarmOS.Counter.Infrastructure/FarmOS.Counter.Infrastructure.csproj src/FarmOS.Counter.API/FarmOS.Counter.API.csproj src/FarmOS.Counter.Domain.Tests/FarmOS.Counter.Domain.Tests.csproj
dotnet build src/FarmOS.Counter.API/FarmOS.Counter.API.csproj
dotnet test src/FarmOS.Counter.Domain.Tests/ -v minimal
git add src/FarmOS.Counter.Domain/ src/FarmOS.Counter.Domain.Tests/ src/FarmOS.Counter.Application/ src/FarmOS.Counter.Infrastructure/ src/FarmOS.Counter.API/ FarmOS.sln
git commit -m "feat(counter): add full Counter context — Register, Sale, CashDrawer aggregates with tests"
```

---

### Task 15: Commerce Buying Clubs + Wholesale Extension

Extends Commerce context with BuyingClub and WholesaleAccount aggregates.

**Files:**
- Modify: `src/FarmOS.Commerce.Domain/Types.cs` — add BuyingClub/Wholesale types
- Modify: `src/FarmOS.Commerce.Domain/Events.cs` — add BuyingClub/Wholesale events
- Create: `src/FarmOS.Commerce.Domain/Aggregates/BuyingClub.cs`
- Create: `src/FarmOS.Commerce.Domain/Aggregates/WholesaleAccount.cs`
- Modify: `src/FarmOS.Commerce.Application/ICommerceEventStore.cs`
- Create: `src/FarmOS.Commerce.Application/Commands/BuyingClubCommands.cs`
- Create: `src/FarmOS.Commerce.Application/Commands/WholesaleCommands.cs`
- Create: `src/FarmOS.Commerce.Application/Commands/Handlers/BuyingClubCommandHandlers.cs`
- Create: `src/FarmOS.Commerce.Application/Commands/Handlers/WholesaleCommandHandlers.cs`
- Modify: `src/FarmOS.Commerce.Infrastructure/CommerceEventStore.cs`
- Modify: `src/FarmOS.Commerce.API/CommerceEndpoints.cs`

**New Types:**

```csharp
public record BuyingClubId(Guid Value) { public static BuyingClubId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record WholesaleAccountId(Guid Value) { public static WholesaleAccountId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }

public enum ClubStatus { Active, Paused, Closed }
public enum OrderCycleFrequency { Weekly, BiWeekly, Monthly }

public record DropSite(string Name, string Address, string ContactPerson, string ContactPhone, DayOfWeek DeliveryDay, TimeOnly DeliveryWindow);
public record StandingOrder(string ProductName, Quantity Qty, decimal UnitPrice, string? Notes);
public record DeliveryRoute(string Name, IReadOnlyList<string> DropSiteIds, decimal EstimatedMiles);
```

**Key business rules:**
- BuyingClub: order cycles open/close, cannot add drop site to closed club
- WholesaleAccount: standing orders auto-generate draft orders at cycle frequency

**Endpoints:** `/api/commerce/buying-clubs` and `/api/commerce/wholesale`

```bash
dotnet build src/FarmOS.Commerce.API/FarmOS.Commerce.API.csproj
git add src/FarmOS.Commerce.Domain/ src/FarmOS.Commerce.Application/ src/FarmOS.Commerce.Infrastructure/ src/FarmOS.Commerce.API/
git commit -m "feat(commerce): add BuyingClub and WholesaleAccount aggregates with commands and endpoints"
```

---

### Task 16: Phase 2 Verification

```bash
dotnet build src/FarmOS.Campus.API/FarmOS.Campus.API.csproj
dotnet build src/FarmOS.Counter.API/FarmOS.Counter.API.csproj
dotnet build src/FarmOS.Commerce.API/FarmOS.Commerce.API.csproj
dotnet test src/FarmOS.Campus.Domain.Tests/ -v minimal
dotnet test src/FarmOS.Counter.Domain.Tests/ -v minimal
git commit --allow-empty -m "milestone: Phase 2 complete — Campus, Counter, Commerce Buying Clubs + Wholesale"
```

---

## Phase 3: Financial Intelligence (depends on Phase 1+2 events)

---

### Task 17: Ledger Enterprise Accounting Extension

**Files:**
- Modify: `src/FarmOS.Ledger.Domain/Types.cs` — extend enums + add new types
- Modify: `src/FarmOS.Ledger.Domain/Events.cs` — add enterprise tagging events
- Modify: `src/FarmOS.Ledger.Infrastructure/LedgerEventStore.cs` — register new event types
- Modify: `src/FarmOS.Ledger.API/LedgerEndpoints.cs` — add reporting endpoints

**Type changes to add:**

```csharp
// Extend LedgerContext enum — add:
// Crew, Campus, Counter, Compliance

// Extend ExpenseCategory — add:
// Permits, Certification, GrantMatch, Tour, Wages, Stipend

// Extend RevenueCategory — add:
// Tours, Workshops, BuyingClub, Wholesale, CafeFood, CafeBeverage, Retail

// New types:
public record EnterpriseCode(LedgerContext Context, string? SubEnterprise);
public record CostAllocationRule(EnterpriseCode From, EnterpriseCode To, decimal Percentage, string Basis);
```

**New events:**

```csharp
public record ExpenseEnterpriseTagged(ExpenseId Id, EnterpriseCode Enterprise, DateTimeOffset OccurredAt) : IDomainEvent;
public record RevenueEnterpriseTagged(RevenueId Id, EnterpriseCode Enterprise, DateTimeOffset OccurredAt) : IDomainEvent;
public record CostAllocationRuleSet(string RuleId, CostAllocationRule Rule, DateTimeOffset OccurredAt) : IDomainEvent;
```

**New endpoints:**

```
POST /api/ledger/expenses/{id}/tag-enterprise
POST /api/ledger/revenue/{id}/tag-enterprise
POST /api/ledger/cost-allocation
GET  /api/ledger/reports/enterprise-pnl
GET  /api/ledger/reports/tax-categories
```

```bash
dotnet build src/FarmOS.Ledger.API/FarmOS.Ledger.API.csproj
git add src/FarmOS.Ledger.Domain/ src/FarmOS.Ledger.Infrastructure/ src/FarmOS.Ledger.API/
git commit -m "feat(ledger): add enterprise accounting — context tagging, cost allocation, P&L reporting"
```

---

### Task 18: Codex Decision Trees Extension

Add DecisionTree aggregate to existing Codex context.

**Files:**
- Modify: `src/FarmOS.Codex.Domain/Types.cs` — add DecisionTreeId, DecisionNode
- Modify: `src/FarmOS.Codex.Domain/Events.cs` — add DecisionTree events
- Create: `src/FarmOS.Codex.Domain/Aggregates/DecisionTree.cs`
- Modify: `src/FarmOS.Codex.Application/ICodexEventStore.cs`
- Create: `src/FarmOS.Codex.Application/Commands/DecisionTreeCommands.cs`
- Create: `src/FarmOS.Codex.Application/Commands/Handlers/DecisionTreeCommandHandlers.cs`
- Modify: `src/FarmOS.Codex.Infrastructure/CodexEventStore.cs`
- Modify: `src/FarmOS.Codex.API/CodexEndpoints.cs`

**New Types:**

```csharp
public record DecisionTreeId(Guid Value) { public static DecisionTreeId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record DecisionNode(string NodeId, string Question, string? YesNodeId, string? NoNodeId, string? ActionIfTerminal, string? Notes);
```

**Business rules:**
- Root node always has NodeId "root"
- Terminal nodes have ActionIfTerminal but no Yes/No children

```bash
dotnet build src/FarmOS.Codex.API/FarmOS.Codex.API.csproj
git add src/FarmOS.Codex.Domain/ src/FarmOS.Codex.Application/ src/FarmOS.Codex.Infrastructure/ src/FarmOS.Codex.API/
git commit -m "feat(codex): add DecisionTree aggregate for diagnostic flowcharts"
```

---

### Task 19: Compliance Grants Extension

Add Grant aggregate to existing Compliance context.

**Files:**
- Modify: `src/FarmOS.Compliance.Domain/Types.cs` — add GrantId, GrantStatus, GrantMilestone
- Modify: `src/FarmOS.Compliance.Domain/Events.cs` — add Grant events
- Create: `src/FarmOS.Compliance.Domain/Aggregates/Grant.cs`
- Modify application/infrastructure/API layers

**New Types:**

```csharp
public record GrantId(Guid Value) { public static GrantId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public enum GrantStatus { Applied, Awarded, Active, Reporting, Closed, Denied }
public record GrantMilestone(string Description, DateOnly DueDate, bool Completed, string? ReportPath);
```

**Business rules:**
- Grant milestones trigger compliance alerts when due dates approach
- Cannot complete a milestone on a Denied/Closed grant

```bash
dotnet build src/FarmOS.Compliance.API/FarmOS.Compliance.API.csproj
git add src/FarmOS.Compliance.Domain/ src/FarmOS.Compliance.Application/ src/FarmOS.Compliance.Infrastructure/ src/FarmOS.Compliance.API/
git commit -m "feat(compliance): add Grant aggregate with milestones and lifecycle"
```

---

### Task 20: Crew Apprentice Programs Extension

Add ApprenticeProgram aggregate to existing Crew context.

**Files:**
- Modify: `src/FarmOS.Crew.Domain/Types.cs` — add ApprenticeProgramId, RotationAssignment
- Modify: `src/FarmOS.Crew.Domain/Events.cs` — add apprentice events
- Create: `src/FarmOS.Crew.Domain/Aggregates/ApprenticeProgram.cs`
- Modify application/infrastructure/API layers

**New Types:**

```csharp
public record ApprenticeProgramId(Guid Value) { public static ApprenticeProgramId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString(); }
public record RotationAssignment(Enterprise Enterprise, DateOnly StartDate, DateOnly EndDate, string? Mentor);
```

**Business rules:**
- Rotation enforces min 2 weeks, max 12 weeks per enterprise
- Cannot rotate to same enterprise back-to-back

```bash
dotnet build src/FarmOS.Crew.API/FarmOS.Crew.API.csproj
git add src/FarmOS.Crew.Domain/ src/FarmOS.Crew.Application/ src/FarmOS.Crew.Infrastructure/ src/FarmOS.Crew.API/
git commit -m "feat(crew): add ApprenticeProgram aggregate with rotation management"
```

---

### Task 21: Phase 3 Verification + Final Build

```bash
dotnet build FarmOS.sln 2>&1 | tail -20
dotnet test --filter "FullyQualifiedName~Crew|Compliance|Codex|Campus|Counter" -v minimal
git commit --allow-empty -m "milestone: Phase 3 complete — Ledger extensions, Decision Trees, Grants, Apprentice Programs"
```

---

## Summary — Total Scope

| Phase | Contexts | Aggregates | Events | Tests |
|-------|----------|------------|--------|-------|
| Phase 1 | Crew, Compliance, Codex, Commerce CRM | Worker, Shift, Permit, InsurancePolicy, Procedure, Playbook, Customer | ~30 | ~25 |
| Phase 2 | Campus, Counter, Commerce ext | Event, Booking, Register, Sale, CashDrawer, BuyingClub, WholesaleAccount | ~25 | ~20 |
| Phase 3 | Ledger ext, Codex ext, Compliance ext, Crew ext | DecisionTree, Grant, ApprenticeProgram + Ledger projections | ~15 | ~10 |
| **Total** | **7 feature areas** | **~17 aggregates** | **~70 events** | **~55 tests** |

---

## Frontend Tasks (Post-Backend)

After all backend phases are complete, create 5 new frontend apps:

1. **Crew-OS** (`frontend/crew-os/`) — Worker profiles, shift scheduling, certifications
2. **Campus-OS** (`frontend/campus-os/`) — Event management, bookings, attendance
3. **Counter-OS** (`frontend/counter-os/`) — POS terminal, sale flow, cash drawer
4. **Commerce-OS** (`frontend/commerce-os/`) — CRM, buying clubs, wholesale, dedup UI
5. **Codex-OS** (`frontend/codex-os/`) — SOP editor, playbook browser, decision tree viewer

Each frontend app follows the apiary-os pattern:
- `deno.json` + `fresh.config.ts` + `main.ts`
- `routes/` for pages, `islands/` for interactive components, `components/` for static
- `utils/farmos-client.ts` typed API client
- NavBar, dashboard, detail panels, forms

Frontend implementation will be planned separately after backend stabilization.
