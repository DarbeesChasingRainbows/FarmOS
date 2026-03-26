import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import type { ActiveFermentationMonitorDto } from "../utils/farmos-client.ts";

type PhaseFilter = "All" | "Primary" | "Secondary" | "Bottled" | "Complete";

const PHASE_FILTERS: PhaseFilter[] = [
  "All",
  "Primary",
  "Secondary",
  "Bottled",
  "Complete",
];

const TEA_TYPES = ["Black", "Green", "Oolong", "White"] as const;

function generateBatchCode(): string {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, "0");
  return `KB-${yyyy}-${mm}`;
}

export default function ArrowKombuchaDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      batches: [] as ActiveFermentationMonitorDto[],
      loading: true,
      error: null as string | null,
      phaseFilter: "All" as PhaseFilter,
      showNewForm: false,
      newBatchCode: generateBatchCode(),
      newTeaType: "Black" as string,
      newSugarGrams: "200",
      submitting: false,
    });

    const loadData = async () => {
      try {
        const { FermentationAPI } = await import("../utils/farmos-client.ts");
        const all = (await FermentationAPI.getActiveMonitoring()) ?? [];
        state.batches = all.filter((b) => b.productType === "Kombucha");
      } catch (err: unknown) {
        state.error =
          err instanceof Error ? err.message : "Failed to load kombucha data";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    // ─── Derived counts ───────────────────────────────────────────
    const totalCount = () => state.batches.length;
    const primaryCount = () =>
      state.batches.filter((b) => b.phase === "Primary").length;
    const secondaryCount = () =>
      state.batches.filter((b) => b.phase === "Secondary").length;
    const alertCount = () => state.batches.filter((b) => !b.isSafe).length;

    const phaseCount = (phase: PhaseFilter) => {
      if (phase === "All") return totalCount();
      return state.batches.filter((b) => b.phase === phase).length;
    };

    const filteredBatches = () => {
      if (state.phaseFilter === "All") return state.batches;
      return state.batches.filter((b) => b.phase === state.phaseFilter);
    };

    // ─── New batch submission ─────────────────────────────────────
    const handleSubmit = async () => {
      state.submitting = true;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.startKombucha({
          batchCode: state.newBatchCode,
          scobyCultureId: "",
          teaType: state.newTeaType,
          sugarGrams: Number(state.newSugarGrams),
        });
        state.showNewForm = false;
        state.newBatchCode = generateBatchCode();
        state.newTeaType = "Black";
        state.newSugarGrams = "200";
        state.loading = true;
        await loadData();
      } catch (err: unknown) {
        state.error =
          err instanceof Error ? err.message : "Failed to create batch";
      } finally {
        state.submitting = false;
      }
    };

    // ─── pH color helper ──────────────────────────────────────────
    const phColor = (ph: number | null) => {
      if (ph === null) return "text-stone-400";
      if (ph <= 3.5) return "text-emerald-600";
      if (ph <= 4.2) return "text-amber-600";
      return "text-red-600";
    };

    // ─── Batch card ───────────────────────────────────────────────
    const batchCard = (batch: ActiveFermentationMonitorDto) => {
      const borderClass = batch.isSafe
        ? "border-emerald-200"
        : "border-red-300";
      const phDisplay =
        batch.currentPH !== null ? batch.currentPH.toFixed(2) : "\u2014";
      const dropDisplay =
        batch.dropRatePerHour !== null
          ? batch.dropRatePerHour.toFixed(3) + "/hr"
          : "\u2014";

      return html`
        <div
          class="bg-white rounded-2xl border-2 ${borderClass} shadow-sm p-5 hover:shadow-md transition-shadow"
        >
          <div class="flex items-center justify-between mb-3">
            <h3 class="text-sm font-bold text-stone-800">
              ${batch.batchCode}
            </h3>
            <span
              class="text-xs font-semibold px-2 py-0.5 rounded-full ${batch.isSafe
                ? "bg-emerald-50 text-emerald-700"
                : "bg-red-50 text-red-700"}"
            >
              ${batch.isSafe ? "Safe" : "Unsafe"}
            </span>
          </div>
          <div class="space-y-2">
            <div class="flex items-center justify-between">
              <span class="text-xs text-stone-400 uppercase tracking-wider"
                >Phase</span
              >
              <span class="text-sm font-medium text-stone-700"
                >${batch.phase}</span
              >
            </div>
            <div class="flex items-center justify-between">
              <span class="text-xs text-stone-400 uppercase tracking-wider"
                >pH</span
              >
              <span class="text-sm font-bold ${phColor(batch.currentPH)}"
                >${phDisplay}</span
              >
            </div>
            <div class="flex items-center justify-between">
              <span class="text-xs text-stone-400 uppercase tracking-wider"
                >Drop Rate</span
              >
              <span class="text-sm font-medium text-stone-600"
                >${dropDisplay}</span
              >
            </div>
          </div>
          ${batch.statusMessage
            ? html`<p
                class="mt-3 text-xs text-stone-500 border-t border-stone-100 pt-2"
              >
                ${batch.statusMessage}
              </p>`
            : html``}
        </div>
      `;
    };

    // ─── New batch form ───────────────────────────────────────────
    const newBatchForm = () => html`
      <div
        class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6"
      >
        <h2
          class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
        >
          New Kombucha Batch
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          ${ArrowFormField({
            label: "Batch Code",
            required: true,
            children: html`
              <input
                type="text"
                class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none"
                value="${() => state.newBatchCode}"
                @input="${(e: Event) => {
                  state.newBatchCode = (e.target as HTMLInputElement).value;
                }}"
              />
            `,
          })}
          ${ArrowFormField({
            label: "Tea Type",
            required: true,
            children: html`
              <select
                class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none bg-white"
                @change="${(e: Event) => {
                  state.newTeaType = (e.target as HTMLSelectElement).value;
                }}"
              >
                ${TEA_TYPES.map(
                  (t) =>
                    html`<option value="${t}" selected="${() => state.newTeaType === t}">${t}</option>`,
                )}
              </select>
            `,
          })}
          ${ArrowFormField({
            label: "Sugar (grams)",
            required: true,
            children: html`
              <input
                type="number"
                min="0"
                class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none"
                value="${() => state.newSugarGrams}"
                @input="${(e: Event) => {
                  state.newSugarGrams = (e.target as HTMLInputElement).value;
                }}"
              />
            `,
          })}
        </div>
        <div class="flex gap-3 mt-4">
          <button
            type="button"
            class="px-4 py-2.5 bg-orange-600 text-white rounded-lg text-sm font-semibold hover:bg-orange-700 transition disabled:opacity-50"
            @click="${handleSubmit}"
            disabled="${() => state.submitting}"
          >
            ${() => (state.submitting ? "Creating..." : "Create Batch")}
          </button>
          <button
            type="button"
            class="px-4 py-2.5 bg-stone-100 text-stone-600 rounded-lg text-sm font-semibold hover:bg-stone-200 transition"
            @click="${() => {
              state.showNewForm = false;
            }}"
          >
            Cancel
          </button>
        </div>
      </div>
    `;

    // ─── Template ─────────────────────────────────────────────────
    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <!-- Header -->
        <header class="flex items-center justify-between mb-8">
          <div>
            <h1
              class="text-3xl font-extrabold text-stone-800 tracking-tight"
            >
              Kombucha Batches
            </h1>
            <p class="text-stone-500 mt-1">
              Track fermentation, pH, and ABV across all active brews.
            </p>
          </div>
          <button
            type="button"
            class="px-4 py-2.5 bg-stone-800 text-white rounded-lg text-sm font-semibold hover:bg-stone-700 transition min-h-[48px] flex items-center gap-2"
            @click="${() => {
              state.showNewForm = !state.showNewForm;
            }}"
          >
            + New Batch
          </button>
        </header>

        ${() => (state.showNewForm ? newBatchForm() : html``)}

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
                      label: "Total Batches",
                      value: () => String(totalCount()),
                      icon: "\uD83C\uDF75",
                      color: "orange",
                    })}
                    ${ArrowKPICard({
                      label: "Primary",
                      value: () => String(primaryCount()),
                      icon: "\u2697\uFE0F",
                      color: "amber",
                    })}
                    ${ArrowKPICard({
                      label: "Secondary",
                      value: () => String(secondaryCount()),
                      icon: "\uD83E\uDDEA",
                      color: "violet",
                    })}
                    ${ArrowKPICard({
                      label: "Alerts",
                      value: () => String(alertCount()),
                      icon: "\u26A0\uFE0F",
                      color: "red",
                    })}
                  </div>

                  <!-- Phase Filter Strip -->
                  <div class="flex gap-2 mb-6 flex-wrap" role="tablist">
                    ${PHASE_FILTERS.map(
                      (phase) => html`
                        <button
                          type="button"
                          role="tab"
                          class="${() =>
                            state.phaseFilter === phase
                              ? "bg-orange-500 text-white"
                              : "bg-stone-100 text-stone-600 hover:bg-stone-200"} px-4 py-2 rounded-lg text-sm font-medium min-h-[48px] min-w-[48px] transition"
                          @click="${() => {
                            state.phaseFilter = phase;
                          }}"
                        >
                          ${phase} (${() => String(phaseCount(phase))})
                        </button>
                      `,
                    )}
                  </div>

                  <!-- Batch Cards Grid -->
                  ${() =>
                    filteredBatches().length === 0
                      ? ArrowEmptyState({
                          icon: "\uD83C\uDF75",
                          title: "No kombucha batches",
                          message:
                            "Start a new batch to begin tracking pH and fermentation.",
                        })
                      : html`
                          <div
                            class="grid gap-4 md:grid-cols-2 lg:grid-cols-3"
                          >
                            ${() =>
                              filteredBatches().map((batch) =>
                                batchCard(batch),
                              )}
                          </div>
                        `}

                  <!-- Safety Reference -->
                  <div
                    class="mt-8 bg-amber-50 border border-amber-200 rounded-xl p-4"
                  >
                    <h3 class="text-sm font-bold text-amber-800 mb-2">
                      Kombucha Safety Thresholds
                    </h3>
                    <div
                      class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs text-amber-700"
                    >
                      <div>
                        <strong>pH Target:</strong> &le; 4.2 within 7 days
                        <br />
                        <span class="text-amber-600">
                          Batches stuck above 4.2 after 7 days must be
                          discarded.
                        </span>
                      </div>
                      <div>
                        <strong>ABV Limit:</strong> &lt; 0.5% (TTB Federal
                        Requirement)
                        <br />
                        <span class="text-amber-600">
                          Commercial kombucha must stay below 0.5% ABV.
                        </span>
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
