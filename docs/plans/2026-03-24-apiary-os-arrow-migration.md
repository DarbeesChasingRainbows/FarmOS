# Apiary OS — Arrow.js Full Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate all apiary-os pages from Preact islands to Arrow.js reactive islands with a modern bento-grid dashboard design inspired by Laws of UX and Dribbble dashboard patterns.

**Architecture:** Fresh 2.2.0 islands remain `.tsx` files for discovery, but each mounts Arrow.js reactive DOM via `useEffect` + `containerRef` pattern (established in hearth-os). Shared Arrow components live in `components/Arrow*.ts`. All pages get a persistent sidebar nav and bento-grid layouts replacing tab-based UIs.

**Tech Stack:** Deno Fresh 2.2.0, Arrow.js (`@arrow-js/core`), Tailwind CSS v4, Zod validation, Vite bundler

---

## Prerequisites

Before starting, ensure:
- `@arrow-js/core` is added to `deno.json` imports
- Existing `utils/farmos-client.ts`, `utils/schemas.ts`, `utils/toastState.ts` remain unchanged
- `islands/ToastProvider.tsx` stays as Preact (global, simple polling pattern)
- The hearth-os Arrow component patterns in `components/Arrow*.ts` serve as reference

## Critical Arrow.js Rules

1. **Never nest backticks** inside `html` tagged templates — use string concatenation (`"text " + variable`) instead
2. **Reactive expressions** must be wrapped in `() =>` arrow functions: `${() => state.value}`
3. **Event handlers** use `@click`, `@input`, `@change` syntax (not `onClick`)
4. **Conditional rendering**: Arrow can't render `null` — use `html``\`` (empty template) as fallback
5. **Lists**: `${() => state.items.map(item => cardTemplate(item))}` — must be inside a reactive wrapper

---

### Task 1: Add Arrow.js Dependency & Shared Components

**Files:**
- Modify: `frontend/apiary-os/deno.json`
- Create: `frontend/apiary-os/components/ArrowKPICard.ts`
- Create: `frontend/apiary-os/components/ArrowStatusBadge.ts`
- Create: `frontend/apiary-os/components/ArrowFormField.ts`
- Create: `frontend/apiary-os/components/ArrowTooltip.ts`
- Create: `frontend/apiary-os/components/ArrowConfirmDialog.ts`
- Create: `frontend/apiary-os/components/ArrowEmptyState.ts`
- Create: `frontend/apiary-os/components/ArrowProgressRing.ts`

**Step 1: Add @arrow-js/core to deno.json**

In `frontend/apiary-os/deno.json`, add to the `"imports"` section:

```json
"@arrow-js/core": "npm:@arrow-js/core@^1.0.0-alpha.12"
```

**Step 2: Create ArrowKPICard component**

Create `frontend/apiary-os/components/ArrowKPICard.ts`:

```typescript
import { html } from "@arrow-js/core";

export interface KPICardProps {
  label: string;
  value: string | (() => string);
  trend?: string | (() => string);
  trendDirection?: "up" | "down" | "flat" | (() => "up" | "down" | "flat");
  icon: string;
  color?: "amber" | "emerald" | "red" | "violet" | "sky" | "stone";
}

const colorStyles: Record<string, { bg: string; iconBg: string; trend: string }> = {
  amber: { bg: "bg-white", iconBg: "bg-amber-50 text-amber-600", trend: "text-amber-600" },
  emerald: { bg: "bg-white", iconBg: "bg-emerald-50 text-emerald-600", trend: "text-emerald-600" },
  red: { bg: "bg-white", iconBg: "bg-red-50 text-red-600", trend: "text-red-600" },
  violet: { bg: "bg-white", iconBg: "bg-violet-50 text-violet-600", trend: "text-violet-600" },
  sky: { bg: "bg-white", iconBg: "bg-sky-50 text-sky-600", trend: "text-sky-600" },
  stone: { bg: "bg-white", iconBg: "bg-stone-100 text-stone-600", trend: "text-stone-600" },
};

export function ArrowKPICard(props: KPICardProps) {
  const c = colorStyles[props.color || "amber"];

  const trendArrow = () => {
    const dir = typeof props.trendDirection === "function" ? props.trendDirection() : props.trendDirection;
    if (dir === "up") return "↗";
    if (dir === "down") return "↘";
    return "→";
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
          return html`
            <span class="${() => trendColor()} text-xs font-bold px-2 py-0.5 rounded-full flex items-center gap-0.5">
              ${() => trendArrow()} ${() => typeof props.trend === "function" ? props.trend() : props.trend}
            </span>
          `;
        }}
      </div>
      <p class="text-2xl font-extrabold text-stone-800 tracking-tight">${props.value}</p>
      <p class="text-xs text-stone-400 mt-1 uppercase tracking-wider font-medium">${props.label}</p>
    </div>
  `;
}
```

**Step 3: Create ArrowStatusBadge component**

Create `frontend/apiary-os/components/ArrowStatusBadge.ts`:

```typescript
import { html } from "@arrow-js/core";

