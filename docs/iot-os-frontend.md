# IoT-OS — Frontend Architecture

> Sensor management micro-frontend: device registration, monitoring zones, compliance dashboards, and excursion tracking.

**Port**: `8003`
**Directory**: `frontend/iot-os/`
**API Context**: `/api/iot` via Caddy gateway

---

## Core Technologies

- **Deno Fresh 2.x**: Server-side rendering with islands architecture
- **Preact**: Island components (not yet migrated to Arrow.js)
- **Tailwind CSS v4**: Utility-first styling via Vite
- **SignalR**: Real-time sensor telemetry via `/hubs/sensors`

---

## Routes

| Route | File | Purpose | Islands |
|-------|------|---------|---------|
| `/` | `index.tsx` | Zone grid dashboard with live readings | `ZoneGridDashboard` |
| `/devices` | `devices.tsx` | Device registry and zone assignment | — |
| `/compliance` | `compliance.tsx` | Zone compliance gauges + reports | `ComplianceGauge` |
| `/excursions` | `excursions.tsx` | Active excursion list + history | `ExcursionList` |

---

## Islands

| Island | Purpose |
|--------|---------|
| `ZoneGridDashboard.tsx` | Grid of monitoring zones with live temperature, humidity, CO2 readings |
| `ComplianceGauge.tsx` | Visual compliance gauge showing zone health (green/amber/red) |
| `ExcursionList.tsx` | Active and historical threshold excursions with duration and severity |

> **Migration note**: These islands are still Preact-based. They should be converted to Arrow.js to match the project standard.

---

## Layout

- **`_app.tsx`**: HTML document wrapper
- **`_layout.tsx`**: Shared navigation sidebar and main content area

---

## Caddy Routing

IoT-OS is also accessible through the Caddy gateway at `/iot-os/*`:
```
handle /iot-os/* {
    reverse_proxy iot-os-ui:8000
}
```

---

## Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `GATEWAY_URL` | `http://caddy:5050` | Backend gateway URL |
| `DENO_ENV` | `production` | Deno environment |
