import { useComputed, useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import {
  type HaHistoryEntry,
  HaSensorAPI,
  type HaSensorSummary,
} from "@/utils/assets-client.ts";

// ─── Sensor type → icon + color mapping ─────────────────────────────────────

const DEVICE_CLASS_META: Record<
  string,
  { icon: string; color: string; label: string }
> = {
  temperature: { icon: "🌡️", color: "text-red-400", label: "Temperature" },
  humidity: { icon: "💧", color: "text-blue-400", label: "Humidity" },
  moisture: { icon: "🌱", color: "text-emerald-400", label: "Moisture" },
  ph: { icon: "⚗️", color: "text-violet-400", label: "pH" },
  voltage: { icon: "⚡", color: "text-yellow-400", label: "Voltage" },
  battery: { icon: "🔋", color: "text-lime-400", label: "Battery" },
  pressure: { icon: "🌀", color: "text-cyan-400", label: "Pressure" },
  power: { icon: "⚙️", color: "text-orange-400", label: "Power" },
  energy: { icon: "🔌", color: "text-amber-400", label: "Energy" },
  illuminance: { icon: "☀️", color: "text-yellow-300", label: "Light" },
};

const DEFAULT_META = { icon: "📡", color: "text-stone-300", label: "Sensor" };

function getMeta(deviceClass?: string) {
  return DEVICE_CLASS_META[deviceClass ?? ""] ?? DEFAULT_META;
}

// ─── Sparkline SVG ──────────────────────────────────────────────────────────

function Sparkline({ data }: { data: HaHistoryEntry[] }) {
  const values = data
    .map((d) => parseFloat(d.state))
    .filter((v) => !isNaN(v));
  if (values.length < 2) {
    return (
      <p class="text-xs text-stone-500 italic mt-2">
        Not enough data for chart
      </p>
    );
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  const w = 280;
  const h = 60;
  const points = values
    .map((v, i) => {
      const x = (i / (values.length - 1)) * w;
      const y = h - ((v - min) / range) * (h - 4) - 2;
      return `${x},${y}`;
    })
    .join(" ");

  return (
    <div class="mt-3 bg-stone-800/50 rounded-lg p-3">
      <div class="flex justify-between text-[10px] text-stone-500 mb-1">
        <span>{max.toFixed(1)}</span>
        <span class="text-stone-600">24h</span>
        <span>{min.toFixed(1)}</span>
      </div>
      <svg viewBox={`0 0 ${w} ${h}`} class="w-full" preserveAspectRatio="none">
        <polyline
          points={points}
          fill="none"
          stroke="url(#sparkGrad)"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <defs>
          <linearGradient id="sparkGrad" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stop-color="#34d399" />
            <stop offset="100%" stop-color="#6ee7b7" />
          </linearGradient>
        </defs>
      </svg>
    </div>
  );
}

// ─── Detail Sidebar ─────────────────────────────────────────────────────────

function SensorDetailSidebar({
  sensor,
  history,
  onClose,
}: {
  sensor: HaSensorSummary;
  history: HaHistoryEntry[];
  onClose: () => void;
}) {
  const meta = getMeta(sensor.deviceClass);
  const lastChanged = new Date(sensor.lastChanged);
  const ago = Math.round((Date.now() - lastChanged.getTime()) / 60000);
  const agoText = ago < 1
    ? "just now"
    : ago < 60
    ? `${ago}m ago`
    : `${Math.round(ago / 60)}h ago`;

  return (
    <div class="fixed inset-y-0 right-0 w-96 bg-stone-900 border-l border-stone-700 shadow-2xl z-50 flex flex-col overflow-y-auto">
      {/* Header */}
      <div class="flex items-center justify-between px-5 py-4 border-b border-stone-800">
        <div class="flex items-center gap-3">
          <span class="text-2xl">{meta.icon}</span>
          <div>
            <h2 class="text-sm font-bold text-stone-100">
              {sensor.friendlyName ?? sensor.entityId}
            </h2>
            <p class="text-xs text-stone-500 font-mono">{sensor.entityId}</p>
          </div>
        </div>
        <button
          onClick={onClose}
          class="text-stone-500 hover:text-stone-300 text-xl"
        >
          ✕
        </button>
      </div>

      {/* Current Value */}
      <div class="px-5 py-6 text-center border-b border-stone-800">
        <p class={`text-4xl font-bold tracking-tight ${meta.color}`}>
          {sensor.state}
          {sensor.unitOfMeasurement && (
            <span class="text-lg text-stone-500 ml-1">
              {sensor.unitOfMeasurement}
            </span>
          )}
        </p>
        <p class="text-xs text-stone-500 mt-2">Updated {agoText}</p>
      </div>

      {/* Sparkline */}
      <div class="px-5 py-4 border-b border-stone-800">
        <h3 class="text-xs font-semibold text-stone-400 uppercase tracking-wider mb-1">
          24-Hour History
        </h3>
        <Sparkline data={history} />
      </div>

      {/* Attributes */}
      <div class="px-5 py-4 flex-1">
        <h3 class="text-xs font-semibold text-stone-400 uppercase tracking-wider mb-3">
          Details
        </h3>
        <dl class="space-y-2 text-sm">
          <div class="flex justify-between">
            <dt class="text-stone-500">Device Class</dt>
            <dd class="text-stone-200">{sensor.deviceClass ?? "—"}</dd>
          </div>
          <div class="flex justify-between">
            <dt class="text-stone-500">Unit</dt>
            <dd class="text-stone-200">{sensor.unitOfMeasurement ?? "—"}</dd>
          </div>
          <div class="flex justify-between">
            <dt class="text-stone-500">Last Changed</dt>
            <dd class="text-stone-200">{lastChanged.toLocaleString()}</dd>
          </div>
        </dl>
      </div>

      {/* Footer */}
      <div class="px-5 py-4 border-t border-stone-800">
        <a
          href="http://localhost:8123"
          target="_blank"
          rel="noopener"
          class="block w-full text-center py-2 px-4 rounded-lg bg-blue-600/20 text-blue-400 text-sm font-medium hover:bg-blue-600/30 transition-colors"
        >
          Open in Home Assistant →
        </a>
      </div>
    </div>
  );
}

// ─── Sensor Card ────────────────────────────────────────────────────────────

function SensorCard({
  sensor,
  onClick,
}: {
  sensor: HaSensorSummary;
  onClick: () => void;
}) {
  const meta = getMeta(sensor.deviceClass);
  const lastChanged = new Date(sensor.lastChanged);
  const ago = Math.round((Date.now() - lastChanged.getTime()) / 60000);
  const stale = ago > 30;

  return (
    <button
      onClick={onClick}
      class="w-full text-left bg-stone-800/60 hover:bg-stone-800 border border-stone-700/50 hover:border-emerald-600/30 rounded-xl p-4 transition-all duration-200 group cursor-pointer"
    >
      <div class="flex items-start justify-between mb-3">
        <span class="text-2xl">{meta.icon}</span>
        <span
          class={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${
            stale
              ? "bg-amber-900/40 text-amber-400"
              : "bg-emerald-900/40 text-emerald-400"
          }`}
        >
          {stale ? "STALE" : "LIVE"}
        </span>
      </div>
      <p class="text-xs text-stone-500 truncate mb-1">
        {sensor.friendlyName ?? sensor.entityId}
      </p>
      <p class={`text-2xl font-bold tracking-tight ${meta.color}`}>
        {sensor.state}
        {sensor.unitOfMeasurement && (
          <span class="text-sm text-stone-500 ml-1">
            {sensor.unitOfMeasurement}
          </span>
        )}
      </p>
      <p class="text-[10px] text-stone-600 mt-2">
        {ago < 1
          ? "just now"
          : ago < 60
          ? `${ago}m ago`
          : `${Math.round(ago / 60)}h ago`}
      </p>
    </button>
  );
}

// ─── Main Panel ─────────────────────────────────────────────────────────────

const FILTER_TABS = [
  { key: "all", label: "All" },
  { key: "temperature", label: "🌡️ Temp" },
  { key: "humidity", label: "💧 Humidity" },
  { key: "moisture", label: "🌱 Moisture" },
  { key: "battery", label: "🔋 Battery" },
  { key: "voltage", label: "⚡ Voltage" },
  { key: "other", label: "📡 Other" },
];

const KNOWN_CLASSES = new Set(Object.keys(DEVICE_CLASS_META));

export default function SensorPanel() {
  const sensors = useSignal<HaSensorSummary[]>([]);
  const loading = useSignal(true);
  const error = useSignal<string | null>(null);
  const haAvailable = useSignal<boolean | null>(null);
  const activeFilter = useSignal("all");
  const selectedSensor = useSignal<HaSensorSummary | null>(null);
  const sensorHistory = useSignal<HaHistoryEntry[]>([]);

  // Load sensors
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [status, list] = await Promise.all([
          HaSensorAPI.status().catch(() => ({ available: false })),
          HaSensorAPI.list().catch(() => [] as HaSensorSummary[]),
        ]);
        if (cancelled) return;
        haAvailable.value = status.available;
        sensors.value = list;
      } catch (e) {
        if (!cancelled) error.value = String(e);
      } finally {
        if (!cancelled) loading.value = false;
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  // Filter sensors
  const filtered = useComputed(() => {
    const f = activeFilter.value;
    if (f === "all") return sensors.value;
    if (f === "other") {
      return sensors.value.filter((s) =>
        !KNOWN_CLASSES.has(s.deviceClass ?? "")
      );
    }
    return sensors.value.filter((s) => s.deviceClass === f);
  });

  // Select sensor
  const selectSensor = async (sensor: HaSensorSummary) => {
    selectedSensor.value = sensor;
    try {
      const history = await HaSensorAPI.history(sensor.entityId, 24);
      sensorHistory.value = history;
    } catch {
      sensorHistory.value = [];
    }
  };

  if (loading.value) {
    return (
      <div class="flex items-center justify-center h-64">
        <div class="animate-pulse text-stone-500">Loading sensors…</div>
      </div>
    );
  }

  return (
    <div class="relative">
      {/* Header */}
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h1 class="text-2xl font-bold text-stone-100">📡 Sensors</h1>
          <p class="text-sm text-stone-500 mt-1">
            Live data from Home Assistant •{" "}
            <span
              class={haAvailable.value ? "text-emerald-400" : "text-red-400"}
            >
              {haAvailable.value ? "Connected" : "Disconnected"}
            </span>
          </p>
        </div>
        <a
          href="http://localhost:8123"
          target="_blank"
          rel="noopener"
          class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600/20 text-blue-400 rounded-lg text-sm font-medium hover:bg-blue-600/30 transition-colors"
        >
          <span>🏠</span> Open Home Assistant
        </a>
      </div>

      {/* Filter Tabs */}
      <div class="flex gap-1 mb-6 overflow-x-auto pb-1">
        {FILTER_TABS.map((tab) => (
          <button
            key={tab.key}
            onClick={() => (activeFilter.value = tab.key)}
            class={`px-3 py-1.5 rounded-lg text-xs font-medium whitespace-nowrap transition-colors ${
              activeFilter.value === tab.key
                ? "bg-emerald-600/20 text-emerald-300"
                : "text-stone-500 hover:bg-stone-800 hover:text-stone-300"
            }`}
          >
            {tab.label}
          </button>
        ))}
        <span class="ml-auto text-xs text-stone-600 self-center">
          {filtered.value.length} sensor{filtered.value.length !== 1 ? "s" : ""}
        </span>
      </div>

      {/* Empty state */}
      {sensors.value.length === 0 && !error.value && (
        <div class="text-center py-16 px-4">
          <p class="text-4xl mb-4">🔌</p>
          <h2 class="text-lg font-semibold text-stone-300 mb-2">
            No Sensors Found
          </h2>
          <p class="text-sm text-stone-500 max-w-md mx-auto mb-6">
            {haAvailable.value
              ? "Home Assistant is connected but no sensor entities were found. Add sensors in Home Assistant and they'll appear here automatically."
              : "Can't reach Home Assistant. Make sure it's running at http://localhost:8123 and you've set the HA_TOKEN environment variable."}
          </p>
          <a
            href="http://localhost:8123"
            target="_blank"
            rel="noopener"
            class="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600/20 text-emerald-400 rounded-lg text-sm font-medium hover:bg-emerald-600/30 transition-colors"
          >
            Configure Home Assistant →
          </a>
        </div>
      )}

      {/* Error */}
      {error.value && (
        <div class="bg-red-900/20 border border-red-800/50 rounded-xl p-4 mb-6">
          <p class="text-sm text-red-400">{error.value}</p>
        </div>
      )}

      {/* Card Grid */}
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
        {filtered.value.map((sensor) => (
          <SensorCard
            key={sensor.entityId}
            sensor={sensor}
            onClick={() => selectSensor(sensor)}
          />
        ))}
      </div>

      {/* Detail Sidebar */}
      {selectedSensor.value && (
        <>
          <div
            class="fixed inset-0 bg-black/40 z-40"
            onClick={() => (selectedSensor.value = null)}
          />
          <SensorDetailSidebar
            sensor={selectedSensor.value}
            history={sensorHistory.value}
            onClose={() => (selectedSensor.value = null)}
          />
        </>
      )}
    </div>
  );
}
