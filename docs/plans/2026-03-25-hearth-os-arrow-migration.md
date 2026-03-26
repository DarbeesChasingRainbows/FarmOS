# Hearth-OS Arrow.js Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate all hearth-os pages to Arrow.js reactive UI with bento-grid layouts following Laws of UX, matching the apiary-os migration pattern.

**Architecture:** Preact Shell + Arrow Core pattern — Fresh 2.2.0 `.tsx` islands use `useRef` + `useEffect` to mount Arrow.js reactive DOM via `html` tagged templates and `reactive()` state. All data loading moves client-side with `Promise.allSettled()` for resilient parallel fetching.

**Tech Stack:** Deno Fresh 2.2.0, Arrow.js 1.0.0-alpha.9, Tailwind CSS v4, Zod validation, Preact 10.x

**Excluded:** Mushroom pages (will become their own project)

---

## Reference: Arrow.js Island Pattern

Every island follows this exact structure:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";

export default function IslandName() {
  const containerRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";
    const state = reactive({ /* ... */ });
    // ... Arrow logic ...
    const template = html`...`;
    template(containerRef.current);
  }, []);
  return <div ref={containerRef}></div>;
}
```

**Critical Arrow.js Rules:**
1. Never nest backticks — use string concatenation or helper functions returning `html` templates
2. Reactive reads must be wrapped: `${() => state.field}` not `${state.field}`
3. Events: `@click="${() => fn()}"`, `@input="${(e: Event) => ...}"`
4. Empty template: `html\`\`` (never return `null`)
5. Lazy API imports: `const { API } = await import("../utils/farmos-client.ts")`

## Reference: Color Palette

- **Primary accent:** orange-500/orange-600 (hearth = fire)
- **Neutral:** stone-50 through stone-900
- **Status:** emerald (success), amber (warning), red (critical), sky (info)
- **Nav bg:** stone-900, active highlight: orange-500/20

---

### Task 1: Copy Shared Components from Apiary-OS

**Files:**
- Create: `frontend/hearth-os/components/ArrowKPICard.ts`
- Create: `frontend/hearth-os/components/ArrowEmptyState.ts`
- Create: `frontend/hearth-os/components/ArrowConfirmDialog.ts`

**Step 1:** Copy `ArrowKPICard.ts` from `frontend/apiary-os/components/ArrowKPICard.ts`, adding `"orange"` color variant:

```ts
import { html } from "@arrow-js/core";

export interface KPICardProps {
  label: string;
  value: string | (() => string);
  trend?: string | (() => string);
  trendDirection?: "up" | "down" | "flat" | (() => "up" | "down" | "flat");
  icon: string;
  color?: "orange" | "amber" | "emerald" | "red" | "violet" | "sky" | "stone";
}

const colorStyles: Record<string, { bg: string; iconBg: string; trend: string }> = {
  orange: { bg: "bg-white", iconBg: "bg-orange-50 text-orange-600", trend: "text-orange-600" },
  amber: { bg: "bg-white", iconBg: "bg-amber-50 text-amber-600", trend: "text-amber-600" },
  emerald: { bg: "bg-white", iconBg: "bg-emerald-50 text-emerald-600", trend: "text-emerald-600" },
  red: { bg: "bg-white", iconBg: "bg-red-50 text-red-600", trend: "text-red-600" },
  violet: { bg: "bg-white", iconBg: "bg-violet-50 text-violet-600", trend: "text-violet-600" },
  sky: { bg: "bg-white", iconBg: "bg-sky-50 text-sky-600", trend: "text-sky-600" },
  stone: { bg: "bg-white", iconBg: "bg-stone-100 text-stone-600", trend: "text-stone-600" },
};

export function ArrowKPICard(props: KPICardProps) {
  const c = colorStyles[props.color || "orange"];
  const trendArrow = () => {
    const dir = typeof props.trendDirection === "function" ? props.trendDirection() : props.trendDirection;
    if (dir === "up") return "\u2197";
    if (dir === "down") return "\u2198";
    return "\u2192";
  };
  const trendColor = () => {
    const dir = typeof props.trendDirection === "function" ? props.trendDirection() : props.trendDirection;
    if (dir === "up") return "text-emerald-600 bg-emerald-50";
    if (dir === "down") return "text-red-600 bg-red-50";
    return "text-stone-500 bg-stone-50";
  };
  return html`
    <div class="${c.bg} rounded-2xl border border-stone-200/60 shadow-sm p-5 hover:shadow-md transition-shadow">
      <div class="flex items-center justify-between mb-3">
        <span class="w-10 h-10 rounded-xl ${c.iconBg} flex items-center justify-center text-lg">${props.icon}</span>
        ${() => {
          const trend = typeof props.trend === "function" ? props.trend() : props.trend;
          if (!trend) return html``;
          return html`<span class="${() => trendColor()} text-xs font-bold px-2 py-0.5 rounded-full flex items-center gap-0.5">${() => trendArrow()} ${() => typeof props.trend === "function" ? props.trend() : props.trend}</span>`;
        }}
      </div>
      <p class="text-2xl font-extrabold text-stone-800 tracking-tight">${props.value}</p>
      <p class="text-xs text-stone-400 mt-1 uppercase tracking-wider font-medium">${props.label}</p>
    </div>
  `;
}
```

**Step 2:** Copy `ArrowEmptyState.ts` from apiary-os (identical):

```ts
import { html } from "@arrow-js/core";

export interface ArrowEmptyStateProps {
  icon?: string;
  title: string;
  message: string;
}

export function ArrowEmptyState(props: ArrowEmptyStateProps) {
  return html`
    <div class="bg-stone-50 border border-stone-200 rounded-2xl p-12 text-center">
      ${props.icon ? html`<span class="text-4xl block mb-3">${props.icon}</span>` : html``}
      <p class="text-lg font-medium text-stone-600 mb-2">${props.title}</p>
      <p class="text-sm text-stone-500 max-w-md mx-auto">${props.message}</p>
    </div>
  `;
}
```

**Step 3:** Create `ArrowConfirmDialog.ts` component (the existing `ArrowConfirmDialog.tsx` island in hearth-os is a Preact island — we need the pure Arrow component version):

```ts
import { html } from "@arrow-js/core";

export interface ArrowConfirmDialogProps {
  isOpen: () => boolean;
  title: string | (() => string);
  message: string | (() => string);
  onConfirm: () => void;
  onCancel: () => void;
  confirmLabel?: string;
  danger?: boolean;
}

export function ArrowConfirmDialog(props: ArrowConfirmDialogProps) {
  const confirmClass = props.danger
    ? "bg-red-600 text-white hover:bg-red-700"
    : "bg-orange-600 text-white hover:bg-orange-700";
  return html`
    <div class="${() => props.isOpen()
      ? "fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50"
      : "hidden"}">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-sm mx-4 p-6">
        <h3 class="text-lg font-bold text-stone-800 mb-2">${props.title}</h3>
        <p class="text-sm text-stone-600 mb-6">${props.message}</p>
        <div class="flex justify-end gap-3">
          <button type="button" @click="${props.onCancel}" class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition">Cancel</button>
          <button type="button" @click="${props.onConfirm}" class="px-4 py-2 rounded-lg font-semibold ${confirmClass} transition shadow-sm">${props.confirmLabel || "Confirm"}</button>
        </div>
      </div>
    </div>
  `;
}
```

**Step 4: Commit**

```bash
git add frontend/hearth-os/components/ArrowKPICard.ts frontend/hearth-os/components/ArrowEmptyState.ts frontend/hearth-os/components/ArrowConfirmDialog.ts
git commit -m "feat(hearth-os): add shared Arrow components (KPICard, EmptyState, ConfirmDialog)"
```

---

### Task 2: ArrowNavBar + Layout Migration

**Files:**
- Create: `frontend/hearth-os/islands/ArrowNavBar.tsx`
- Modify: `frontend/hearth-os/routes/_layout.tsx`
- Modify: `frontend/hearth-os/routes/_app.tsx`

**Step 1:** Create `ArrowNavBar.tsx` with grouped navigation sections:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { html } from "@arrow-js/core";