export type BadgeVariant = "active" | "attention" | "resting" | "queenless" | "healthy" | "moderate" | "critical";

const variants: Record<BadgeVariant, string> = {
  active: "bg-emerald-100 text-emerald-800 border-emerald-200",
  attention: "bg-amber-100 text-amber-800 border-amber-200",
  resting: "bg-stone-100 text-stone-600 border-stone-200",
  queenless: "bg-red-100 text-red-800 border-red-200",
  healthy: "bg-emerald-100 text-emerald-800 border-emerald-200",
  moderate: "bg-amber-100 text-amber-800 border-amber-200",
  critical: "bg-red-100 text-red-800 border-red-200",
};

export interface ArrowStatusBadgeProps {
  variant: BadgeVariant | (() => BadgeVariant);
  label?: string | (() => string);
}

export function ArrowStatusBadge(props: ArrowStatusBadgeProps) {
  return html`
    <span class="${() => {
      const v = typeof props.variant === "function" ? props.variant() : props.variant;
      return "inline-flex items-center gap-1 px-2 py-0.5 text-xs font-semibold uppercase tracking-wider rounded-full border " + variants[v];
    }}">
      ${() => {
        const v = typeof props.variant === "function" ? props.variant() : props.variant;
        const l = typeof props.label === "function" ? props.label() : props.label;
        return l || v.charAt(0).toUpperCase() + v.slice(1);
      }}
    </span>
  `;
}
```

**Step 4: Create ArrowFormField component**

Create `frontend/apiary-os/components/ArrowFormField.ts`:

Copy from `frontend/hearth-os/components/ArrowFormField.ts` exactly (already proven to work).

**Step 5: Create ArrowTooltip component**

Create `frontend/apiary-os/components/ArrowTooltip.ts`:

Copy from `frontend/hearth-os/components/ArrowTooltip.ts` exactly (already proven to work).

**Step 6: Create ArrowConfirmDialog component**

Create `frontend/apiary-os/components/ArrowConfirmDialog.ts`:

```typescript
import { html, reactive } from "@arrow-js/core";

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
    : "bg-amber-600 text-white hover:bg-amber-700";

  return html`
    <div class="${() => props.isOpen() ? 'fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50' : 'hidden'}">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-sm mx-4 p-6">
        <h3 class="text-lg font-bold text-stone-800 mb-2">${props.title}</h3>
        <p class="text-sm text-stone-600 mb-6">${props.message}</p>
        <div class="flex justify-end gap-3">
          <button type="button" @click="${props.onCancel}"
            class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition">
            Cancel
          </button>
          <button type="button" @click="${props.onConfirm}"
            class="px-4 py-2 rounded-lg font-semibold ${confirmClass} transition shadow-sm">
            ${props.confirmLabel || "Confirm"}
          </button>
        </div>
      </div>
    </div>
  `;
}
```

**Step 7: Create ArrowEmptyState component**

Create `frontend/apiary-os/components/ArrowEmptyState.ts`:

```typescript
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

