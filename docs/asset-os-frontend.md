# AssetOS — Frontend Architecture

> Farm infrastructure management micro-frontend: equipment registry, compost tracking, and Home Assistant sensor integration.

**Port**: `8002`
**Directory**: `frontend/asset-os/`
**API Context**: `/api/assets` via Caddy gateway

---

## Core Technologies

- **Deno Fresh 2.x**: Server-side rendering with islands architecture
- **Preact**: Island components (not yet migrated to Arrow.js)
- **Tailwind CSS v4**: Utility-first styling via Vite

---

## Routes

| Route | File | Purpose | Islands |
|-------|------|---------|---------|
| `/` | `index.tsx` | Asset dashboard overview | — |
| `/equipment` | `equipment/index.tsx` | Equipment registry + maintenance logs | `EquipmentPanel`, `RegisterEquipmentForm` |
| `/compost` | `compost/index.tsx` | Compost batch tracking (temps, turns, phases) | `CompostPanel` |
| `/sensors` | `sensors/index.tsx` | Home Assistant sensor bridge | `SensorPanel` |

---

## Islands

| Island | Purpose |
|--------|---------|
| `EquipmentPanel.tsx` | Equipment list with maintenance log, move, and retire actions |
| `RegisterEquipmentForm.tsx` | Modal form for registering new equipment |
| `CompostPanel.tsx` | Compost batch lifecycle: start, record temp, turn, change phase, complete |
| `SensorPanel.tsx` | Browse Home Assistant sensors, view history, check connectivity |

> **Migration note**: These islands are still Preact-based. They should be converted to Arrow.js to match the project standard.

---

## Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `GATEWAY_URL` | `http://caddy:5050` | Backend gateway URL |
| `DENO_ENV` | `production` | Deno environment |
