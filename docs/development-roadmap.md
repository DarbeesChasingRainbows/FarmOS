# Development Roadmap — FarmOS

> Phased build plan with acceptance criteria.

---

## Phase 1: Foundation (Weeks 1–3)

### Infrastructure Provisioning
- [ ] Proxmox: Create K3s VM (4 vCPU, 8GB RAM, 50GB disk)
- [ ] Proxmox: Create ArangoDB LXC (2 vCPU, 4GB RAM, 100GB disk)
- [ ] Proxmox: Verify Home Assistant OS VM (existing)
- [ ] Install K3s (single-node server mode)
- [ ] Deploy ArangoDB 3.11+ in LXC, create `farmos` database
- [ ] Deploy RabbitMQ as K3s StatefulSet with management UI
- [ ] Verify inter-VM networking (K3s pods ↔ ArangoDB LXC ↔ HAOS VM)

### SharedKernel
- [ ] `FarmOS.SharedKernel` project: `AggregateRoot<TId>`, `Result<T, E>`, `DomainEvent` base
- [ ] Event store abstraction (`IEventStore.AppendAsync`, `LoadAsync`, `RebuildAsync`)
- [ ] ArangoDB client wrapper (using `arangodb-net-standard`)
- [ ] RabbitMQ publisher/consumer abstractions
- [ ] MediatR integration (command/query pipeline behaviors)
- [ ] F# interop project for rules shared types

### First Context: Pasture
- [ ] `pasture_events` collection + indexes in ArangoDB
- [ ] `pasture_paddock_view` projection collection
- [ ] `Paddock` aggregate: Create, BeginGrazing, EndGrazing
- [ ] `Animal` aggregate: Register, Isolate, RecordTreatment
- [ ] `Herd` aggregate: Create, MoveToPaddock, Add/Remove Animal
- [ ] F# rules: GrazingRules (45-day minimum, cow-days-per-acre)
- [ ] Pasture.API: Minimal API endpoints (6-8 endpoints)
- [ ] Graph: `paddocks`, `animals`, `herds` vertices + edges

**Acceptance**: Can register animals, create paddocks, move herds, and query paddock status via API. Grazing respects 45-day rest rule.

---

## Phase 2: Core Domains (Weeks 4–8)

### Flora Context
- [ ] `OrchardGuild` aggregate with N.A.P. guild composition
- [ ] `FlowerBed` aggregate with succession planting
- [ ] `SeedLot` aggregate with inventory tracking
- [ ] `guilds`, `plants`, `flower_beds` graph vertices + edges
- [ ] F# rules: Succession scheduling, seed inventory validation
- [ ] Flora.API endpoints

### Hearth Context
- [ ] `SourdoughBatch` aggregate with full HACCP CCP tracking
- [ ] `KombuchaBatch` aggregate with pH monitoring (Jun + Standard)
- [ ] `LivingCulture` aggregate with lineage (split/descended_from)
- [ ] `cultures` graph vertices + `descended_from` edges
- [ ] F# rules: KombuchaRules (pH ≤ 4.2, 7-day discard), SourdoughRules
- [ ] Hearth.API endpoints

### Apiary Context
- [ ] `Hive` aggregate with queen tracking
- [ ] `Inspection` aggregate with mite counts
- [ ] `HoneyHarvest` records
- [ ] `hives` graph vertices + `pollinates`, `located_in` edges
- [ ] Apiary.API endpoints

### FieldOps Frontend (v1)
- [ ] Deno Fresh project scaffolding
- [ ] Paddock dashboard (grid view with rest-day colors)
- [ ] Animal registry (list + filter)
- [ ] Herd movement form
- [ ] Task board (basic version)

**Acceptance**: All four internal contexts (Pasture, Flora, Hearth, Apiary) have full CQRS with commands, queries, projections, and API endpoints. FieldOps shows paddock status and allows herd moves.

---

## Phase 3: Commerce & Integration (Weeks 9–12)

### Commerce Context
- [ ] `Subscription` aggregate (CSA tiers, add-ons)
- [ ] `Order` aggregate (CSA pickup + standalone bakery)
- [ ] `InventoryProjection` (reads cross-context events)
- [ ] `DeliveryRoute` (Atlanta/Chattanooga routing)
- [ ] Commerce.API endpoints

### Cross-Context Integration
- [ ] RabbitMQ event routing (cross-context queues)
- [ ] Commerce subscribes to: `hearth.batch.completed`, `apiary.harvest.completed`, `flora.stems.available`, `pasture.meat.available`
- [ ] Hearth subscribes to: `commerce.demand.*`
- [ ] Integration tests for cross-context event flow

### HearthOS Frontend (v1)
- [ ] Active batch dashboard with status cards
- [ ] pH chart (real-time if sensor available, manual entry fallback)
- [ ] Culture registry with feeding schedule
- [ ] HACCP compliance log (print-optimized)

### ApiaryOS Frontend (v1)
- [ ] Deno Fresh project scaffolding
- [ ] Hive overview with inspection summaries
- [ ] Hive detail sidebar (slide-out panel)
- [ ] Create hive modal

### EdgePortal
- [ ] Deno Fresh project (deployable externally)
- [ ] Available inventory view
- [ ] CSA subscription signup
- [ ] Bakery order form
- [ ] Sync API (outbox pattern for offline resilience)

**Acceptance**: End-to-end flow: Hearth produces sourdough → event → Commerce updates inventory → EdgePortal shows loaves available → Customer orders → Commerce generates pickup → Sync completes.

---

## Phase 4: Intelligence & Polish (Weeks 13–16)

### AI Integration
- [ ] Ollama/vLLM deployment on AI node VM (GPU passthrough verified)
- [ ] `IEcologicalOracle` C# adapter
- [ ] Pasture: Grazing optimization queries
- [ ] Hearth: Fermentation prediction queries
- [ ] Flora: Growing degree day / bloom forecasting

### Sensor Telemetry
- [ ] Home Assistant → RabbitMQ integration (MQTT bridge or webhook)
- [ ] Pasture telemetry consumer (soil moisture, temp, fence voltage)
- [ ] Hearth telemetry consumer (fermentation chamber temp, pH probes)
- [ ] Apiary telemetry consumer (hive weight, temp)
- [ ] Sensor data → graph model updates

### Role-Based Views
- [ ] Apprentice mode for FieldOps (simplified, large touch targets)
- [ ] Helper mode for HearthOS (read-only, icons, timers)
- [ ] Partner role (full access across all frontends)
- [ ] Authentication (simple PIN-based for family, no external auth provider)

### Polish
- [ ] K3s health monitoring dashboard
- [ ] ArangoDB backup automation (`arangodump` cron)
- [ ] Graph-powered insight queries (rotation recommendations, guild health)
- [ ] Offline resilience testing (disconnect internet, verify full operation)

**Acceptance**: Full system operates air-gapped for 72+ hours. AI provides recommendations. Sensor data flows from LoRaWAN through to dashboards. All family members can use their respective UIs.