**Step 8: Create ArrowProgressRing component**

Create `frontend/apiary-os/components/ArrowProgressRing.ts`:

```typescript
import { html } from "@arrow-js/core";

export interface ArrowProgressRingProps {
  percent: number | (() => number);
  size?: number;
  strokeWidth?: number;
  color?: string;
  label?: string | (() => string);
}

export function ArrowProgressRing(props: ArrowProgressRingProps) {
  const size = props.size || 80;
  const stroke = props.strokeWidth || 6;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const center = size / 2;

  return html`
    <div class="inline-flex flex-col items-center gap-1">
      <svg width="${size}" height="${size}" class="transform -rotate-90">
        <circle cx="${center}" cy="${center}" r="${radius}"
          fill="none" stroke="#e7e5e4" stroke-width="${stroke}" />
        <circle cx="${center}" cy="${center}" r="${radius}"
          fill="none" stroke="${props.color || '#f59e0b'}" stroke-width="${stroke}"
          stroke-linecap="round"
          stroke-dasharray="${circumference}"
          stroke-dashoffset="${() => {
            const pct = typeof props.percent === 'function' ? props.percent() : props.percent;
            return circumference - (pct / 100) * circumference;
          }}"
          class="transition-all duration-500" />
      </svg>
      <span class="text-xs font-bold text-stone-700">${() => {
        const pct = typeof props.percent === 'function' ? props.percent() : props.percent;
        return Math.round(pct) + "%";
      }}</span>
      ${() => {
        const l = typeof props.label === 'function' ? props.label() : props.label;
        return l ? html`<span class="text-xs text-stone-400">${l}</span>` : html``;
      }}
    </div>
  `;
}
```

**Step 9: Run deno install to regenerate lock**

```bash
cd frontend/apiary-os && deno install
```

**Step 10: Commit**

```bash
git add frontend/apiary-os/deno.json frontend/apiary-os/components/Arrow*.ts
git commit -m "feat(apiary-os): add arrow.js dependency and shared arrow components"
```

---

### Task 2: Sidebar Navigation & App Layout

**Files:**
- Create: `frontend/apiary-os/islands/ArrowNavBar.tsx`
- Modify: `frontend/apiary-os/routes/_app.tsx`

**Step 1: Create ArrowNavBar island**

Create `frontend/apiary-os/islands/ArrowNavBar.tsx`:

```typescript
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";

interface NavItem {
  href: string;
  label: string;
  icon: string;
  group: "manage" | "insights";
}

const navItems: NavItem[] = [
  { href: "/", label: "Dashboard", icon: "📊", group: "manage" },
  { href: "/hives", label: "Hives", icon: "🐝", group: "manage" },
  { href: "/apiaries", label: "Apiaries", icon: "📍", group: "manage" },
  { href: "/calendar", label: "Calendar", icon: "📅", group: "manage" },
  { href: "/reports", label: "Reports", icon: "📈", group: "insights" },
  { href: "/financials", label: "Financials", icon: "💰", group: "insights" },
];

export default function ArrowNavBar({ currentPath }: { currentPath: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      mobileOpen: false,
    });

    const isActive = (href: string) => {
      if (href === "/") return currentPath === "/";
      return currentPath.startsWith(href);
    };

    const navLink = (item: NavItem) => html`
      <li>
        <a href="${item.href}"
          class="${isActive(item.href)
            ? "bg-amber-600/20 text-amber-300 shadow-sm"
            : "text-stone-400 hover:bg-stone-800 hover:text-stone-200"
          } flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150">
          <span class="text-lg">${item.icon}</span>
          <span class="hidden lg:inline">${item.label}</span>
        </a>
      </li>
    `;

    const manageItems = navItems.filter(n => n.group === "manage");
    const insightItems = navItems.filter(n => n.group === "insights");

    const template = html`
      <nav class="w-16 lg:w-60 min-h-screen bg-stone-900 text-stone-100 flex flex-col border-r border-stone-800 shrink-0 transition-all duration-200">
        <div class="px-3 lg:px-6 py-5 border-b border-stone-800">
          <h1 class="text-xl font-bold tracking-tight text-amber-400 hidden lg:block">
            Apiary OS
          </h1>
          <span class="text-xl lg:hidden block text-center">🐝</span>
          <p class="text-xs text-stone-500 mt-1 uppercase tracking-widest hidden lg:block">
            Colony Management
          </p>
        </div>

        <div class="flex-1 py-4 px-2 lg:px-3">
          <p class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block">Manage</p>
          <ul class="space-y-1 mb-6">
            ${manageItems.map(item => navLink(item))}
          </ul>
          <p class="text-[10px] text-stone-600 uppercase tracking-widest font-bold px-3 mb-2 hidden lg:block">Insights</p>
          <ul class="space-y-1">
            ${insightItems.map(item => navLink(item))}
          </ul>
        </div>

        <div class="px-3 lg:px-6 py-4 border-t border-stone-800">
          <p class="text-xs text-stone-600 hidden lg:block">Sovereign · Offline-First</p>
          <span class="text-xs text-stone-600 lg:hidden block text-center">●</span>
        </div>
      </nav>
    `;

    template(containerRef.current);
  }, [currentPath]);

  return <div ref={containerRef}></div>;
}
```

