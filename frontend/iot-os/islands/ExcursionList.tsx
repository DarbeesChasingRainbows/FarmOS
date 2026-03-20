import { useSignal } from "@preact/signals";
import type { ActiveExcursion } from "../utils/iot-client.ts";

interface Props {
  initialExcursions: ActiveExcursion[];
}

const SEVERITY_STYLES: Record<string, string> = {
  Critical: "border-red-500/50 bg-red-950/50 text-red-200",
  Warning: "border-amber-500/50 bg-amber-950/50 text-amber-200",
};

const SEVERITY_BADGE: Record<string, string> = {
  Critical: "bg-red-600 text-white animate-[pulse-red_1.5s_ease-in-out_infinite]",
  Warning: "bg-amber-600 text-white",
};

export default function ExcursionList({ initialExcursions }: Props) {
  const excursions = useSignal(initialExcursions);

  if (excursions.value.length === 0) {
    return (
      <div class="bg-stone-800/60 rounded-2xl border border-stone-700/50 p-12 text-center">
        <p class="text-4xl mb-4">✅</p>
        <p class="text-lg font-bold text-emerald-400 mb-2">All clear</p>
        <p class="text-sm text-stone-500">
          No active excursions. All zones operating within thresholds.
        </p>
      </div>
    );
  }

  return (
    <div class="space-y-4">
      {excursions.value.map((exc) => {
        const styles = SEVERITY_STYLES[exc.severity] || SEVERITY_STYLES.Warning;
        const badge = SEVERITY_BADGE[exc.severity] || SEVERITY_BADGE.Warning;

        return (
          <div
            key={exc.excursionId}
            class={`rounded-2xl border-2 p-6 ${styles}`}
            style="animation: slideUp 0.3s ease-out"
          >
            {/* Header */}
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-3">
                <span class="text-2xl">
                  {exc.severity === "Critical" ? "🚨" : "⚠️"}
                </span>
                <div>
                  <span class={`px-3 py-1 rounded-lg text-xs font-black ${badge}`}>
                    {exc.severity.toUpperCase()}
                  </span>
                  <span class="ml-3 text-sm font-bold text-stone-300">
                    {exc.sensorType}
                  </span>
                </div>
              </div>
              <span class="text-xs text-stone-500">
                {new Date(exc.startedAt).toLocaleString()}
              </span>
            </div>

            {/* Alert Message */}
            <p class="text-sm font-semibold mb-3 leading-relaxed">
              {exc.alertMessage}
            </p>

            {/* Corrective Action */}
            {exc.correctiveAction && (
              <div class="bg-stone-900/40 rounded-xl p-4 border border-stone-700/30">
                <p class="text-xs font-bold text-amber-400 mb-1">⚡ CORRECTIVE ACTION</p>
                <p class="text-sm text-stone-300">{exc.correctiveAction}</p>
              </div>
            )}

            {/* Quick Actions — big tap targets */}
            <div class="flex gap-3 mt-4">
              <button
                type="button"
                class="flex-1 py-4 px-4 rounded-xl bg-stone-700/50 hover:bg-stone-600/50 active:bg-stone-500/50 text-sm font-bold text-stone-200 transition min-h-[56px]"
              >
                ✓ Acknowledge
              </button>
              <button
                type="button"
                class="flex-1 py-4 px-4 rounded-xl bg-amber-700/50 hover:bg-amber-600/50 active:bg-amber-500/50 text-sm font-bold text-amber-100 transition min-h-[56px]"
              >
                📋 Open CAPA
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
