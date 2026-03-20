import { useSignal } from "@preact/signals";
import { ZONE_TYPE_NAMES } from "../utils/iot-client.ts";
import type { ZoneSummaryDto } from "../utils/iot-client.ts";

const ZONE_COLORS: Record<string, string> = {
  Freezer: "border-blue-500/40 bg-blue-950/40",
  Refrigerator: "border-cyan-500/40 bg-cyan-950/40",
  Storage: "border-amber-500/40 bg-amber-950/40",
  FermentationRoom: "border-purple-500/40 bg-purple-950/40",
  Greenhouse: "border-green-500/40 bg-green-950/40",
  Field: "border-lime-500/40 bg-lime-950/40",
  Apothecary: "border-rose-500/40 bg-rose-950/40",
};

const ZONE_ICONS: Record<string, string> = {
  Freezer: "🧊",
  Refrigerator: "🧊",
  Storage: "📦",
  FermentationRoom: "🫙",
  Greenhouse: "🌿",
  Field: "🌾",
  Apothecary: "🌿",
};

interface Props {
  initialZones: ZoneSummaryDto[];
}

export default function ZoneGridDashboard({ initialZones }: Props) {
  const zones = useSignal(initialZones);

  if (zones.value.length === 0) {
    return (
      <div class="bg-stone-800/60 rounded-2xl border border-stone-700/50 p-12 text-center">
        <p class="text-4xl mb-4">📡</p>
        <p class="text-lg font-bold text-stone-400 mb-2">No zones configured</p>
        <p class="text-sm text-stone-500">
          Create zones via the IoT API to start monitoring.
        </p>
      </div>
    );
  }

  return (
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
      {zones.value.map((zone) => {
        const typeName = ZONE_TYPE_NAMES[zone.zoneType] || "Unknown";
        const colors = ZONE_COLORS[typeName] || "border-stone-600/40 bg-stone-800/40";
        const icon = ZONE_ICONS[typeName] || "📊";

        return (
          <div
            key={zone.id}
            class={`rounded-2xl border-2 p-6 transition-all hover:scale-[1.02] cursor-pointer active:scale-[0.98] ${colors}`}
          >
            {/* Zone Header */}
            <div class="flex items-center justify-between mb-4">
              <div class="flex items-center gap-3">
                <span class="text-3xl">{icon}</span>
                <div>
                  <h3 class="text-lg font-black text-amber-50">{zone.name}</h3>
                  <span class="text-xs font-semibold text-stone-400 uppercase tracking-wider">
                    {typeName}
                  </span>
                </div>
              </div>
              <div class="w-3 h-3 rounded-full bg-emerald-500 animate-pulse" title="Online" />
            </div>

            {/* Placeholder for live readings — will be populated via SignalR */}
            <div class="grid grid-cols-2 gap-3 mt-4">
              <div class="bg-stone-900/50 rounded-xl p-3 text-center">
                <div class="text-2xl font-black text-amber-50">--</div>
                <div class="text-xs text-stone-500 mt-1">🌡️ Temp</div>
              </div>
              <div class="bg-stone-900/50 rounded-xl p-3 text-center">
                <div class="text-2xl font-black text-amber-50">--</div>
                <div class="text-xs text-stone-500 mt-1">💧 Humidity</div>
              </div>
            </div>

            {/* Status bar */}
            <div class="mt-4 flex items-center justify-between text-xs">
              <span class="text-stone-500">Awaiting data...</span>
              <span class="px-2 py-1 rounded-lg bg-emerald-900/30 text-emerald-400 font-bold border border-emerald-700/30">
                Normal
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