**Step 2: Update _app.tsx with sidebar layout**

Replace `frontend/apiary-os/routes/_app.tsx` content:

```typescript
import { define } from "../utils.ts";
import ToastProvider from "../islands/ToastProvider.tsx";
import ArrowNavBar from "../islands/ArrowNavBar.tsx";

export default define.page(function App({ Component, url }) {
  return (
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <meta name="theme-color" content="#1c1917" />
        <title>Apiary OS — farmOS</title>
        <link rel="stylesheet" href="/styles.css" />
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
        <style>
          {`
          body { font-family: 'Inter', system-ui, sans-serif; margin: 0; }
          @keyframes slideIn {
            from { opacity: 0; transform: translateX(20px); }
            to { opacity: 1; transform: translateX(0); }
          }
          @keyframes scaleIn {
            from { opacity: 0; transform: scale(0.95); }
            to { opacity: 1; transform: scale(1); }
          }
          @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
          }
          `}
        </style>
      </head>
      <body class="bg-stone-50 text-stone-900">
        <div class="flex min-h-screen">
          <ArrowNavBar currentPath={url.pathname} />
          <main class="flex-1 overflow-auto">
            <ToastProvider />
            <Component />
          </main>
        </div>
      </body>
    </html>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowNavBar.tsx frontend/apiary-os/routes/_app.tsx
git commit -m "feat(apiary-os): add arrow.js sidebar navigation and app layout"
```

---

### Task 3: Dashboard Page (Bento Grid)

**Files:**
- Create: `frontend/apiary-os/islands/ArrowDashboard.tsx`
- Modify: `frontend/apiary-os/routes/index.tsx`

**Step 1: Create ArrowDashboard island**

Create `frontend/apiary-os/islands/ArrowDashboard.tsx`:

This is the hero page — a bento grid dashboard with:
- 4 KPI cards (active hives, attention hives, honey yield, survival rate)
- Mite trend mini-table
- Upcoming tasks list
- Quick actions row
- Weather badge

The island follows the Preact-shell + Arrow-core pattern:

