import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import RecordPHForm from "./RecordPHForm.tsx";
import ConfirmDialog from "./ConfirmDialog.tsx";
import NewBatchForm from "./NewBatchForm.tsx";
import { showToast } from "../utils/toastState.ts";

interface Batch {
  id: string;
  code: string;
  type: "sourdough" | "kombucha";
  phase: string;
  ph: number;
  startedAt: string;
  tea?: string;
}

const phaseDescriptions: Record<string, string> = {
  BulkFerment:
    "Dough is fermenting at room temp. Stretch & fold every 30min. Target: 25–50% volume increase.",
  Proofing:
    "Shaped dough rising in banneton. Ready when poke test springs back slowly (~1–2h).",
  Primary:
    "SCOBY is converting sugar to acids. Don't disturb. Takes 7–14 days.",
  Secondary:
    "Flavoring stage. Carbonation builds in sealed bottles (2–4 days at room temp).",
  Complete: "Batch is finished. Ready for consumption or storage.",
};

const phaseVariant = (phase: string) => {
  if (phase === "Complete") return "complete" as const;
  if (phase === "Proofing" || phase === "Secondary") return "active" as const;
  return "fermenting" as const;
};

export default function BatchDetailPanel() {
  const sourdoughBatches = useSignal<Batch[]>([
    {
      id: "a1b2c3d4",
      code: "SD-2024-03-A",
      phase: "BulkFerment",
      ph: 4.2,
      startedAt: "Mar 1, 2024",
      type: "sourdough",
    },
    {
      id: "e5f6g7h8",
      code: "SD-2024-03-B",
      phase: "Proofing",
      ph: 3.9,
      startedAt: "Feb 28, 2024",
      type: "sourdough",
    },
  ]);

  const kombuchaBatches = useSignal<Batch[]>([
    {
      id: "k1l2m3n4",
      code: "KB-MAR-01",
      phase: "Primary",
      ph: 3.2,
      startedAt: "Feb 25, 2024",
      type: "kombucha",
      tea: "Green",
    },
    {
      id: "o5p6q7r8",
      code: "KB-MAR-02",
      phase: "Secondary",
      ph: 2.8,
      startedAt: "Feb 20, 2024",
      type: "kombucha",
      tea: "Black",
    },
  ]);

  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);
  const confirmAdvance = useSignal(false);
  const confirmComplete = useSignal(false);

  const allBatches = [...sourdoughBatches.value, ...kombuchaBatches.value];
  const selectedBatch = allBatches.find((b) => b.id === selectedId.value);

  const phColor = (ph: number) => {
    if (ph >= 4.0) return "text-emerald-600";
    if (ph >= 3.0) return "text-amber-600";
    return "text-red-600";
  };

  const openSidebar = (id: string) => {
    selectedId.value = id;
    isSidebarOpen.value = true;
  };

  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null;
    }, 300);
  };

  const handleAdvance = async (batch: Batch) => {
    confirmAdvance.value = false;
    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      if (batch.type === "sourdough") {
        await HearthAPI.advanceSourdough(batch.id, { newPhase: 2 });
      } else {
        await HearthAPI.advanceKombucha(batch.id, { newPhase: 2 });
      }
      showToast(
        "success",
        "Phase advanced",
        `${batch.code} moved to next phase.`,
      );
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to advance",
        err instanceof Error ? err.message : "Unknown error",
      );
    }
  };

  const handleComplete = async (batch: Batch) => {
    confirmComplete.value = false;
    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      if (batch.type === "sourdough") {
        await HearthAPI.completeSourdough(batch.id, {
          yieldQty: { value: 1, unit: "loaves", type: "count" },
        });
      } else {
        await HearthAPI.completeKombucha(batch.id, {
          yieldQty: { value: 1, unit: "gallons", type: "volume" },
        });
      }
      showToast(
        "success",
        "Batch completed!",
        `${batch.code} marked as complete.`,
      );
      closeSidebar();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to complete",
        err instanceof Error ? err.message : "Unknown error",
      );
    }
  };

  const renderCard = (batch: Batch) => {
    const isSelected = selectedId.value === batch.id;
    return (
      <button
        type="button"
        onClick={() => openSidebar(batch.id)}
        class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left w-full cursor-pointer ${
          isSelected
            ? "border-amber-400 ring-2 ring-amber-200"
            : "border-stone-200 hover:border-amber-200"
        }`}
      >
        <div class="flex items-start justify-between mb-3">
          <div>
            <h3 class="text-lg font-bold text-stone-800">{batch.code}</h3>
            <p class="text-xs text-stone-400 mt-0.5">
              Started {batch.startedAt}
              {batch.tea ? ` · ${batch.tea} tea` : ""}
            </p>
          </div>
          <StatusBadge
            variant={phaseVariant(batch.phase)}
            label={batch.phase}
          />
        </div>
        <div class="flex items-center justify-between mt-4 pt-3 border-t border-stone-100">
          <div>
            <span class={`text-3xl font-mono font-bold ${phColor(batch.ph)}`}>
              {batch.ph}
            </span>
            <span class="text-xs text-stone-400 ml-1 uppercase">pH</span>
          </div>
          <span class="text-xs text-stone-400">View Details →</span>
        </div>
      </button>
    );
  };

  return (
    <div class="relative flex min-h-[500px]">
      {/* Main Grid Section */}
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[420px]" : ""
        }`}
      >
        {/* Section: Sourdough */}
        <section class="mb-10">
          <h2 class="text-lg font-bold text-stone-700 mb-4 flex items-center gap-2">
            <span>🍞</span> Sourdough
          </h2>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            {sourdoughBatches.value.map(renderCard)}
          </div>
        </section>

        {/* Section: Kombucha */}
        <section class="mb-8">
          <h2 class="text-lg font-bold text-stone-700 mb-4 flex items-center gap-2">
            <span>🫖</span> Kombucha
          </h2>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            {kombuchaBatches.value.map(renderCard)}
          </div>
        </section>

        {/* New Batch Modal Trigger */}
        <NewBatchForm />
      </div>

      {/* Mobile Backdrop */}
      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30 transition-opacity"
          onClick={closeSidebar}
        />
      )}

      {/* Slide-out Sidebar */}
      <aside
        class={`fixed top-0 right-0 h-full w-[400px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selectedBatch && (
          <div class="p-6 h-full flex flex-col">
            {/* Sidebar Header */}
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <div class="flex items-center gap-2 mb-2">
                  <span class="text-2xl">
                    {selectedBatch.type === "sourdough" ? "🍞" : "🫖"}
                  </span>
                  <h3 class="text-2xl font-bold text-stone-800">
                    {selectedBatch.code}
                  </h3>
                </div>
                <div class="flex items-center gap-3">
                  <StatusBadge
                    variant={phaseVariant(selectedBatch.phase)}
                    label={selectedBatch.phase}
                  />
                  <span class="text-sm text-stone-500">
                    {selectedBatch.type === "sourdough"
                      ? "Sourdough"
                      : "Kombucha"} · Started {selectedBatch.startedAt}
                    {selectedBatch.tea ? ` · ${selectedBatch.tea} tea` : ""}
                  </span>
                </div>
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>

            <div class="flex-1">
              {/* Phase Description */}
              <div class="bg-amber-50 rounded-lg p-4 mb-5 border border-amber-100">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-sm font-semibold text-amber-800">
                    Current Phase: {selectedBatch.phase}
                  </span>
                  <Tooltip text="Each batch progresses through phases. Advance when conditions are met.">
                    <InfoIcon />
                  </Tooltip>
                </div>
                <p class="text-xs text-amber-700 leading-relaxed">
                  {phaseDescriptions[selectedBatch.phase]}
                </p>
              </div>

              {/* Stats Grid */}
              <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-3">
                Readings
              </h4>
              <div class="grid grid-cols-2 gap-3 mb-6">
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p
                    class={`text-2xl font-mono font-bold ${
                      phColor(selectedBatch.ph)
                    }`}
                  >
                    {selectedBatch.ph}
                  </p>
                  <div class="flex items-center justify-center gap-1 mt-1">
                    <p class="text-xs text-stone-500 uppercase tracking-wide">
                      pH
                    </p>
                    <Tooltip
                      text={selectedBatch.type === "kombucha"
                        ? "Target: 2.5–3.5. Below 2.5 is very acidic."
                        : "Target: 3.5–4.5 during bulk ferment."}
                      position="left"
                    >
                      <InfoIcon />
                    </Tooltip>
                  </div>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-mono font-bold text-stone-700">
                    75°F
                  </p>
                  <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">
                    Temp
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-mono font-bold text-stone-700">3</p>
                  <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">
                    pH Readings
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-lg font-mono font-bold text-stone-700">
                    {selectedBatch.startedAt.split(",")[0]}
                  </p>
                  <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">
                    Started
                  </p>
                </div>
              </div>

              {/* pH Form */}
              <div class="mb-4">
                <RecordPHForm
                  batchId={selectedBatch.id}
                  batchType={selectedBatch.type}
                />
              </div>
            </div>

            {/* Quick Actions */}
            <div class="pt-4 border-t border-stone-100 mt-auto">
              <p class="text-xs text-stone-500 uppercase tracking-wider mb-2 font-semibold">
                Quick Actions
              </p>
              <div class="flex flex-col gap-2">
                <button
                  onClick={() => confirmAdvance.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition border border-blue-100 flex items-center justify-center gap-2"
                >
                  ⏭ Advance Phase
                </button>
                <button
                  onClick={() => confirmComplete.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 transition border border-emerald-100 flex items-center justify-center gap-2"
                >
                  ✓ Complete Batch
                </button>
              </div>
            </div>
          </div>
        )}
      </aside>

      {/* Confirmation Dialogs */}
      {selectedBatch && (
        <>
          <ConfirmDialog
            open={confirmAdvance.value}
            title={`Advance ${selectedBatch.code}?`}
            message={`Move "${selectedBatch.code}" from ${selectedBatch.phase} to the next phase. Make sure conditions are met before advancing.`}
            confirmLabel="Advance Phase"
            onConfirm={() => handleAdvance(selectedBatch)}
            onCancel={() => confirmAdvance.value = false}
          />
          <ConfirmDialog
            open={confirmComplete.value}
            title={`Complete ${selectedBatch.code}?`}
            message={`Mark "${selectedBatch.code}" as complete. This finalizes the batch and records the yield.`}
            confirmLabel="Complete Batch"
            variant="safe"
            onConfirm={() => handleComplete(selectedBatch)}
            onCancel={() => confirmComplete.value = false}
          />
        </>
      )}
    </div>
  );
}
