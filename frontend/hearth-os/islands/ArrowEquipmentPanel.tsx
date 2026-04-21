import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

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

const categoryIcon: Record<string, string> = { fridge: "🧊", freezer: "❄️", hothold: "♨️", proofbox: "🌡️" };
const safeStatus = (eq: Equipment) => {
  if (eq.lastTempF < eq.safeMin) return "attention";
  if (eq.safeMax !== undefined && eq.lastTempF > eq.safeMax) return "attention";
  return "active";
};
const tempColor = (eq: Equipment) => safeStatus(eq) === "attention" ? "text-red-600" : "text-emerald-600";
const statusDot = (eq: Equipment) => safeStatus(eq) === "attention" ? "bg-red-500" : "bg-emerald-500";

const MOCK_EQUIPMENT: Equipment[] = [
  { id: "eq-fridge-1", name: "Walk-in Fridge", category: "fridge", lastTempF: 38, minTodayF: 36, maxTodayF: 41, lastChecked: "Today 8:00 AM", safeMin: 32, safeMax: 41 },
  { id: "eq-freezer-1", name: "Chest Freezer", category: "freezer", lastTempF: -5, minTodayF: -8, maxTodayF: 0, lastChecked: "Today 8:00 AM", safeMin: -20, safeMax: 0 },
  { id: "eq-hothold-1", name: "Steam Table", category: "hothold", lastTempF: 143, minTodayF: 140, maxTodayF: 165, lastChecked: "Today 11:00 AM", safeMin: 140 },
  { id: "eq-proofbox-1", name: "Proof Box", category: "proofbox", lastTempF: 80, minTodayF: 78, maxTodayF: 82, lastChecked: "Today 9:30 AM", safeMin: 70, safeMax: 90 },
];