```typescript
import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import type {
  HiveSummary,
  SeasonalTask,
  ColonySurvivalReport,
  YieldReport,
} from "../utils/farmos-client.ts";

export default function ArrowDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      hives: [] as HiveSummary[],
      tasks: [] as SeasonalTask[],
      survival: null as ColonySurvivalReport | null,
      yieldReport: null as YieldReport | null,
      loading: true,
      error: null as string | null,
    });

    // Load all dashboard data in parallel
    const loadData = async () => {
      try {
        const { ApiaryReportsAPI } = await import("../utils/farmos-client.ts");
        const currentMonth = new Date().getMonth() + 1;
        const [hives, tasks, survival, yieldReport] = await Promise.allSettled([
          ApiaryReportsAPI.getAllHives(),
          ApiaryReportsAPI.getCalendar(currentMonth),
          ApiaryReportsAPI.getSurvivalReport(),
          ApiaryReportsAPI.getYieldReport(),
        ]);
        state.hives = hives.status === "fulfilled" ? hives.value : [];
        state.tasks = tasks.status === "fulfilled" ? tasks.value : [];
        state.survival = survival.status === "fulfilled" ? survival.value : null;
        state.yieldReport = yieldReport.status === "fulfilled" ? yieldReport.value : null;
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load dashboard";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const activeCount = () => state.hives.filter(h => h.status === "Active").length;
    const attentionCount = () => state.hives.filter(h => h.status === "Attention" || (h.miteCount && h.miteCount > 3)).length;
    const honeyYield = () => state.yieldReport ? state.yieldReport.totalHoneyLbs.toFixed(0) + " lbs" : "—";
    const survivalRate = () => state.survival ? Math.round(state.survival.survivalRate * 100) + "%" : "—";

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Dashboard</h1>
          <p class="text-stone-500 mt-1">Colony overview and daily check-in.</p>
        </header>

        ${() => state.loading ? html`
          <div class="flex items-center justify-center py-20">
            <div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"></div>
          </div>
        ` : html`
          <div>
            ${() => state.error ? html`
              <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">${state.error}</div>
            ` : html``}

            <!-- KPI Row -->
            <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
              ${ArrowKPICard({ label: "Active Hives", value: () => String(activeCount()), icon: "🐝", color: "amber" })}
              ${ArrowKPICard({ label: "Need Attention", value: () => String(attentionCount()), icon: "⚠", color: attentionCount() > 0 ? "red" : "stone" })}
              ${ArrowKPICard({ label: "Honey Yield", value: honeyYield, icon: "🍯", color: "emerald" })}
              ${ArrowKPICard({ label: "Survival Rate", value: survivalRate, icon: "📈", color: "violet" })}
            </div>

            <!-- Bento Grid: Hive Health + Upcoming Tasks -->
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
              <!-- Hive Health Summary -->
              <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Hive Health</h2>
                ${() => state.hives.length === 0
                  ? ArrowEmptyState({ icon: "🐝", title: "No hives yet", message: "Register your first hive to see health data here." })
                  : html`
                    <div class="space-y-3">
                      ${() => state.hives.map(hive => {
                        const miteLevel = (hive.miteCount || 0) <= 1 ? "healthy" : (hive.miteCount || 0) <= 3 ? "moderate" : "critical";
                        const barColor = miteLevel === "healthy" ? "bg-emerald-500" : miteLevel === "moderate" ? "bg-amber-500" : "bg-red-500";
                        const barWidth = Math.min(((hive.miteCount || 0) / 10) * 100, 100);
                        return html`
                          <div class="flex items-center gap-3">
                            <a href="/hives" class="text-sm font-medium text-stone-700 w-28 truncate hover:text-amber-600 transition">${hive.name}</a>
                            <div class="flex-1 bg-stone-100 rounded-full h-2">
                              <div class="${barColor} h-2 rounded-full transition-all" style="width: ${barWidth}%"></div>
                            </div>
                            <span class="text-xs font-bold text-stone-500 w-14 text-right">${hive.miteCount || 0}/100</span>
                          </div>
                        `;
                      })}
                    </div>
                  `
                }
              </div>

              <!-- Upcoming Tasks -->
              <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
                <div class="flex items-center justify-between mb-4">
                  <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider">Upcoming Tasks</h2>
                  <a href="/calendar" class="text-xs text-amber-600 font-semibold hover:text-amber-700 transition">View All</a>
                </div>
                ${() => state.tasks.length === 0
                  ? html`<p class="text-sm text-stone-400">No tasks this month.</p>`
                  : html`
                    <div class="space-y-2">
                      ${() => state.tasks.slice(0, 5).map(task => {
                        const priorityDot = task.priority === "High" ? "bg-red-500" : task.priority === "Medium" ? "bg-amber-500" : "bg-emerald-500";
                        return html`
                          <div class="flex items-start gap-3 py-2 border-b border-stone-50 last:border-0">
                            <span class="w-2 h-2 rounded-full ${priorityDot} mt-1.5 shrink-0"></span>
                            <div class="flex-1 min-w-0">
                              <p class="text-sm font-medium text-stone-700">${task.title}</p>
                              <p class="text-xs text-stone-400 truncate">${task.description}</p>
                            </div>
                            <span class="text-xs text-stone-400 shrink-0">${task.category}</span>
                          </div>
                        `;
                      })}
                    </div>
                  `
                }
              </div>
            </div>

            <!-- Quick Actions -->
            <div class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6">
              <h2 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">Quick Actions</h2>
              <div class="flex flex-wrap gap-3">
                <a href="/hives" class="px-4 py-2.5 bg-amber-50 text-amber-700 rounded-xl text-sm font-semibold hover:bg-amber-100 transition border border-amber-100 flex items-center gap-2">
                  <span>🐝</span> Manage Hives
                </a>
                <a href="/apiaries" class="px-4 py-2.5 bg-teal-50 text-teal-700 rounded-xl text-sm font-semibold hover:bg-teal-100 transition border border-teal-100 flex items-center gap-2">
                  <span>📍</span> Apiary Locations
                </a>
                <a href="/reports" class="px-4 py-2.5 bg-violet-50 text-violet-700 rounded-xl text-sm font-semibold hover:bg-violet-100 transition border border-violet-100 flex items-center gap-2">
                  <span>📊</span> View Reports
                </a>
                <a href="/financials" class="px-4 py-2.5 bg-emerald-50 text-emerald-700 rounded-xl text-sm font-semibold hover:bg-emerald-100 transition border border-emerald-100 flex items-center gap-2">
                  <span>💰</span> Financials
                </a>
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

