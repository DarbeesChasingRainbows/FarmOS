# FarmOS → Enterprise SaaS Evolution

> Analysis of what it takes to transform a sovereign family farm OS into a multi-tenant SaaS product for the regenerative agriculture market.

---

## Gap Analysis: What farmos.org Covers That We're Missing

farmos.org defines a general-purpose farm data model. After studying their schema, here's what we should absorb:

### Asset Types We're Missing

| farmos.org Asset | Our Status | Action Needed |
|-----------------|------------|---------------|
| **Land** | ✅ Covered as `Paddock` | Generalize to `LandAsset` (paddock, field, forest, pond, etc.) |
| **Plant** | ✅ Covered in Flora (guild members, flower beds) | Good |
| **Animal** | ✅ Strong (individual tracking, health, lineage) | We're ahead of farmos.org here |
| **Equipment** | ❌ **Missing entirely** | Need `Equipment` aggregate (tractors, tillers, irrigation, fencing tools, butchering equipment). Track maintenance schedules, depreciation, location. |
| **Compost** | ❌ **Missing** | Need `CompostBatch` in Hearth context (windrow tracking, temperature logs, C:N ratio, turning schedule) |
| **Structure** | ❌ **Missing** | Need `Structure` asset (barns, greenhouses, hoop houses, walk-in coolers, processing rooms). Critical for HACCP zone tracking. |
| **Sensor** | ⚠️ Partially covered | We have telemetry pipeline but no explicit Sensor asset registry mapping physical devices to logical locations |
| **Water** | ❌ **Missing** | Need `WaterSource` asset (ponds, wells, rainwater catchment, irrigation lines). Critical for drought planning. |
| **Material** | ⚠️ Partially covered | `SeedLot` exists but general materials (mulch, compost amendments, organic inputs, packaging) are missing |
| **Product** | ⚠️ Partially covered | Commerce has implicit products but no standalone `Product` catalog |
| **Group** | ✅ Covered as `Herd` | Good |

### Log Types We're Missing

