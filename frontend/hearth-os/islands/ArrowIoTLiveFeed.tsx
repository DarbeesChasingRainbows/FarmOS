import { useEffect, useRef } from "preact/hooks";
import { effect } from "@preact/signals";
import { html, reactive } from "@arrow-js/core";
import {
  type ConnectionStatus,
  connectionStatus,
  type SensorEvent,
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

export default function ArrowIoTLiveFeed() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      status: "offline" as ConnectionStatus,
      events: [] as SensorEvent[],
    });

    const disposeSync = effect(() => {
      state.status = connectionStatus.value;
      state.events = [...sensorEvents.value];
    });

    const getWarningCount = () =>
      state.events.filter((e: SensorEvent) => e.alert.level === "Warning")
        .length;
    const getCriticalCount = () =>
      state.events.filter((e: SensorEvent) => e.alert.level === "Critical")
        .length;

    const clearEvents = () => {
      sensorEvents.value = []; // Mutate the true source
    };

    const template = html`
      <div class="flex flex-col h-full overflow-hidden -m-6">
        <!-- Header -->
        <div
          class="px-5 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50"
        >
          <div class="flex items-center gap-3">
            <h3
              class="text-[13px] font-bold text-stone-700 uppercase tracking-widest px-2"
            >
              Live Feed
            </h3>
            ${() =>
              getCriticalCount() > 0
                ? html`
                  <span
                    class="bg-red-100 text-red-700 text-xs font-bold px-2 py-0.5 rounded-full animate-pulse"
                  >${getCriticalCount()} CRITICAL</span>
                `
                : ""} ${() =>
              getWarningCount() > 0
                ? html`
                  <span
                    class="bg-amber-100 text-amber-700 text-xs font-bold px-2 py-0.5 rounded-full"
                  >${getWarningCount()} warnings</span>
                `
                : ""}
          </div>
          <div class="flex items-center gap-2">
            <span class="${() =>
              `w-2 h-2 rounded-full ${
                state.status === "connected"
                  ? "bg-emerald-500 animate-pulse"
                  : "bg-stone-300"
              }`}"></span>
            <span class="${() =>
              `text-xs font-semibold ${
                state.status === "connected"
                  ? "text-emerald-700"
                  : "text-stone-400"
              }`}">${() =>
              state.status === "connected" ? "Live" : "Connecting..."}</span>
          </div>
        </div>

        <!-- Feed -->
        <div class="flex-1 overflow-y-auto">
          ${() =>
            state.status === "offline"
              ? html`
                <div class="px-5 py-3 bg-amber-50 border-b border-amber-100">
                  <p class="text-xs text-amber-700">
                    ⚠️ Unable to connect to live sensor feed.
                  </p>
                </div>
              `
              : ""} ${() => {
            if (state.events.length === 0) {
              return html`
                <div class="flex flex-col items-center justify-center py-10 text-stone-400">
                  <p class="text-3xl mb-2">📡</p>
                  <p class="text-sm">Awaiting sensor data...</p>
                  <p class="text-xs mt-1">Readings will appear here in real-time</p>
                </div>
              `;
            }

            return state.events.map((evt: SensorEvent, i: number) => {
              const styles = alertStyles[evt.alert.level] ?? alertStyles.Safe;
              const dot = alertDot[evt.alert.level] ?? alertDot.Safe;
              const icon = sensorIcon[evt.reading.sensorType] ?? "📊";

              return html`
                <div class="${`flex items-start gap-3 px-4 py-3 border-b border-stone-50 ${
                  i === 0 ? "animate-[slideDown_0.2s_ease-out]" : ""
                }`}">
                  <div class="mt-1 shrink-0">
                    <span class="${`w-2 h-2 rounded-full inline-block ${dot}`}"></span>
                  </div>
                  <div class="${`flex-1 rounded-lg border px-3 py-2 text-xs ${styles}`}">
                    <div class="flex items-center justify-between mb-0.5">
                      <span class="font-bold">${icon} ${evt.reading
                        .deviceId}</span>
                      <span class="opacity-60 ml-2 shrink-0">${new Date(
                        evt.reading.timestamp,
                      ).toLocaleTimeString()}</span>
                    </div>
                    <p>
                      ${evt.reading.sensorType}: <strong>${evt.reading
                        .value}${evt.reading.unit}</strong>
                      <span class="ml-2 opacity-75">(${evt.alert.level})</span>
                    </p>
                    <p class="opacity-80 mt-0.5">${evt.alert.message}</p>
                    ${evt.alert.correctiveAction
                      ? html`
                        <p class="mt-1 font-semibold">⚡ ${evt.alert
                          .correctiveAction}</p>
                      `
                      : ""}
                  </div>
                </div>
              `;
            });
          }}
        </div>

        <!-- Footer -->
        ${() =>
          state.events.length > 0
            ? html`
              <div
                class="px-4 py-2 bg-stone-50 border-t border-stone-100 flex items-center justify-between"
              >
                <span class="text-xs text-stone-400">${() =>
                  state.events.length} event${() =>
                  state.events.length !== 1 ? "s" : ""}</span>
                <button
                  type="button"
                  @click="${clearEvents}"
                  class="text-xs text-stone-400 hover:text-stone-600 transition"
                >
                  Clear
                </button>
              </div>
            `
            : ""}
      </div>
    `;

    template(containerRef.current);

    return () => disposeSync();
  }, []);

  return <div ref={containerRef} class="h-full"></div>;
}
