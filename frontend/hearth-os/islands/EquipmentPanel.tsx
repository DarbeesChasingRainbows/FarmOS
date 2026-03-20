import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  type FieldErrors,
  TempLogSchema,
} from "../utils/schemas.ts";

interface Equipment {
  id: string;
  name: string;
  category: "fridge" | "freezer" | "hothold" | "proofbox";
  lastTempF: number;
  minTodayF: number;
  maxTodayF: number;
  lastChecked: string;
  safeMin: number;
  safeMax?: number;
}

const categoryIcon: Record<string, string> = {
  fridge: "🧊",
  freezer: "❄️",
  hothold: "♨️",
  proofbox: "🌡️",
};

function safeStatus(eq: Equipment): "active" | "attention" | "idle" {
  const { lastTempF, safeMin, safeMax } = eq;
  if (lastTempF < safeMin) return "attention";
  if (safeMax !== undefined && lastTempF > safeMax) return "attention";
  return "active";
}

export default function EquipmentPanel() {
  const equipment = useSignal<Equipment[]>([
    {
      id: "eq-fridge-1",
      name: "Walk-in Fridge",
      category: "fridge",
      lastTempF: 38,
      minTodayF: 36,
      maxTodayF: 41,
      lastChecked: "Today 8:00 AM",
      safeMin: 32,
      safeMax: 41,
    },
    {
      id: "eq-freezer-1",
      name: "Chest Freezer",
      category: "freezer",
      lastTempF: -5,
      minTodayF: -8,
      maxTodayF: 0,
      lastChecked: "Today 8:00 AM",
      safeMin: -20,
      safeMax: 0,
    },
    {
      id: "eq-hothold-1",
      name: "Steam Table",
      category: "hothold",
      lastTempF: 143,
      minTodayF: 140,
      maxTodayF: 165,
      lastChecked: "Today 11:00 AM",
      safeMin: 140,
    },
    {
      id: "eq-proofbox-1",
      name: "Proof Box",
      category: "proofbox",
      lastTempF: 80,
      minTodayF: 78,
      maxTodayF: 82,
      lastChecked: "Today 9:30 AM",
      safeMin: 70,
      safeMax: 90,
    },
  ]);

  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);
  const showLogForm = useSignal(false);
  const tempValue = useSignal("");
  const tempNotes = useSignal("");
  const logErrors = useSignal<FieldErrors>({});
  const isSubmitting = useSignal(false);

  const selected = equipment.value.find((e) => e.id === selectedId.value);

  const openSidebar = (id: string) => {
    selectedId.value = id;
    showLogForm.value = false;
    isSidebarOpen.value = true;
  };

  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null;
    }, 300);
  };

  const handleLogTemp = async (eq: Equipment) => {
    const result = TempLogSchema.safeParse({
      equipmentId: eq.id,
      tempF: tempValue.value,
      notes: tempNotes.value || undefined,
    });
    if (!result.success) {
      logErrors.value = extractErrors(result);
      return;
    }
    isSubmitting.value = true;
    logErrors.value = {};
    try {
      const { KitchenAPI } = await import("../utils/farmos-client.ts");
      await KitchenAPI.logTemp(result.data);
      showToast(
        "success",
        "Temperature logged",
        `${eq.name}: ${tempValue.value}°F recorded.`,
      );
      tempValue.value = "";
      tempNotes.value = "";
      showLogForm.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to log temp",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const tempColor = (eq: Equipment) => {
    const s = safeStatus(eq);
    return s === "attention" ? "text-red-600" : "text-emerald-600";
  };

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
      logErrors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  return (
    <div class="relative flex min-h-[400px]">
      {/* Cards */}
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[400px]" : ""
        }`}
      >
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {equipment.value.map((eq) => {
            const status = safeStatus(eq);
            const isSelected = selectedId.value === eq.id;
            return (
              <button
                type="button"
                onClick={() => openSidebar(eq.id)}
                class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : status === "attention"
                    ? "border-red-300 bg-red-50/30"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <div class="flex items-start justify-between mb-3">
                  <span class="text-2xl">{categoryIcon[eq.category]}</span>
                  <StatusBadge variant={status} />
                </div>
                <h3 class="text-base font-bold text-stone-800 mb-1">
                  {eq.name}
                </h3>
                <p class={`text-3xl font-mono font-bold ${tempColor(eq)}`}>
                  {eq.lastTempF}°F
                </p>
                <p class="text-xs text-stone-400 mt-2">
                  Last: {eq.lastChecked}
                </p>
                <p class="text-xs text-stone-400">
                  Range today: {eq.minTodayF}–{eq.maxTodayF}°F
                </p>
              </button>
            );
          })}
        </div>
      </div>

      {/* Mobile Backdrop */}
      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30"
          onClick={closeSidebar}
        />
      )}

      {/* Sidebar */}
      <aside
        class={`fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selected && (
          <div class="p-6 flex flex-col h-full">
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-2xl">
                    {categoryIcon[selected.category]}
                  </span>
                  <h3 class="text-xl font-bold text-stone-800">
                    {selected.name}
                  </h3>
                </div>
                <StatusBadge variant={safeStatus(selected)} />
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>

            <div class="flex-1">
              {/* Safe Zone Info */}
              <div class="bg-blue-50 rounded-lg p-4 mb-5 border border-blue-100 text-sm text-blue-800">
                <p class="font-semibold mb-1">Safe Temperature Zone</p>
                <p class="text-xs text-blue-700">
                  Min: <strong>{selected.safeMin}°F</strong>
                  {selected.safeMax !== undefined && (
                    <>
                      · Max: <strong>{selected.safeMax}°F</strong>
                    </>
                  )}
                  {selected.category === "hothold" && " · FDA: hot-hold ≥140°F"}
                  {selected.category === "fridge" &&
                    " · FDA: refrigeration ≤41°F"}
                  {selected.category === "freezer" && " · FDA: frozen ≤0°F"}
                </p>
              </div>

              {/* Stats */}
              <div class="grid grid-cols-3 gap-3 mb-6">
                <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                  <p
                    class={`text-xl font-mono font-bold ${tempColor(selected)}`}
                  >
                    {selected.lastTempF}°F
                  </p>
                  <p class="text-xs text-stone-400 mt-1">Current</p>
                </div>
                <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                  <p class="text-xl font-mono font-bold text-stone-700">
                    {selected.minTodayF}°F
                  </p>
                  <p class="text-xs text-stone-400 mt-1">Min Today</p>
                </div>
                <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                  <p class="text-xl font-mono font-bold text-stone-700">
                    {selected.maxTodayF}°F
                  </p>
                  <p class="text-xs text-stone-400 mt-1">Max Today</p>
                </div>
              </div>

              {/* Log Temp Form */}
              {showLogForm.value && (
                <div class="mb-5 p-4 bg-stone-50 rounded-lg border border-stone-200">
                  <h4 class="text-sm font-bold text-stone-700 mb-3">
                    Log Temperature Reading
                  </h4>
                  <div class="flex flex-col gap-3">
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">
                        Temperature (°F)
                      </label>
                      <input
                        type="number"
                        step="0.1"
                        class={`mt-1 ${inputClass("tempF")}`}
                        placeholder="e.g. 38.5"
                        value={tempValue.value}
                        onInput={(e) =>
                          tempValue.value =
                            (e.target as HTMLInputElement).value}
                      />
                      {logErrors.value.tempF && (
                        <p class="text-xs text-red-500 mt-1">
                          {logErrors.value.tempF}
                        </p>
                      )}
                    </div>
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">
                        Notes (optional)
                      </label>
                      <input
                        type="text"
                        class={`mt-1 ${inputClass("notes")}`}
                        placeholder="e.g. After restocking"
                        value={tempNotes.value}
                        onInput={(e) =>
                          tempNotes.value =
                            (e.target as HTMLInputElement).value}
                      />
                    </div>
                    <div class="flex gap-2">
                      <button
                        onClick={() => handleLogTemp(selected)}
                        disabled={isSubmitting.value || !tempValue.value}
                        class="flex-1 py-2 text-sm font-bold bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 transition"
                      >
                        {isSubmitting.value ? "Saving..." : "Log Reading"}
                      </button>
                      <button
                        onClick={() => showLogForm.value = false}
                        class="py-2 px-3 text-sm font-bold text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition"
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div class="pt-4 border-t border-stone-100 mt-auto">
              {!showLogForm.value && (
                <button
                  onClick={() => showLogForm.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-800 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2"
                >
                  🌡️ Log Temperature Reading
                </button>
              )}
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