export default function ArrowEquipmentPanel() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      equipment: [...MOCK_EQUIPMENT],
      selectedId: null as string | null,
      sidebarOpen: false,
      showLogForm: false,
      tempValue: "",
      tempNotes: "",
      errors: {} as Record<string, string>,
      submitting: false,
    });

    const selected = () => state.equipment.find(e => e.id === state.selectedId);
    const openSidebar = (id: string) => { state.selectedId = id; state.showLogForm = false; state.sidebarOpen = true; };
    const closeSidebar = () => { state.sidebarOpen = false; setTimeout(() => { state.selectedId = null; }, 300); };

    const handleLogTemp = async () => {
      const eq = selected();
      if (!eq) return;
      if (!state.tempValue) { setErrors(state.errors, { tempF: "Temperature is required" }); return; }
      state.submitting = true;
      clearErrors(state.errors);
      try {
        const { KitchenAPI } = await import("../utils/farmos-client.ts");
        await KitchenAPI.logTemp({ equipmentId: eq.id, tempF: Number(state.tempValue), notes: state.tempNotes || undefined });
        showToast("success", "Temperature logged", `${eq.name}: ${state.tempValue}°F recorded.`);
        state.tempValue = "";
        state.tempNotes = "";
        state.showLogForm = false;
      } catch (err: unknown) {
        showToast("error", "Failed to log temp", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.submitting = false;
      }
    };

    const safeZoneInfo = (eq: Equipment) => {
      let info = `Min: <strong>${eq.safeMin}°F</strong>`;
      if (eq.safeMax !== undefined) info += ` · Max: <strong>${eq.safeMax}°F</strong>`;
      if (eq.category === "hothold") info += " · FDA: hot-hold ≥140°F";
      if (eq.category === "fridge") info += " · FDA: refrigeration ≤41°F";
      if (eq.category === "freezer") info += " · FDA: frozen ≤0°F";
      return info;
    };

    html`
      <div class="relative flex min-h-[400px]">
        <div class="${() => `flex-1 transition-all duration-300 ${state.sidebarOpen ? 'mr-[400px]' : ''}`}">
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            ${() => state.equipment.map(eq => html`
              <button type="button"
                @click="${() => openSidebar(eq.id)}"
                class="${`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                  state.selectedId === eq.id ? 'border-amber-400 ring-2 ring-amber-200'
                  : safeStatus(eq) === 'attention' ? 'border-red-300 bg-red-50/30'
                  : 'border-stone-200 hover:border-amber-200'
                }`}"
              >
                <div class="flex items-start justify-between mb-3">
                  <span class="text-2xl">${categoryIcon[eq.category]}</span>
                  <span class="${`inline-block w-2.5 h-2.5 rounded-full ${statusDot(eq)}`}"></span>
                </div>
                <h3 class="text-base font-bold text-stone-800 mb-1">${eq.name}</h3>
                <p class="${`text-3xl font-mono font-bold ${tempColor(eq)}`}">${eq.lastTempF}°F</p>
                <p class="text-xs text-stone-400 mt-2">Last: ${eq.lastChecked}</p>
                <p class="text-xs text-stone-400">Range today: ${eq.minTodayF}–${eq.maxTodayF}°F</p>
              </button>
            `.key(eq.id))}
          </div>
        </div>

        ${() => state.sidebarOpen ? html`<div class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30" @click="${closeSidebar}"></div>` : html`<span></span>`}

        <aside class="${() => `fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${state.sidebarOpen ? 'translate-x-0' : 'translate-x-full'}`}">
          ${() => {
            const eq = selected();
            if (!eq) return html`<span></span>`;
            return html`
              <div class="p-6 flex flex-col h-full">
                <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
                  <div>
                    <div class="flex items-center gap-2 mb-1">
                      <span class="text-2xl">${categoryIcon[eq.category]}</span>
                      <h3 class="text-xl font-bold text-stone-800">${eq.name}</h3>
                    </div>
                    <span class="${`inline-block w-2.5 h-2.5 rounded-full ${statusDot(eq)}`}"></span>
                  </div>
                  <button @click="${closeSidebar}" class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition">✕</button>
                </div>
                <div class="flex-1">
                  <div class="bg-blue-50 rounded-lg p-4 mb-5 border border-blue-100 text-sm text-blue-800">
                    <p class="font-semibold mb-1">Safe Temperature Zone</p>
                    <p class="text-xs text-blue-700">${safeZoneInfo(eq)}</p>
                  </div>
                  <div class="grid grid-cols-3 gap-3 mb-6">
                    <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                      <p class="${`text-xl font-mono font-bold ${tempColor(eq)}`}">${eq.lastTempF}°F</p>
                      <p class="text-xs text-stone-400 mt-1">Current</p>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                      <p class="text-xl font-mono font-bold text-stone-700">${eq.minTodayF}°F</p>
                      <p class="text-xs text-stone-400 mt-1">Min Today</p>
                    </div>
                    <div class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100">
                      <p class="text-xl font-mono font-bold text-stone-700">${eq.maxTodayF}°F</p>
                      <p class="text-xs text-stone-400 mt-1">Max Today</p>
                    </div>
                  </div>
                  ${() => state.showLogForm ? html`
                    <div class="mb-5 p-4 bg-stone-50 rounded-lg border border-stone-200">
                      <h4 class="text-sm font-bold text-stone-700 mb-3">Log Temperature Reading</h4>
                      <div class="flex flex-col gap-3">
                        <div>
                          <label class="text-xs font-semibold text-stone-600 uppercase">Temperature (°F)</label>
                          <input type="number" step="0.1" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g. 38.5"
                            @input="${(e: Event) => state.tempValue = (e?.target as HTMLInputElement)?.value ?? ''}">
                          ${() => state.errors.tempF ? html`<p class="text-xs text-red-500 mt-1">${state.errors.tempF}</p>` : html`<span></span>`}
                        </div>
                        <div>
                          <label class="text-xs font-semibold text-stone-600 uppercase">Notes (optional)</label>
                          <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g. After restocking"
                            @input="${(e: Event) => state.tempNotes = (e?.target as HTMLInputElement)?.value ?? ''}">
                        </div>
                        <div class="flex gap-2">
                          <button @click="${handleLogTemp}" class="flex-1 py-2 text-sm font-bold bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition">${() => state.submitting ? "Saving..." : "Log Reading"}</button>
                          <button @click="${() => state.showLogForm = false}" class="py-2 px-3 text-sm font-bold text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition">Cancel</button>
                        </div>
                      </div>
                    </div>
                  ` : html`<span></span>`}
                </div>
                <div class="pt-4 border-t border-stone-100 mt-auto">
                  ${() => !state.showLogForm ? html`
                    <button @click="${() => state.showLogForm = true}" class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-800 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2">🌡️ Log Temperature Reading</button>
                  ` : html`<span></span>`}
                </div>
              </div>
            `;
          }}
        </aside>
      </div>
    `(containerRef.current);
  }, []);

  return <div ref={containerRef} />;
}
