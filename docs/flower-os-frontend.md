# FlowerOS — Frontend Architecture

> Cut flower farm management micro-frontend: flower bed succession planning, crop plans, seed inventory, post-harvest processing, and bouquet recipe design.

**Port**: `8004`
**Directory**: `frontend/flower-os/`
**API Context**: `/api/flora` via Caddy gateway

---

## Core Technologies

- **Deno Fresh 2.x**: Server-side rendering with islands architecture
- **Preact**: Island components (not yet migrated to Arrow.js)
- **Tailwind CSS v4**: Utility-first styling via Vite

---

## Routes

| Route | File | Purpose | Islands |
|-------|------|---------|---------|
| `/` | `index.tsx` | Dashboard overview | — |
| `/beds` | `beds/index.tsx` | Flower bed succession calendar | `BedManagementPanel` |
| `/plans` | `plans/index.tsx` | Crop plans and analytics | `CropPlanDashboard` |
| `/seeds` | `seeds/index.tsx` | Seed lot inventory | `SeedInventoryPanel` |
| `/batches` | `batches/index.tsx` | Post-harvest batch processing | `PostHarvestPanel` |
| `/recipes` | `recipes/index.tsx` | Bouquet recipe designer | `RecipeDesigner` |
| `/api/[name]` | `api/[name].tsx` | Server-side API proxy | — |

---

## Islands

| Island | Purpose |
|--------|---------|
| `BedManagementPanel.tsx` | Flower bed CRUD, succession planting schedule, transplant/harvest tracking |
| `CropPlanDashboard.tsx` | Annual crop plans with revenue projections, seeding schedules |
| `SeedInventoryPanel.tsx` | Seed lot management: inventory, germination rates, supplier tracking |
| `PostHarvestPanel.tsx` | Batch processing: grading, conditioning, cooler placement, quality holds |
| `RecipeDesigner.tsx` | Bouquet recipe creation: stem composition, pricing, production runs |

> **Migration note**: These islands are still Preact-based. They should be converted to Arrow.js to match the project standard.

---

## Domain Concepts

- **Successions**: Sequential plantings of the same variety to extend harvest windows
- **Post-Harvest**: Grading (Premium/Standard/Compost), conditioning, and cooler management
- **Recipes**: Bouquet compositions with stem counts, which drive production planning
- **Revenue channels**: Farmers Market, Wholesale, CSA, Wedding/Event

---

## Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `GATEWAY_URL` | `http://caddy:5050` | Backend gateway URL |
| `DENO_ENV` | `production` | Deno environment |
