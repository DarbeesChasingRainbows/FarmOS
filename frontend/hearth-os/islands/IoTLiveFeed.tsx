import {
  connectionStatus,
  sensorEvents,
} from "../utils/connectionState.ts";

const alertStyles: Record<string, string> = {
  Safe: "border-emerald-200 bg-emerald-50 text-emerald-800",
  Warning: "border-amber-200 bg-amber-50 text-amber-800",
  Critical: "border-red-200 bg-red-50 text-red-800",
};

const alertDot: Record<string, string> = {
  Safe: "bg-emerald-500",
  Warning: "bg-amber-500",
  Critical: "bg-red-500",
};

const sensorIcon: Record<string, string> = {
  Temperature: "🌡️",
  PH: "⚗️",
  Humidity: "💧",
  CO2: "🫧",
};

export default function IoTLiveFeed() {
  const connected = connectionStatus.value === "connected";
  const events = sensorEvents.value;

  const criticalCount =
    events.filter((e) => e.alert.level === "Critical").length;
  const warningCount =
    events.filter((e) => e.alert.level === "Warning").length;

  return (
    <div class="bg-white rounded-xl border border-stone-200 shadow-sm overflow-hidden">
      {/* Header */}
      <div class="px-5 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
        <div class="flex items-center gap-3">
          <h3 class="text-base font-bold text-stone-800">
            🌡️ Live Sensor Feed
          </h3>
          {criticalCount > 0 && (
            <span class="bg-red-100 text-red-700 text-xs font-bold px-2 py-0.5 rounded-full animate-pulse">
              {criticalCount} CRITICAL
            </span>
          )}
          {warningCount > 0 && (
            <span class="bg-amber-100 text-amber-700 text-xs font-bold px-2 py-0.5 rounded-full">
              {warningCount} warnings
            </span>
          )}
        </div>
        <div class="flex items-center gap-2">
          <span
            class={`w-2 h-2 rounded-full ${
              connected ? "bg-emerald-500 animate-pulse" : "bg-stone-300"
            }`}
          />
          <span
            class={`text-xs font-semibold ${
              connected ? "text-emerald-700" : "text-stone-400"
            }`}
          >
            {connected ? "Live" : "Connecting..."}
          </span>
        </div>
      </div>

      {/* Feed */}
      <div class="max-h-72 overflow-y-auto">
        {!connected && connectionStatus.value === "offline" && (
          <div class="px-5 py-3 bg-amber-50 border-b border-amber-100">
            <p class="text-xs text-amber-700">⚠️ Unable to connect to live sensor feed.</p>
          </div>
        )}

        {events.length === 0
          ? (
            <div class="flex flex-col items-center justify-center py-10 text-stone-400">
              <p class="text-3xl mb-2">📡</p>
              <p class="text-sm">Awaiting sensor data...</p>
              <p class="text-xs mt-1">Readings will appear here in real-time</p>
            </div>
          )
          : (
            events.map((evt, i) => {
              const styles = alertStyles[evt.alert.level] ?? alertStyles.Safe;
              const dot = alertDot[evt.alert.level] ?? alertDot.Safe;
              const icon = sensorIcon[evt.reading.sensorType] ?? "📊";
              return (
                <div
                  key={i}
                  class={`flex items-start gap-3 px-4 py-3 border-b border-stone-50 ${
                    i === 0 ? "animate-[slideDown_0.2s_ease-out]" : ""
                  }`}
                >
                  <div class="mt-1 shrink-0">
                    <span class={`w-2 h-2 rounded-full inline-block ${dot}`} />
                  </div>
                  <div
                    class={`flex-1 rounded-lg border px-3 py-2 text-xs ${styles}`}
                  >
                    <div class="flex items-center justify-between mb-0.5">
                      <span class="font-bold">
                        {icon} {evt.reading.deviceId}
                      </span>
                      <span class="opacity-60 ml-2 shrink-0">
                        {new Date(evt.reading.timestamp).toLocaleTimeString()}
                      </span>
                    </div>
                    <p>
                      {evt.reading.sensorType}:{" "}
                      <strong>{evt.reading.value}{evt.reading.unit}</strong>
                      <span class="ml-2 opacity-75">({evt.alert.level})</span>
                    </p>
                    <p class="opacity-80 mt-0.5">{evt.alert.message}</p>
                    {evt.alert.correctiveAction && (
                      <p class="mt-1 font-semibold">
                        ⚡ {evt.alert.correctiveAction}
                      </p>
                    )}
                  </div>
                </div>
              );
            })
          )}
      </div>

      {/* Footer */}
      {events.length > 0 && (
        <div class="px-4 py-2 bg-stone-50 border-t border-stone-100 flex items-center justify-between">
          <span class="text-xs text-stone-400">
            {events.length} event{events.length !== 1 ? "s" : ""}
          </span>
          <button
            type="button"
            onClick={() => sensorEvents.value = []}
            class="text-xs text-stone-400 hover:text-stone-600 transition"
          >
            Clear
          </button>
        </div>
      )}
    </div>
  );
}
