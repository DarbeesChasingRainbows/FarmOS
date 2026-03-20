import { define } from "../../utils.ts";

/**
 * Kombucha batch listing page.
 * Displays all active kombucha batches with phase filtering.
 */
export default define.page(function KombuchaIndex() {
  // TODO: Fetch batches from API via handler and pass as props
  return (
    <div class="p-6 max-w-6xl mx-auto">
      {/* Header */}
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-stone-800">🫖 Kombucha Batches</h1>
          <p class="text-sm text-stone-500 mt-1">
            Track fermentation, pH, and ABV across all active brews
          </p>
        </div>
        <a
          href="/batches"
          class="px-4 py-2.5 bg-stone-800 text-white rounded-lg text-sm font-semibold hover:bg-stone-700 transition min-h-[48px] flex items-center gap-2"
        >
          + New Kombucha Batch
        </a>
      </div>

      {/* Phase Filter Tabs — uses Fresh Partials for SPA-like switching */}
      <div class="flex gap-2 mb-6" role="tablist">
        {["All", "Primary", "Secondary", "Bottled", "Complete"].map((phase) => (
          <button
            key={phase}
            type="button"
            class="px-4 py-2 rounded-lg text-sm font-medium min-h-[48px] min-w-[48px] transition border border-stone-200 bg-white text-stone-600 hover:bg-stone-50"
            role="tab"
          >
            {phase}
          </button>
        ))}
      </div>

      {/* Batch Cards Placeholder */}
      <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <div class="bg-white rounded-xl border border-stone-200 p-6 text-center text-stone-400">
          <p class="text-3xl mb-2">🫖</p>
          <p class="text-sm font-medium">No active kombucha batches</p>
          <p class="text-xs mt-1">
            Start a new batch to begin tracking pH and fermentation
          </p>
        </div>
      </div>

      {/* Safety Reference */}
      <div class="mt-8 bg-amber-50 border border-amber-200 rounded-xl p-4">
        <h3 class="text-sm font-bold text-amber-800 mb-2">
          ⚠️ Kombucha Safety Thresholds
        </h3>
        <div class="grid grid-cols-2 gap-4 text-xs text-amber-700">
          <div>
            <strong>pH Target:</strong> ≤ 4.2 within 7 days
            <br />
            <span class="text-amber-600">
              Batches stuck above 4.2 after 7 days must be discarded
            </span>
          </div>
          <div>
            <strong>ABV Limit:</strong> {"<"} 0.5% (TTB Federal Requirement)
            <br />
            <span class="text-amber-600">
              Commercial kombucha must stay below 0.5% ABV
            </span>
          </div>
        </div>
      </div>
    </div>
  );
});
