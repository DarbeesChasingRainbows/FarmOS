import { useSignal } from "@preact/signals";
import { MushroomAPI } from "../utils/farmos-client.ts";
import { showToast } from "../utils/toastState.ts";

interface Props {
  batchId: string;
  currentPhase: number; // 0=Incubation, 1=Pinning, 2=Fruiting, 3=Completed, 4=Contaminated
}

export default function MushroomActionPanel({ batchId, currentPhase }: Props) {
  const isSubmitting = useSignal(false);
  const activeTab = useSignal<"environment" | "phase" | "flush">("environment");

  // Env Form
  const tempF = useSignal("");
  const humidity = useSignal("");

  // Flush Form
  const flushYield = useSignal("");

  const recordEnvironment = async () => {
    isSubmitting.value = true;
    try {
      if (tempF.value) {
        await MushroomAPI.recordTemperature(batchId, {
          temperatureF: Number(tempF.value),
        });
      }
      if (humidity.value) {
        await MushroomAPI.recordHumidity(batchId, {
          humidityPercent: Number(humidity.value),
        });
      }
      showToast("success", "Environment recorded successfully!");
      tempF.value = "";
      humidity.value = "";
    } catch (err: unknown) {
      showToast(
        "error",
        err instanceof Error ? err.message : "Failed to record environment",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const advancePhase = async (newPhaseStr: string) => {
    isSubmitting.value = true;
    try {
      await MushroomAPI.advancePhase(batchId, { newPhase: newPhaseStr });
      showToast("success", `Advanced to ${newPhaseStr}!`);
      setTimeout(() => window.location.reload(), 1000);
    } catch (err: unknown) {
      showToast(
        "error",
        err instanceof Error ? err.message : "Failed to advance phase",
      );
      isSubmitting.value = false;
    }
  };

  const recordFlush = async () => {
    if (!flushYield.value) return;
    isSubmitting.value = true;
    try {
      await MushroomAPI.recordFlush(batchId, {
        yieldQty: {
          value: Number(flushYield.value),
          unit: "lbs",
          type: "mass",
        },
        flushNumber: 1, // Simplifying for demo
        date: new Date().toISOString(),
      });
      showToast("success", "Harvest flush recorded!");
      flushYield.value = "";
    } catch (err: unknown) {
      showToast(
        "error",
        err instanceof Error ? err.message : "Failed to record flush",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const inputClass =
    "block w-full rounded-lg border-0 py-2 px-3 text-sm text-stone-900 shadow-sm ring-1 ring-inset ring-stone-300 focus:ring-2 focus:ring-inset focus:ring-emerald-600";
  const btnClass =
    "w-full rounded-md bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500 disabled:opacity-50 transition";
  const tabClass = (id: string) =>
    `flex-1 py-2 text-xs font-bold text-center border-b-2 transition ${
      activeTab.value === id
        ? "border-emerald-600 text-emerald-700"
        : "border-transparent text-stone-500 hover:text-stone-700 border-b-stone-200"
    }`;

  return (
    <div class="space-y-4">
      <div class="flex">
        <button
          class={tabClass("environment")}
          onClick={() => activeTab.value = "environment"}
        >
          Environment
        </button>
        <button
          class={tabClass("phase")}
          onClick={() => activeTab.value = "phase"}
        >
          Phase
        </button>
        <button
          class={tabClass("flush")}
          onClick={() => activeTab.value = "flush"}
          disabled={currentPhase < 2}
        >
          Harvest
        </button>
      </div>

      {activeTab.value === "environment" && (
        <div class="space-y-4 pt-2">
          <div>
            <label class="block text-xs font-medium text-stone-700 mb-1">
              Temperature (°F)
            </label>
            <input
              type="number"
              class={inputClass}
              placeholder="e.g. 72"
              value={tempF.value}
              onInput={(e) =>
                tempF.value = (e.target as HTMLInputElement).value}
            />
          </div>
          <div>
            <label class="block text-xs font-medium text-stone-700 mb-1">
              Humidity (%)
            </label>
            <input
              type="number"
              class={inputClass}
              placeholder="e.g. 85"
              value={humidity.value}
              onInput={(e) =>
                humidity.value = (e.target as HTMLInputElement).value}
            />
          </div>
          <button
            onClick={recordEnvironment}
            disabled={isSubmitting.value || (!tempF.value && !humidity.value)}
            class={btnClass}
          >
            Log Environment
          </button>
        </div>
      )}

      {activeTab.value === "phase" && (
        <div class="space-y-3 pt-2">
          <p class="text-xs text-stone-500 mb-2">
            Advance the biological phase of this block.
          </p>
          {currentPhase === 0 && (
            <button
              onClick={() => advancePhase("Pinning")}
              disabled={isSubmitting.value}
              class="w-full rounded-md bg-blue-100 text-blue-800 border border-blue-200 px-4 py-2 text-sm font-bold hover:bg-blue-200 transition"
            >
              Start Pinning
            </button>
          )}
          {currentPhase === 1 && (
            <button
              onClick={() => advancePhase("Fruiting")}
              disabled={isSubmitting.value}
              class="w-full rounded-md bg-emerald-100 text-emerald-800 border border-emerald-200 px-4 py-2 text-sm font-bold hover:bg-emerald-200 transition"
            >
              Start Fruiting
            </button>
          )}
          <button class="w-full rounded-md bg-stone-100 text-stone-600 px-4 py-2 text-sm hover:bg-stone-200 transition">
            Mark Completed
          </button>
          <div class="pt-4 border-t border-stone-100">
            <button class="w-full rounded-md bg-red-50 text-red-700 border border-red-200 px-4 py-2 text-sm hover:bg-red-100 transition">
              Mark Contaminated
            </button>
          </div>
        </div>
      )}

      {activeTab.value === "flush" && (
        <div class="space-y-4 pt-2">
          <div>
            <label class="block text-xs font-medium text-stone-700 mb-1">
              Yield (lbs)
            </label>
            <input
              type="number"
              step="0.1"
              class={inputClass}
              placeholder="e.g. 1.5"
              value={flushYield.value}
              onInput={(e) =>
                flushYield.value = (e.target as HTMLInputElement).value}
            />
          </div>
          <button
            onClick={recordFlush}
            disabled={isSubmitting.value || !flushYield.value}
            class={btnClass}
          >
            Record Harvest Flush
          </button>
        </div>
      )}
    </div>
  );
}
