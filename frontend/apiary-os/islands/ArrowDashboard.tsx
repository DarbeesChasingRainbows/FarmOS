import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import type {
  ColonySurvivalReport,
  HiveSummary,
  SeasonalTask,
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

    const loadData = async () => {
      try {
        const { ApiaryReportsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        const currentMonth = new Date().getMonth() + 1;
        const [hives, tasks, survival, yieldReport] = await Promise.allSettled([
          ApiaryReportsAPI.getAllHives(),
          ApiaryReportsAPI.getCalendar(currentMonth),
          ApiaryReportsAPI.getSurvivalReport(),
          ApiaryReportsAPI.getYieldReport(),
        ]);
        state.hives = hives.status === "fulfilled" ? (hives.value ?? []) : [];
        state.tasks = tasks.status === "fulfilled" ? (tasks.value ?? []) : [];
        state.survival = survival.status === "fulfilled"
          ? survival.value
          : null;
        state.yieldReport = yieldReport.status === "fulfilled"
          ? yieldReport.value
          : null;
      } catch (err: unknown) {
        state.error = err instanceof Error
          ? err.message
          : "Failed to load dashboard";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const activeCount = () =>
      state.hives.filter((h) => h.status === "Active").length;
    const attentionCount = () =>
      state.hives.filter(
        (h) => h.status === "Attention" || (h.miteCount && h.miteCount > 3),
      ).length;
    const honeyYield = () =>
      state.yieldReport
        ? state.yieldReport.totalHoneyLbs.toFixed(0) + " lbs"
        : "\u2014";
    const survivalRate = () =>
      state.survival
        ? Math.round(state.survival.survivalRate * 100) + "%"
        : "\u2014";

    // Hive health bar row
    const hiveHealthRow = (hive: HiveSummary) => {
      const miteVal = hive.miteCount || 0;
      const barColor = miteVal <= 1
        ? "bg-emerald-500"
        : miteVal <= 3
        ? "bg-amber-500"
        : "bg-red-500";
      const barWidth = Math.min((miteVal / 10) * 100, 100);
      return html`
        <div class="flex items-center gap-3">
          <a
            href="/hives"
            class="text-sm font-medium text-stone-700 w-28 truncate hover:text-amber-600 transition"
          >${hive.name}</a>
          <div class="flex-1 bg-stone-100 rounded-full h-2">
            <div
              class="${barColor} h-2 rounded-full transition-all"
              style="width: ${barWidth}%"
            >
            </div>
          </div>
          <span class="text-xs font-bold text-stone-500 w-14 text-right"
          >${miteVal}/100</span>
        </div>
      `;
    };

    // Task row
    const taskRow = (task: SeasonalTask) => {
      const priorityDot = task.priority === "High"
        ? "bg-red-500"
        : task.priority === "Medium"
        ? "bg-amber-500"
        : "bg-emerald-500";
      return html`
        <div
          class="flex items-start gap-3 py-2 border-b border-stone-50 last:border-0"
        >
          <span
            class="w-2 h-2 rounded-full ${priorityDot} mt-1.5 shrink-0"
          ></span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-stone-700">${task.title}</p>
            <p class="text-xs text-stone-400 truncate">${task.description}</p>
          </div>
          <span class="text-xs text-stone-400 shrink-0">${task.category}</span>
        </div>
      `;
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1
            class="text-3xl font-extrabold text-stone-800 tracking-tight"
          >
            Dashboard
          </h1>
          <p class="text-stone-500 mt-1">
            Colony overview and daily check-in.
          </p>
        </header>

        ${() =>
          state.loading
            ? html`
              <div class="flex items-center justify-center py-20">
                <div
                  class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"
                >
                </div>
              </div>
            `
            : html`
              <div>
                ${() =>
                  state.error
                    ? html`
                      <div
                        class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm"
                      >
                        ${state.error}
                      </div>
                    `
                    : html`

                    `}

                <!-- KPI Row -->
                <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                  ${ArrowKPICard({
                    label: "Active Hives",
                    value: () => String(activeCount()),
                    icon: "\uD83D\uDC1D",
                    color: "amber",
                  })} ${ArrowKPICard({
                    label: "Need Attention",
                    value: () => String(attentionCount()),
                    icon: "\u26A0\uFE0F",
                    color: "red",
                  })} ${ArrowKPICard({
                    label: "Honey Yield",
                    value: honeyYield,
                    icon: "\uD83C\uDF6F",
                    color: "emerald",
                  })} ${ArrowKPICard({
                    label: "Survival Rate",
                    value: survivalRate,
                    icon: "\uD83D\uDCC8",
                    color: "violet",
                  })}
                </div>

                <!-- Bento Grid: Hive Health + Upcoming Tasks -->
                <div
                  class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6"
                >
                  <!-- Hive Health Summary -->
                  <div
                    class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                  >
                    <h2
                      class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                    >
                      Hive Health
                    </h2>
                    ${() =>
                      state.hives.length === 0
                        ? ArrowEmptyState({
                          icon: "\uD83D\uDC1D",
                          title: "No hives yet",
                          message:
                            "Register your first hive to see health data here.",
                        })
                        : html`
                          <div class="space-y-3">
                            ${() =>
                              state.hives.map((hive) => hiveHealthRow(hive))}
                          </div>
                        `}
                  </div>

                  <!-- Upcoming Tasks -->
                  <div
                    class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                  >
                    <div
                      class="flex items-center justify-between mb-4"
                    >
                      <h2
                        class="text-sm font-bold text-stone-800 uppercase tracking-wider"
                      >
                        Upcoming Tasks
                      </h2>
                      <a
                        href="/calendar"
                        class="text-xs text-amber-600 font-semibold hover:text-amber-700 transition"
                      >View All</a>
                    </div>
                    ${() =>
                      state.tasks.length === 0
                        ? html`
                          <p class="text-sm text-stone-400">
                            No tasks this month.
                          </p>
                        `
                        : html`
                          <div class="space-y-2">
                            ${() =>
                              state.tasks
                                .slice(0, 5)
                                .map((task) => taskRow(task))}
                          </div>
                        `}
                  </div>
                </div>

                <!-- Quick Actions -->
                <div
                  class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                >
                  <h2
                    class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                  >
                    Quick Actions
                  </h2>
                  <div class="flex flex-wrap gap-3">
                    <a
                      href="/hives"
                      class="px-4 py-2.5 bg-amber-50 text-amber-700 rounded-xl text-sm font-semibold hover:bg-amber-100 transition border border-amber-100 flex items-center gap-2"
                    >
                      <span>\\uD83D\\uDC1D</span> Manage Hives
                    </a>
                    <a
                      href="/apiaries"
                      class="px-4 py-2.5 bg-teal-50 text-teal-700 rounded-xl text-sm font-semibold hover:bg-teal-100 transition border border-teal-100 flex items-center gap-2"
                    >
                      <span>\\uD83D\\uDCCD</span> Apiary Locations
                    </a>
                    <a
                      href="/reports"
                      class="px-4 py-2.5 bg-violet-50 text-violet-700 rounded-xl text-sm font-semibold hover:bg-violet-100 transition border border-violet-100 flex items-center gap-2"
                    >
                      <span>\\uD83D\\uDCCA</span> View Reports
                    </a>
                    <a
                      href="/financials"
                      class="px-4 py-2.5 bg-emerald-50 text-emerald-700 rounded-xl text-sm font-semibold hover:bg-emerald-100 transition border border-emerald-100 flex items-center gap-2"
                    >
                      <span>\\uD83D\\uDCB0</span> Financials
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