**Step 2: Update routes/index.tsx**

Replace `frontend/apiary-os/routes/index.tsx`:

```typescript
import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import ArrowDashboard from "../islands/ArrowDashboard.tsx";

export default define.page(function Home() {
  return (
    <div>
      <Head>
        <title>Dashboard — Apiary OS</title>
      </Head>
      <ArrowDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowDashboard.tsx frontend/apiary-os/routes/index.tsx
git commit -m "feat(apiary-os): add bento grid dashboard with KPI cards and Arrow.js"
```

---

### Task 4: Hive Manager Page

**Files:**
- Create: `frontend/apiary-os/islands/ArrowHiveManager.tsx`
- Modify: `frontend/apiary-os/routes/hives/index.tsx`

**Step 1: Create ArrowHiveManager island**

Create `frontend/apiary-os/islands/ArrowHiveManager.tsx`:

This is the most complex island — it includes:
- Filter bar (All / Active / Attention / Resting) with counts
- Hive card grid with inline mite bar and status badges
- Slide-out detail panel (Arrow.js reactive)
- Inspect form, treat form, harvest action — all Arrow.js
- Create hive modal

The full implementation should:
1. Initialize `state = reactive({ hives, selectedId, filter, showCreateModal, showTreatForm, ... })`
2. Load hives via `ApiaryReportsAPI.getAllHives()` on mount
3. Filter bar buttons that set `state.filter` and reactively update the grid
4. Clicking a hive card sets `state.selectedId` and opens the slide-out sidebar
5. Sidebar shows vital stats, mite count with color coding, queen status
6. Quick action buttons: Log Inspection, Record Treatment, Record Harvest
7. Each action opens an inline form within the sidebar (Arrow.js forms with Zod validation)
8. Create Hive modal opens on "+ New Hive" button click
9. All forms call the appropriate `ApiaryAPI` methods and show toasts

