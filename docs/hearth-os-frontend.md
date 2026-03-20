# Hearth OS — Frontend Architecture & Guide

Hearth OS is the dedicated micro-frontend for farm processing, kitchen activities, indoor cultivation, and regulatory compliance. It is designed to be easily accessible from tablets in the kitchen or mushroom fruiting rooms.

## Core Technologies

Hearth OS is built upon a modern, zero-build-step (or near-zero) server-side rendered stack:

- **Deno Fresh 2.x**: Providing the core routing and rendering. All routes use the updated type-safe `define.page()` and `define.handlers()` API, ensuring strict type linkages between server data fetching and client presentation.
- **Preact & `@preact/signals`**: For highly reactive, lightweight interactive components ("Islands").
- **Tailwind CSS v4**: For utility-first styling, driven directly through Vite.
- **Zod**: For strict runtime validation of data payloads sent to the backend API.
- **SignalR (`@microsoft/signalr`)**: For establishing WebSocket connections to the API Gateway to receive live IoT telemetry.

---

## Domain Modules & Routing

### 1. Dashboard (`/`)
The main entry point provides a high-level overview of active farm operations occurring indoors.
- **Statistics Cards**: Quick glances at Active Batches, Cultures, Fruiting Blocks, and Compliance Tasks.
- **IoT Live Feed**: An island (`IoTLiveFeed.tsx`) that maintains a SignalR connection to the kitchen hub. It displays real-time telemetry (Temperature, Humidity, CO2) from sensors in the kitchen and mushroom rooms, immediately flagging Critical or Warning alerts.
- **Demand Signal Cards**: When Commerce publishes `ProductionRequested` events (e.g., "need 30 loaves by Saturday"), these appear as visible demand cards so the baker knows what to produce.
- **Connection Banner**: A `ConnectionBanner.tsx` island reads from the global `connectionStatus` signal and displays a persistent connection state indicator (green=live, amber=reconnecting, red=offline with last-data timestamp).

### 2. Sourdough & Fermentation (`/batches`, `/cultures`)
Tracks the lifecycle of fermented goods.
- **Batches**: Logs dough temperatures, ambient conditions (pulling from IoT sensors), and pH readings across different fermentation phases (Mixing → BulkFerment → Shaping → Proofing → Baking → Cooling → Complete).
- **Cultures**: Manages the feeding schedules of living sourdough starters (`FeedingTimer.tsx`), ensuring they are active before a bake day.
- **Culture Lineage**: A `CultureLineageTree.tsx` island on `/cultures/{id}` queries the ArangoDB graph for `descended_from` edges and renders a visual tree of culture splits. Nodes display health status:
  - 🟢 Thriving (green)
  - 🟡 NeedsFeed (amber)
  - ⚪ Dormant (gray)
  - ~~Retired~~ (strikethrough)

### 3. Kombucha (`/kombucha`)
A specialized domain for kombucha batches with a distinct lifecycle (Primary → Secondary → Bottled → Complete) and unique safety requirements.
- **`/kombucha`**: Lists all active kombucha batches with phase filter tabs (All/Primary/Secondary/Bottled/Complete). Uses Fresh Partials for filter switching without full page reload.
- **`KombuchaPHChart.tsx`**: An island rendering a time-series pH chart with:
  - A horizontal reference line at pH 4.2 (the safety threshold)
  - Color-coded zones: green (<4.2), amber (approaching 4.2), red (stuck above 4.2 after 7 days → discard required per `KombuchaRules.validatePH`)
- **ABV Tracking**: A prominent alcohol-by-volume display with a red warning threshold at 0.5% ABV. This is a federal TTB (Alcohol and Tobacco Tax and Trade Bureau) requirement for commercial kombucha — not optional.
- **Secondary Flavoring**: A form to log `Flavoring` records (ingredient, quantity) during the F2 (second fermentation) phase, using the existing `HearthAPI.addKombuchaFlavoring` endpoint.

