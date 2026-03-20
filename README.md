# FarmOS — Sovereign Farm Operating System

> *A fully air-gapped, anti-fragile digital operating system for a 20-acre permaculture farm in Rome, Georgia.*

## Philosophy

FarmOS is not a web app bolted onto farm life. It is a **digital nervous system** that mirrors the biological reality of a diversified permaculture operation. Every design decision serves three axioms:

1. **Sovereignty** — The system runs entirely on local hardware. Zero cloud dependencies. Internet is a luxury, not a requirement.
2. **Immutability** — Every event that happens on this farm is an append-only historical fact. You never update the past; you only record what happened next.
3. **Biological Fidelity** — The data model must respect ecological relationships. A paddock is not a row in a table; it is a node in a living graph connected to soil biology, grazing history, and seasonal cycles.

---

## Infrastructure Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     PROXMOX VE HYPERVISOR                       │
│                  (Industrial PC + MSI Raider)                   │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐ │
│  │  VM1: HAOS   │  │ VM2: AI Node │  │   VM3: K3s Cluster     │ │
│  │              │  │              │  │                        │ │
│  │  LoRaWAN     │  │  Ollama/vLLM │  │  ┌──────────────────┐  │ │
│  │  Weather API │  │  RTX 4060    │  │  │  App Pods        │  │ │
│  │  Fence Mon.  │  │  GPU Pass.   │  │  │  ─────────────   │  │ │
│  │              │  │  READ-ONLY   │  │  │  Pasture.API     │  │ │
│  │  Publishes   │  │              │  │  │  Flora.API       │  │ │
│  │  raw events  │  │  Zero write  │  │  │  Hearth.API      │  │ │
│  │  to RabbitMQ │  │  authority   │  │  │  Apiary.API      │  │ │
│  └──────┬───────┘  └──────────────┘  │  │  Commerce.API    │  │ │
│         │                            │  │  RabbitMQ (pod)  │  │ │
│         │         ┌──────────────┐   │  │  Deno Fresh FEs  |  │ │
│         └────────►│   RabbitMQ   │◄──│  └──────────────────┘  │ │
│                   │  (message    │   │                        │ │
│                   │   bus)       │   └────────────────────────┘ │
│                   └──────────────┘                              │
│  ┌──────────────────┐                                           │
│  │  LXC: ArangoDB   │  ◄── Dedicated container, direct I/O      │
│  │  Document + Graph│                                           |
│  └──────────────────┘                                           │
└─────────────────────────────────────────────────────────────────┘
```

### Placement Decisions

| Component | Runs In | Rationale |
|-----------|---------|-----------|
| **ArangoDB** | LXC container on Proxmox | Databases need dedicated resources, direct disk I/O, and an independent lifecycle. A K3s upgrade should never bounce your data layer. |
| **RabbitMQ** | StatefulSet pod in K3s | Co-located with the services it connects. K3s provides service discovery (`rabbitmq.default.svc.cluster.local`). Messages are transient — persistence is nice-to-have, not critical. |
| **Home Assistant** | Dedicated VM (HAOS) | Official deployment model. Handles hardware abstraction only — LoRaWAN, weather, fencing. Zero farm logic. |
| **AI Node** | Dedicated VM w/ GPU passthrough | RTX 4060 passthrough requires VM-level isolation. Air-gapped, read-only. Queried via HTTP by backend services. |
| **App Services** | K3s pods | All C#/F# microservices + Deno Fresh frontends. K3s handles health checks, restarts, and rolling updates. |

### Why K3s (Not Docker Compose)

K3s is the right call even for a family operation:
- **Self-healing**: If the sourdough pH monitor pipeline crashes at 2 AM, K3s restarts it automatically
- **Single binary**: K3s is <100MB, designed for edge/IoT — not enterprise bloat
- **Service discovery**: Services find each other by name, no hardcoded IPs
- **Future-proof**: The MSI Raider can join as a second agent node with one command
- **Start single-node**: Run `k3s server` on one VM. That's it. Add workers later if needed.

---

## Bounded Contexts

Five strictly isolated domains. Communication is **events on RabbitMQ only**.

| Context | Governs | Aggregate Roots |
|---------|---------|-----------------|
| **Pasture** | Biomass, soil rest, livestock movement, individual animal health/lifecycle | `Paddock`, `Animal`, `Herd`, `GrazingRotation` |
| **Flora** | Orchard guilds, cut flower succession, seed inventory | `OrchardGuild`, `FlowerBed`, `CropSuccession`, `SeedLot` |
| **Hearth** | Sourdough batches, kombucha fermentation, HACCP compliance, living cultures | `SourdoughBatch`, `KombuchaBatch`, `LivingCulture`, `HACCPPlan` |
| **Apiary** | Hive inspections, queen tracking, mite counts, honey harvests | `Hive`, `Queen`, `HoneyHarvest`, `Inspection` |
| **Commerce** | CSA subscriptions, inventory projections, delivery routing, bakery orders | `Subscription`, `Order`, `DeliveryRoute`, `InventoryProjection` |

> **See**: [docs/domain-models.md](docs/domain-models.md) for full aggregate root definitions, value objects, and domain event catalogs per context.

---

## Database: Why ArangoDB

### The Case For ArangoDB

This farm's data is fundamentally **multi-model**. You need:

1. **Document Store** → Append-only CQRS event ledger (immutable JSON events)
2. **Graph Database** → Ecological relationships that no relational DB models cleanly
3. **Operational simplicity** → One database to back up, one to monitor, one to learn

| Alternative | Why Not |
|-------------|---------|
| PostgreSQL + Marten (events) + Apache AGE (graph) | Two paradigms duct-taped together. AGE is immature. |
| EventStoreDB + Neo4j | Two databases to operate, back up, and monitor. Operational burden too high for a family. |
| PostgreSQL + JSONB | Graph queries via recursive CTEs are painful and limited. No native traversal. |
| MongoDB | No native graph. Would need a second database anyway. |

### ArangoDB Fits Because:

- **Single engine, multiple models**: Documents, graphs, and key-value in one process
- **AQL**: One query language for document lookups AND graph traversals
- **Edge-friendly**: Runs efficiently on modest hardware (4GB RAM minimum)
- **C# Driver**: `arangodb-net-standard` (community-maintained, .NET Standard 2.0, supports System.Text.Json)
- **Backup simplicity**: `arangodump` → single backup of your entire farm's history

### Collections Architecture

```
Event Store (Document Collections):
  pasture_events        ← append-only, immutable
  flora_events          ← append-only, immutable
  hearth_events         ← append-only, immutable
  apiary_events         ← append-only, immutable
  commerce_events       ← append-only, immutable

