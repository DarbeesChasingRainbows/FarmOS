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
        state.batches =
          batchResult.status === "fulfilled"
            ? (batchResult.value ?? [])
            : [];
      } catch (err: unknown) {
        state.error =
          err instanceof Error ? err.message : "Failed to load dashboard";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    // KPI computed values
    const activeCount = () => state.batches.length;
    const attentionCount = () =>
      state.batches.filter((b) => !b.isSafe).length;
    const avgPH = () => {
      const withPH = state.batches.filter((b) => b.currentPH !== null);
      if (withPH.length === 0) return "\u2014";
      const sum = withPH.reduce((acc, b) => acc + (b.currentPH ?? 0), 0);
      return (sum / withPH.length).toFixed(1);
    };
    const safeCount = () =>
      state.batches.filter((b) => b.isSafe).length;

    // pH color logic
    const phColor = (ph: number | null) => {
      if (ph === null) return "text-stone-400";
      if (ph <= 3.5) return "text-emerald-600";
      if (ph <= 4.2) return "text-amber-600";
      return "text-red-600";
    };

    // Batch row
    const batchRow = (batch: ActiveFermentationMonitorDto) => {
      const safetyBadge = batch.isSafe
        ? "bg-emerald-50 text-emerald-700 border-emerald-100"
        : "bg-red-50 text-red-700 border-red-100";
      const safetyLabel = batch.isSafe ? "Safe" : "Unsafe";
      return html`
        <div
          class="flex items-center gap-3 py-3 border-b border-stone-50 last:border-0"
        >
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-stone-700 truncate">
              ${batch.batchCode}
            </p>
            <p class="text-xs text-stone-400 truncate">
              ${batch.productType} &middot; ${batch.phase}
            </p>
          </div>
          <span
            class="text-sm font-bold ${phColor(batch.currentPH)} tabular-nums"
          >
            ${batch.currentPH !== null ? batch.currentPH.toFixed(1) : "\u2014"}
          </span>
          <span
            class="text-xs font-semibold px-2 py-0.5 rounded-full border ${safetyBadge}"
          >
            ${safetyLabel}
          </span>
        </div>
      `;
    };

    // Quick action link
    const quickAction = (
      href: string,
      label: string,
      icon: string,
      bgClass: string,
      textClass: string,
      borderClass: string,
    ) => html`
      <a
        href="${href}"
        class="px-4 py-3 ${bgClass} ${textClass} rounded-xl text-sm font-semibold hover:opacity-80 transition border ${borderClass} flex items-center gap-2"
      >
        <span>${icon}</span> ${label}
      </a>
    `;

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1
            class="text-3xl font-extrabold text-stone-800 tracking-tight"
          >
            Dashboard
          </h1>
          <p class="text-stone-500 mt-1">
            Fermentation overview and quick actions.
          </p>
        </header>

        ${() =>
          state.loading
            ? html`
                <div class="flex items-center justify-center py-20">
                  <div
                    class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"
                  ></div>
                </div>
              `
            : html`
                <div>
                  ${() =>
                    state.error
                      ? html`<div
                          class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm"
                        >
                          ${state.error}
                        </div>`
                      : html``}

                  <!-- KPI Row -->
                  <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                    ${ArrowKPICard({
                      label: "Active Batches",
                      value: () => String(activeCount()),
                      icon: "\uD83E\uDDEA",
                      color: "orange",
                    })}
                    ${ArrowKPICard({
                      label: "Need Attention",
                      value: () => String(attentionCount()),
                      icon: "\u26A0\uFE0F",
                      color: "red",
                    })}
                    ${ArrowKPICard({
                      label: "Avg pH",
                      value: avgPH,
                      icon: "\uD83E\uDDEA",
                      color: "orange",
                    })}
                    ${ArrowKPICard({
                      label: "Safe Batches",
                      value: () => String(safeCount()),
                      icon: "\u2705",
                      color: "emerald",
                    })}
                  </div>

                  <!-- Bento Grid: Active Fermentations + Quick Actions -->
                  <div
                    class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6"
                  >
                    <!-- Active Fermentations -->
                    <div
                      class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                    >
                      <div
                        class="flex items-center justify-between mb-4"
                      >
                        <h2
                          class="text-sm font-bold text-stone-800 uppercase tracking-wider"
                        >
                          Active Fermentations
                        </h2>
                        <a
                          href="/batches"
                          class="text-xs text-orange-600 font-semibold hover:text-orange-700 transition"
                          >View All</a
                        >
                      </div>
                      ${() =>
                        state.batches.length === 0
                          ? html`<p class="text-sm text-stone-400">
                              No active fermentations.
                            </p>`
                          : html`
                              <div>
                                ${() =>
                                  state.batches.map((batch) =>
                                    batchRow(batch),
                                  )}
                              </div>
                            `}
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
                      <div class="grid grid-cols-2 gap-3">
                        ${quickAction(
                          "/batches",
                          "Batches",
                          "\uD83E\uDDEA",
                          "bg-orange-50",
                          "text-orange-700",
                          "border-orange-100",
                        )}
                        ${quickAction(
                          "/cultures",
                          "Cultures",
                          "\uD83E\uDDA0",
                          "bg-violet-50",
                          "text-violet-700",
                          "border-violet-100",
                        )}
                        ${quickAction(
                          "/batches",
                          "Kombucha",
                          "\uD83C\uDF75",
                          "bg-teal-50",
                          "text-teal-700",
                          "border-teal-100",
                        )}
                        ${quickAction(
                          "/compliance",
                          "Compliance",
                          "\uD83D\uDCCB",
                          "bg-sky-50",
                          "text-sky-700",
                          "border-sky-100",
                        )}
                        ${quickAction(
                          "/iot",
                          "IoT",
                          "\uD83D\uDCE1",
                          "bg-emerald-50",
                          "text-emerald-700",
                          "border-emerald-100",
                        )}
                        ${quickAction(
                          "/iot/zones",
                          "Freeze Dryer",
                          "\u2744\uFE0F",
                          "bg-sky-50",
                          "text-sky-700",
                          "border-sky-100",
                        )}
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
