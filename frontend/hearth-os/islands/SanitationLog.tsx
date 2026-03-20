import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  type FieldErrors,
  SanitationLogSchema,
} from "../utils/schemas.ts";

interface SanitationTask {
  id: string;
  surfaceType: number; // 0=FloorDrain, 1=CuttingBoard, 2=PrepTable, 3=Equipment, 4=HandwashSink, 5=GeneralSurface
  name: string;
  area: string;
  cleaningMethod: string;
  defaultSanitizer: number; // 0=Quat, 1=Bleach, 2=StarSan, 3=None
  frequencyHours: number;
  lastCleanedAt: string; // ISO
  lastCleanedBy: string;
  logs: { cleanedAt: string; cleanedBy: string; notes?: string }[];
}

function taskStatus(task: SanitationTask): "active" | "attention" | "idle" {
  const hoursSince = (Date.now() - new Date(task.lastCleanedAt).getTime()) /
    3_600_000;
  if (hoursSince >= task.frequencyHours) return "attention";
  if (hoursSince >= task.frequencyHours * 0.8) return "idle";
  return "active";
}

function statusLabel(task: SanitationTask) {
  const s = taskStatus(task);
  return s === "attention"
    ? "Overdue"
    : s === "idle"
    ? "Due Soon"
    : "On Schedule";
}

