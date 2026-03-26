import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowProgressRing } from "../components/ArrowProgressRing.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import type {
  MiteTrendPoint,
  YieldReport,
  ColonySurvivalReport,
  WeatherCorrelation,
} from "../utils/farmos-client.ts";

export default function ArrowReportsDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      miteTrends: [] as MiteTrendPoint[],
      yieldReport: null as YieldReport | null,
      survival: null as ColonySurvivalReport | null,
      weather: [] as WeatherCorrelation[],
      loading: true,
    });

    const loadData = async () => {
      try {
        const { ApiaryReportsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        const [mites, yld, surv, weather] = await Promise.allSettled([
          ApiaryReportsAPI.getMiteTrends(),
          ApiaryReportsAPI.getYieldReport(),
          ApiaryReportsAPI.getSurvivalReport(),
          ApiaryReportsAPI.getWeatherCorrelations(),
        ]);
        state.miteTrends =
          mites.status === "fulfilled" ? mites.value : [];
        state.yieldReport =
          yld.status === "fulfilled" ? yld.value : null;
        state.survival =
          surv.status === "fulfilled" ? surv.value : null;
        state.weather =
          weather.status === "fulfilled" ? weather.value : [];
      } catch {
        // silent
      } finally {
        state.loading = false;
      }
    };

    loadData();

    // Mite status helper
    const miteStatus = (count: number) => {
      if (count <= 1)
        return {
          label: "Low",
          cls: "text-emerald-700 bg-emerald-50",
        };
      if (count <= 3)
        return {
          label: "Moderate",
          cls: "text-amber-700 bg-amber-50",
        };
      return { label: "HIGH", cls: "text-red-700 bg-red-50 font-bold" };
    };

    // Panel: Mite Trends
    const miteTrendPanel = () => html`
      <div
        class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
      >
        <h2
          class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <span>\uD83D\uDD2C</span> Mite Trends
        </h2>
        ${() =>
          state.miteTrends.length === 0
            ? html`<p class="text-sm text-stone-400">
                No mite data recorded yet.
              </p>`
            : html`
                <div class="overflow-x-auto">
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b border-stone-100">
                        <th
                          class="text-left py-2 text-xs text-stone-500 font-medium"
                        >
                          Hive
                        </th>
                        <th
                          class="text-right py-2 text-xs text-stone-500 font-medium"
                        >
                          Mites
                        </th>
                        <th
                          class="text-right py-2 text-xs text-stone-500 font-medium"
                        >
                          Status
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      ${() =>
                        state.miteTrends.map((m) => {
                          const s = miteStatus(m.miteCount);
                          return html`
                            <tr class="border-b border-stone-50">
                              <td class="py-2 text-stone-700 font-medium">
                                ${m.hiveName}
                              </td>
                              <td
                                class="py-2 text-right font-bold text-stone-800"
                              >
                                ${m.miteCount}
                              </td>
                              <td class="py-2 text-right">
                                <span
                                  class="${s.cls} text-xs px-2 py-0.5 rounded-full"
                                  >${s.label}</span
                                >
                              </td>
                            </tr>
                          `;
                        })}
                    </tbody>
                  </table>
                </div>
              `}
      </div>
    `;

    // Panel: Yield Report
    const yieldPanel = () => html`
      <div
        class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
      >
        <h2
          class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <span>\uD83C\uDF6F</span> Yield Report
        </h2>
        ${() =>
          !state.yieldReport
            ? html`<p class="text-sm text-stone-400">
                No yield data yet.
              </p>`
            : html`
                <div class="grid grid-cols-3 gap-3 mb-4">
                  <div
                    class="bg-amber-50 rounded-xl p-3 text-center border border-amber-100"
                  >
                    <p class="text-xl font-extrabold text-amber-700">
                      ${() => state.yieldReport!.totalHoneyLbs.toFixed(0)}
                    </p>
                    <p
                      class="text-[10px] text-amber-600 uppercase tracking-wider"
                    >
                      Honey lbs
                    </p>
                  </div>
                  <div
                    class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
                  >
                    <p class="text-xl font-extrabold text-stone-700">
                      ${() => state.yieldReport!.totalWaxLbs.toFixed(0)}
                    </p>
                    <p
                      class="text-[10px] text-stone-500 uppercase tracking-wider"
                    >
                      Wax lbs
                    </p>
                  </div>
                  <div
                    class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
                  >
                    <p class="text-xl font-extrabold text-stone-700">
                      ${() => state.yieldReport!.harvestCount}
                    </p>
                    <p
                      class="text-[10px] text-stone-500 uppercase tracking-wider"
                    >
                      Harvests
                    </p>
                  </div>
                </div>
                ${() => {
                  const byProduct = state.yieldReport!.byProduct;
                  const entries = Object.entries(byProduct);
                  if (entries.length === 0) return html``;
                  const total = entries.reduce(
                    (sum, [, v]) => sum + v,
                    0,
                  );
                  return html`
                    <div class="space-y-2">
                      ${entries.map(([name, val]) => {
                        const pct =
                          total > 0
                            ? Math.round((val / total) * 100)
                            : 0;
                        return html`
                          <div>
                            <div
                              class="flex items-center justify-between text-xs text-stone-600 mb-0.5"
                            >
                              <span>${name}</span>
                              <span class="font-bold">${pct}%</span>
                            </div>
                            <div
                              class="bg-stone-100 rounded-full h-2"
                            >
                              <div
                                class="bg-amber-500 h-2 rounded-full transition-all"
                                style="width: ${pct}%"
                              ></div>
                            </div>
                          </div>
                        `;
                      })}
                    </div>
                  `;
                }}
              `}
      </div>
    `;

    // Panel: Colony Survival
    const survivalPanel = () => html`
      <div
        class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
      >
        <h2
          class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <span>\uD83D\uDCC8</span> Colony Survival
        </h2>
        ${() =>
          !state.survival
            ? html`<p class="text-sm text-stone-400">
                No survival data yet.
              </p>`
            : html`
                <div class="flex items-center gap-6">
                  ${ArrowProgressRing({
                    percent: () =>
                      Math.round(state.survival!.survivalRate * 100),
                    size: 100,
                    strokeWidth: 8,
                    color: "#f59e0b",
                    label: "Survival",
                  })}
                  <div class="space-y-2 flex-1">
                    <div
                      class="flex items-center justify-between text-sm"
                    >
                      <span class="text-stone-500"
                        >\u25CF Active</span
                      >
                      <span class="font-bold text-stone-700"
                        >${() => state.survival!.currentlyActive}</span
                      >
                    </div>
                    <div
                      class="flex items-center justify-between text-sm"
                    >
                      <span class="text-stone-500"
                        >\u25CF Dead</span
                      >
                      <span class="font-bold text-stone-700"
                        >${() => state.survival!.dead}</span
                      >
                    </div>
                    <div
                      class="flex items-center justify-between text-sm"
                    >
                      <span class="text-stone-500"
                        >\u25CF Swarmed</span
                      >
                      <span class="font-bold text-stone-700"
                        >${() => state.survival!.swarmed}</span
                      >
                    </div>
                    <div
                      class="flex items-center justify-between text-sm pt-2 border-t border-stone-100"
                    >
                      <span class="text-stone-500 font-medium"
                        >Total Created</span
                      >
                      <span class="font-bold text-stone-700"
                        >${() => state.survival!.totalCreated}</span
                      >
                    </div>
                  </div>
                </div>
              `}
      </div>
    `;

    // Panel: Weather Correlation
    const weatherPanel = () => html`
      <div
        class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
      >
        <h2
          class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4 flex items-center gap-2"
        >
          <span>\u2600\uFE0F</span> Weather Correlation
        </h2>
        ${() =>
          state.weather.length === 0
            ? html`<p class="text-sm text-stone-400">
                No weather data yet.
              </p>`
            : html`
                <div class="overflow-x-auto">
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b border-stone-100">
                        <th
                          class="text-left py-2 text-xs text-stone-500 font-medium"
                        >
                          Date
                        </th>
                        <th
                          class="text-right py-2 text-xs text-stone-500 font-medium"
                        >
                          Temp
                        </th>
                        <th
                          class="text-right py-2 text-xs text-stone-500 font-medium"
                        >
                          Humidity
                        </th>
                        <th
                          class="text-right py-2 text-xs text-stone-500 font-medium"
                        >
                          Mites
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      ${() =>
                        state.weather.slice(0, 8).map(
                          (w) => html`
                            <tr class="border-b border-stone-50">
                              <td class="py-2 text-stone-700">
                                ${w.date}
                              </td>
                              <td
                                class="py-2 text-right text-stone-700"
                              >
                                ${w.tempF}\u00B0F
                              </td>
                              <td
                                class="py-2 text-right text-stone-700"
                              >
                                ${w.humidity}%
                              </td>
                              <td
                                class="py-2 text-right font-bold text-stone-800"
                              >
                                ${w.miteCount ?? "\u2014"}
                              </td>
                            </tr>
                          `,
                        )}
                    </tbody>
                  </table>
                </div>
              `}
      </div>
    `;

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1
            class="text-3xl font-extrabold text-stone-800 tracking-tight"
          >
            Reports & Analytics
          </h1>
          <p class="text-stone-500 mt-1">
            Data-driven insights from inspections and harvests.
          </p>
        </header>

        ${() =>
          state.loading
            ? html`
                <div class="flex items-center justify-center py-20">
                  <div
                    class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"
                  ></div>
                </div>
              `
            : html`
                <div
                  class="grid grid-cols-1 lg:grid-cols-2 gap-6"
                >
                  ${miteTrendPanel()} ${yieldPanel()}
                  ${survivalPanel()} ${weatherPanel()}
                </div>
              `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