### 4. Mushroom Cultivation (`/mushrooms`)
A specialized domain for managing fungi blocks (e.g., Lion's Mane, Blue Oyster).
- **`/mushrooms`**: Lists all active fruiting blocks and their current biological phase (Incubation, Pinning, Fruiting).
- **`/mushrooms/new`**: An inoculation form (`NewMushroomBatchForm.tsx`) validating Species, Batch Code, and Substrate Type against `MushroomBatchSchema`.
- **`/mushrooms/[id]`**: A detail view housing the `MushroomActionPanel.tsx`. This interactive sidebar allows cultivators to quickly:
  - Record periodic Temperature and Humidity.
  - Advance the block's phase.
  - Log harvest yields (flush records in lbs) and completion/contamination events.
  - Uses Fresh Partials to update the phase badge and reading logs without a full page refresh.

### 5. Regulatory Compliance (`/compliance`)
Built to satisfy commercial kitchen requirements (such as those of the Georgia Department of Agriculture).
- **Sanitation Logging (`SanitationLog.tsx`)**: An island dedicated to logging daily and post-production cleaning. Validation requires specifying the Surface Type, Cleaning Method, Sanitizer Type, and critically, the **Sanitizer PPM** (Parts Per Million) to prove food-safe concentrations.
- **Inspector-Ready Printing**: The SSR route (`routes/compliance/index.tsx`) includes strict `@media print` CSS. When the "Print for Inspector" button is clicked, sidebars, interactive buttons, and web-specific styling are stripped away, yielding a perfectly formatted, 8.5×11 black-and-white table ready for immediate physical filing or review by a health inspector.
- **HACCP Plan Template (`/compliance/haccp-plan`)**: Renders the seven HACCP principles as a structured, printable document:
  1. **Hazard Analysis** — identified hazards per product line
  2. **CCP Identification** — Critical Control Points per product (e.g., "Sourdough Internal Bake Temp", "Kombucha pH by Day 7")
  3. **Critical Limits** — measurable boundaries (≥190°F, ≤4.2 pH)
  4. **Monitoring Procedures** — who measures, how often, with what instrument
  5. **Corrective Actions** — what happens when a limit is exceeded
  6. **Verification Procedures** — periodic checks that the plan is followed
  7. **Record Keeping** — links to the digital logs maintained by HearthOS
- **Corrective Action Enforcement**: When a CCP reading has `WithinLimits: false`, the `HACCPReadingSchema` Zod refinement makes the `correctiveAction` field **required** (not optional). An inspector will flag missing corrective actions immediately.
- **Verification Schedule**: A simple task/reminder system for weekly log reviews and annual plan reviews.
- **Print Partials**: The HACCP print export endpoint uses `skipAppWrapper: true` and `skipInheritedLayouts: true` to return only the table content for printing.

---

## Architecture Patterns

### Error Handling

Hearth OS implements a unified error handling strategy using Fresh 2.x patterns:

- **`routes/_error.tsx`**: A single error template that replaces the legacy `_404.tsx`/`_500.tsx` split. Uses `HttpError` status codes to render context-appropriate messages (404 = resource not found, 503 = gateway unreachable, 500 = unexpected error).
- **`app.onError()`**: A global error handler registered in `main.ts` that catches all unhandled errors, logs them, and returns appropriate HTTP responses.
- **`HttpError` in route handlers**: When `farmos-client.ts` API calls fail, route handlers throw typed `HttpError` instances (`throw new HttpError(404, "Batch not found")`) instead of allowing white screens.
- **Offline Degradation**: When the Gateway is unreachable, a clear "Offline — last data from X minutes ago" banner is shown rather than a blank or error state. Kitchen environments have flaky WiFi; a dead API must not produce a white screen.

### Layout Architecture

Fresh 2.x file-system-based layout inheritance is used for clean separation:

- **`routes/_app.tsx`**: The outermost HTML document wrapper — `<html>`, `<head>` (fonts, meta), `<body>`. Contains no page chrome.
- **`routes/_layout.tsx`**: The root page layout wrapping all routes with shared chrome: sidebar `NavBar`, `ToastProvider`, `ConnectionBanner`, and the `<main>` content area.
- **`routes/compliance/_layout.tsx`**: A compliance-specific layout that strips the sidebar and uses print-optimized formatting. Uses `LayoutConfig` with `skipInheritedLayouts: true` for compliance print routes so they render clean for inspector output.

### Island-Based Client Hydration
By leveraging Deno Fresh, maximum performance is achieved by serving pure HTML/CSS from the server for static content (like forms and tables). JavaScript is only shipped to the client for specific `islands/` components (like the `IoTLiveFeed` or the `SanitationLog` form).

### State Management
Instead of complex Redux stores or heavy Context APIs, Hearth OS utilizes **Preact Signals**. 
- Form states (`useSignal("")`) are co-located in the forms.
- Global, lightweight states (such as application-wide Toast notifications) are managed via exported standalone signals in `utils/toastState.ts`. The `ToastProvider.tsx` island listens to this signal and dynamically renders success/error banners globally without causing main tree re-renders.
- **Connection state**: A global `connectionStatus` signal in `utils/connectionState.ts` tracks WebSocket health (`"connected" | "reconnecting" | "offline"`). Every telemetry-dependent island reads this to adjust its display.

### Offline Resilience & Stale Data

SignalR provides live telemetry, but WiFi drops are guaranteed in kitchen and mushroom room environments. HearthOS handles this gracefully:

- **Connection state signal**: `connectionStatus: signal<"connected" | "reconnecting" | "offline">` in `utils/connectionState.ts`, read by all telemetry-dependent islands.
- **Visual indicator**: `ConnectionBanner.tsx` renders a persistent status indicator:
  - 🟢 Green dot + "Live" = connected
  - 🟡 Amber dot + "Reconnecting…" = SignalR automatic reconnect in progress
  - 🔴 Red dot + "Offline — last data X min ago" = connection failed
- **Stale data display**: When disconnected, `IoTLiveFeed.tsx` continues displaying the last known readings with a "Last updated X minutes ago" label instead of blanking or showing an error.
- **Automatic reconnection**: SignalR's built-in `withAutomaticReconnect([0, 1000, 2000, 5000, 10000])` is already configured. The UI reflects state transitions (`onreconnecting`, `onreconnected`, `onclose`).
- **Fallback polling**: If the WebSocket connection fails entirely, the feed falls back to periodic `GET /api/hearth/telemetry/latest` polling at a configurable interval (default 30s via `FRESH_PUBLIC_POLL_INTERVAL_MS`).

### Fresh Partials (SPA-like Navigation)

Fresh 2.x `<Partial>` components are used for efficient in-page updates:

- **Batch list filter tabs** (`/batches`): Switching between All/Sourdough/Kombucha/Mushroom filters updates only the list content, not the entire page layout.
- **Mushroom detail action panel** (`/mushrooms/[id]`): After recording a temperature or advancing a phase, the phase badge and reading logs update via partial without full page refresh.
- **HACCP print export**: Uses `skipAppWrapper: true` and `skipInheritedLayouts: true` to return only the compliance table content for printing.

### Batch-to-Commerce Handoff

When a batch completes, it becomes available inventory in the Commerce context:

- **"Published to Inventory"** status indicator on completed batches, showing whether the `BatchCompleted` → Commerce `InventoryProjection` event has been processed.
- **Manual "Mark Available for Sale"** action for human gating (some batches are personal use, not sale).
- **Demand signal display**: When Commerce publishes `ProductionRequested` events, HearthOS renders these as demand cards on the dashboard.

### Type-Safe API Client
All communication with the `FarmOS.*.API` backend services occurs through `utils/farmos-client.ts`. 
- Functions (e.g., `MushroomAPI.startBatch`) strongly type their arguments based on Zod inference (`z.infer<typeof MushroomBatchSchema>`).
- The client encapsulates the HTTP boundary, meaning frontend UI components never directly construct `fetch` requests or manually serialize JSON.
- On API failure, the client throws `HttpError` instances for structured error handling in route handlers.

---

## Responsive & Touch-Optimized Design

HearthOS targets tablet-first usage in kitchen and cultivation environments:

| Spec | Value | Rationale |
|------|-------|-----------|
| **Minimum touch target** | 48×48px | All buttons, form fields, cards — WCAG/Google recommendation |
| **Primary breakpoint** | 1024px (tablet landscape) | Design target; phone screens are secondary |
| **Body text minimum** | 16px | Kitchen readability (flour-covered hands, dim lighting, distance) |
| **Color contrast** | WCAG AA (4.5:1) minimum | All text; phase badges use background color + high-contrast text |
| **Destructive action spacing** | 24px+ gap from common actions | Prevent accidental taps with wet/floured hands |

Phase badges use both background color AND high-contrast text labels (not color alone) for visibility through flour-dusted screen glare.

---

## Notification & Alerting Strategy

Beyond the existing `ToastProvider.tsx` (auto-dismissing after 4s), HearthOS implements a tiered alerting system:

### Critical Alerts (Non-Dismissible)
Cannot be closed without explicit acknowledgment. Used for:
- Batch contamination detected
- pH exceeded threshold + time limit (discard required)
- CCP reading out of limits (triggers corrective action prompt)

### Persistent Warnings
Remain visible until resolved. Used for:
- Culture health dropped to `NeedsFeed` — amber alert on dashboard
- Timer expiring (sourdough proof, mushroom colonization max days)

### Informational Toasts
Auto-dismiss after 4 seconds (current behavior). Used for:
- Successful form submissions
- Batch phase advances
- Culture feedings logged

### Audible Alerts (Optional)
Browser audio notification for critical safety alerts (contamination, CCP failures). Configurable, off by default. Respects `Notification` API permissions.

---

## Environment Variables

```bash
# Server-side only (used in route handlers and farmos-client.ts)
FARMOS_GATEWAY_URL=http://localhost:5050

# Client-side (inlined at build time via FRESH_PUBLIC_ prefix)
FRESH_PUBLIC_SIGNALR_HUB=http://localhost:5050/hubs/kitchen
FRESH_PUBLIC_POLL_INTERVAL_MS=30000
```

> **Note**: `FRESH_PUBLIC_*` variables are inlined during `deno task build`. They cannot be read at runtime in islands — they are compile-time constants.

---

## Testing Strategy

### Route Handler Tests
Fresh provides a `createHandler` utility for testing route handlers and middleware in isolation:
```typescript
const handler = await createHandler(manifest);
const resp = await handler(new Request("http://localhost/batches"));
assertEquals(resp.status, 200);
```

### Zod Schema Tests
Unit tests covering edge cases for all validation schemas:
- Empty strings, negative pH values, future dates
- **HACCP corrective action enforcement**: `withinLimits: false` without `correctiveAction` must fail validation

### Island Component Tests
Preact Testing Library for critical interaction flows:
- Recording a CCP reading
- Advancing a batch phase
- Kombucha pH form submission

### Print Layout Regression
Visual regression test that renders the HACCP compliance log and compares against a known-good snapshot.

---

## Deployment & Build

Hearth OS is packaged as a Docker container (`farmos-hearth-os-ui`) alongside the other FarmOS micro-frontends.
- Commands like `deno task build` leverage Vite to compile Tailwind and process islands.
- Standard execution utilizes `deno serve -A _fresh/server.js`, hosting the frontend independently on port `8000`.
- Environment variables are loaded from `.env` files using the `--env-file` flag in production, or set directly in `docker-compose.yml`.