export default function SanitationLog() {
  const tasks = useSignal<SanitationTask[]>([
    {
      id: "san-prep-surfaces",
      surfaceType: 2, // PrepTable
      name: "Prep Surfaces",
      area: "Main Kitchen",
      cleaningMethod: "Wash, Rinse, Sanitize",
      defaultSanitizer: 0, // Quat
      frequencyHours: 4,
      lastCleanedAt: new Date(Date.now() - 3 * 3_600_000).toISOString(),
      lastCleanedBy: "Maria",
      logs: [{
        cleanedAt: new Date(Date.now() - 3 * 3_600_000).toISOString(),
        cleanedBy: "Maria",
        notes: "Sanitized with KAY QT-40",
      }],
    },
    {
      id: "san-cutting-boards",
      surfaceType: 1, // CuttingBoard
      name: "Cutting Boards",
      area: "Main Kitchen",
      cleaningMethod: "Wash, Rinse, Sanitize, Air Dry",
      defaultSanitizer: 0, // Quat
      frequencyHours: 4,
      lastCleanedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(),
      lastCleanedBy: "James",
      logs: [{
        cleanedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(),
        cleanedBy: "James",
      }],
    },
    {
      id: "san-floor-drains",
      surfaceType: 0, // FloorDrain
      name: "Floor Drains",
      area: "Kitchen Floor",
      cleaningMethod: "Scrub, Rinse, Apply Quat",
      defaultSanitizer: 0, // Quat
      frequencyHours: 24,
      lastCleanedAt: new Date(Date.now() - 18 * 3_600_000).toISOString(),
      lastCleanedBy: "Sam",
      logs: [{
        cleanedAt: new Date(Date.now() - 18 * 3_600_000).toISOString(),
        cleanedBy: "Sam",
      }],
    },
    {
      id: "san-exhaust-hood",
      surfaceType: 3, // Equipment
      name: "Exhaust Hood",
      area: "Cooking Line",
      cleaningMethod: "Deep Clean / Degrease",
      defaultSanitizer: 3, // None
      frequencyHours: 168,
      lastCleanedAt: new Date(Date.now() - 200 * 3_600_000).toISOString(),
      lastCleanedBy: "Vendor",
      logs: [{
        cleanedAt: new Date(Date.now() - 200 * 3_600_000).toISOString(),
        cleanedBy: "Vendor",
        notes: "Quarterly deep clean",
      }],
    },
    {
      id: "san-walk-in",
      surfaceType: 5, // GeneralSurface
      name: "Walk-in Cooler",
      area: "Storage",
      cleaningMethod: "Sweep, Mop, Sanitize Racks",
      defaultSanitizer: 0, // Quat
      frequencyHours: 168,
      lastCleanedAt: new Date(Date.now() - 72 * 3_600_000).toISOString(),
      lastCleanedBy: "Maria",
      logs: [{
        cleanedAt: new Date(Date.now() - 72 * 3_600_000).toISOString(),
        cleanedBy: "Maria",
      }],
    },
  ]);

  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);
  const showLogForm = useSignal(false);
  const cleanedBy = useSignal("");
  const cleanNotes = useSignal("");
  const sanitizerPpm = useSignal("");
  const logErrors = useSignal<FieldErrors>({});
  const isSubmitting = useSignal(false);

  const selected = tasks.value.find((t: SanitationTask) =>
    t.id === selectedId.value
  );

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

  const handleLogClean = async (task: SanitationTask) => {
    const result = SanitationLogSchema.safeParse({
      surfaceType: task.surfaceType,
      area: task.area,
      cleaningMethod: task.cleaningMethod,
      sanitizer: task.defaultSanitizer,
      sanitizerPpm: sanitizerPpm.value ? Number(sanitizerPpm.value) : undefined,
      cleanedBy: cleanedBy.value,
      notes: cleanNotes.value || undefined,
    });
    if (!result.success) {
      logErrors.value = extractErrors(result);
      return;
    }
    isSubmitting.value = true;
    logErrors.value = {};
    try {
      const { KitchenAPI } = await import("../utils/farmos-client.ts");
      await KitchenAPI.logSanitation(result.data);
      showToast(
        "success",
        "Cleaning logged!",
        `${task.name} signed off by ${cleanedBy.value}.`,
      );
      cleanedBy.value = "";
      cleanNotes.value = "";
      sanitizerPpm.value = "";
      showLogForm.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to log",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const formatRelative = (iso: string) => {
    const h = Math.round((Date.now() - new Date(iso).getTime()) / 3_600_000);
    if (h < 1) return "< 1 hour ago";
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  };

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
      logErrors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const statusBadgeVariant = (t: SanitationTask) => {
    const s = taskStatus(t);
    return s === "attention"
      ? "attention" as const
      : s === "idle"
      ? "idle" as const
      : "active" as const;
  };

  return (
    <div class="relative flex min-h-[400px]">
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[400px]" : ""
        }`}
      >
        <div class="flex flex-col gap-3">
          {tasks.value.map((task: SanitationTask) => {
            const status = taskStatus(task);
            const isSelected = selectedId.value === task.id;
            return (
              <button
                type="button"
                onClick={() => openSidebar(task.id)}
                class={`bg-white rounded-xl border shadow-sm px-5 py-4 hover:shadow-md transition text-left cursor-pointer w-full flex items-center gap-4 ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : status === "attention"
                    ? "border-red-200 bg-red-50/20"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <div class="text-2xl">
                  {status === "attention"
                    ? "🔴"
                    : status === "idle"
                    ? "🟡"
                    : "🟢"}
                </div>
                <div class="flex-1">
                  <p class="text-base font-bold text-stone-800">{task.name}</p>
                  <p class="text-xs text-stone-400">
                    {task.area} · Every {task.frequencyHours < 24
                      ? `${task.frequencyHours}h`
                      : `${task.frequencyHours / 24}d`}
                  </p>
                </div>
                <div class="text-right">
                  <StatusBadge
                    variant={statusBadgeVariant(task)}
                    label={statusLabel(task)}
                  />
                  <p class="text-xs text-stone-400 mt-1">
                    {formatRelative(task.lastCleanedAt)}
                  </p>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30"
          onClick={closeSidebar}
        />
      )}

      <aside
        class={`fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selected && (
          <div class="p-6 flex flex-col h-full">
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <h3 class="text-xl font-bold text-stone-800">
                  {selected.name}
                </h3>
                <p class="text-sm text-stone-500 mt-1">
                  {selected.area} · Every {selected.frequencyHours < 24
                    ? `${selected.frequencyHours}h`
                    : `${selected.frequencyHours / 24}d`}
                </p>
                <div class="mt-2">
                  <StatusBadge
                    variant={statusBadgeVariant(selected)}
                    label={statusLabel(selected)}
                  />
                </div>
              </div>
              <button
                type="button"
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>

            <div class="flex-1">
              <p class="text-xs text-stone-400 uppercase tracking-wider font-semibold mb-3">
                Last: {formatRelative(selected.lastCleanedAt)} by{" "}
                {selected.lastCleanedBy}
              </p>

              {/* Log history */}
              <h4 class="text-sm font-bold text-stone-700 mb-2">
                Cleaning History
              </h4>
              <div class="flex flex-col gap-2 mb-5 max-h-48 overflow-y-auto">
                {selected.logs.map((
                  log: { cleanedAt: string; cleanedBy: string; notes?: string },
                  i: number,
                ) => (
                  <div
                    key={i}
                    class="bg-stone-50 rounded-lg p-3 border border-stone-100"
                  >
                    <div class="flex items-center justify-between">
                      <p class="text-xs font-semibold text-stone-700">
                        {log.cleanedBy}
                      </p>
                      <p class="text-xs text-stone-400">
                        {formatRelative(log.cleanedAt)}
                      </p>
                    </div>
                    {log.notes && (
                      <p class="text-xs text-stone-500 mt-1">{log.notes}</p>
                    )}
                  </div>
                ))}
              </div>

              {showLogForm.value && (
                <div class="p-4 bg-stone-50 rounded-lg border border-stone-200 mb-4">
                  <h4 class="text-sm font-bold text-stone-700 mb-3">
                    Log Cleaning
                  </h4>
                  <div class="flex flex-col gap-3">
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">
                        Cleaned By
                      </label>
                      <input
                        type="text"
                        class={`mt-1 ${inputClass("cleanedBy")}`}
                        placeholder="Staff name"
                        value={cleanedBy.value}
                        onInput={(e) =>
                          cleanedBy.value =
                            (e.target as HTMLInputElement).value}
                      />
                      {logErrors.value.cleanedBy && (
                        <p class="text-xs text-red-500 mt-1">
                          {logErrors.value.cleanedBy}
                        </p>
                      )}
                    </div>
                    {selected.defaultSanitizer !== 3 && ( // Only show if sanitizer is not None
                      <div>
                        <label class="text-xs font-semibold text-stone-600 uppercase">
                          Sanitizer PPM (Concentration)
                        </label>
                        <input
                          type="number"
                          class={`mt-1 ${inputClass("sanitizerPpm")}`}
                          placeholder="e.g., 200"
                          value={sanitizerPpm.value}
                          onInput={(e) =>
                            sanitizerPpm.value =
                              (e.target as HTMLInputElement).value}
                        />
                        {logErrors.value.sanitizerPpm && (
                          <p class="text-xs text-red-500 mt-1">
                            {logErrors.value.sanitizerPpm}
                          </p>
                        )}
                        <p class="text-xs text-stone-400 mt-1">
                          Crucial for DOH compliance verification.
                        </p>
                      </div>
                    )}
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">
                        Notes (optional)
                      </label>
                      <input
                        type="text"
                        class={`mt-1 ${inputClass("notes")}`}
                        placeholder="Products used, observations..."
                        value={cleanNotes.value}
                        onInput={(e) =>
                          cleanNotes.value =
                            (e.target as HTMLInputElement).value}
                      />
                    </div>
                    <div class="flex gap-2">
                      <button
                        type="button"
                        onClick={() => handleLogClean(selected)}
                        disabled={isSubmitting.value || !cleanedBy.value}
                        class="flex-1 py-2 text-sm font-bold bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 transition"
                      >
                        {isSubmitting.value ? "Saving..." : "Sign Off"}
                      </button>
                      <button
                        type="button"
                        onClick={() => showLogForm.value = false}
                        class="py-2 px-3 text-sm text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition"
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
                  type="button"
                  onClick={() => showLogForm.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-emerald-50 text-emerald-800 hover:bg-emerald-100 transition border border-emerald-100 flex items-center justify-center gap-2"
                >
                  🧹 Log Cleaning
                </button>
              )}
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
