# Micro-Frontends — FarmOS

> Deno Fresh islands architecture, role-based routing, and component strategy.

---

## Architecture Principle

All three frontends are **Deno Fresh** apps using the **islands architecture**:
- Pages are **server-side rendered** (SSR) by default — zero JS shipped
- Only interactive components ("islands") opt into client-side hydration
- Uses **Preact** for lightweight island components (~3KB)

---

## Frontend Apps

### FieldOps (`frontends/field-ops/`)

**Users**: Steward, Partner, Apprentice (older kids)

| Route | Purpose | Islands (JS) |
|-------|---------|--------------|
| `/paddocks` | Paddock grid with rest-day color coding | `PaddockMap` (interactive click-to-select) |
| `/paddocks/:id` | Paddock detail: grazing history, biomass, soil | `GrazingTimeline` (scrollable) |
| `/animals` | Animal registry, filterable by species/status | `AnimalFilter` (dropdown + search) |
| `/animals/:id` | Animal detail: medical history, lineage | None (static SSR) |
| `/herds/:id/move` | Move herd to new paddock (command form) | `PaddockSelector` (map-based) |
| `/flowers` | Flower bed succession calendar | `SuccessionCalendar` (interactive timeline) |
| `/guilds` | Orchard guild visual map | `GuildVisualizer` (SVG) |
| `/tasks` | Daily task board for the family | `TaskBoard` (drag-and-drop) |

**Apprentice Mode** (ages 10–12): Same routes, but:
- Simplified language ("Move the chickens" vs "Relocate Broiler Tractor")
- Large touch targets for muddy-handed field use
- Task board shows only tasks assigned to them
- No access to animal medical/financial records

### HearthOS (`frontends/hearth-os/`)

**Users**: All family members (role-filtered views)

| Route | Purpose | Islands (JS) |
|-------|---------|--------------|
| `/batches` | Active batch dashboard (sourdough + kombucha) | `BatchStatusCards` (live pH) |
| `/batches/:id` | Batch detail: HACCP log, pH graph | `PHChart` (real-time sensor) |
| `/cultures` | Living culture registry + feeding schedule | `FeedingTimer` (countdown) |
| `/cultures/:id` | Culture lineage tree | `LineageTree` (interactive graph) |
| `/haccp` | HACCP compliance log (printable) | None (static SSR, print-optimized) |

**Helper Mode** (ages 4–7): 
- Read-only access to recipe displays and timers
- Large icons with color coding (green = safe pH, red = needs attention)
- No data entry capabilities

### EdgePortal (`frontends/edge-portal/`)

**Users**: CSA Customers  
**Deployment**: External host (Deno Deploy / Vercel / VPS)

| Route | Purpose | Islands (JS) |
|-------|---------|--------------|
| `/` | Available inventory for the week | None (static SSR) |
| `/subscribe` | CSA signup + tier selection | `SubscriptionForm` |
| `/orders` | Order history + upcoming pickup | None (static SSR) |
| `/orders/:id` | Order detail + pickup location map | `PickupMap` (embedded map) |
| `/bakery` | Sourdough ordering (standalone) | `BakeryOrderForm` |

### ApiaryOS (`frontends/apiary-os/`)

**Users**: Beekeepers, Steward
**Port**: `8001`

| Route | Purpose | Islands (JS) |
|-------|---------|--------------|
| `/hives` | Hive overview with last inspection summaries | `CreateHiveForm` (Modal), `HiveDetailPanel` (Sidebar) |
| `/hives/:id` | Hive detail: inspection log, weight graph | `WeightChart` |

**Beekeeper Mode**: 
- Focused purely on apiary data, inspections, treatments, and honey harvests
- Streamlined slide-out panels and modals (via `@preact/signals` state management) for rapid data entry in the field


---

## Sync Architecture (EdgePortal ↔ Local)

```
Local FarmOS (K3s)                     External EdgePortal
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

## Shared Design System

All three apps share a design token package:

```
frontends/
  shared/
    tokens/
      colors.css        # Farm-appropriate palette (earth tones, greens)
      typography.css     # Inter font, size scale
      spacing.css        # 4px grid system
      components.css     # Shared button, card, form styles
```

### Design Principles

- **Outdoor-readable**: High contrast ratios, large text, no thin fonts
- **Glove-friendly**: Minimum 48px touch targets on FieldOps
- **Print-friendly**: HACCP logs render cleanly on standard paper
- **Connectivity-agnostic**: SSR means pages work even if JS fails to load
