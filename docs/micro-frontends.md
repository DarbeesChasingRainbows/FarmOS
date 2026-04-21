# Micro-Frontends — FarmOS

> Deno Fresh islands architecture, Arrow.js reactive UI, role-based routing, and component strategy.

---

## Architecture Principle

All frontends are **Deno Fresh 2.x** apps using the **islands architecture**:
- Pages are **server-side rendered** (SSR) by default — zero JS shipped
- Only interactive components ("islands") opt into client-side hydration
- Uses **Arrow.js** (`@arrow-js/core`) for reactive island components — zero-dependency, ~2KB
- Each island is a thin Preact wrapper (`useRef` + `useEffect`) that mounts Arrow's `reactive()` state and `html` tagged templates

---

## Frontend Apps

### HearthOS (`frontend/hearth-os/`)

**Users**: All family members (role-filtered views)
**Port**: `8000`

| Route | Purpose | Islands |
|-------|---------|---------|
| `/` | Dashboard: active batches, live IoT, demand signals | `ArrowHearthDashboard`, `ArrowIoTLiveFeed` |
| `/batches` | Active batch list (sourdough + kombucha) | `ArrowBatchStatusCards` |
| `/batches/:id` | Batch detail: HACCP log, phase tracking | `ArrowBatchDetailPanel` |
| `/batches/new` | Create new batch form | `ArrowNewBatchForm` |
| `/cultures` | Living culture registry + feeding schedule | `ArrowFeedingTimer`, `ArrowCreateCultureForm` |
| `/cultures/:id` | Culture detail + lineage tree | `ArrowCultureDetailPanel` |
| `/kombucha` | Kombucha batches, pH tracking | `ArrowKombuchaDashboard`, `ArrowRecordPHForm` |
| `/mushrooms` | Mushroom fruiting blocks | `ArrowMushroomBatchList` |
| `/mushrooms/new` | Inoculation form | `ArrowNewMushroomBatchForm` |
| `/mushrooms/:id` | Block detail + action panel | `ArrowMushroomActionPanel` |
| `/freeze-dryer` | Harvest Right cycles + readings | `ArrowFreezeDryerPanel` |
| `/compliance` | Equipment temps, sanitation, certs, deliveries | `ArrowEquipmentPanel`, `ArrowSanitationLog`, `ArrowStaffCertifications`, `ArrowDeliveryLog` |
| `/compliance/haccp-plan` | HACCP 7-principle template (print-optimized) | `ArrowHACCPPlanBuilder` |
| `/compliance/capa` | Corrective/Preventive Action tracking | `ArrowCAPADashboard` |
| `/iot` | IoT zones + device management | `ArrowIoTDashboard`, `ArrowZonesDashboard` |
| `/iot/zones/:id` | Zone detail + live telemetry | `ArrowZoneDetail` |
| `/settings` | App configuration | `ArrowSettingsPanel` |

---

### ApiaryOS (`frontend/apiary-os/`)

**Users**: Beekeepers, Steward
**Port**: `8001`

| Route | Purpose | Islands |
|-------|---------|---------|
| `/hives` | Hive overview with inspection summaries | `CreateHiveForm` (Modal), `HiveDetailPanel` (Sidebar) |
| `/hives/:id` | Hive detail: inspections, treatments, harvests | `WeightChart` |

---

### AssetOS (`frontend/asset-os/`)

**Users**: Steward, Partner
**Port**: `8002`

| Route | Purpose |
|-------|---------|
| `/equipment` | Farm equipment registry, maintenance logs |
| `/structures` | Barns, greenhouses, coolers |
| `/water` | Water sources + flow rates |
| `/compost` | Compost batches, temperature tracking |
| `/materials` | Supplies inventory |

---

### IoT-OS (`frontend/iot-os/`)

**Users**: Steward (sensor management)
**Port**: `8003`

| Route | Purpose |
|-------|---------|
| `/devices` | Device registration, zone assignment |
| `/zones` | Monitoring zones + live telemetry |
| `/alerts` | Alert rules, notification history |

---

### FlowerOS (`frontend/flower-os/`)

**Users**: Steward, Partner, Apprentice
**Port**: `8004`

| Route | Purpose |
|-------|---------|
| `/beds` | Flower bed succession calendar |
| `/guilds` | Orchard guild visual map |
| `/seeds` | Seed lot inventory |

---

### Shared (`frontend/shared/`)

Shared design tokens and component library consumed by all frontends:

```
frontend/shared/
  tokens/
    colors.css        # Farm-appropriate palette (earth tones, stone/amber)
    typography.css     # Inter font, size scale
    spacing.css        # 4px grid system
    components.css     # Shared button, card, form styles
```

---

## Docker Compose Service Map

| Service | Container | Port | Notes |
|---------|-----------|------|-------|
| HearthOS | `hearth-os-ui` | 8000 | Primary kitchen/production frontend |
| ApiaryOS | `apiary-os-ui` | 8001 | Beekeeping frontend |
| AssetOS | `asset-os-ui` | 8002 | Equipment/infrastructure |
| IoT-OS | `iot-os-ui` | 8003 | Sensor management |
| FlowerOS | `flower-os-ui` | 8004 | Market garden / orchard |

All frontends communicate with backend APIs through the **Caddy reverse proxy** at `http://caddy:5050` (Docker internal) / `http://localhost:5050` (host).

---

## Sync Architecture (EdgePortal ↔ Local)

```
Local FarmOS (Docker)                  External EdgePortal
━━━━━━━━━━━━━━━━━━                    ━━━━━━━━━━━━━━━━━━
Commerce.API                           EdgePortal API
    │                                      │
    │  PUSH (when internet available):     │
    │  ─► Available inventory              │
    │  ─► Pickup schedules                 │
    │  ─► Order status updates             │
    │                                      │
    │  PULL (when internet available):     │
    │  ◄─ New orders                       │
    │  ◄─ Subscription changes             │
    │  ◄─ Customer messages                │
    │                                      │
    │  Uses outbox pattern:                │
    │  Events queued locally during        │
    │  outage, synced when                 │
    │  connectivity restored               │
```

---

## Design Principles

- **Outdoor-readable**: High contrast ratios, large text, no thin fonts
- **Glove-friendly**: Minimum 48px touch targets on FieldOps
- **Print-friendly**: HACCP logs render cleanly on standard paper
- **Connectivity-agnostic**: SSR means pages work even if JS fails to load
- **Kitchen-safe**: Flour-covered hands, wet surfaces — large buttons with spacing