interface NavItem {
  href: string;
  label: string;
  icon: string;
  group: "fermentation" | "production" | "operations";
}

const navItems: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "\uD83C\uDFE0", group: "fermentation" },
  { href: "/batches", label: "Batches", icon: "\uD83C\uDF5E", group: "fermentation" },
  { href: "/cultures", label: "Cultures", icon: "\uD83E\uDDEB", group: "fermentation" },
  { href: "/kombucha", label: "Kombucha", icon: "\uD83E\uDED6", group: "fermentation" },
  { href: "/freeze-dryer", label: "Freeze Dryer", icon: "\u2744\uFE0F", group: "production" },
  { href: "/iot", label: "IoT Devices", icon: "\uD83D\uDCE1", group: "operations" },
  { href: "/compliance", label: "Compliance", icon: "\uD83D\uDCCB", group: "operations" },
];

export default function ArrowNavBar({ currentPath }: { currentPath: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const isActive = (href: string) => {
      if (href === "/") return currentPath === "/";
      return currentPath.startsWith(href);
    };

    const navLink = (item: NavItem) => html`
      <li>
        <a href="${item.href}"
          class="${isActive(item.href)
            ? "bg-orange-600/20 text-orange-300 shadow-sm"
            : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"} flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150">
          <span class="text-lg">${item.icon}</span>
          <span class="hidden lg:inline">${item.label}</span>
        </a>
      </li>
    `;

    const fermentationItems = navItems.filter((n) => n.group === "fermentation");
    const productionItems = navItems.filter((n) => n.group === "production");
    const operationsItems = navItems.filter((n) => n.group === "operations");

    const groupLabel = (label: string) => html`
      <p class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block">${label}</p>
    `;

    const template = html`
      <nav class="w-16 lg:w-60 min-h-screen bg-stone-900 text-stone-100 flex flex-col border-r border-stone-800 shrink-0 transition-all duration-200">
        <div class="px-3 lg:px-6 py-5 border-b border-stone-800">
          <h1 class="text-xl font-bold tracking-tight text-orange-400 hidden lg:flex items-center gap-2">
            <span>\uD83D\uDD25</span> Hearth OS
          </h1>
          <span class="text-xl lg:hidden block text-center">\uD83D\uDD25</span>
          <p class="text-xs text-stone-500 mt-1 uppercase tracking-widest hidden lg:block">FarmOS Kitchen</p>
        </div>

        <div class="flex-1 py-4 px-2 lg:px-3">
          ${groupLabel("Fermentation")}
          <ul class="space-y-1 mb-6">${fermentationItems.map((item) => navLink(item))}</ul>
          ${groupLabel("Production")}
          <ul class="space-y-1 mb-6">${productionItems.map((item) => navLink(item))}</ul>
          ${groupLabel("Operations")}
          <ul class="space-y-1">${operationsItems.map((item) => navLink(item))}</ul>
        </div>

        <div class="px-2 lg:px-3 pb-4 border-t border-stone-800 pt-3">
          <a href="/settings"
            class="${isActive("/settings")
              ? "bg-orange-600/20 text-orange-300 shadow-sm"
              : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"} flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150">
            <span class="text-lg">\u2699\uFE0F</span>
            <span class="hidden lg:inline">Settings</span>
          </a>
        </div>

        <div class="px-3 lg:px-6 py-4 border-t border-stone-800">
          <p class="text-xs text-stone-600 hidden lg:block">Sovereign \u00B7 Offline-First</p>
          <span class="text-xs text-stone-600 lg:hidden block text-center">\u25CF</span>
        </div>
      </nav>
    `;

    template(containerRef.current);
  }, [currentPath]);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Update `_layout.tsx` — replace Preact NavBar with ArrowNavBar:

```tsx
import { define } from "../utils.ts";
import ArrowNavBar from "../islands/ArrowNavBar.tsx";
import ArrowToastProvider from "../islands/ArrowToastProvider.tsx";
import ConnectionBanner from "../islands/ConnectionBanner.tsx";

export default define.page(function Layout({ Component, url }) {
  return (
    <div class="flex min-h-screen bg-stone-50 text-stone-900 font-sans selection:bg-orange-100 selection:text-orange-900">
      <ArrowNavBar currentPath={url.pathname} />
      <div class="flex-1 flex flex-col overflow-y-auto">
        <ConnectionBanner />
        <main class="flex-1">
          <Component />
        </main>
      </div>
      <ArrowToastProvider />
    </div>
  );
});
```

**Step 3:** Add `fadeIn` keyframe to `_app.tsx` styles (add after existing `scaleIn` keyframe):

Add this CSS to the existing `<style>` block:
```css
@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
```

**Step 4: Commit**

```bash
git add frontend/hearth-os/islands/ArrowNavBar.tsx frontend/hearth-os/routes/_layout.tsx frontend/hearth-os/routes/_app.tsx
git commit -m "feat(hearth-os): add ArrowNavBar sidebar with grouped navigation"
```

---

### Task 3: Dashboard Bento Grid

**Files:**
- Create: `frontend/hearth-os/islands/ArrowHearthDashboard.tsx`
- Modify: `frontend/hearth-os/routes/index.tsx`

**Step 1:** Create `ArrowHearthDashboard.tsx`:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import type { ActiveFermentationMonitorDto } from "../utils/farmos-client.ts";

