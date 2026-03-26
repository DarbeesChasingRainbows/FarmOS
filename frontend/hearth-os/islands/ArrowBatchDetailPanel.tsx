import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowStatusBadge } from "../components/ArrowStatusBadge.ts";
import { ArrowTooltip, ArrowInfoIcon } from "../components/ArrowTooltip.ts";
import { showToast } from "../utils/toastState.ts";
import { type ActiveFermentationMonitorDto } from "../utils/farmos-client.ts";

interface Batch {
  id: string;
  code: string;
  type: string;
  phase: string;
  ph: number;
  startedAt: string;
  icon: string;
}

// ── Type → icon mapping ──────────────────────────────────────────────
const typeIcons: Record<string, string> = {
  sourdough: "🍞", kombucha: "🫖", kimchi: "🥬", sauerkraut: "🥒",
  jun: "🍵", miso: "🫘", "hot-sauce": "🌶️", pickles: "🥒",
  yogurt: "🥛", tempeh: "🫘", vinegar: "🫙",
};

const phaseDescriptions: Record<string, string> = {
  BulkFerment: "Dough is fermenting at room temp. Stretch & fold every 30min.",
  Proofing: "Shaped dough rising. Ready when poke test springs back slowly.",
  Primary: "Active fermentation in progress. Don't disturb unless testing.",
  Secondary: "Second fermentation — flavoring, carbonation, or aging.",
  Complete: "Batch is finished. Ready for consumption or storage.",
  Inoculation: "Culture has been introduced to the substrate.",
  ActiveFerment: "Bubbling, acid production, or visible fermentation activity.",
  Aging: "Extended rest for flavor development.",
  Bottling: "Transferred to bottles or jars for storage/distribution.",
};

const phaseVariant = (phase: string) => {
  if (phase === "Complete") return "complete" as const;
  if (["Proofing", "Secondary", "Aging", "Bottling"].includes(phase)) return "active" as const;
  return "fermenting" as const;
};

const phColor = (ph: number) => {
  if (ph >= 4.0) return "text-emerald-600";
  if (ph >= 3.0) return "text-amber-600";
  return "text-red-600";
};

export interface ArrowBatchDetailPanelProps {
  initialBatches: ActiveFermentationMonitorDto[];
}

