import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

interface SanitationTask {
  id: string;
  surfaceType: number;
  name: string;
  area: string;
  cleaningMethod: string;
  defaultSanitizer: number;
  frequencyHours: number;
  lastCleanedAt: string;
  lastCleanedBy: string;
  logs: { cleanedAt: string; cleanedBy: string; notes?: string }[];
}

const taskStatus = (t: SanitationTask) => {
  const h = (Date.now() - new Date(t.lastCleanedAt).getTime()) / 3_600_000;
  if (h >= t.frequencyHours) return "attention";
  if (h >= t.frequencyHours * 0.8) return "idle";
  return "active";
};
const statusLabel = (t: SanitationTask) => {
  const s = taskStatus(t);
  return s === "attention" ? "Overdue" : s === "idle" ? "Due Soon" : "On Schedule";
};
const statusIcon = (t: SanitationTask) => {
  const s = taskStatus(t);
  return s === "attention" ? "🔴" : s === "idle" ? "🟡" : "🟢";
};
const statusBadgeCls = (t: SanitationTask) => {
  const s = taskStatus(t);
  return s === "attention" ? "bg-red-50 text-red-700" : s === "idle" ? "bg-amber-50 text-amber-700" : "bg-emerald-50 text-emerald-700";
};
const formatRelative = (iso: string) => {
  const h = Math.round((Date.now() - new Date(iso).getTime()) / 3_600_000);
  if (h < 1) return "< 1 hour ago";
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
};

const MOCK_TASKS: SanitationTask[] = [
  { id: "san-prep-surfaces", surfaceType: 2, name: "Prep Surfaces", area: "Main Kitchen", cleaningMethod: "Wash, Rinse, Sanitize", defaultSanitizer: 0, frequencyHours: 4, lastCleanedAt: new Date(Date.now() - 3 * 3_600_000).toISOString(), lastCleanedBy: "Maria", logs: [{ cleanedAt: new Date(Date.now() - 3 * 3_600_000).toISOString(), cleanedBy: "Maria", notes: "Sanitized with KAY QT-40" }] },
  { id: "san-cutting-boards", surfaceType: 1, name: "Cutting Boards", area: "Main Kitchen", cleaningMethod: "Wash, Rinse, Sanitize, Air Dry", defaultSanitizer: 0, frequencyHours: 4, lastCleanedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(), lastCleanedBy: "James", logs: [{ cleanedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(), cleanedBy: "James" }] },
  { id: "san-floor-drains", surfaceType: 0, name: "Floor Drains", area: "Kitchen Floor", cleaningMethod: "Scrub, Rinse, Apply Quat", defaultSanitizer: 0, frequencyHours: 24, lastCleanedAt: new Date(Date.now() - 18 * 3_600_000).toISOString(), lastCleanedBy: "Sam", logs: [{ cleanedAt: new Date(Date.now() - 18 * 3_600_000).toISOString(), cleanedBy: "Sam" }] },
  { id: "san-exhaust-hood", surfaceType: 3, name: "Exhaust Hood", area: "Cooking Line", cleaningMethod: "Deep Clean / Degrease", defaultSanitizer: 3, frequencyHours: 168, lastCleanedAt: new Date(Date.now() - 200 * 3_600_000).toISOString(), lastCleanedBy: "Vendor", logs: [{ cleanedAt: new Date(Date.now() - 200 * 3_600_000).toISOString(), cleanedBy: "Vendor", notes: "Quarterly deep clean" }] },
  { id: "san-walk-in", surfaceType: 5, name: "Walk-in Cooler", area: "Storage", cleaningMethod: "Sweep, Mop, Sanitize Racks", defaultSanitizer: 0, frequencyHours: 168, lastCleanedAt: new Date(Date.now() - 72 * 3_600_000).toISOString(), lastCleanedBy: "Maria", logs: [{ cleanedAt: new Date(Date.now() - 72 * 3_600_000).toISOString(), cleanedBy: "Maria" }] },
];

