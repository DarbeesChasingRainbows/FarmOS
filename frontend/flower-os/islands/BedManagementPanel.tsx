import { useSignal } from "@preact/signals";
import type {
  FlowerBedDetail,
  FlowerBedSummary,
} from "../utils/farmos-client.ts";
import StatusBadge from "../components/StatusBadge.tsx";

export default function BedManagementPanel() {
  const beds = useSignal<FlowerBedSummary[]>([]);
  const selectedBed = useSignal<FlowerBedDetail | null>(null);
  const loading = useSignal(true);
  const error = useSignal("");
  const showCreateForm = useSignal(false);

  // Create form state
  const newName = useSignal("");
  const newBlock = useSignal("");
  const newLength = useSignal(100);
  const newWidth = useSignal(4);

  // Load beds on mount
  const loadBeds = async () => {
    loading.value = true;
    error.value = "";
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const result = await FloraAPI.getBeds();
      beds.value = result ?? [];
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load beds";
    } finally {
      loading.value = false;
    }
  };

  const selectBed = async (id: string) => {
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const detail = await FloraAPI.getBed(id);
      selectedBed.value = detail;
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load bed";
    }
  };

  const createBed = async () => {
    if (!newName.value.trim() || !newBlock.value.trim()) return;
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      await FloraAPI.createBed({
        name: newName.value,
        block: newBlock.value,
        dimensions: { lengthFeet: newLength.value, widthFeet: newWidth.value },
      });
      showCreateForm.value = false;
      newName.value = "";
      newBlock.value = "";
      await loadBeds();
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to create bed";
    }
  };

  // Initial load
  if (loading.value && beds.value.length === 0) {
    loadBeds();
  }

  return (
    <div>
      {/* Error Banner */}
      {error.value && (
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error.value}
          <button
            onClick={() => (error.value = "")}
            class="ml-2 text-red-500 hover:text-red-700"
          >
            ✕
          </button>
        </div>
      )}

      <div class="flex gap-6">
        {/* Bed List */}
        <div class="w-80 flex-shrink-0">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide">
              All Beds
            </h3>
            <button
              onClick={() => (showCreateForm.value = !showCreateForm.value)}
              class="px-3 py-1.5 text-sm font-medium bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition-colors"
            >
              + New Bed
            </button>
          </div>

          {/* Create Form */}
          {showCreateForm.value && (
            <div class="mb-4 p-4 bg-white rounded-xl border border-stone-200 space-y-3">
              <input
                type="text"
                placeholder="Bed name (e.g., Bed A-1)"
                value={newName.value}
                onInput={(e) =>
                  (newName.value = (e.target as HTMLInputElement).value)}
                class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
              />
              <input
                type="text"
                placeholder="Block (e.g., North Field)"
                value={newBlock.value}
                onInput={(e) =>
                  (newBlock.value = (e.target as HTMLInputElement).value)}
                class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
              />
              <div class="flex gap-2">
                <div class="flex-1">
                  <label class="text-xs text-stone-500">Length (ft)</label>
                  <input
                    type="number"
                    value={newLength.value}
                    onInput={(e) =>
                      (newLength.value =
                        +(e.target as HTMLInputElement).value)}
                    class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg"
                  />
                </div>
                <div class="flex-1">
                  <label class="text-xs text-stone-500">Width (ft)</label>
                  <input
                    type="number"
                    value={newWidth.value}
                    onInput={(e) =>
                      (newWidth.value =
                        +(e.target as HTMLInputElement).value)}
                    class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg"
                  />
                </div>
              </div>
              <div class="flex gap-2">
                <button
                  onClick={createBed}
                  class="flex-1 px-3 py-2 text-sm font-medium bg-emerald-600 text-white rounded-lg hover:bg-emerald-700"
                >
                  Create
                </button>
                <button
                  onClick={() => (showCreateForm.value = false)}
                  class="px-3 py-2 text-sm text-stone-500 hover:text-stone-700"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {/* Bed Cards */}
          {loading.value
            ? (
              <div class="text-center py-8 text-stone-400">
                Loading beds...
              </div>
            )
            : beds.value.length === 0
            ? (
              <div class="text-center py-8 text-stone-400">
                <p class="text-lg">No beds yet</p>
                <p class="text-sm mt-1">
                  Create your first flower bed to get started.
                </p>
              </div>
            )
            : (
              <div class="space-y-2">
                {beds.value.map((bed) => (
                  <button
                    key={bed.id}
                    onClick={() => selectBed(bed.id)}
                    class={`w-full text-left p-4 rounded-xl border transition-all duration-150 ${
                      selectedBed.value?.id === bed.id
                        ? "bg-emerald-50 border-emerald-300 shadow-sm"
                        : "bg-white border-stone-200 hover:border-emerald-200 hover:shadow-sm"
                    }`}
                  >
                    <div class="flex items-center justify-between">
                      <span class="font-semibold text-stone-800">
                        {bed.name}
                      </span>
                      <StatusBadge
                        status={bed.successionCount > 0
                          ? "growing"
                          : "planned"}
                        label={`${bed.successionCount} succ.`}
                      />
                    </div>
                    <div class="text-sm text-stone-500 mt-1">
                      {bed.block} · {bed.lengthFeet}×{bed.widthFeet} ft
                    </div>
                  </button>
                ))}
              </div>
            )}
        </div>

        {/* Bed Detail */}
        <div class="flex-1">
          {selectedBed.value
            ? <BedDetail bed={selectedBed.value} onRefresh={loadBeds} />
            : (
              <div class="flex items-center justify-center h-64 text-stone-400 bg-white rounded-xl border border-dashed border-stone-300">
                <p>Select a bed to view details</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

// ─── Bed Detail Sub-component ─────────────────────────────────────

function BedDetail(
  { bed, onRefresh }: { bed: FlowerBedDetail; onRefresh: () => void },
) {
  return (
    <div
      class="bg-white rounded-xl border border-stone-200 p-6"
      style="animation: slideIn 0.2s ease-out"
    >
      <div class="flex items-center justify-between mb-6">
        <div>
          <h2 class="text-2xl font-bold text-stone-800">{bed.name}</h2>
          <p class="text-stone-500">
            {bed.block} · {bed.lengthFeet}×{bed.widthFeet} ft ({bed.lengthFeet *
              bed.widthFeet} sq ft)
          </p>
        </div>
      </div>

      {/* Successions */}
      <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-3">
        Successions ({bed.successions.length})
      </h3>

      {bed.successions.length === 0
        ? (
          <div class="text-center py-6 text-stone-400 border border-dashed border-stone-200 rounded-lg">
            No successions planned yet.
          </div>
        )
        : (
          <div class="space-y-3">
            {bed.successions.map((succ) => {
              const totalHarvested = succ.harvests.reduce(
                (sum, h) => sum + h.stemCount,
                0,
              );
              return (
                <div
                  key={succ.id}
                  class="p-4 bg-stone-50 rounded-lg border border-stone-100"
                >
                  <div class="flex items-center justify-between">
                    <div>
                      <span class="font-semibold text-stone-800">
                        {succ.species}
                      </span>
                      <span class="text-stone-500 ml-1">'{succ.cultivar}'</span>
                      {succ.color && (
                        <span class="text-xs text-stone-400 ml-2">
                          ({succ.color})
                        </span>
                      )}
                    </div>
                    <StatusBadge
                      status={succ.harvests.length > 0
                        ? "harvesting"
                        : "planned"}
                    />
                  </div>
                  <div class="mt-2 grid grid-cols-4 gap-4 text-sm">
                    <div>
                      <span class="text-stone-400 block text-xs">Sow</span>
                      <span class="text-stone-700">{succ.sowDate}</span>
                    </div>
                    <div>
                      <span class="text-stone-400 block text-xs">
                        Transplant
                      </span>
                      <span class="text-stone-700">{succ.transplantDate}</span>
                    </div>
                    <div>
                      <span class="text-stone-400 block text-xs">
                        Harvest Start
                      </span>
                      <span class="text-stone-700">{succ.harvestStart}</span>
                    </div>
                    <div>
                      <span class="text-stone-400 block text-xs">
                        Stems Cut
                      </span>
                      <span class="font-medium text-emerald-700">
                        {totalHarvested}
                      </span>
                    </div>
                  </div>
                  <div class="text-xs text-stone-400 mt-1">
                    {succ.daysToMaturity} days to maturity
                  </div>
                </div>
              );
            })}
          </div>
        )}
    </div>
  );
}
