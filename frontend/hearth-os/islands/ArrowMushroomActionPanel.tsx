import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { MushroomAPI } from "../utils/farmos-client.ts";
import { showToast } from "../utils/toastState.ts";

interface Props {
  batchId: string;
  currentPhase: number;
}

export default function ArrowMushroomActionPanel({ batchId, currentPhase }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      isSubmitting: false,
      activeTab: "environment" as "environment" | "phase" | "flush",
      tempF: "",
      humidity: "",
      flushYield: "",
    });

    const inputClass = "block w-full rounded-lg border-0 py-2 px-3 text-sm text-stone-900 shadow-sm ring-1 ring-inset ring-stone-300 focus:ring-2 focus:ring-inset focus:ring-emerald-600";
    const btnClass = "w-full rounded-md bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500 disabled:opacity-50 transition";

    const tabClass = (id: string) => () =>
      `flex-1 py-2 text-xs font-bold text-center border-b-2 transition ${state.activeTab === id ? "border-emerald-600 text-emerald-700" : "border-transparent text-stone-500 hover:text-stone-700"}`;

    const recordEnvironment = async () => {
      state.isSubmitting = true;
      try {
        if (state.tempF) {
          await MushroomAPI.recordTemperature(batchId, { temperatureF: Number(state.tempF) });
        }
        if (state.humidity) {
          await MushroomAPI.recordHumidity(batchId, { humidityPercent: Number(state.humidity) });
        }
        showToast("success", "Environment recorded successfully!");
        state.tempF = "";
        state.humidity = "";
      } catch (err: unknown) {
        showToast("error", err instanceof Error ? err.message : "Failed to record environment");
      } finally {
        state.isSubmitting = false;
      }
    };

    const advancePhase = async (newPhaseStr: string) => {
      state.isSubmitting = true;
      try {
        await MushroomAPI.advancePhase(batchId, { newPhase: newPhaseStr });
        showToast("success", `Advanced to ${newPhaseStr}!`);
        setTimeout(() => globalThis.location.reload(), 1000);
      } catch (err: unknown) {
        showToast("error", err instanceof Error ? err.message : "Failed to advance phase");
        state.isSubmitting = false;
      }
    };

    const recordFlush = async () => {
      if (!state.flushYield) return;
      state.isSubmitting = true;
      try {
        await MushroomAPI.recordFlush(batchId, {
          yieldQty: { value: Number(state.flushYield), unit: "lbs", type: "mass" },
          flushNumber: 1,
          date: new Date().toISOString(),
        });
        showToast("success", "Harvest flush recorded!");
        state.flushYield = "";
      } catch (err: unknown) {
        showToast("error", err instanceof Error ? err.message : "Failed to record flush");
      } finally {
        state.isSubmitting = false;
      }
    };

    const template = html`
      <div class="space-y-4">
        <!-- Tabs -->
        <div class="flex">
          <button class="${tabClass("environment")}" @click="${() => state.activeTab = "environment"}">Environment</button>
          <button class="${tabClass("phase")}" @click="${() => state.activeTab = "phase"}">Phase</button>
          <button
            class="${tabClass("flush")}"
            @click="${() => state.activeTab = "flush"}"
            disabled="${currentPhase < 2}"
          >Harvest</button>
        </div>

        <!-- Environment Tab -->
        ${() => state.activeTab === "environment" ? html`
          <div class="space-y-4 pt-2">
            <div>
              <label class="block text-xs font-medium text-stone-700 mb-1">Temperature (°F)</label>
              <input
                type="number"
                class="${inputClass}"
                placeholder="e.g. 72"
                value="${() => state.tempF}"
                @input="${(e: Event) => state.tempF = (e.target as HTMLInputElement).value}"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-stone-700 mb-1">Humidity (%)</label>
              <input
                type="number"
                class="${inputClass}"
                placeholder="e.g. 85"
                value="${() => state.humidity}"
                @input="${(e: Event) => state.humidity = (e.target as HTMLInputElement).value}"
              />
            </div>
            <button
              @click="${recordEnvironment}"
              disabled="${() => state.isSubmitting || (!state.tempF && !state.humidity)}"
              class="${btnClass}"
            >
              Log Environment
            </button>
          </div>
        ` : ""}

        <!-- Phase Tab -->
        ${() => state.activeTab === "phase" ? html`
          <div class="space-y-3 pt-2">
            <p class="text-xs text-stone-500 mb-2">Advance the biological phase of this block.</p>
            ${currentPhase === 0 ? html`
              <button
                @click="${() => advancePhase("Pinning")}"
                disabled="${() => state.isSubmitting}"
                class="w-full rounded-md bg-blue-100 text-blue-800 border border-blue-200 px-4 py-2 text-sm font-bold hover:bg-blue-200 transition"
              >Start Pinning</button>
            ` : ""}
            ${currentPhase === 1 ? html`
              <button
                @click="${() => advancePhase("Fruiting")}"
                disabled="${() => state.isSubmitting}"
                class="w-full rounded-md bg-emerald-100 text-emerald-800 border border-emerald-200 px-4 py-2 text-sm font-bold hover:bg-emerald-200 transition"
              >Start Fruiting</button>
            ` : ""}
            <button class="w-full rounded-md bg-stone-100 text-stone-600 px-4 py-2 text-sm hover:bg-stone-200 transition">
              Mark Completed
            </button>
            <div class="pt-4 border-t border-stone-100">
              <button class="w-full rounded-md bg-red-50 text-red-700 border border-red-200 px-4 py-2 text-sm hover:bg-red-100 transition">
                Mark Contaminated
              </button>
            </div>
          </div>
        ` : ""}

        <!-- Flush/Harvest Tab -->
        ${() => state.activeTab === "flush" ? html`
          <div class="space-y-4 pt-2">
            <div>
              <label class="block text-xs font-medium text-stone-700 mb-1">Yield (lbs)</label>
              <input
                type="number"
                step="0.1"
                class="${inputClass}"
                placeholder="e.g. 1.5"
                value="${() => state.flushYield}"
                @input="${(e: Event) => state.flushYield = (e.target as HTMLInputElement).value}"
              />
            </div>
            <button
              @click="${recordFlush}"
              disabled="${() => state.isSubmitting || !state.flushYield}"
              class="${btnClass}"
            >
              Record Harvest Flush
            </button>
          </div>
        ` : ""}
      </div>
    `;

    template(containerRef.current);
  }, [batchId, currentPhase]);

  return <div ref={containerRef}></div>;
}