**Key patterns to follow:**
- Use `showToast()` from `../utils/toastState.ts` for feedback
- Use Zod schemas from `../utils/schemas.ts` for validation
- Dynamic import `await import("../utils/farmos-client.ts")` for API calls (code-splitting)
- Use string concatenation (not template literals) for dynamic strings inside `html` templates
- Use `html``\`` as empty template fallback for conditional rendering

**Step 2: Update routes/hives/index.tsx**

```typescript
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowHiveManager from "../../islands/ArrowHiveManager.tsx";

export default define.page(function HivesPage() {
  return (
    <div>
      <Head>
        <title>Hives — Apiary OS</title>
      </Head>
      <ArrowHiveManager />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowHiveManager.tsx frontend/apiary-os/routes/hives/index.tsx
git commit -m "feat(apiary-os): add arrow.js hive manager with filter bar and detail panel"
```

---

### Task 5: Apiaries Page

**Files:**
- Create: `frontend/apiary-os/islands/ArrowApiaryManager.tsx`
- Modify: `frontend/apiary-os/routes/apiaries/index.tsx`

**Step 1: Create ArrowApiaryManager island**

This island shows:
- Apiary location cards with capacity progress bars
- Hive assignments per apiary
- Create apiary modal (Arrow.js form with Zod validation)
- "Move Hive Here" action per apiary

Data comes from `ApiaryReportsAPI.getAllApiaries()` + `ApiaryReportsAPI.getAllHives()`.

**Step 2: Update routes/apiaries/index.tsx**

```typescript
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowApiaryManager from "../../islands/ArrowApiaryManager.tsx";

export default define.page(function ApiariesPage() {
  return (
    <div>
      <Head>
        <title>Apiaries — Apiary OS</title>
      </Head>
      <ArrowApiaryManager />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowApiaryManager.tsx frontend/apiary-os/routes/apiaries/index.tsx
git commit -m "feat(apiary-os): add arrow.js apiary location manager with capacity tracking"
```

---

### Task 6: Reports Dashboard (Bento Grid)

**Files:**
- Create: `frontend/apiary-os/islands/ArrowReportsDashboard.tsx`
- Modify: `frontend/apiary-os/routes/reports/index.tsx`

**Step 1: Create ArrowReportsDashboard island**

Replaces the tabbed reports with a 2x2 bento grid showing all 4 report panels simultaneously:
- Top-left: Mite Trends (bar chart + table)
- Top-right: Yield Report (stat cards + product breakdown bars)
- Bottom-left: Colony Survival (progress ring + breakdown)
- Bottom-right: Weather Correlation (compact table)

All 4 data fetches happen in parallel via `Promise.allSettled()`.

**Step 2: Update route**

```typescript
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowReportsDashboard from "../../islands/ArrowReportsDashboard.tsx";

export default define.page(function ReportsPage() {
  return (
    <div>
      <Head>
        <title>Reports — Apiary OS</title>
      </Head>
      <ArrowReportsDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowReportsDashboard.tsx frontend/apiary-os/routes/reports/index.tsx
git commit -m "feat(apiary-os): add arrow.js reports bento grid replacing tab UI"
```

---

### Task 7: Financial Dashboard (Unified)

**Files:**
- Create: `frontend/apiary-os/islands/ArrowFinancialDashboard.tsx`
- Modify: `frontend/apiary-os/routes/financials/index.tsx`

**Step 1: Create ArrowFinancialDashboard island**

Replaces tabbed financials with unified view:
- 3 KPI cards at top (Expenses, Revenue, Net Profit)
- Side-by-side: Expense breakdown (horizontal bars) + Revenue by product (horizontal bars)
- Combined transaction list at bottom (both expenses and revenue sorted by date)

All 3 data fetches in parallel.

**Step 2: Update route**

```typescript
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowFinancialDashboard from "../../islands/ArrowFinancialDashboard.tsx";

export default define.page(function FinancialsPage() {
  return (
    <div>
      <Head>
        <title>Financials — Apiary OS</title>
      </Head>
      <ArrowFinancialDashboard />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowFinancialDashboard.tsx frontend/apiary-os/routes/financials/index.tsx
git commit -m "feat(apiary-os): add arrow.js unified financial dashboard"
```

