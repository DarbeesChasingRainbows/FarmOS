import { useSignal } from "@preact/signals";
import type { BatchDetail, BatchSummary } from "../utils/farmos-client.ts";
import StatusBadge from "../components/StatusBadge.tsx";

const GRADE_LABELS = ["Premium", "Standard", "Seconds", "Cull"];

export default function PostHarvestPanel() {
  const batches = useSignal<BatchSummary[]>([]);
  const selectedBatch = useSignal<BatchDetail | null>(null);
  const loading = useSignal(true);
  const error = useSignal("");

  const loadBatches = async () => {
    loading.value = true;
    error.value = "";
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const result = await FloraAPI.getBatches();
      batches.value = result ?? [];
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load batches";
    } finally {
      loading.value = false;
    }
  };

  const selectBatch = async (id: string) => {
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const detail = await FloraAPI.getBatch(id);
      selectedBatch.value = detail;
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load batch";
    }
  };

  if (loading.value && batches.value.length === 0) {
    loadBatches();
  }

  const batchStatus = (b: BatchSummary) => {
    if (b.inCooler) return "cooler";
    if (b.isConditioned) return "conditioned";
    return "active";
  };

  return (
    <div>
      {error.value && (
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error.value}
          <button onClick={() => (error.value = "")} class="ml-2 text-red-500 hover:text-red-700">✕</button>
        </div>
      )}

      <div class="flex gap-6">
        {/* Batch List */}
        <div class="w-80 flex-shrink-0">
          <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-4">
            Active Batches
          </h3>

          {loading.value
            ? <div class="text-center py-8 text-stone-400">Loading batches...</div>
            : batches.value.length === 0
            ? (
              <div class="text-center py-8 text-stone-400">
                <p class="text-lg">No batches yet</p>
                <p class="text-sm mt-1">Create a batch after harvesting stems from a bed.</p>
              </div>
            )
            : (
              <div class="space-y-2">
                {batches.value.map((batch) => (
                  <button
                    key={batch.id}
                    onClick={() => selectBatch(batch.id)}
                    class={`w-full text-left p-4 rounded-xl border transition-all duration-150 ${
                      selectedBatch.value?.id === batch.id
                        ? "bg-cyan-50 border-cyan-300 shadow-sm"
                        : "bg-white border-stone-200 hover:border-cyan-200 hover:shadow-sm"
                    }`}
                  >
                    <div class="flex items-center justify-between">
                      <span class="font-semibold text-stone-800">
                        {batch.species} '{batch.cultivar}'
                      </span>
                      <StatusBadge status={batchStatus(batch)} />
                    </div>
                    <div class="text-sm text-stone-500 mt-1">
                      {batch.stemsRemaining}/{batch.totalStems} stems · {batch.harvestDate}
                    </div>
                  </button>
                ))}
              </div>
            )}
        </div>

        {/* Batch Detail */}
        <div class="flex-1">
          {selectedBatch.value
            ? <BatchDetailView batch={selectedBatch.value} />
            : (
              <div class="flex items-center justify-center h-64 text-stone-400 bg-white rounded-xl border border-dashed border-stone-300">
                <p>Select a batch to view post-harvest details</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

function BatchDetailView({ batch }: { batch: BatchDetail }) {
  const pct = batch.totalStems > 0
    ? Math.round(((batch.totalStems - batch.stemsUsed) / batch.totalStems) * 100)
    : 0;

  return (
    <div class="bg-white rounded-xl border border-stone-200 p-6" style="animation: slideIn 0.2s ease-out">
      <h2 class="text-2xl font-bold text-stone-800 mb-1">
        {batch.species} '{batch.cultivar}'
      </h2>
      <p class="text-stone-500 text-sm mb-6">
        Harvested {batch.harvestDate}
      </p>

      {/* Pipeline Status */}
      <div class="flex items-center gap-4 mb-6">
        <PipelineStep label="Harvested" active done />
        <PipelineArrow />
        <PipelineStep label="Graded" active={batch.grades.length > 0} done={batch.grades.length > 0} />
        <PipelineArrow />
        <PipelineStep label="Conditioned" active={batch.isConditioned} done={batch.isConditioned} />
        <PipelineArrow />
        <PipelineStep label="In Cooler" active={batch.inCooler} done={batch.inCooler} />
      </div>

      {/* Stats Grid */}
      <div class="grid grid-cols-4 gap-4 mb-6">
        <StatCard label="Total Stems" value={batch.totalStems} />
        <StatCard label="Remaining" value={batch.stemsRemaining} highlight />
        <StatCard label="Used" value={batch.stemsUsed} />
        <StatCard label="Available %" value={`${pct}%`} />
      </div>

      {/* Grades */}
      {batch.grades.length > 0 && (
        <div class="mb-6">
          <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-3">
            Stem Grades
          </h3>
          <div class="grid grid-cols-4 gap-3">
            {batch.grades.map((g, i) => (
              <div key={i} class="p-3 bg-stone-50 rounded-lg border border-stone-100 text-center">
                <StatusBadge status={GRADE_LABELS[g.grade]?.toLowerCase() ?? "default"} label={GRADE_LABELS[g.grade] ?? `Grade ${g.grade}`} />
                <div class="text-lg font-bold text-stone-800 mt-2">{g.stemCount}</div>
                <div class="text-xs text-stone-400">{g.stemLengthInches}" avg</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Conditioning + Cooler */}
      <div class="grid grid-cols-2 gap-4">
        {batch.isConditioned && (
          <div class="p-4 bg-cyan-50 rounded-lg border border-cyan-200">
            <h4 class="text-sm font-semibold text-cyan-700 mb-2">Conditioning</h4>
            <p class="text-sm text-stone-700">{batch.conditioningSolution}</p>
            <p class="text-xs text-stone-500 mt-1">Water temp: {batch.waterTempF}°F</p>
          </div>
        )}
        {batch.inCooler && (
          <div class="p-4 bg-sky-50 rounded-lg border border-sky-200">
            <h4 class="text-sm font-semibold text-sky-700 mb-2">Cooler</h4>
            <p class="text-sm text-stone-700">Temp: {batch.coolerTempF}°F</p>
            {batch.coolerSlot && <p class="text-xs text-stone-500 mt-1">Slot: {batch.coolerSlot}</p>}
          </div>
        )}
      </div>
    </div>
  );
}

function PipelineStep({ label, active, done }: { label: string; active: boolean; done: boolean }) {
  return (
    <div class={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium ${
      done ? "bg-emerald-100 text-emerald-700" : active ? "bg-amber-100 text-amber-700" : "bg-stone-100 text-stone-400"
    }`}>
      {done ? "✓" : "○"} {label}
    </div>
  );
}

function PipelineArrow() {
  return <span class="text-stone-300">→</span>;
}

function StatCard({ label, value, highlight }: { label: string; value: string | number; highlight?: boolean }) {
  return (
    <div class="p-3 bg-stone-50 rounded-lg border border-stone-100 text-center">
      <div class="text-xs text-stone-400">{label}</div>
      <div class={`text-xl font-bold mt-1 ${highlight ? "text-emerald-600" : "text-stone-800"}`}>
        {value}
      </div>
    </div>
  );
}