export default function ArrowHearthDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      batches: [] as ActiveFermentationMonitorDto[],
      loading: true,
      error: null as string | null,
    });

    const loadData = async () => {
      try {
        const { FermentationAPI } = await import("../utils/farmos-client.ts");
        const [batchResult] = await Promise.allSettled([
          FermentationAPI.getActiveMonitoring(),
        ]);
        state.batches = batchResult.status === "fulfilled" ? (batchResult.value ?? []) : [];
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load dashboard";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const activeCount = () => state.batches.filter((b) => !b.isSafe === false).length;
    const unsafeCount = () => state.batches.filter((b) => !b.isSafe).length;
    const avgPH = () => {
      const withPH = state.batches.filter((b) => b.currentPH !== null);
      if (withPH.length === 0) return "\u2014";
      const avg = withPH.reduce((s, b) => s + (b.currentPH ?? 0), 0) / withPH.length;
      return avg.toFixed(1);
    };

    const phColor = (ph: number | null) => {
      if (ph === null) return "text-stone-400";
      if (ph <= 3.5) return "text-emerald-600";
      if (ph <= 4.2) return "text-amber-600";
      return "text-red-600";
    };

    const batchRow = (batch: ActiveFermentationMonitorDto) => {
      const icon = batch.productType === "Sourdough" ? "\uD83C\uDF5E" : "\uD83E\uDED6";
      return html`
        <a href="/batches" class="flex items-center gap-3 py-3 px-2 rounded-lg hover:bg-stone-50 transition border-b border-stone-50 last:border-0">
          <span class="text-lg">${icon}</span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-stone-800">${batch.batchCode}</p>
            <p class="text-xs text-stone-400">${batch.phase} \u00B7 ${batch.productType}</p>
          </div>
          <div class="text-right">
            <p class="text-sm font-bold ${phColor(batch.currentPH)}">${batch.currentPH !== null ? "pH " + batch.currentPH.toFixed(1) : "\u2014"}</p>
            <p class="text-xs ${batch.isSafe ? "text-emerald-500" : "text-red-500"} font-medium">${batch.isSafe ? "Safe" : "Alert"}</p>
          </div>
        </a>
      `;
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Dashboard</h1>
          <p class="text-stone-500 mt-1">Fermentation overview and daily check-in.</p>
        </header>

        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-20"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : html`
            <div>
              ${() => state.error ? html`<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">${state.error}</div>` : html``}

              <!-- KPI Row -->
              <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                ${ArrowKPICard({ label: "Active Batches", value: () => String(state.batches.length), icon: "\uD83C\uDF5E", color: "orange" })}
                ${ArrowKPICard({ label: "Need Attention", value: () => String(unsafeCount()), icon: "\u26A0\uFE0F", color: "red" })}
                ${ArrowKPICard({ label: "Avg pH", value: avgPH, icon: "\u2697\uFE0F", color: "emerald" })}
                ${ArrowKPICard({ label: "Safe Batches", value: () => String(activeCount()), icon: "\u2705", color: "sky" })}
              </div>

              <!-- Bento Grid -->
              <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
                <!-- Active Fermentations -->
                <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                  <div class="flex items-center justify-between mb-4">
                    <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider">Active Fermentations</h2>
                    <a href="/batches" class="text-xs text-orange-600 font-semibold hover:text-orange-700 transition">View All</a>
                  </div>
                  ${() => state.batches.length === 0
                    ? html`<p class="text-sm text-stone-400 py-4">No active batches.</p>`
                    : html`<div>${() => state.batches.slice(0, 6).map((b) => batchRow(b))}</div>`}
                </div>

                <!-- Quick Actions -->
                <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                  <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Quick Actions</h2>
                  <div class="grid grid-cols-2 gap-3">
                    <a href="/batches" class="px-4 py-3 bg-orange-50 text-orange-700 rounded-xl text-sm font-semibold hover:bg-orange-100 transition border border-orange-100 flex items-center gap-2">
                      <span>\uD83C\uDF5E</span> Batches
                    </a>
                    <a href="/cultures" class="px-4 py-3 bg-violet-50 text-violet-700 rounded-xl text-sm font-semibold hover:bg-violet-100 transition border border-violet-100 flex items-center gap-2">
                      <span>\uD83E\uDDEB</span> Cultures
                    </a>
                    <a href="/kombucha" class="px-4 py-3 bg-teal-50 text-teal-700 rounded-xl text-sm font-semibold hover:bg-teal-100 transition border border-teal-100 flex items-center gap-2">
                      <span>\uD83E\uDED6</span> Kombucha
                    </a>
                    <a href="/compliance" class="px-4 py-3 bg-sky-50 text-sky-700 rounded-xl text-sm font-semibold hover:bg-sky-100 transition border border-sky-100 flex items-center gap-2">
                      <span>\uD83D\uDCCB</span> Compliance
                    </a>
                    <a href="/iot" class="px-4 py-3 bg-emerald-50 text-emerald-700 rounded-xl text-sm font-semibold hover:bg-emerald-100 transition border border-emerald-100 flex items-center gap-2">
                      <span>\uD83D\uDCE1</span> IoT
                    </a>
                    <a href="/freeze-dryer" class="px-4 py-3 bg-sky-50 text-sky-700 rounded-xl text-sm font-semibold hover:bg-sky-100 transition border border-sky-100 flex items-center gap-2">
                      <span>\u2744\uFE0F</span> Freeze Dryer
                    </a>
                  </div>
                </div>
              </div>
            </div>
          `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Update `routes/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import ArrowHearthDashboard from "../islands/ArrowHearthDashboard.tsx";

export default define.page(function Dashboard() {
  return (
    <div>
      <Head>
        <title>Dashboard — Hearth OS</title>
      </Head>
      <ArrowHearthDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/islands/ArrowHearthDashboard.tsx frontend/hearth-os/routes/index.tsx
git commit -m "feat(hearth-os): replace dashboard with Arrow bento grid"
```

---

### Task 4: Batches + Cultures Route Bento Wrappers

**Files:**
- Modify: `frontend/hearth-os/routes/batches/index.tsx`
- Modify: `frontend/hearth-os/routes/cultures/index.tsx`

These pages already use Arrow islands. We just clean up the route shells to use consistent bento styling and remove server-side data fetching (let the islands handle it client-side).

**Step 1:** Update `routes/batches/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowBatchDetailPanel from "../../islands/ArrowBatchDetailPanel.tsx";
import ArrowNewBatchForm from "../../islands/ArrowNewBatchForm.tsx";

export default define.page(function BatchesList() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Batches — Hearth OS</title>
      </Head>

      <header class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Fermentation Batches
          </h1>
          <p class="text-stone-500 mt-1">
            Click any batch to view details, record pH, or advance the phase.
          </p>
        </div>
        <ArrowNewBatchForm />
      </header>

      <ArrowBatchDetailPanel />
    </div>
  );
});
```

Note: Remove the `async` and server-side `FermentationAPI.getActiveMonitoring()` call. The `ArrowBatchDetailPanel` already fetches data client-side. If `ArrowBatchDetailPanel` currently expects `initialBatches` props, it should fall back to fetching its own data when no props are passed. Check the island — if it requires `initialBatches`, keep the server fetch for now.

**Step 2:** Update `routes/cultures/index.tsx` — remove Preact Tooltip, use consistent styling:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowCultureDetailPanel from "../../islands/ArrowCultureDetailPanel.tsx";
import ArrowCreateCultureForm from "../../islands/ArrowCreateCultureForm.tsx";

export default define.page(function CulturesPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Cultures — Hearth OS</title>
      </Head>

      <header class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Living Cultures
          </h1>
          <p class="text-stone-500 mt-1">
            Click any culture to view details, feed, or split it.
          </p>
        </div>
        <ArrowCreateCultureForm />
      </header>

      <ArrowCultureDetailPanel />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/routes/batches/index.tsx frontend/hearth-os/routes/cultures/index.tsx
git commit -m "feat(hearth-os): apply bento styling to batches and cultures routes"
```

---

### Task 5: Kombucha Dashboard (Full Implementation)

**Files:**
- Create: `frontend/hearth-os/islands/ArrowKombuchaDashboard.tsx`
- Modify: `frontend/hearth-os/routes/kombucha/index.tsx`

**Step 1:** Create `ArrowKombuchaDashboard.tsx`:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import type { ActiveFermentationMonitorDto } from "../utils/farmos-client.ts";

const PHASES = ["All", "Primary", "Secondary", "Bottled", "Complete"] as const;

export default function ArrowKombuchaDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      batches: [] as ActiveFermentationMonitorDto[],
      selectedPhase: "All" as string,
      loading: true,
      error: null as string | null,
      showNewForm: false,
      newBatchCode: "",
      newTeaType: "black",
      newSugarGrams: "200",
      formErrors: {} as Record<string, string>,
      isSubmitting: false,
    });

    const loadBatches = async () => {
      state.loading = true;
      try {
        const { FermentationAPI } = await import("../utils/farmos-client.ts");
        const all = (await FermentationAPI.getActiveMonitoring()) ?? [];
        state.batches = all.filter((b) => b.productType === "Kombucha");
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load";
      } finally {
        state.loading = false;
      }
    };

    loadBatches();

    const filtered = () => {
      if (state.selectedPhase === "All") return state.batches;
      return state.batches.filter((b) => b.phase === state.selectedPhase);
    };

    const phaseCount = (phase: string) => {
      if (phase === "All") return state.batches.length;
      return state.batches.filter((b) => b.phase === phase).length;
    };

    const phColor = (ph: number | null) => {
      if (ph === null) return "text-stone-400";
      if (ph <= 3.5) return "text-emerald-600";
      if (ph <= 4.2) return "text-amber-600";
      return "text-red-600";
    };

    const phaseBtn = (phase: string) => html`
      <button type="button" @click="${() => { state.selectedPhase = phase; }}"
        class="${() => state.selectedPhase === phase
          ? "bg-orange-500 text-white shadow-md"
          : "bg-stone-100 text-stone-600 hover:bg-stone-200"} px-3 py-2 rounded-xl text-sm font-semibold transition-all min-w-[48px] text-center">
        ${phase} <span class="text-xs opacity-70">${() => String(phaseCount(phase))}</span>
      </button>
    `;

    const batchCard = (batch: ActiveFermentationMonitorDto) => {
      const safeClass = batch.isSafe ? "border-emerald-200 bg-emerald-50/30" : "border-red-200 bg-red-50/30";
      return html`
        <div class="bg-white rounded-2xl border ${safeClass} p-5 hover:shadow-sm transition-shadow">
          <div class="flex items-start justify-between mb-3">
            <div>
              <h4 class="font-bold text-sm text-stone-800">${batch.batchCode}</h4>
              <p class="text-xs text-stone-400">${batch.phase}</p>
            </div>
            <span class="text-xs font-bold px-2 py-0.5 rounded-full ${batch.isSafe ? "bg-emerald-100 text-emerald-700" : "bg-red-100 text-red-700"}">${batch.isSafe ? "Safe" : "Alert"}</span>
          </div>
          <div class="grid grid-cols-2 gap-2 text-xs">
            <div>
              <span class="text-stone-400">pH</span>
              <p class="font-bold ${phColor(batch.currentPH)}">${batch.currentPH !== null ? batch.currentPH.toFixed(1) : "\u2014"}</p>
            </div>
            <div>
              <span class="text-stone-400">Drop Rate</span>
              <p class="font-bold text-stone-700">${batch.dropRatePerHour !== null ? batch.dropRatePerHour.toFixed(2) + "/hr" : "\u2014"}</p>
            </div>
          </div>
          <p class="text-xs text-stone-500 mt-2">${batch.statusMessage}</p>
        </div>
      `;
    };

    const autoCode = () => {
      const now = new Date();
      const m = String(now.getMonth() + 1).padStart(2, "0");
      return "KB-" + now.getFullYear() + "-" + m;
    };

    const submitNewBatch = async () => {
      state.formErrors = {};
      if (!state.newBatchCode.trim()) {
        state.formErrors = { batchCode: "Required" };
        return;
      }
      state.isSubmitting = true;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.startKombucha({
          batchCode: state.newBatchCode,
          scobyCultureId: "",
          teaType: state.newTeaType,
          sugarGrams: Number(state.newSugarGrams),
        });
        state.showNewForm = false;
        state.newBatchCode = "";
        await loadBatches();
      } catch (err: unknown) {
        state.formErrors = { submit: err instanceof Error ? err.message : "Failed" };
      } finally {
        state.isSubmitting = false;
      }
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Kombucha Batches</h1>
            <p class="text-stone-500 mt-1">Track fermentation, pH, and safety across all active brews.</p>
          </div>
          <button type="button" @click="${() => { state.showNewForm = !state.showNewForm; state.newBatchCode = autoCode(); }}"
            class="px-5 py-2.5 bg-orange-600 text-white rounded-xl text-sm font-semibold hover:bg-orange-700 transition shadow-sm">
            + New Batch
          </button>
        </header>

        <!-- New Batch Form -->
        ${() => state.showNewForm ? html`
          <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6">
            <h3 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Start Kombucha Batch</h3>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
              ${ArrowFormField({ label: "Batch Code", required: true, error: () => state.formErrors.batchCode, children: html`
                <input type="text" .value="${() => state.newBatchCode}" @input="${(e: Event) => { state.newBatchCode = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" />
              ` })}
              ${ArrowFormField({ label: "Tea Type", children: html`
                <select .value="${() => state.newTeaType}" @change="${(e: Event) => { state.newTeaType = (e.target as HTMLSelectElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none">
                  <option value="black">Black Tea</option>
                  <option value="green">Green Tea</option>
                  <option value="oolong">Oolong Tea</option>
                  <option value="white">White Tea</option>
                </select>
              ` })}
              ${ArrowFormField({ label: "Sugar (grams)", children: html`
                <input type="number" .value="${() => state.newSugarGrams}" @input="${(e: Event) => { state.newSugarGrams = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" />
              ` })}
            </div>
            ${() => state.formErrors.submit ? html`<p class="text-sm text-red-600 mb-3">${state.formErrors.submit}</p>` : html``}
            <div class="flex gap-3">
              <button type="button" @click="${submitNewBatch}" .disabled="${() => state.isSubmitting}"
                class="px-4 py-2 bg-orange-600 text-white rounded-lg text-sm font-semibold hover:bg-orange-700 transition disabled:opacity-50">
                ${() => state.isSubmitting ? "Starting..." : "Start Batch"}
              </button>
              <button type="button" @click="${() => { state.showNewForm = false; }}"
                class="px-4 py-2 text-stone-600 hover:bg-stone-100 rounded-lg text-sm font-medium transition">Cancel</button>
            </div>
          </div>
        ` : html``}

        <!-- KPI Row -->
        ${() => state.loading ? html`` : html`
          <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            ${ArrowKPICard({ label: "Total Batches", value: () => String(state.batches.length), icon: "\uD83E\uDED6", color: "orange" })}
            ${ArrowKPICard({ label: "Primary", value: () => String(phaseCount("Primary")), icon: "\u2697\uFE0F", color: "amber" })}
            ${ArrowKPICard({ label: "Secondary", value: () => String(phaseCount("Secondary")), icon: "\uD83C\uDF78", color: "violet" })}
            ${ArrowKPICard({ label: "Alerts", value: () => String(state.batches.filter((b) => !b.isSafe).length), icon: "\u26A0\uFE0F", color: "red" })}
          </div>
        `}

        <!-- Phase Filter -->
        <div class="flex gap-2 mb-6 overflow-x-auto pb-2">
          ${PHASES.map((phase) => phaseBtn(phase))}
        </div>

        <!-- Loading / Error / Content -->
        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-16"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : html`
            ${() => filtered().length === 0
              ? html`${ArrowEmptyState({ icon: "\uD83E\uDED6", title: "No kombucha batches", message: "Start a new batch to begin tracking pH and fermentation." })}`
              : html`<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">${() => filtered().map((b) => batchCard(b))}</div>`
            }
          `}

        <!-- Safety Reference -->
        <div class="mt-8 bg-amber-50 border border-amber-200 rounded-2xl p-5">
          <h3 class="text-sm font-bold text-amber-800 mb-2">\u26A0\uFE0F Kombucha Safety Thresholds</h3>
          <div class="grid grid-cols-2 gap-4 text-xs text-amber-700">
            <div><strong>pH Target:</strong> \u2264 4.2 within 7 days. Batches above 4.2 after 7 days must be discarded.</div>
            <div><strong>ABV Limit:</strong> &lt; 0.5% (TTB Federal Requirement). Commercial kombucha must stay below 0.5% ABV.</div>
          </div>
        </div>
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Update `routes/kombucha/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowKombuchaDashboard from "../../islands/ArrowKombuchaDashboard.tsx";

export default define.page(function KombuchaIndex() {
  return (
    <div>
      <Head>
        <title>Kombucha — Hearth OS</title>
      </Head>
      <ArrowKombuchaDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/islands/ArrowKombuchaDashboard.tsx frontend/hearth-os/routes/kombucha/index.tsx
git commit -m "feat(hearth-os): implement kombucha dashboard with Arrow.js"
```

---

### Task 6: IoT Dashboard (Full Arrow Migration)

**Files:**
- Create: `frontend/hearth-os/islands/ArrowIoTDashboard.tsx`
- Modify: `frontend/hearth-os/routes/iot/index.tsx`

**Step 1:** Create `ArrowIoTDashboard.tsx` — replaces the inline Preact table with a bento-grid device dashboard:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import type { DeviceSummaryDto } from "../utils/farmos-client.ts";

const SENSOR_TYPES = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
const STATUS_NAMES = ["Pending", "Active", "Offline", "Maintenance", "Decommissioned"];
const STATUS_STYLES: Record<number, { bg: string; text: string }> = {
  0: { bg: "bg-amber-100", text: "text-amber-800" },
  1: { bg: "bg-emerald-100", text: "text-emerald-800" },
  2: { bg: "bg-red-100", text: "text-red-800" },
  3: { bg: "bg-stone-100", text: "text-stone-800" },
  4: { bg: "bg-stone-200", text: "text-stone-500" },
};

export default function ArrowIoTDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      devices: [] as DeviceSummaryDto[],
      loading: true,
      error: null as string | null,
      showRegister: false,
      regCode: "",
      regName: "",
      regSensor: "0",
      regErrors: {} as Record<string, string>,
      isSubmitting: false,
    });

    const loadDevices = async () => {
      state.loading = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.devices = (await IoTAPI.getDevices()) ?? [];
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load devices";
      } finally {
        state.loading = false;
      }
    };

    loadDevices();

    const activeCount = () => state.devices.filter((d) => d.status === 1).length;
    const offlineCount = () => state.devices.filter((d) => d.status === 2).length;
    const unassignedCount = () => state.devices.filter((d) => !d.zoneId).length;

    const statusBadge = (status: number) => {
      const s = STATUS_STYLES[status] || STATUS_STYLES[4];
      return html`<span class="${s.bg} ${s.text} px-2.5 py-0.5 rounded-full text-xs font-semibold">${STATUS_NAMES[status] || "Unknown"}</span>`;
    };

    const deviceCard = (device: DeviceSummaryDto) => html`
      <a href="${"/iot/devices/" + device.id}" class="bg-white rounded-2xl border border-stone-200/60 p-5 hover:shadow-md transition-shadow block">
        <div class="flex items-start justify-between mb-3">
          <div>
            <h4 class="font-bold text-sm text-stone-800">${device.name}</h4>
            <p class="text-xs text-stone-400 font-mono">${device.deviceCode}</p>
          </div>
          ${statusBadge(device.status)}
        </div>
        <div class="flex items-center justify-between text-xs text-stone-500">
          <span>${SENSOR_TYPES[device.sensorType] || "Unknown"}</span>
          <span>${device.zoneId ? "Assigned" : "Unassigned"}</span>
        </div>
      </a>
    `;

    const submitRegister = async () => {
      state.regErrors = {};
      if (!state.regCode.trim()) { state.regErrors = { code: "Required" }; return; }
      if (!state.regName.trim()) { state.regErrors = { name: "Required" }; return; }
      state.isSubmitting = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.registerDevice({
          deviceCode: state.regCode,
          name: state.regName,
          sensorType: Number(state.regSensor),
        });
        state.showRegister = false;
        state.regCode = "";
        state.regName = "";
        await loadDevices();
      } catch (err: unknown) {
        state.regErrors = { submit: err instanceof Error ? err.message : "Failed" };
      } finally {
        state.isSubmitting = false;
      }
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">IoT Devices</h1>
            <p class="text-stone-500 mt-1">Manage connected sensors and hardware.</p>
          </div>
          <div class="flex gap-3">
            <a href="/iot/zones" class="px-4 py-2.5 bg-white border border-stone-200 hover:bg-stone-50 text-stone-700 rounded-xl text-sm font-semibold shadow-sm transition">Manage Zones</a>
            <button type="button" @click="${() => { state.showRegister = !state.showRegister; }}"
              class="px-5 py-2.5 bg-orange-600 text-white rounded-xl text-sm font-semibold hover:bg-orange-700 transition shadow-sm">+ Register Device</button>
          </div>
        </header>

        <!-- Register Form -->
        ${() => state.showRegister ? html`
          <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6">
            <h3 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Register New Device</h3>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Device Code *</label>
                <input type="text" .value="${() => state.regCode}" @input="${(e: Event) => { state.regCode = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" placeholder="MAC or serial" />
                ${() => state.regErrors.code ? html`<p class="text-xs text-red-500 mt-1">${state.regErrors.code}</p>` : html``}
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Name *</label>
                <input type="text" .value="${() => state.regName}" @input="${(e: Event) => { state.regName = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" placeholder="e.g. Greenhouse Temp 1" />
                ${() => state.regErrors.name ? html`<p class="text-xs text-red-500 mt-1">${state.regErrors.name}</p>` : html``}
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Sensor Type</label>
                <select .value="${() => state.regSensor}" @change="${(e: Event) => { state.regSensor = (e.target as HTMLSelectElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none">
                  ${SENSOR_TYPES.map((t, i) => html`<option value="${String(i)}">${t}</option>`)}
                </select>
              </div>
            </div>
            ${() => state.regErrors.submit ? html`<p class="text-sm text-red-600 mb-3">${state.regErrors.submit}</p>` : html``}
            <div class="flex gap-3">
              <button type="button" @click="${submitRegister}" .disabled="${() => state.isSubmitting}"
                class="px-4 py-2 bg-orange-600 text-white rounded-lg text-sm font-semibold hover:bg-orange-700 transition disabled:opacity-50">
                ${() => state.isSubmitting ? "Registering..." : "Register"}
              </button>
              <button type="button" @click="${() => { state.showRegister = false; }}"
                class="px-4 py-2 text-stone-600 hover:bg-stone-100 rounded-lg text-sm font-medium transition">Cancel</button>
            </div>
          </div>
        ` : html``}

        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-16"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : html`
            <div>
              ${() => state.error ? html`<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">${state.error}</div>` : html``}

              <!-- KPI Row -->
              <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                ${ArrowKPICard({ label: "Total Devices", value: () => String(state.devices.length), icon: "\uD83D\uDCE1", color: "orange" })}
                ${ArrowKPICard({ label: "Active", value: () => String(activeCount()), icon: "\u2705", color: "emerald" })}
                ${ArrowKPICard({ label: "Offline", value: () => String(offlineCount()), icon: "\u274C", color: "red" })}
                ${ArrowKPICard({ label: "Unassigned", value: () => String(unassignedCount()), icon: "\uD83D\uDCCD", color: "amber" })}
              </div>

              <!-- Device Grid -->
              ${() => state.devices.length === 0
                ? html`${ArrowEmptyState({ icon: "\uD83D\uDCE1", title: "No devices registered", message: "Register your first IoT device to start monitoring." })}`
                : html`<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">${() => state.devices.map((d) => deviceCard(d))}</div>`
              }
            </div>
          `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Update `routes/iot/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowIoTDashboard from "../../islands/ArrowIoTDashboard.tsx";

export default define.page(function IoTPage() {
  return (
    <div>
      <Head>
        <title>IoT Devices — Hearth OS</title>
      </Head>
      <ArrowIoTDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/islands/ArrowIoTDashboard.tsx frontend/hearth-os/routes/iot/index.tsx
git commit -m "feat(hearth-os): replace IoT device page with Arrow bento dashboard"
```

---

### Task 7: IoT Zones + Zone Detail + Device Detail

**Files:**
- Create: `frontend/hearth-os/islands/ArrowZonesDashboard.tsx`
- Create: `frontend/hearth-os/islands/ArrowZoneDetail.tsx`
- Create: `frontend/hearth-os/islands/ArrowDeviceDetail.tsx`
- Modify: `frontend/hearth-os/routes/iot/zones/index.tsx`
- Modify: `frontend/hearth-os/routes/iot/zones/[id].tsx`
- Modify: `frontend/hearth-os/routes/iot/devices/[id].tsx`

**Step 1:** Create `ArrowZonesDashboard.tsx`:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import type { ZoneSummaryDto } from "../utils/farmos-client.ts";

const ZONE_TYPES = ["Greenhouse", "Field", "Barn", "Cellar", "Storage", "Other"];

export default function ArrowZonesDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      zones: [] as ZoneSummaryDto[],
      loading: true,
      error: null as string | null,
      showCreate: false,
      newName: "",
      newType: "0",
      newDescription: "",
      formErrors: {} as Record<string, string>,
      isSubmitting: false,
    });

    const loadZones = async () => {
      state.loading = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.zones = (await IoTAPI.getZones()) ?? [];
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load zones";
      } finally {
        state.loading = false;
      }
    };

    loadZones();

    const submitCreate = async () => {
      state.formErrors = {};
      if (!state.newName.trim()) { state.formErrors = { name: "Required" }; return; }
      state.isSubmitting = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.createZone({ name: state.newName, zoneType: Number(state.newType), description: state.newDescription || undefined });
        state.showCreate = false;
        state.newName = "";
        state.newDescription = "";
        await loadZones();
      } catch (err: unknown) {
        state.formErrors = { submit: err instanceof Error ? err.message : "Failed" };
      } finally {
        state.isSubmitting = false;
      }
    };

    const zoneCard = (zone: ZoneSummaryDto) => html`
      <a href="${"/iot/zones/" + zone.id}" class="bg-white rounded-2xl border border-stone-200/60 p-5 hover:shadow-md transition-shadow block">
        <div class="flex items-start justify-between mb-2">
          <h4 class="font-bold text-sm text-stone-800">${zone.name}</h4>
          <span class="bg-stone-100 text-stone-600 px-2 py-0.5 rounded-full text-xs font-semibold">${ZONE_TYPES[zone.zoneType] || "Unknown"}</span>
        </div>
        <p class="text-xs text-stone-400">${zone.parentZoneId ? "Has parent zone" : "Top-level zone"}</p>
      </a>
    `;

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <div class="mb-2">
          <a href="/iot" class="text-orange-600 hover:text-orange-700 text-sm font-semibold transition">\u2190 Back to Devices</a>
        </div>
        <header class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">IoT Zones</h1>
            <p class="text-stone-500 mt-1">Group and locate devices across the farm.</p>
          </div>
          <button type="button" @click="${() => { state.showCreate = !state.showCreate; }}"
            class="px-5 py-2.5 bg-orange-600 text-white rounded-xl text-sm font-semibold hover:bg-orange-700 transition shadow-sm">+ Create Zone</button>
        </header>

        ${() => state.showCreate ? html`
          <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6">
            <h3 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Create Zone</h3>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
              ${ArrowFormField({ label: "Name", required: true, error: () => state.formErrors.name, children: html`
                <input type="text" .value="${() => state.newName}" @input="${(e: Event) => { state.newName = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" />
              ` })}
              ${ArrowFormField({ label: "Zone Type", children: html`
                <select .value="${() => state.newType}" @change="${(e: Event) => { state.newType = (e.target as HTMLSelectElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none">
                  ${ZONE_TYPES.map((t, i) => html`<option value="${String(i)}">${t}</option>`)}
                </select>
              ` })}
              ${ArrowFormField({ label: "Description", children: html`
                <input type="text" .value="${() => state.newDescription}" @input="${(e: Event) => { state.newDescription = (e.target as HTMLInputElement).value; }}"
                  class="w-full px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none" />
              ` })}
            </div>
            ${() => state.formErrors.submit ? html`<p class="text-sm text-red-600 mb-3">${state.formErrors.submit}</p>` : html``}
            <div class="flex gap-3">
              <button type="button" @click="${submitCreate}" .disabled="${() => state.isSubmitting}"
                class="px-4 py-2 bg-orange-600 text-white rounded-lg text-sm font-semibold hover:bg-orange-700 transition disabled:opacity-50">
                ${() => state.isSubmitting ? "Creating..." : "Create Zone"}
              </button>
              <button type="button" @click="${() => { state.showCreate = false; }}" class="px-4 py-2 text-stone-600 hover:bg-stone-100 rounded-lg text-sm font-medium transition">Cancel</button>
            </div>
          </div>
        ` : html``}

        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-16"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : html`
            <div>
              ${() => state.error ? html`<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">${state.error}</div>` : html``}
              <div class="grid grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
                ${ArrowKPICard({ label: "Total Zones", value: () => String(state.zones.length), icon: "\uD83D\uDCCD", color: "orange" })}
              </div>
              ${() => state.zones.length === 0
                ? html`${ArrowEmptyState({ icon: "\uD83D\uDCCD", title: "No zones created", message: "Create your first zone to organize your sensor network." })}`
                : html`<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">${() => state.zones.map((z) => zoneCard(z))}</div>`
              }
            </div>
          `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Create `ArrowZoneDetail.tsx`:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowConfirmDialog } from "../components/ArrowConfirmDialog.ts";
import type { ZoneDetailDto } from "../utils/farmos-client.ts";

const STATUS_NAMES = ["Pending", "Active", "Offline", "Maintenance", "Decommissioned"];
const STATUS_STYLES: Record<number, { bg: string; text: string }> = {
  0: { bg: "bg-amber-100", text: "text-amber-800" },
  1: { bg: "bg-emerald-100", text: "text-emerald-800" },
  2: { bg: "bg-red-100", text: "text-red-800" },
  3: { bg: "bg-stone-100", text: "text-stone-800" },
  4: { bg: "bg-stone-200", text: "text-stone-500" },
};

export default function ArrowZoneDetail({ zoneId }: { zoneId: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      zone: null as ZoneDetailDto | null,
      loading: true,
      error: null as string | null,
      showArchiveConfirm: false,
      archiveReason: "",
    });

    const loadZone = async () => {
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.zone = await IoTAPI.getZone(zoneId);
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Zone not found";
      } finally {
        state.loading = false;
      }
    };

    loadZone();

    const archiveZone = async () => {
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.archiveZone(zoneId, { zoneId, reason: state.archiveReason || "Archived" });
        globalThis.location.href = "/iot/zones";
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to archive";
      }
    };

    const statusBadge = (status: number) => {
      const s = STATUS_STYLES[status] || STATUS_STYLES[4];
      return html`<span class="${s.bg} ${s.text} px-2.5 py-0.5 rounded-full text-xs font-semibold">${STATUS_NAMES[status] || "Unknown"}</span>`;
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <div class="mb-2">
          <a href="/iot/zones" class="text-orange-600 hover:text-orange-700 text-sm font-semibold transition">\u2190 Back to Zones</a>
        </div>

        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-16"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : state.error
            ? html`<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">${state.error}</div>`
            : state.zone === null
              ? html`<p class="text-stone-500">Zone not found.</p>`
              : html`
                <div>
                  <!-- Zone Header -->
                  <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6">
                    <div class="flex items-center justify-between">
                      <div>
                        <h1 class="text-2xl font-extrabold text-stone-800">${state.zone.name}</h1>
                        <p class="text-sm text-stone-500 mt-1">${state.zone.description || "No description"}</p>
                      </div>
                      <div class="flex items-center gap-3">
                        <span class="bg-stone-100 text-stone-600 px-3 py-1 rounded-full text-xs font-semibold">${["Greenhouse", "Field", "Barn", "Cellar", "Storage", "Other"][state.zone.zoneType] || "Unknown"}</span>
                        <button type="button" @click="${() => { state.showArchiveConfirm = true; }}"
                          class="px-4 py-2 bg-red-50 text-red-600 rounded-lg text-sm font-semibold hover:bg-red-100 transition">Archive</button>
                      </div>
                    </div>
                  </div>

                  <!-- Devices -->
                  <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Devices in Zone (${String(state.zone.devices.length)})</h2>
                  ${state.zone.devices.length === 0
                    ? html`${ArrowEmptyState({ icon: "\uD83D\uDCE1", title: "No devices assigned", message: "Assign devices to this zone from the device detail page." })}`
                    : html`
                      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        ${state.zone.devices.map((d) => html`
                          <a href="${"/iot/devices/" + d.id}" class="bg-white rounded-2xl border border-stone-200/60 p-5 hover:shadow-md transition-shadow block">
                            <div class="flex items-start justify-between mb-2">
                              <h4 class="font-bold text-sm text-stone-800">${d.name}</h4>
                              ${statusBadge(d.status)}
                            </div>
                            <p class="text-xs text-stone-400 font-mono">${d.deviceCode}</p>
                          </a>
                        `)}
                      </div>
                    `}

                  ${ArrowConfirmDialog({
                    isOpen: () => state.showArchiveConfirm,
                    title: "Archive Zone",
                    message: "This will archive the zone. Devices will become unassigned.",
                    confirmLabel: "Archive",
                    danger: true,
                    onConfirm: archiveZone,
                    onCancel: () => { state.showArchiveConfirm = false; },
                  })}
                </div>
              `}
      </div>
    `;

    template(containerRef.current);
  }, [zoneId]);

  return <div ref={containerRef}></div>;
}
```

**Step 3:** Create `ArrowDeviceDetail.tsx`:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowConfirmDialog } from "../components/ArrowConfirmDialog.ts";
import type { DeviceDetailDto, ZoneSummaryDto } from "../utils/farmos-client.ts";

const SENSOR_TYPES = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
const STATUS_NAMES = ["Pending", "Active", "Offline", "Maintenance", "Decommissioned"];

export default function ArrowDeviceDetail({ deviceId }: { deviceId: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      device: null as DeviceDetailDto | null,
      zones: [] as ZoneSummaryDto[],
      loading: true,
      error: null as string | null,
      selectedZone: "",
      showDecommission: false,
      decommissionReason: "",
    });

    const loadData = async () => {
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        const [deviceResult, zonesResult] = await Promise.allSettled([
          IoTAPI.getDevice(deviceId),
          IoTAPI.getZones(),
        ]);
        state.device = deviceResult.status === "fulfilled" ? deviceResult.value : null;
        state.zones = zonesResult.status === "fulfilled" ? (zonesResult.value ?? []) : [];
        if (state.device?.zoneId) state.selectedZone = state.device.zoneId;
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Device not found";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const assignZone = async () => {
      if (!state.selectedZone) return;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.assignDeviceToZone(deviceId, { deviceId, zoneId: state.selectedZone });
        await loadData();
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to assign";
      }
    };

    const decommission = async () => {
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.decommissionDevice(deviceId, { deviceId, reason: state.decommissionReason || "Decommissioned" });
        globalThis.location.href = "/iot";
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to decommission";
      }
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <div class="mb-2">
          <a href="/iot" class="text-orange-600 hover:text-orange-700 text-sm font-semibold transition">\u2190 Back to Devices</a>
        </div>

        ${() => state.loading
          ? html`<div class="flex items-center justify-center py-16"><div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"></div></div>`
          : state.device === null
            ? html`<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">${() => state.error || "Device not found"}</div>`
            : html`
              <div>
                <!-- Device Header -->
                <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6">
                  <div class="flex items-center justify-between">
                    <div>
                      <h1 class="text-2xl font-extrabold text-stone-800">${state.device.name}</h1>
                      <p class="text-sm text-stone-400 font-mono mt-1">${state.device.deviceCode}</p>
                    </div>
                    <div class="flex items-center gap-3">
                      <span class="bg-stone-100 text-stone-600 px-3 py-1 rounded-full text-xs font-semibold">${STATUS_NAMES[state.device.status] || "Unknown"}</span>
                      <button type="button" @click="${() => { state.showDecommission = true; }}"
                        class="px-4 py-2 bg-red-50 text-red-600 rounded-lg text-sm font-semibold hover:bg-red-100 transition">Decommission</button>
                    </div>
                  </div>
                </div>

                <!-- Info Grid -->
                <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
                  <!-- Zone Assignment -->
                  <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                    <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Zone Assignment</h2>
                    <div class="flex gap-3">
                      <select .value="${() => state.selectedZone}" @change="${(e: Event) => { state.selectedZone = (e.target as HTMLSelectElement).value; }}"
                        class="flex-1 px-3 py-2 border border-stone-200 rounded-lg text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none">
                        <option value="">Unassigned</option>
                        ${state.zones.map((z) => html`<option value="${z.id}">${z.name}</option>`)}
                      </select>
                      <button type="button" @click="${assignZone}"
                        class="px-4 py-2 bg-orange-600 text-white rounded-lg text-sm font-semibold hover:bg-orange-700 transition">Assign</button>
                    </div>
                  </div>

                  <!-- Device Info -->
                  <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                    <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Device Info</h2>
                    <div class="space-y-2 text-sm">
                      <div class="flex justify-between"><span class="text-stone-400">Sensor Type</span><span class="font-medium text-stone-700">${SENSOR_TYPES[state.device.sensorType] || "Unknown"}</span></div>
                      <div class="flex justify-between"><span class="text-stone-400">ID</span><span class="font-mono text-stone-600 text-xs">${state.device.id}</span></div>
                      ${state.device.gridPos ? html`<div class="flex justify-between"><span class="text-stone-400">Grid Position</span><span class="font-medium text-stone-700">${state.device.gridPos.x}, ${state.device.gridPos.y}, ${state.device.gridPos.z}</span></div>` : html``}
                    </div>
                  </div>
                </div>

                <!-- Metadata -->
                ${Object.keys(state.device.metadata).length > 0 ? html`
                  <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                    <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Metadata</h2>
                    <div class="space-y-2 text-sm">
                      ${Object.entries(state.device.metadata).map(([k, v]) => html`
                        <div class="flex justify-between"><span class="text-stone-400">${k}</span><span class="font-medium text-stone-700">${v}</span></div>
                      `)}
                    </div>
                  </div>
                ` : html``}

                ${ArrowConfirmDialog({
                  isOpen: () => state.showDecommission,
                  title: "Decommission Device",
                  message: "This device will be permanently decommissioned.",
                  confirmLabel: "Decommission",
                  danger: true,
                  onConfirm: decommission,
                  onCancel: () => { state.showDecommission = false; },
                })}
              </div>
            `}
      </div>
    `;

    template(containerRef.current);
  }, [deviceId]);

  return <div ref={containerRef}></div>;
}
```

**Step 4:** Update route files:

`routes/iot/zones/index.tsx`:
```tsx
import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowZonesDashboard from "../../../islands/ArrowZonesDashboard.tsx";

export default define.page(function IoTZonesPage() {
  return (
    <div>
      <Head><title>IoT Zones — Hearth OS</title></Head>
      <ArrowZonesDashboard />
    </div>
  );
});
```

`routes/iot/zones/[id].tsx`:
```tsx
import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowZoneDetail from "../../../islands/ArrowZoneDetail.tsx";

export default define.page(function ZoneDetailPage(props) {
  const zoneId = props.params.id;
  return (
    <div>
      <Head><title>Zone Detail — Hearth OS</title></Head>
      <ArrowZoneDetail zoneId={zoneId} />
    </div>
  );
});
```

`routes/iot/devices/[id].tsx`:
```tsx
import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowDeviceDetail from "../../../islands/ArrowDeviceDetail.tsx";

export default define.page(function DeviceDetailPage(props) {
  const deviceId = props.params.id;
  return (
    <div>
      <Head><title>Device Detail — Hearth OS</title></Head>
      <ArrowDeviceDetail deviceId={deviceId} />
    </div>
  );
});
```

**Step 5: Commit**

```bash
git add frontend/hearth-os/islands/ArrowZonesDashboard.tsx frontend/hearth-os/islands/ArrowZoneDetail.tsx frontend/hearth-os/islands/ArrowDeviceDetail.tsx frontend/hearth-os/routes/iot/zones/index.tsx frontend/hearth-os/routes/iot/zones/\[id\].tsx frontend/hearth-os/routes/iot/devices/\[id\].tsx
git commit -m "feat(hearth-os): migrate IoT zones and device detail pages to Arrow.js"
```

---

### Task 8: Compliance Hub (Bento Redesign)

**Files:**
- Create: `frontend/hearth-os/islands/ArrowComplianceHub.tsx`
- Modify: `frontend/hearth-os/routes/compliance/index.tsx`

**Step 1:** Create `ArrowComplianceHub.tsx` — replaces the 4 inline Preact islands with a unified bento-grid hub showing section cards with links to sub-pages, plus inline panels for equipment temps, sanitation, certs, and delivery:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";

interface ComplianceSection {
  title: string;
  description: string;
  icon: string;
  href: string;
  color: string;
}

const SECTIONS: ComplianceSection[] = [
  { title: "Traceability & FSMA 204", description: "CTEs and KDEs for FDA 24-hour audit compliance", icon: "\uD83D\uDD17", href: "/compliance/traceability", color: "emerald" },
  { title: "HACCP Plan", description: "Critical control points and monitoring procedures", icon: "\uD83D\uDCCB", href: "/compliance/haccp-plan", color: "sky" },
  { title: "CAPA Tracker", description: "Corrective and preventive action management", icon: "\u26A0\uFE0F", href: "/compliance/capa", color: "amber" },
];

export default function ArrowComplianceHub() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      loading: false,
    });

    const sectionCard = (section: ComplianceSection) => {
      const bgMap: Record<string, string> = { emerald: "bg-emerald-50 border-emerald-200 hover:bg-emerald-100", sky: "bg-sky-50 border-sky-200 hover:bg-sky-100", amber: "bg-amber-50 border-amber-200 hover:bg-amber-100" };
      const textMap: Record<string, string> = { emerald: "text-emerald-700", sky: "text-sky-700", amber: "text-amber-700" };
      return html`
        <a href="${section.href}" class="${bgMap[section.color] || "bg-stone-50 border-stone-200"} border rounded-2xl p-6 transition-all block hover:shadow-md">
          <div class="flex items-start gap-4">
            <span class="text-2xl">${section.icon}</span>
            <div>
              <h3 class="font-bold text-sm ${textMap[section.color] || "text-stone-700"}">${section.title}</h3>
              <p class="text-xs text-stone-500 mt-1">${section.description}</p>
            </div>
          </div>
        </a>
      `;
    };

    // Inline compliance sub-sections (Equipment, Sanitation, Certs, Delivery)
    // These remain as separate Preact islands imported below the bento grid
    // since they already have full CRUD functionality

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Kitchen Compliance</h1>
            <p class="text-stone-500 mt-1">Food safety, sanitation, certifications, and cold chain tracking.</p>
          </div>
          <button type="button" @click="${() => globalThis.print()}"
            class="no-print px-5 py-2.5 bg-stone-800 text-white rounded-xl text-sm font-bold hover:bg-stone-900 transition shadow-md flex items-center gap-2">
            \uD83D\uDDA8\uFE0F Print for Inspector
          </button>
        </header>

        <!-- Compliance Section Cards -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          ${SECTIONS.map((s) => sectionCard(s))}
        </div>

        <!-- Inline Sections (mounted below) -->
        <div id="compliance-panels"></div>
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
```

**Step 2:** Update `routes/compliance/index.tsx` to combine the Arrow hub with existing Preact panels:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowComplianceHub from "../../islands/ArrowComplianceHub.tsx";
import EquipmentPanel from "../../islands/EquipmentPanel.tsx";
import SanitationLog from "../../islands/SanitationLog.tsx";
import StaffCertifications from "../../islands/StaffCertifications.tsx";
import DeliveryLog from "../../islands/DeliveryLog.tsx";

export default define.page(function CompliancePage() {
  return (
    <div>
      <Head>
        <title>Compliance — Hearth OS</title>
        <style
          dangerouslySetInnerHTML={{
            __html: `
          @media print {
            body { background: white !important; margin: 0; padding: 0; color: black !important; }
            button, nav, .no-print { display: none !important; }
            section { page-break-inside: avoid; margin-bottom: 2rem !important; }
            h1, h2, h3, h4, h5, p, span, div { color: black !important; }
            .bg-white, .bg-stone-50, .bg-emerald-50, .bg-red-50 { background: white !important; border: 1px solid #ccc !important; box-shadow: none !important; }
            @page { margin: 0.5in; }
            * { transition: none !important; }
          }
        `,
          }}
        />
      </Head>

      <ArrowComplianceHub />

      {/* Existing Preact panels below the bento hub */}
      <div class="px-6 max-w-7xl mx-auto space-y-8 pb-8">
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">🌡️</span>
            <h2 class="text-lg font-bold text-stone-800">Equipment Temperatures</h2>
          </div>
          <EquipmentPanel />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">🧹</span>
            <h2 class="text-lg font-bold text-stone-800">Sanitation Log</h2>
          </div>
          <SanitationLog />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">🪪</span>
            <h2 class="text-lg font-bold text-stone-800">Staff Certifications</h2>
          </div>
          <StaffCertifications />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">📦</span>
            <h2 class="text-lg font-bold text-stone-800">Delivery Receiving Log</h2>
          </div>
          <DeliveryLog />
        </section>
      </div>
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/islands/ArrowComplianceHub.tsx frontend/hearth-os/routes/compliance/index.tsx
git commit -m "feat(hearth-os): add compliance bento hub with section cards"
```

---

### Task 9: HACCP, CAPA, Traceability Route Cleanup

These pages already delegate to existing Preact islands (`HACCPPlanBuilder`, `CAPADashboard`, `TraceabilityDashboard`). For now, apply consistent bento styling to the route shells. Full Arrow migration of these complex islands can happen in a follow-up.

**Files:**
- Modify: `frontend/hearth-os/routes/compliance/haccp-plan.tsx` — apply `px-6 py-8 max-w-7xl mx-auto` wrapper
- Modify: `frontend/hearth-os/routes/compliance/capa.tsx` — apply consistent styling
- Modify: `frontend/hearth-os/routes/compliance/traceability/index.tsx` — apply consistent styling

**Step 1:** Update each file to use consistent bento wrapper styling. Add breadcrumb back-link and `max-w-7xl` container. Keep existing islands.

For `haccp-plan.tsx`: Change outer `<div class="p-8">` to `<div class="px-6 py-8 max-w-7xl mx-auto">` and add back-link: `<a href="/compliance" class="text-orange-600 ...">← Back to Compliance</a>`.

For `capa.tsx`: Same wrapper change + back-link.

For `traceability/index.tsx`: Same wrapper change + back-link.

**Step 2: Commit**

```bash
git add frontend/hearth-os/routes/compliance/haccp-plan.tsx frontend/hearth-os/routes/compliance/capa.tsx frontend/hearth-os/routes/compliance/traceability/index.tsx
git commit -m "feat(hearth-os): apply bento styling to compliance sub-pages"
```

---

### Task 10: Freeze Dryer + Settings Bento Wrappers

**Files:**
- Modify: `frontend/hearth-os/routes/freeze-dryer/index.tsx`
- Modify: `frontend/hearth-os/routes/settings/index.tsx`

**Step 1:** Update `routes/freeze-dryer/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import FreezeDryerPanel from "../../islands/FreezeDryerPanel.tsx";

export default define.page(function FreezeDryerPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Freeze-Dryer — Hearth OS</title>
      </Head>
      <header class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Freeze-Dryer Management</h1>
        <p class="text-stone-500 mt-1">Track Harvest Right cycles — vacuum, shelf temperature, batch weights, and phase progression.</p>
      </header>
      <FreezeDryerPanel />
    </div>
  );
});
```

**Step 2:** Update `routes/settings/index.tsx`:

```tsx
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowSettingsPanel from "../../islands/ArrowSettingsPanel.tsx";

export default define.page(function SettingsPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Settings — Hearth OS</title>
      </Head>
      <header class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Settings</h1>
        <p class="text-stone-500 mt-1">Manage farm configuration, dropdown menus, notifications, and integrations.</p>
      </header>
      <ArrowSettingsPanel />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/hearth-os/routes/freeze-dryer/index.tsx frontend/hearth-os/routes/settings/index.tsx
git commit -m "feat(hearth-os): apply bento styling to freeze-dryer and settings pages"
```

---

### Task 11: Build Verification + Cleanup

**Step 1: Run build**

```bash
cd frontend/hearth-os && deno task build
```

Expected: Build succeeds with 0 errors.

**Step 2: Fix any TypeScript/import errors**

Common issues:
- Missing type imports — add `import type { ... } from "../utils/farmos-client.ts"`
- Arrow.js `.value` property binding — ensure `.value="${() => state.field}"` not `value="${...}"`
- `define.page` props access — use `props.params.id` for dynamic routes (check Fresh 2.2 API)

**Step 3: Verify old imports removed**

Check that modified route files no longer import unused Preact components:
- `routes/index.tsx` should NOT import `BatchStatusCards`, `ArrowBatchStatusCards`, `ArrowExperiment`, `FermentationAPI`
- `routes/iot/index.tsx` should NOT import `IoTAPI`, `DeviceSummaryDto`, `ArrowDeviceRegistrationModal`
- `routes/iot/zones/index.tsx` should NOT import `IoTAPI`, `ZoneSummaryDto`, `ArrowZoneCreationModal`

**Step 4: Commit**

```bash
git add -A frontend/hearth-os/
git commit -m "fix(hearth-os): build fixes and cleanup after Arrow migration"
```

---

## Task Execution Order

```
Task 1 (foundation) → Task 2 (nav + layout) → Task 3 (dashboard)
Then in parallel:
  Task 4 (batches + cultures)
  Task 5 (kombucha)
  Task 6 (IoT dashboard)
After Task 6:
  Task 7 (IoT zones + devices)
Independent of IoT:
  Task 8 (compliance hub)
  Task 9 (compliance sub-pages)
  Task 10 (freeze-dryer + settings)
Final:
  Task 11 (build verification)
```

Total: **11 tasks, ~17 files created/modified, 7 new Arrow islands**