export default function ArrowBatchDetailPanel({ initialBatches }: ArrowBatchDetailPanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const mapDto = (dto: ActiveFermentationMonitorDto): Batch => {
      const typeLower = dto.productType.toLowerCase();
      return {
        id: dto.batchId,
        code: dto.batchCode,
        type: typeLower,
        phase: dto.phase,
        ph: dto.currentPH ?? 0,
        startedAt: dto.statusMessage,
        icon: typeIcons[typeLower] ?? "🧪",
      };
    };

    const state = reactive({
      batches: initialBatches.map(mapDto),
      selectedId: null as string | null,
      isSidebarOpen: false,
      confirmAdvance: false,
      confirmComplete: false,
    });

    const getSelected = () => state.batches.find(b => b.id === state.selectedId) ?? null;

    const openSidebar = (id: string) => {
      state.selectedId = id;
      state.isSidebarOpen = true;
    };

    const closeSidebar = () => {
      state.isSidebarOpen = false;
      setTimeout(() => { state.selectedId = null; }, 300);
    };

    const handleAdvance = async (batch: Batch) => {
      state.confirmAdvance = false;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        if (batch.type === "sourdough") {
          await HearthAPI.advanceSourdough(batch.id, { newPhase: 2 });
        } else if (batch.type === "kombucha") {
          await HearthAPI.advanceKombucha(batch.id, { newPhase: 2 });
        } else {
          // Generic — use sourdough advance as fallback
          await HearthAPI.advanceSourdough(batch.id, { newPhase: 2 });
        }
        showToast("success", "Phase advanced", `${batch.code} moved to next phase.`);
      } catch (err: unknown) {
        showToast("error", "Failed to advance", err instanceof Error ? err.message : "Unknown error");
      }
    };

    const handleComplete = async (batch: Batch) => {
      state.confirmComplete = false;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        if (batch.type === "sourdough") {
          await HearthAPI.completeSourdough(batch.id, { yieldQty: { value: 1, unit: "loaves", type: "count" } });
        } else if (batch.type === "kombucha") {
          await HearthAPI.completeKombucha(batch.id, { yieldQty: { value: 1, unit: "gallons", type: "volume" } });
        } else {
          await HearthAPI.completeSourdough(batch.id, { yieldQty: { value: 1, unit: "batch", type: "count" } });
        }
        showToast("success", "Batch completed!", `${batch.code} marked as complete.`);
        closeSidebar();
      } catch (err: unknown) {
        showToast("error", "Failed to complete", err instanceof Error ? err.message : "Unknown error");
      }
    };

    const renderBatchCard = (batch: Batch) => {
      const isSelected = () => state.selectedId === batch.id;
      return html`
        <button
          type="button"
          @click="${() => openSidebar(batch.id)}"
          class="${() => `bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left w-full cursor-pointer ${isSelected() ? "border-amber-400 ring-2 ring-amber-200" : "border-stone-200 hover:border-amber-200"}`}"
        >
          <div class="flex items-start justify-between mb-3">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-lg">${batch.icon}</span>
                <h3 class="text-lg font-bold text-stone-800">${batch.code}</h3>
              </div>
              <p class="text-xs text-stone-400 mt-0.5 capitalize">${batch.type} · Started ${batch.startedAt}</p>
            </div>
            ${ArrowStatusBadge({ variant: phaseVariant(batch.phase), label: batch.phase })}
          </div>
          <div class="flex items-center justify-between mt-4 pt-3 border-t border-stone-100">
            <div>
              <span class="${`text-3xl font-mono font-bold ${phColor(batch.ph)}`}">${batch.ph}</span>
              <span class="text-xs text-stone-400 ml-1 uppercase">pH</span>
            </div>
            <span class="text-xs text-stone-400">View Details →</span>
          </div>
        </button>
      `.key(batch.id);
    };

    const template = html`
      <div class="relative flex min-h-[500px]">
        <!-- Main Grid Section -->
        <div class="${() => `flex-1 transition-all duration-300 ${state.isSidebarOpen ? "mr-[420px]" : ""}`}">

          ${() => {
            if (state.batches.length === 0) {
              return html`
                <div class="flex flex-col items-center justify-center py-20 px-8">
                  <div class="w-24 h-24 bg-amber-50 rounded-full flex items-center justify-center mb-6 border-2 border-amber-100">
                    <span class="text-5xl">🫙</span>
                  </div>
                  <h2 class="text-2xl font-bold text-stone-800 mb-2">No Active Batches</h2>
                  <p class="text-stone-500 text-center max-w-md mb-8 leading-relaxed">
                    Start tracking your fermentations — sourdough, kombucha, kimchi, sauerkraut, jun, or anything else. Begin by clicking "+ New Batch" above.
                  </p>
                  <div class="flex flex-wrap items-center justify-center gap-4 text-sm text-stone-400">
                    <span class="flex items-center gap-1.5">🍞 Sourdough</span>
                    <span class="flex items-center gap-1.5">🫖 Kombucha</span>
                    <span class="flex items-center gap-1.5">🥬 Kimchi</span>
                    <span class="flex items-center gap-1.5">🥒 Sauerkraut</span>
                    <span class="flex items-center gap-1.5">🍵 Jun</span>
                    <span class="flex items-center gap-1.5">🌶️ Hot Sauce</span>
                    <span class="flex items-center gap-1.5">🧪 + more</span>
                  </div>
                </div>
              `;
            }

            // Group batches by type for organized display
            const types = [...new Set(state.batches.map(b => b.type))];
            return html`${types.map(type => {
              const typeBatches = state.batches.filter(b => b.type === type);
              const icon = typeIcons[type] ?? "🧪";
              return html`
                <section class="mb-8">
                  <h2 class="text-lg font-bold text-stone-700 mb-4 flex items-center gap-2">
                    <span>${icon}</span> <span class="capitalize">${type}</span>
                    <span class="text-xs font-medium bg-stone-100 text-stone-500 px-2 py-0.5 rounded-full">${typeBatches.length}</span>
                  </h2>
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    ${typeBatches.map(renderBatchCard)}
                  </div>
                </section>
              `;
            })}`;
          }}
        </div>

        <!-- Mobile Backdrop -->
        ${() => state.isSidebarOpen ? html`
          <div class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30 transition-opacity" @click="${closeSidebar}"></div>
        ` : ""}

        <!-- Slide-out Sidebar -->
        <aside class="${() => `fixed top-0 right-0 h-full w-[400px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${state.isSidebarOpen ? "translate-x-0" : "translate-x-full"}`}">
          ${() => {
            const batch = getSelected();
            if (!batch) return "";

            return html`
              <div class="p-6 h-full flex flex-col">
                <!-- Sidebar Header -->
                <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
                  <div>
                    <div class="flex items-center gap-2 mb-2">
                      <span class="text-2xl">${batch.icon}</span>
                      <h3 class="text-2xl font-bold text-stone-800">${batch.code}</h3>
                    </div>
                    <div class="flex items-center gap-3">
                      ${ArrowStatusBadge({ variant: phaseVariant(batch.phase), label: batch.phase })}
                      <span class="text-sm text-stone-500 capitalize">
                        ${batch.type} · Started ${batch.startedAt}
                      </span>
                    </div>
                  </div>
                  <button type="button" @click="${closeSidebar}" class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition">✕</button>
                </div>

                <div class="flex-1">
                  <!-- Phase Description -->
                  <div class="bg-amber-50 rounded-lg p-4 mb-5 border border-amber-100">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="text-sm font-semibold text-amber-800">Current Phase: ${batch.phase}</span>
                      ${ArrowTooltip({
                        text: "Each batch progresses through phases. Advance when conditions are met.",
                        children: ArrowInfoIcon()
                      })}
                    </div>
                    <p class="text-xs text-amber-700 leading-relaxed">${phaseDescriptions[batch.phase] ?? "Fermentation is in progress."}</p>
                  </div>

                  <!-- Stats Grid -->
                  <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-3">Readings</h4>
                  <div class="grid grid-cols-2 gap-3 mb-6">
                    <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                      <p class="${`text-2xl font-mono font-bold ${phColor(batch.ph)}`}">${batch.ph}</p>
                      <div class="flex items-center justify-center gap-1 mt-1">
                        <p class="text-xs text-stone-500 uppercase tracking-wide">pH</p>
                        ${ArrowTooltip({
                          text: "Fermentation lowers pH. Target depends on product type.",
                          position: "top",
                          children: ArrowInfoIcon()
                        })}
                      </div>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                      <p class="text-2xl font-mono font-bold text-stone-700">75°F</p>
                      <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">Temp</p>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                      <p class="text-2xl font-mono font-bold text-stone-700">3</p>
                      <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">pH Readings</p>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                      <p class="text-lg font-mono font-bold text-stone-700">${batch.startedAt.split(",")[0]}</p>
                      <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">Started</p>
                    </div>
                  </div>

                  <!-- pH Form Mount -->
                  <div class="mb-4" id="ph-form-mount-${batch.id}"></div>
                </div>

                <!-- Quick Actions -->
                <div class="pt-4 border-t border-stone-100 mt-auto">
                  <p class="text-xs text-stone-500 uppercase tracking-wider mb-2 font-semibold">Quick Actions</p>
                  <div class="flex flex-col gap-2">
                    <button
                      type="button"
                      @click="${() => state.confirmAdvance = true}"
                      class="w-full py-2.5 text-sm font-bold rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition border border-blue-100 flex items-center justify-center gap-2"
                    >⏭ Advance Phase</button>
                    <button
                      type="button"
                      @click="${() => state.confirmComplete = true}"
                      class="w-full py-2.5 text-sm font-bold rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 transition border border-emerald-100 flex items-center justify-center gap-2"
                    >✓ Complete Batch</button>
                  </div>
                </div>
              </div>

              <!-- Inline Confirm: Advance -->
              ${() => state.confirmAdvance ? html`
                <div class="fixed inset-0 z-50 flex items-center justify-center" @click="${() => state.confirmAdvance = false}">
                  <div class="absolute inset-0 bg-black/40 backdrop-blur-sm"></div>
                  <div class="relative bg-white rounded-xl shadow-2xl border border-stone-200 p-6 max-w-md w-full mx-4 animate-[scaleIn_0.2s_ease-out]" @click="${(e: Event) => e.stopPropagation()}">
                    <h3 class="text-lg font-bold text-stone-800 mb-2">Advance ${batch.code}?</h3>
                    <p class="text-sm text-stone-600 mb-6 leading-relaxed">Move "${batch.code}" from ${batch.phase} to the next phase. Make sure conditions are met before advancing.</p>
                    <div class="flex gap-3 justify-end">
                      <button @click="${() => state.confirmAdvance = false}" class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition">Cancel</button>
                      <button @click="${() => handleAdvance(batch)}" class="px-4 py-2 text-sm font-semibold bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition shadow-sm">Advance Phase</button>
                    </div>
                  </div>
                </div>
              ` : ""}

              <!-- Inline Confirm: Complete -->
              ${() => state.confirmComplete ? html`
                <div class="fixed inset-0 z-50 flex items-center justify-center" @click="${() => state.confirmComplete = false}">
                  <div class="absolute inset-0 bg-black/40 backdrop-blur-sm"></div>
                  <div class="relative bg-white rounded-xl shadow-2xl border border-stone-200 p-6 max-w-md w-full mx-4 animate-[scaleIn_0.2s_ease-out]" @click="${(e: Event) => e.stopPropagation()}">
                    <h3 class="text-lg font-bold text-stone-800 mb-2">Complete ${batch.code}?</h3>
                    <p class="text-sm text-stone-600 mb-6 leading-relaxed">Mark "${batch.code}" as complete. This finalizes the batch and records the yield.</p>
                    <div class="flex gap-3 justify-end">
                      <button @click="${() => state.confirmComplete = false}" class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition">Cancel</button>
                      <button @click="${() => handleComplete(batch)}" class="px-4 py-2 text-sm font-semibold bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition shadow-sm">Complete Batch</button>
                    </div>
                  </div>
                </div>
              ` : ""}
            `;
          }}
        </aside>
      </div>
    `;

    template(containerRef.current);
  }, [initialBatches]);

  return <div ref={containerRef}></div>;
}