| farmos.org Log | Our Status | Action Needed |
|---------------|------------|---------------|
| **Activity** | ⚠️ Implicit in events | Need a general-purpose `ActivityLog` for tasks that don't fit specific categories |
| **Observation** | ❌ **Missing** | Need `Observation` log (notes, photos, field observations that aren't formally structured) |
| **Input** | ❌ **Missing** | Need `InputLog` (organic amendments, foliar sprays, compost tea applications, mineral supplements for livestock) |
| **Harvest** | ✅ In Flora (flower stems) + Apiary (honey) | Generalize to work across all contexts |
| **Lab Test** | ❌ **Missing** | Need `LabTest` log (soil tests, water quality, leaf tissue analysis, pathogen tests) |
| **Maintenance** | ❌ **Missing** | Need `MaintenanceLog` for equipment and structures |
| **Medical** | ✅ In Pasture (animal treatments) | Good |
| **Seeding** | ✅ In Flora (succession planting) | Good |
| **Transplanting** | ⚠️ Implicit | Should be explicit as distinct from seeding |

### Other Gaps

| Feature | Description |
|---------|-------------|
| **Geometry/GIS** | farmos.org stores GeoJSON geometry on every asset and log. We need spatial data on paddocks, beds, hive locations, structures. |
| **Photo/File attachments** | farmos.org attaches images and files to every record type. We need a file storage strategy. |
| **Quantity model** | farmos.org has a reusable `Quantity` type (value, unit, measure, label) attached to logs. We should adopt this pattern. |
| **Data Streams** | farmos.org has first-class sensor data streams. Our telemetry pipeline needs a persistent time-series strategy. |
| **Weather integration** | Degree-day accumulation, frost date tracking, precipitation logging. Critical for Flora scheduling. |

---

## Enterprise SaaS Architecture

### Multi-Tenancy Model

The single-tenant sovereign design must evolve to support multiple farms while keeping the same domain logic.

```
                    ┌──────────────────────────────────────────────┐
                    │           SAAS CONTROL PLANE                 │
                    │                                              │
                    │  ┌──────────┐  ┌──────────┐  ┌───────────┐  │
                    │  │ Identity │  │ Billing  │  │ Tenant    │  │
                    │  │ (Auth0/  │  │ (Stripe) │  │ Mgmt      │  │
                    │  │  Keycloak)│  │          │  │           │  │
                    │  └──────────┘  └──────────┘  └───────────┘  │
                    └──────────────────┬───────────────────────────┘
                                       │
                    ┌──────────────────┼───────────────────────────┐
                    │    APPLICATION PLANE (per-region K8s)        │
                    │                  │                           │
                    │  ┌───────────────┴──────────────────┐       │
                    │  │     API Gateway (Caddy)           │       │
                    │  │     Tenant resolution from JWT    │       │
                    │  └───────────────┬──────────────────┘       │
                    │                  │                           │
                    │  ┌───────┬───────┼───────┬──────────┐       │
                    │  │       │       │       │          │       │
                    │  │Pasture│ Flora │Hearth │ Apiary   │       │
                    │  │.API   │.API   │.API   │.API      │       │
                    │  │       │       │       │          │       │
                    │  └───┬───┴───┬───┴───┬───┴────┬─────┘       │
                    │      │       │       │        │              │
                    │  ┌───┴───────┴───────┴────────┴─────┐       │
                    │  │         RabbitMQ Cluster          │       │
                    │  └──────────────────────────────────┘       │
                    │                                              │
                    │  ┌──────────────────────────────────┐       │
                    │  │  ArangoDB Cluster (sharded)      │       │
                    │  │  Tenant isolation: collection    │       │
                    │  │  prefix per tenant               │       │
                    │  └──────────────────────────────────┘       │
                    └──────────────────────────────────────────────┘
```

### Tenant Isolation Strategy

| Strategy | How | Trade-off |
|----------|-----|-----------|
| **Database-per-tenant** | Each farm gets its own ArangoDB database | Best isolation, worst resource efficiency. Good for enterprise tier. |
| **Collection-prefix-per-tenant** | `tenant_abc_pasture_events` | Good balance. Medium isolation, efficient. Good for standard tier. |
| **Shared collections with tenant_id** | `WHERE tenant_id = @tid` | Most efficient, weakest isolation. Risky for farm data. **Not recommended.** |

**Recommendation**: **Hybrid** — Standard tier gets collection-prefix isolation. Enterprise tier gets database-per-tenant. Both share the same ArangoDB cluster.

### What Changes From Sovereign → SaaS

| Component | Sovereign (Your Farm) | SaaS (Multi-Tenant) |
|-----------|----------------------|---------------------|
| **Auth** | PIN-based family auth | OAuth2/OIDC (Keycloak self-hosted or Auth0) |
| **Database** | Single ArangoDB LXC | ArangoDB cluster (3+ nodes, sharded) |
| **Message Bus** | Single RabbitMQ pod | RabbitMQ cluster with vhosts per tenant |
| **AI** | Local Ollama on your GPU | Shared GPU pool or per-tenant GPU allocation (expensive tier) |
| **Hosting** | Proxmox on your hardware | Cloud K8s (EKS/GKE/AKS) or Hetzner bare-metal |
| **Frontends** | 3 Deno Fresh apps on LAN | CDN-distributed, tenant-branded |
| **Sensors** | Direct LoRaWAN → Home Assistant | MQTT broker as a service, webhook ingestion API |
| **Backups** | Local `arangodump` | Automated S3 snapshots per tenant |
| **Billing** | N/A | Stripe integration with usage metering |

---

## Context Evolution for SaaS

The 5 bounded contexts expand to handle multi-farm generality:

### New Bounded Context: **Assets**

A cross-cutting context that generalizes the asset types farmos.org identified:

```csharp
public abstract class FarmAsset : AggregateRoot<AssetId>
{
    public TenantId TenantId { get; }
    public string Name { get; protected set; }
    public AssetStatus Status { get; protected set; }         // Active | Inactive | Archived
    public GeoJsonGeometry? Geometry { get; protected set; }  // GIS location
    public IReadOnlyList<IdTag> IdTags { get; }               // Multiple ID schemes
    public IReadOnlyList<FileAttachment> Files { get; }
    public IReadOnlyList<ImageAttachment> Images { get; }
}

// Concrete types
public sealed class EquipmentAsset : FarmAsset { /* manufacturer, model, serial, maintenance schedule */ }
public sealed class StructureAsset : FarmAsset { /* type (barn, greenhouse, cooler), capacity */ }
public sealed class WaterAsset : FarmAsset { /* type (well, pond, catchment), capacity_gallons, flow_rate */ }
public sealed class CompostAsset : FarmAsset { /* windrow/bin, current_temp, cn_ratio, start_date */ }
public sealed class MaterialAsset : FarmAsset { /* type (mulch, amendment, packaging), quantity_on_hand */ }
```

### New Bounded Context: **Ledger**

Financial tracking that no farm SaaS can ignore:

```csharp
public sealed class Expense : AggregateRoot<ExpenseId>
{
    public TenantId TenantId { get; }
    public string Category { get; }           // Feed, Seed, Equipment, Fuel, Veterinary, Packaging
    public decimal Amount { get; }
    public string Vendor { get; }
    public DateOnly Date { get; }
    public IReadOnlyList<AssetId> RelatedAssets { get; }      // What was this expense for?
}

public sealed class Revenue : AggregateRoot<RevenueId>
{
    public TenantId TenantId { get; }
    public string Source { get; }             // CSA, Farmers Market, Wholesale, Direct Sale
    public decimal Amount { get; }
    public OrderId? OrderId { get; }
    public DateOnly Date { get; }
}

// Projections: profit/loss per enterprise, cost per unit of production, ROI per paddock
```

### Bounded Context Summary (SaaS)

| # | Context | Sovereign | SaaS Addition |
|---|---------|-----------|---------------|
| 1 | **Pasture** | ✅ | Add tenant isolation, GIS paddock boundaries |
| 2 | **Flora** | ✅ | Add weather/GDD integration, transplant logging |
| 3 | **Hearth** | ✅ | Add compost tracking, generalize batch types |
| 4 | **Apiary** | ✅ | No major changes |
| 5 | **Commerce** | ✅ | Add Stripe billing, multi-market support |
| 6 | **Assets** | **NEW** | Equipment, structures, water, materials, sensors |
| 7 | **Ledger** | **NEW** | Financial tracking, P&L, cost-per-unit |
| 8 | **Identity** | **NEW** | Tenant management, roles, team invitations |

---

## Pricing Model

### Competitive Positioning

| Competitor | Pricing | Weakness |
|-----------|---------|----------|
| **Granular** (Corteva) | ~$30/mo+ | Row-crop focused, not permaculture-aware |
| **Bushel Farm** | Tiered (free → enterprise) | Commodity grain focused |
| **AgriWebb** | Per-head livestock | Livestock only, no crop/fermentation |
| **farmos.org** | Free (self-hosted) | No hosted option, Drupal-based, limited UI |
| **Farmbrite** | $29–$99/mo | General but shallow — no HACCP, no graph |

**Our differentiator**: The only platform purpose-built for **diversified regenerative operations** — permaculture guilds, rotational grazing, fermentation, and direct-to-consumer commerce in one system. Everyone else is either row-crop or livestock-only.

### Proposed Tiers

| Tier | Price | Target | Includes |
|------|-------|--------|----------|
| **Seedling** | $29/mo | Market garden / cut flower farm | Flora + Commerce + Assets. 3 users, 10 acres. |
| **Homestead** | $79/mo | Diversified family farm | All contexts. 10 users, 100 acres, HACCP. |
| **Ranch** | $149/mo | Livestock-focused operation | All contexts. 25 users, 500 acres, advanced grazing analytics. |
| **Enterprise** | Custom | Multi-site / co-ops / land trusts | Database-per-tenant, dedicated AI, SLA, white-label. |

**Usage add-ons**:
- Additional sensor endpoints: $5/mo per device
- AI insights (if using our GPU pool): $0.02/query
- Additional acreage: $0.50/acre/mo over tier limit

---

## Hybrid Architecture: Sovereign + SaaS

The killer feature — your farm runs sovereign, but can opt into the SaaS cloud for specific capabilities:

```
┌──────────────────────────┐        ┌────────────────────────┐
│   YOUR FARM (Sovereign)  │        │   FARMOS CLOUD (SaaS)  │
│                          │        │                        │
│   Full FarmOS running    │  ───►  │   Optional sync:       │
│   on your Proxmox box    │  sync  │   - Offsite backup     │
│                          │  ◄───  │   - AI insights pool   │
│   Works 100% offline     │        │   - EdgePortal hosting  │
│   You own your data      │        │   - Weather data feed  │
│                          │        │   - Community benchmarks│
│   PIN auth for family    │        │   - Mobile app proxy   │
└──────────────────────────┘        └────────────────────────┘
```

This "sovereign-first, cloud-optional" model is unique in the market and deeply aligned with the regenerative farming ethos of self-reliance.

---

## Mobile Strategy

SaaS customers expect mobile. Two approaches:

| Approach | Pros | Cons |
|----------|------|------|
| **PWA (Progressive Web App)** | Same Deno Fresh SSR codebase, works offline, no app store | Limited native APIs |
| **Capacitor / Tauri Mobile** | Wrap existing web UI in native shell, access camera/GPS/NFC | Build/deploy complexity |

**Recommendation**: PWA first. The SSR-by-default architecture already works great as a PWA. Add service worker for offline caching of read models. Camera access for photo observations. GPS for mapping paddock walks.

---

## API Strategy for Third-Party Integration

```
Public API (REST + JSON:API)
    │
    ├── /api/v1/assets          CRUD for all asset types
    ├── /api/v1/logs            CRUD for all log types  
    ├── /api/v1/plans           Farm plans and schedules
    ├── /api/v1/taxonomy        Terms, categories, vocabularies
    ├── /api/v1/telemetry       Sensor data ingestion
    │
    ├── Webhooks                Event notifications to external systems
    ├── OAuth2 Scopes           read:pasture, write:hearth, admin:tenant
    │
    └── GraphQL (optional)      For complex graph queries
```

**Integration targets**:
- **QuickBooks / Wave** — Ledger sync
- **Mailchimp / SendGrid** — CSA customer communication
- **Zapier** — Workflow automation for non-technical farmers
- **USDA / NRCS** — Compliance reporting for organic cert / conservation programs

---

## Technical Debt Avoidance: Build Once, Scale Twice

The key architectural insight: **build your sovereign version with tenant-awareness baked in from day one**.

```csharp
// Even in sovereign mode, every aggregate carries a TenantId
// In sovereign: it's always the same single GUID
// In SaaS: it's resolved from the JWT

public abstract class AggregateRoot<TId>
{
    public TenantId TenantId { get; protected set; }
    public TId Id { get; protected set; }
    public int Version { get; protected set; }
    // ...
}

// Sovereign: TenantId = hardcoded farm GUID
// SaaS: TenantId = extracted from authenticated user's claims
```

This means you **never refactor your domain model** when you go multi-tenant. The only changes are infrastructure-level (auth, routing, database provisioning).

---

## SaaS Development Roadmap Delta

These phases build ON TOP of the existing 16-week sovereign roadmap:

### Phase 5: SaaS Foundation (Weeks 17–20)
- Keycloak integration (replaces PIN auth for cloud)
- Tenant provisioning workflow (sign up → create database/collections → seed)
- Collection-prefix tenant isolation in ArangoDB
- API Gateway with tenant resolution from JWT
- Stripe billing integration (subscription lifecycle)

### Phase 6: Missing Domains (Weeks 21–24)
- Assets context (Equipment, Structure, Water, Compost, Material)
- Ledger context (Expenses, Revenue, P&L projections)
- GIS/Geometry on all land assets and logs
- Photo/file upload + S3 storage
- Lab Test and Input log types

### Phase 7: Cloud Infrastructure (Weeks 25–28)
- Cloud K8s deployment (EKS or Hetzner)
- ArangoDB cluster provisioning
- RabbitMQ cluster with per-tenant vhosts
- CDN for frontends
- Monitoring (Prometheus + Grafana)

### Phase 8: Go-To-Market (Weeks 29–32)
- PWA mobile support
- Onboarding wizard (farm type → enable relevant contexts)
- Public API + documentation
- Landing page + marketing site
- Beta program (10 farms)
