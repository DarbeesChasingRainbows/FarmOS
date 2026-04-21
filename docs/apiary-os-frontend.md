# ApiaryOS — Frontend Architecture

> Beekeeping management micro-frontend: hive registry, inspections, queen tracking, harvests, financials, and seasonal calendar.

**Port**: `8001`
**Directory**: `frontend/apiary-os/`
**API Context**: `/api/apiary` via Caddy gateway

---

## Core Technologies

- **Deno Fresh 2.x**: Server-side rendering with islands architecture
- **Arrow.js (`@arrow-js/core`)**: Reactive islands (partially migrated — some legacy Preact islands remain)
- **Tailwind CSS v4**: Utility-first styling via Vite
- **Preact**: Used as Fresh island shell (`useRef` + `useEffect` for Arrow mounting)

---

## Routes

| Route | File | Purpose | Islands |
|-------|------|---------|---------|
| `/` | `index.tsx` | Dashboard overview | `ArrowDashboard` |
| `/hives` | `hives/index.tsx` | Hive registry + detail sidebar | `ArrowHiveManager` |
| `/apiaries` | `apiaries/index.tsx` | Apiary yard management | `ArrowApiaryManager` |
| `/calendar` | `calendar/index.tsx` | Seasonal beekeeping tasks | `ArrowTaskCalendar` |
| `/financials` | `financials/index.tsx` | Revenue vs. expenses | `ArrowFinancialDashboard` |
| `/reports` | `reports/index.tsx` | Mite trends, yield, survival | `ArrowReportsDashboard` |
| `/api/[name]` | `api/[name].tsx` | Server-side API proxy | — |

---

## Islands

### Arrow.js Islands (Current)
| Island | Purpose |
|--------|---------|
| `ArrowDashboard.tsx` | Main dashboard with hive summary cards |
| `ArrowHiveManager.tsx` | Hive list + slide-out detail panel for inspections, treatments, harvests |
| `ArrowApiaryManager.tsx` | Apiary yard CRUD + hive assignment |
| `ArrowTaskCalendar.tsx` | Monthly/yearly seasonal task calendar |
| `ArrowFinancialDashboard.tsx` | Expenses, revenue, P&L breakdown |
| `ArrowReportsDashboard.tsx` | Mite count trends, yield reports, survival statistics |
| `ArrowNavBar.tsx` | Sidebar navigation |

### Legacy Preact Islands (Pending Migration)
| Island | Status |
|--------|--------|
| `HiveDetailPanel.tsx` | Superseded by `ArrowHiveManager` sidebar |
| `FinancialDashboard.tsx` | Superseded by `ArrowFinancialDashboard` |
| `ReportsDashboard.tsx` | Superseded by `ArrowReportsDashboard` |
| `TaskCalendar.tsx` | Superseded by `ArrowTaskCalendar` |
| `ToastProvider.tsx` | Shared toast notification provider |

> **Cleanup note**: The legacy Preact islands can be deleted once routes are verified to use only Arrow equivalents.

---

## API Client

Located in `utils/` — communicates with backend through the server-side API proxy at `/api/[name]`, which forwards to `http://caddy:5050/api/apiary/*`.

---

## Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `FARMOS_URL` | `http://caddy:5050` | Backend gateway URL |
| `PORT` | `8001` | Listen port |
| `DENO_ENV` | `production` | Deno environment |
