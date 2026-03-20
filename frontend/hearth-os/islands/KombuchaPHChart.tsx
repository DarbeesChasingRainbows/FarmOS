import { useSignal } from "@preact/signals";

/**
 * Kombucha pH Chart island.
 *
 * Renders a time-series pH chart with:
 * - Horizontal reference line at pH 4.2 (safety threshold)
 * - Color-coded zones: green (<4.2), amber (approaching), red (>4.2 after 7 days)
 * - ABV display with 0.5% TTB warning threshold
 *
 * Props are passed from the batch detail route handler.
 */

interface PHReading {
  timestamp: string;
  pH: number;
  notes?: string;
}

interface Props {
  batchId: string;
  batchCode: string;
  startedAt: string;
  currentPH: number | null;
  alcoholContentPct: number | null;
  phReadings: PHReading[];
}

const PH_THRESHOLD = 4.2;
const ABV_LIMIT = 0.5;
const MAX_FERMENTATION_DAYS = 7;

function getPHStatus(
  pH: number,
  daysElapsed: number,
): { label: string; color: string; bgColor: string } {
  if (pH <= PH_THRESHOLD) {
    return {
      label: "Safe",
      color: "text-emerald-700",
      bgColor: "bg-emerald-50",
    };
  }
  if (daysElapsed >= MAX_FERMENTATION_DAYS) {
    return {
      label: "DISCARD REQUIRED",
      color: "text-red-700",
      bgColor: "bg-red-50",
    };
  }
  return {
    label: "Needs More Time",
    color: "text-amber-700",
    bgColor: "bg-amber-50",
  };
}