---

### Task 8: Calendar Page (Month Strip)

**Files:**
- Create: `frontend/apiary-os/islands/ArrowTaskCalendar.tsx`
- Modify: `frontend/apiary-os/routes/calendar/index.tsx`

**Step 1: Create ArrowTaskCalendar island**

Redesigned calendar with:
- Horizontal month strip with current month visually emphasized
- Task count badge per month
- Cards grouped by category with priority indicators
- High priority tasks first (Serial Position Effect)

**Step 2: Update route**

```typescript
import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowTaskCalendar from "../../islands/ArrowTaskCalendar.tsx";

export default define.page(function CalendarPage() {
  return (
    <div>
      <Head>
        <title>Calendar — Apiary OS</title>
      </Head>
      <ArrowTaskCalendar />
    </div>
  );
});
```

**Step 3: Commit**

```bash
git add frontend/apiary-os/islands/ArrowTaskCalendar.tsx frontend/apiary-os/routes/calendar/index.tsx
git commit -m "feat(apiary-os): add arrow.js task calendar with month strip"
```

---

### Task 9: Cleanup & Docker Build Verification

**Files:**
- Modify: `frontend/apiary-os/deno.json` (verify @arrow-js/core is present)
- Verify: `frontend/apiary-os/vite.config.ts` (no changes needed)
- Delete or keep old Preact components (keep for reference, or delete if desired)

**Step 1: Regenerate deno.lock**

```bash
cd frontend/apiary-os && rm deno.lock && deno install
```

**Step 2: Test local build**

```bash
cd frontend/apiary-os && deno task build
```

Expected: Build succeeds with no errors about Arrow.js or nested backticks.

**Step 3: Docker build test**

```bash
docker compose build apiary-os-ui --no-cache
```

Expected: Docker image builds successfully.

**Step 4: Docker run test**

```bash
docker compose up -d apiary-os-ui
```

Navigate to `http://localhost:8001` and verify:
- Sidebar navigation visible
- Dashboard loads with KPI cards
- Each page route works
- No console errors about Arrow.js or hydration

**Step 5: Final commit**

```bash
git add -A frontend/apiary-os/
git commit -m "chore(apiary-os): regenerate lock and verify arrow.js migration build"
```

---

## UX Laws Applied

| Law | Where Applied |
|-----|---------------|
| **Miller's Law** (7±2) | Dashboard KPI row: exactly 4 cards |
| **Hick's Law** | Filter bar on hives page reduces options; no tabs on reports |
| **Von Restorff Effect** | Attention hives get amber border + warning icon; active nav item highlighted |
| **Serial Position Effect** | Critical metrics (active hives, alerts) in first and last KPI positions |
| **Law of Common Region** | Sidebar nav groups (MANAGE / INSIGHTS); bento grid card boundaries |
| **Law of Proximity** | Related stats grouped within cards; action buttons grouped together |
| **Doherty Threshold** (<400ms) | All data loaded via parallel `Promise.allSettled()`; optimistic UI |
| **Pareto Principle** | Reports show all 4 panels at once (80% of users want the overview) |
| **Aesthetic-Usability Effect** | Rounded cards, subtle shadows, warm amber palette, Inter font |
| **Fitts's Law** | Large click targets on hive cards and nav items; CTA buttons prominent |

## Design Tokens

| Token | Value | Usage |
|-------|-------|-------|
| Border radius | `rounded-2xl` (16px) | Cards, panels |
| Card shadow | `shadow-sm` → `shadow-md` on hover | Depth hierarchy |
| Sidebar width | `w-60` (240px) desktop, `w-16` (64px) mobile | Responsive nav |
| KPI card height | Auto, ~110px | Consistent row height |
| Font | Inter 400/500/600/700/800 | All text |
| Spacing grid | 4px base (Tailwind default) | Consistent rhythm |