Read Projections (Document Collections):
  pasture_paddock_view  ← rebuilt from events
  pasture_animal_view   ← rebuilt from events
  flora_flower_bed_view ← rebuilt from events
  hearth_batch_view     ← rebuilt from events
  ...

Graph Collections:
  farm_graph            ← vertices: Paddock, Animal, Plant, Hive, Guild, Bed
                        ← edges: GRAZED, FOLLOWS_IN_ROTATION, FIXES_NITROGEN_FOR,
                                 POLLINATES, MEMBER_OF_GUILD, BORN_FROM, HARVESTED_FROM
```

> **See**: [docs/graph-model.md](docs/graph-model.md) for the full graph schema and example AQL traversal queries.

---

## CQRS Architecture

> **See**: [docs/cqrs-architecture.md](docs/cqrs-architecture.md) for the full event store design, command/query separation patterns, and F# rules engine integration.

### Core Rules

1. **Commands** return `Result<Guid, Error>` — never domain data
2. **Queries** hit flattened projection collections — never the event store
3. **Events** are C# `record` types — immutable, append-only
4. **Domain Rules** are F# modules — pure functions, no side effects
5. **Cross-context communication** is RabbitMQ events only

### Flow

```
Command → Validate (F# rules) → Persist Event → Publish to RabbitMQ
                                      │
                              ┌───────┴────────┐
                              │  Event Store   │ (append-only)
                              │  (ArangoDB)    │
                              └───────┬────────┘
                                      │
                              ┌───────┴────────┐
                              │  Projector     │ (rebuilds read models)
                              │  (Background)  │
                              └───────┬────────┘
                                      │
                              ┌───────┴────────┐
                              │  Read Model    │ (ArangoDB flat docs)
                              └────────────────┘
                                      │
Query ────────────────────────────────┘
```

---

## Micro-Frontends (Deno Fresh)

Three role-specific UIs, all server-side rendered, shipping **zero JS by default**.

| Frontend | Users | Purpose |
|----------|-------|---------|
| **FieldOps** | You, wife, older kids (10, 12) | Paddock dashboard, animal tracking, task boards ("move broilers to paddock 7"), flower bed schedules |
| **HearthOS** | You, wife, all kids | Kitchen dashboard, batch tracking, pH monitoring, HACCP logs, culture feeding schedules |
| **EdgePortal** | CSA customers | Order management, pickup schedules, available inventory. **Hosted externally** with a read-only API sync from local Commerce context. |

### Role System

```
roles:
  steward:        # You — full access, all contexts
  partner:        # Wife — full access, all contexts
  apprentice:     # Older kids (10, 12) — FieldOps + HearthOS, simplified views
  helper:         # Younger kids (4, 7) — HearthOS read-only (recipe display, timer)
  customer:       # CSA members — EdgePortal only
```

### EdgePortal Architecture

The EdgePortal is **not** served from your farm hardware. Instead:

```
┌─────────────────┐         ┌──────────────────┐
│  Local FarmOS    │  ──►   │  External Host    │
│  Commerce API    │  sync  │  (Vercel/Deno     │
│  (K3s pod)       │  API   │  Deploy/VPS)      │
│                  │        │                   │
│  Pushes:         │        │  EdgePortal UI    │
│  - Available     │        │  + small DB       │
│    inventory     │        │  (SQLite/Turso)   │
│  - Pickup slots  │        │                   │
│  - Order status  │        │  Customers see    │
│                  │        │  this site        │
└─────────────────┘         └──────────────────┘
```

> **See**: [docs/micro-frontends.md](docs/micro-frontends.md) for component architecture, island hydration strategy, and routing.

---

## Sensor Telemetry Pipeline

```
Physical Sensors (LoRaWAN)
    │
    ▼
Home Assistant (VM1)
    │  Raw MQTT/webhook events
    ▼
RabbitMQ Exchange: "farm.telemetry"
    │
    ├──► Pasture.Telemetry.Consumer  (soil moisture, temp, fence voltage)
    ├──► Flora.Telemetry.Consumer    (greenhouse temp/humidity)
    ├──► Hearth.Telemetry.Consumer   (fermentation chamber temp, pH probes)
    └──► Apiary.Telemetry.Consumer   (hive weight, temp)
```

Home Assistant is a **dumb sensor gateway**. It publishes structured events like:

```json
{
  "source": "lorawan:soil-probe-paddock-3",
  "type": "soil.moisture.reading",
  "value": 42.7,
  "unit": "percent",
  "timestamp": "2026-03-15T08:30:00Z"
}
```

Each bounded context's consumer decides what to do with it. Home Assistant knows nothing about paddocks, rotations, or guilds.

---

## AI Integration Layer

The local LLM (Ollama/vLLM on RTX 4060) is a **read-only oracle**:

- **Zero write authority** — Cannot issue commands or modify state
- **Queried by backend** — C# services call it for insights, never the reverse
- **Use cases**:
  - "Given paddock 3's grazing history and current biomass estimate, what is the optimal rest period?"
  - "Based on this sourdough starter's pH trajectory, is it likely to be ready for baking by 6 AM?"
  - "Which orchard guild members are likely to bloom in the next 2 weeks given current GDD accumulation?"

```csharp
// Example: AI query from Pasture context
public record AiInsightQuery(string Context, string Prompt, IReadOnlyDictionary<string, object> Data);
public record AiInsightResponse(string Insight, float Confidence, string Reasoning);

// The AI adapter is injected, never directly coupled
public interface IEcologicalOracle
{
    Task<AiInsightResponse> QueryAsync(AiInsightQuery query, CancellationToken ct);
}
```

---

## Project Structure

```
FarmOS/
├── src/
│   ├── FarmOS.SharedKernel/           # Cross-cutting: EventStore abstractions, Result types, RabbitMQ contracts
│   ├── FarmOS.Pasture/
│   │   ├── FarmOS.Pasture.Domain/     # Aggregate roots, value objects, F# rules
│   │   ├── FarmOS.Pasture.Application/# Commands, queries, handlers (MediatR)
│   │   ├── FarmOS.Pasture.Infrastructure/ # ArangoDB repos, RabbitMQ publishers
│   │   └── FarmOS.Pasture.API/        # Minimal API endpoints
│   ├── FarmOS.Flora/                  # Same layered structure
│   ├── FarmOS.Hearth/                 # Same layered structure
│   ├── FarmOS.Apiary/                 # Same layered structure
│   ├── FarmOS.Commerce/               # Same layered structure
│   └── FarmOS.AI/                     # Ollama/vLLM adapter, IEcologicalOracle
├── frontends/
│   ├── field-ops/                     # Deno Fresh app
│   ├── hearth-os/                     # Deno Fresh app
│   └── edge-portal/                   # Deno Fresh app (deployed externally)
├── deploy/
│   ├── k3s/                           # Kubernetes manifests
│   ├── proxmox/                       # VM/LXC provisioning scripts
│   └── docker/                        # Dockerfiles per service
├── docs/
│   ├── domain-models.md               # Full aggregate root definitions
│   ├── graph-model.md                 # ArangoDB graph schema
│   ├── cqrs-architecture.md           # Event store & projection design
│   ├── micro-frontends.md             # Deno Fresh component architecture
│   └── development-roadmap.md         # Phased build plan
├── tests/
│   ├── FarmOS.Pasture.Tests/
│   ├── FarmOS.Flora.Tests/
│   ├── FarmOS.Hearth.Tests/
│   ├── FarmOS.Apiary.Tests/
│   └── FarmOS.Commerce.Tests/
├── FarmOS.sln
└── README.md
```

---

## Development Roadmap

### Phase 1: Foundation (Weeks 1–3)
- Proxmox VM/LXC provisioning (K3s VM, ArangoDB LXC, HAOS VM)
- SharedKernel: Event store abstraction, RabbitMQ integration, Result types
- ArangoDB collections, indexes, and graph schema
- First bounded context: **Pasture** (paddock CRUD, animal registration, grazing events)

### Phase 2: Core Domains (Weeks 4–8)
- Flora context (orchard guilds, flower bed management, succession scheduling)
- Hearth context (sourdough batch tracking, kombucha pH logging, HACCP plans)
- Apiary context (hive inspections, queen tracking, honey harvests)
- FieldOps frontend (paddock map, animal dashboard, task board)

### Phase 3: Commerce & Integration (Weeks 9–12)
- Commerce context (CSA subscriptions, inventory projections, order management)
- HearthOS frontend (kitchen dashboard, batch timers, pH graphs)
- EdgePortal + external hosting + sync API
- Cross-context event integration (e.g., Hearth honey demand → Apiary harvest signal)

### Phase 4: Intelligence & Polish (Weeks 13–16)
- AI Node integration (Ollama, IEcologicalOracle)
- Sensor telemetry pipeline (Home Assistant → RabbitMQ → consumers)
- Graph-powered insights (rotation recommendations, guild health analysis)
- Role-based simplified views for children

---

## Detailed Documentation

| Document | Contents |
|----------|----------|
| [docs/system-status-and-setup.md](docs/system-status-and-setup.md) | **[START HERE]** System status, Docker Compose, ArangoDB prep, & Gateway runtime instructions |
| [docs/frontend-integration-guide.md](docs/frontend-integration-guide.md) | Deno fetch-client guide, CQRS patterns, Gateway routing, & Token auth specification |
| [docs/api-reference-pasture.md](docs/api-reference-pasture.md) | Extensive Pasture API contract (the reference implementation) mapping out REST endpoints |
| [docs/projectors-architecture.md](docs/projectors-architecture.md) | CQRS background workers mapping out Eventual Consistency updates into rapid view collections |
| [docs/api-reference-flora.md](docs/api-reference-flora.md) | Extensive Flora API contract mapping out Orchard Guilds, Beds, and Seeds REST endpoints |
| [docs/api-reference-hearth.md](docs/api-reference-hearth.md) | Hearth OS API detailing batches (Sourdough/Kombucha) and Living Culture feedings |
| [docs/api-reference-commerce.md](docs/api-reference-commerce.md) | Farm economic operations API: CSA Seasons, Memberships, & Asynchronous custom orders |
| [docs/domain-models.md](docs/domain-models.md) | Aggregate roots, value objects, domain events, and F# rule modules for all 5 contexts |
| [docs/graph-model.md](docs/graph-model.md) | ArangoDB graph schema, edge definitions, and example AQL traversals |
| [docs/cqrs-architecture.md](docs/cqrs-architecture.md) | Event store design, projection rebuilding, command/query handler patterns |
| [docs/micro-frontends.md](docs/micro-frontends.md) | Deno Fresh islands architecture, role-based routing, component catalog |
| [docs/development-roadmap.md](docs/development-roadmap.md) | Detailed phase breakdown with acceptance criteria |
| [docs/enterprise-saas-evolution.md](docs/enterprise-saas-evolution.md) | SaaS transformation: multi-tenancy, gap analysis vs farmos.org, pricing, hybrid architecture |
| [docs/implementation-guide.md](docs/implementation-guide.md) | Step-by-step build guide: solution structure, SharedKernel, API endpoint catalog, Deno frontend conventions, Docker/K3s |

---

*Built for a family. Run on sovereign iron. Designed to outlast the software industry's next hype cycle.*