function getDaysElapsed(startedAt: string): number {
  const start = new Date(startedAt);
  const now = new Date();
  return Math.floor((now.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
}

export default function KombuchaPHChart(props: Props) {
  const {
    batchCode,
    startedAt,
    currentPH,
    alcoholContentPct,
    phReadings,
  } = props;
  const showDetails = useSignal(false);

  const daysElapsed = getDaysElapsed(startedAt);
  const phStatus = currentPH !== null
    ? getPHStatus(currentPH, daysElapsed)
    : null;

  // Calculate chart data — normalize pH values to visual bar heights
  const maxPH = 7;
  const minPH = 2;

  return (
    <div class="bg-white rounded-xl border border-stone-200 shadow-sm overflow-hidden">
      {/* Header */}
      <div class="px-5 py-4 border-b border-stone-100 bg-stone-50">
        <div class="flex items-center justify-between">
          <div>
            <h3 class="text-base font-bold text-stone-800">
              ⚗️ pH Trend — {batchCode}
            </h3>
            <p class="text-xs text-stone-500 mt-0.5">
              Day {daysElapsed} of fermentation
            </p>
          </div>
          {phStatus && (
            <span
              class={`px-3 py-1 rounded-full text-xs font-bold ${phStatus.color} ${phStatus.bgColor}`}
            >
              {phStatus.label}
            </span>
          )}
        </div>
      </div>

      {/* pH Chart — Visual bar representation */}
      <div class="p-5">
        <div class="relative h-48 flex items-end gap-1 border-b border-stone-200 mb-2">
          {/* Threshold line at pH 4.2 */}
          <div
            class="absolute left-0 right-0 border-t-2 border-dashed border-red-400 z-10"
            style={{
              bottom: `${((PH_THRESHOLD - minPH) / (maxPH - minPH)) * 100}%`,
            }}
          >
            <span class="absolute -top-3 right-0 text-[10px] font-bold text-red-500 bg-white px-1">
              pH 4.2
            </span>
          </div>

          {phReadings.length === 0
            ? (
              <div class="flex-1 flex items-center justify-center text-stone-400 text-sm">
                No pH readings yet
              </div>
            )
            : (
              phReadings.map((reading, i) => {
                const height = ((reading.pH - minPH) / (maxPH - minPH)) * 100;
                const isBelow = reading.pH <= PH_THRESHOLD;
                const barColor = isBelow
                  ? "bg-emerald-400"
                  : daysElapsed >= MAX_FERMENTATION_DAYS
                  ? "bg-red-400"
                  : "bg-amber-400";

                return (
                  <div
                    key={i}
                    class="flex-1 flex flex-col items-center justify-end"
                    title={`pH ${reading.pH} — ${
                      new Date(reading.timestamp).toLocaleDateString()
                    }`}
                  >
                    <div
                      class={`w-full rounded-t ${barColor} transition-all min-w-[8px]`}
                      style={{ height: `${Math.max(height, 2)}%` }}
                    />
                  </div>
                );
              })
            )}
        </div>

        {/* Legend */}
        <div class="flex gap-4 text-[10px] text-stone-500">
          <span class="flex items-center gap-1">
            <span class="w-2 h-2 rounded-full bg-emerald-400" />{" "}
            Below 4.2 (Safe)
          </span>
          <span class="flex items-center gap-1">
            <span class="w-2 h-2 rounded-full bg-amber-400" />{" "}
            Above 4.2 (Fermenting)
          </span>
          <span class="flex items-center gap-1">
            <span class="w-2 h-2 rounded-full bg-red-400" />{" "}
            Above 4.2 + 7d (Discard)
          </span>
        </div>
      </div>

      {/* ABV + Current pH Summary */}
      <div class="px-5 py-4 bg-stone-50 border-t border-stone-100 grid grid-cols-3 gap-4">
        <div>
          <p class="text-[10px] font-medium text-stone-400 uppercase tracking-wide">
            Current pH
          </p>
          <p class="text-lg font-bold text-stone-800">
            {currentPH !== null ? currentPH.toFixed(2) : "—"}
          </p>
        </div>
        <div>
          <p class="text-[10px] font-medium text-stone-400 uppercase tracking-wide">
            ABV
          </p>
          <p
            class={`text-lg font-bold ${
              alcoholContentPct !== null && alcoholContentPct >= ABV_LIMIT
                ? "text-red-600"
                : "text-stone-800"
            }`}
          >
            {alcoholContentPct !== null
              ? `${alcoholContentPct.toFixed(2)}%`
              : "—"}
            {alcoholContentPct !== null && alcoholContentPct >= ABV_LIMIT && (
              <span class="ml-2 text-xs bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-bold animate-pulse">
                TTB LIMIT
              </span>
            )}
          </p>
        </div>
        <div>
          <p class="text-[10px] font-medium text-stone-400 uppercase tracking-wide">
            Readings
          </p>
          <p class="text-lg font-bold text-stone-800">{phReadings.length}</p>
        </div>
      </div>

      {/* Expandable readings list */}
      {phReadings.length > 0 && (
        <div class="border-t border-stone-100">
          <button
            type="button"
            onClick={() => (showDetails.value = !showDetails.value)}
            class="w-full px-5 py-3 text-xs text-stone-500 hover:bg-stone-50 transition text-left font-medium min-h-[48px]"
          >
            {showDetails.value ? "▼ Hide" : "▶ Show"} pH Reading History (
            {phReadings.length})
          </button>
          {showDetails.value && (
            <div class="max-h-48 overflow-y-auto">
              {phReadings.map((r, i) => (
                <div
                  key={i}
                  class="px-5 py-2 border-t border-stone-50 flex items-center justify-between text-xs"
                >
                  <span class="text-stone-500">
                    {new Date(r.timestamp).toLocaleString()}
                  </span>
                  <span
                    class={`font-bold ${
                      r.pH <= PH_THRESHOLD
                        ? "text-emerald-700"
                        : "text-amber-700"
                    }`}
                  >
                    pH {r.pH.toFixed(2)}
                  </span>
                  {r.notes && (
                    <span class="text-stone-400 truncate max-w-[200px] ml-2">
                      {r.notes}
                    </span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