export default function ArrowSanitationLog() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      tasks: [...MOCK_TASKS],
      selectedId: null as string | null,
      sidebarOpen: false,
      showLogForm: false,
      cleanedBy: "",
      cleanNotes: "",
      sanitizerPpm: "",
      errors: {} as Record<string, string>,
      submitting: false,
    });

    const selected = () => state.tasks.find(t => t.id === state.selectedId);
    const openSidebar = (id: string) => { state.selectedId = id; state.showLogForm = false; state.sidebarOpen = true; };
    const closeSidebar = () => { state.sidebarOpen = false; setTimeout(() => { state.selectedId = null; }, 300); };

    const handleLogClean = async () => {
      const task = selected();
      if (!task) return;
      const errs: Record<string, string> = {};
      if (!state.cleanedBy) errs.cleanedBy = "Required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      state.submitting = true;
      clearErrors(state.errors);
      try {
        const { KitchenAPI } = await import("../utils/farmos-client.ts");
        await KitchenAPI.logSanitation({
          surfaceType: task.surfaceType,
          area: task.area,
          cleaningMethod: task.cleaningMethod,
          sanitizer: task.defaultSanitizer,
          sanitizerPpm: state.sanitizerPpm ? Number(state.sanitizerPpm) : undefined,
          cleanedBy: state.cleanedBy,
          notes: state.cleanNotes || undefined,
        });
        showToast("success", "Cleaning logged!", `${task.name} signed off by ${state.cleanedBy}.`);
        state.cleanedBy = "";
        state.cleanNotes = "";
        state.sanitizerPpm = "";
        state.showLogForm = false;
      } catch (err: unknown) {
        showToast("error", "Failed to log", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.submitting = false;
      }
    };

    const freqLabel = (h: number) => h < 24 ? `${h}h` : `${h / 24}d`;

    html`
      <div class="relative flex min-h-[400px]">
        <div class="${() => `flex-1 transition-all duration-300 ${state.sidebarOpen ? 'mr-[400px]' : ''}`}">
          <div class="flex flex-col gap-3">
            ${() => state.tasks.map(task => html`
              <button type="button" @click="${() => openSidebar(task.id)}"
                class="${`bg-white rounded-xl border shadow-sm px-5 py-4 hover:shadow-md transition text-left cursor-pointer w-full flex items-center gap-4 ${
                  state.selectedId === task.id ? 'border-amber-400 ring-2 ring-amber-200'
                  : taskStatus(task) === 'attention' ? 'border-red-200 bg-red-50/20'
                  : 'border-stone-200 hover:border-amber-200'
                }`}">
                <div class="text-2xl">${statusIcon(task)}</div>
                <div class="flex-1">
                  <p class="text-base font-bold text-stone-800">${task.name}</p>
                  <p class="text-xs text-stone-400">${task.area} · Every ${freqLabel(task.frequencyHours)}</p>
                </div>
                <div class="text-right">
                  <span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${statusBadgeCls(task)}`}">${statusLabel(task)}</span>
                  <p class="text-xs text-stone-400 mt-1">${formatRelative(task.lastCleanedAt)}</p>
                </div>
              </button>
            `.key(task.id))}
          </div>
        </div>

        ${() => state.sidebarOpen ? html`<div class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30" @click="${closeSidebar}"></div>` : html`<span></span>`}

        <aside class="${() => `fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${state.sidebarOpen ? 'translate-x-0' : 'translate-x-full'}`}">
          ${() => {
            const task = selected();
            if (!task) return html`<span></span>`;
            return html`
              <div class="p-6 flex flex-col h-full">
                <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
                  <div>
                    <h3 class="text-xl font-bold text-stone-800">${task.name}</h3>
                    <p class="text-sm text-stone-500 mt-1">${task.area} · Every ${freqLabel(task.frequencyHours)}</p>
                    <div class="mt-2"><span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${statusBadgeCls(task)}`}">${statusLabel(task)}</span></div>
                  </div>
                  <button @click="${closeSidebar}" class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition">✕</button>
                </div>
                <div class="flex-1">
                  <p class="text-xs text-stone-400 uppercase tracking-wider font-semibold mb-3">Last: ${formatRelative(task.lastCleanedAt)} by ${task.lastCleanedBy}</p>
                  <h4 class="text-sm font-bold text-stone-700 mb-2">Cleaning History</h4>
                  <div class="flex flex-col gap-2 mb-5 max-h-48 overflow-y-auto">
                    ${task.logs.map((log, i) => html`
                      <div class="bg-stone-50 rounded-lg p-3 border border-stone-100">
                        <div class="flex items-center justify-between">
                          <p class="text-xs font-semibold text-stone-700">${log.cleanedBy}</p>
                          <p class="text-xs text-stone-400">${formatRelative(log.cleanedAt)}</p>
                        </div>
                        ${log.notes ? html`<p class="text-xs text-stone-500 mt-1">${log.notes}</p>` : html`<span></span>`}
                      </div>
                    `.key(String(i)))}
                  </div>

                  ${() => state.showLogForm ? html`
                    <div class="p-4 bg-stone-50 rounded-lg border border-stone-200 mb-4">
                      <h4 class="text-sm font-bold text-stone-700 mb-3">Log Cleaning</h4>
                      <div class="flex flex-col gap-3">
                        <div>
                          <label class="text-xs font-semibold text-stone-600 uppercase">Cleaned By</label>
                          <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Staff name"
                            @input="${(e: Event) => state.cleanedBy = (e?.target as HTMLInputElement)?.value ?? ''}">
                          ${() => state.errors.cleanedBy ? html`<p class="text-xs text-red-500 mt-1">${state.errors.cleanedBy}</p>` : html`<span></span>`}
                        </div>
                        ${task.defaultSanitizer !== 3 ? html`
                          <div>
                            <label class="text-xs font-semibold text-stone-600 uppercase">Sanitizer PPM</label>
                            <input type="number" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g., 200"
                              @input="${(e: Event) => state.sanitizerPpm = (e?.target as HTMLInputElement)?.value ?? ''}">
                            <p class="text-xs text-stone-400 mt-1">Crucial for DOH compliance verification.</p>
                          </div>
                        ` : html`<span></span>`}
                        <div>
                          <label class="text-xs font-semibold text-stone-600 uppercase">Notes (optional)</label>
                          <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Products used, observations..."
                            @input="${(e: Event) => state.cleanNotes = (e?.target as HTMLInputElement)?.value ?? ''}">
                        </div>
                        <div class="flex gap-2">
                          <button @click="${handleLogClean}" class="flex-1 py-2 text-sm font-bold bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition">${() => state.submitting ? "Saving..." : "Sign Off"}</button>
                          <button @click="${() => state.showLogForm = false}" class="py-2 px-3 text-sm text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition">Cancel</button>
                        </div>
                      </div>
                    </div>
                  ` : html`<span></span>`}
                </div>
                <div class="pt-4 border-t border-stone-100 mt-auto">
                  ${() => !state.showLogForm ? html`
                    <button @click="${() => state.showLogForm = true}" class="w-full py-2.5 text-sm font-bold rounded-lg bg-emerald-50 text-emerald-800 hover:bg-emerald-100 transition border border-emerald-100 flex items-center justify-center gap-2">🧹 Log Cleaning</button>
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
