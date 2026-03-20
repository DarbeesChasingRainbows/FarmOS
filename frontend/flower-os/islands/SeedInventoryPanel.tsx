import { useSignal } from "@preact/signals";
import type { SeedLotSummary } from "../utils/farmos-client.ts";
import StatusBadge from "../components/StatusBadge.tsx";

export default function SeedInventoryPanel() {
  const lots = useSignal<SeedLotSummary[]>([]);
  const loading = useSignal(true);
  const error = useSignal("");

  const loadLots = async () => {
    loading.value = true;
    error.value = "";
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const result = await FloraAPI.getSeedLots();
      lots.value = result ?? [];
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load seeds";
    } finally {
      loading.value = false;
    }
  };

  if (loading.value && lots.value.length === 0) {
    loadLots();
  }

  return (
    <div>
      {error.value && (
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error.value}
          <button onClick={() => (error.value = "")} class="ml-2 text-red-500 hover:text-red-700">✕</button>
        </div>
      )}

      {loading.value
        ? <div class="text-center py-12 text-stone-400">Loading seed inventory...</div>
        : lots.value.length === 0
        ? (
          <div class="text-center py-12 text-stone-400">
            <p class="text-lg">No seed lots yet</p>
            <p class="text-sm mt-1">Add seed lots to track inventory and germination rates.</p>
          </div>
        )
        : (
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {lots.value.map((lot) => {
              const lowStock = lot.qtyOnHand < 50;
              return (
                <div
                  key={lot.id}
                  class={`p-5 bg-white rounded-xl border transition-colors ${
                    lowStock ? "border-amber-300 bg-amber-50" : "border-stone-200"
                  }`}
                >
                  <div class="flex items-center justify-between mb-2">
                    <h3 class="font-semibold text-stone-800">{lot.species}</h3>
                    {lot.isOrganic && <StatusBadge status="active" label="Organic" />}
                  </div>
                  <p class="text-sm text-stone-500">'{lot.cultivar}'</p>
                  <div class="mt-3 grid grid-cols-2 gap-y-2 text-sm">
                    <div>
                      <span class="text-stone-400 text-xs block">On Hand</span>
                      <span class={`font-semibold ${lowStock ? "text-amber-600" : "text-stone-800"}`}>
                        {lot.qtyOnHand} {lot.unit}
                      </span>
                    </div>
                    <div>
                      <span class="text-stone-400 text-xs block">Germination</span>
                      <span class="font-semibold text-stone-800">{lot.germinationPct}%</span>
                    </div>
                    <div>
                      <span class="text-stone-400 text-xs block">Supplier</span>
                      <span class="text-stone-700">{lot.supplier}</span>
                    </div>
                    <div>
                      <span class="text-stone-400 text-xs block">Year</span>
                      <span class="text-stone-700">{lot.harvestYear}</span>
                    </div>
                  </div>
                  {lowStock && (
                    <div class="mt-3 text-xs text-amber-600 font-medium">
                      ⚠ Low stock — consider reordering
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
    </div>
  );
}
