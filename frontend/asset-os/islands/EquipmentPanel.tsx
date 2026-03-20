import { useEffect, useState } from "preact/hooks";
import StatusBadge from "../components/StatusBadge.tsx";
import {
  extractErrors,
  type FieldErrors,
  MaintenanceSchema,
  RetireSchema,
} from "../utils/schemas.ts";
import type {
  EquipmentSummary,
  MaintenanceEntry,
} from "../utils/assets-client.ts";

const STATUS_VARIANT: Record<string, "active" | "maintenance" | "retired"> = {
  Active: "active",
  Maintenance: "maintenance",
  Retired: "retired",
};

const STATUS_ICON: Record<string, string> = {
  Active: "🟢",
  Maintenance: "🟡",
  Retired: "⚫",
};

type ModalState = "none" | "maintenance" | "retire";

export default function EquipmentPanel() {
  const [items, setItems] = useState<EquipmentSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  // Sidebar
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [sidebarHistory, setSidebarHistory] = useState<MaintenanceEntry[]>([]);

  // Active modal
  const [modal, setModal] = useState<ModalState>("none");

  // Maintenance form
  const [fDate, setFDate] = useState("");
  const [fDesc, setFDesc] = useState("");
  const [fCost, setFCost] = useState("");
  const [fTech, setFTech] = useState("");
  const [maintErrors, setMaintErrors] = useState<FieldErrors>({});
  const [isMaintSubmitting, setIsMaintSubmitting] = useState(false);

  // Retire form
  const [fReason, setFReason] = useState("");
  const [retireErrors, setRetireErrors] = useState<FieldErrors>({});
  const [isRetireSubmitting, setIsRetireSubmitting] = useState(false);

  const selected = items.find((e) => e.id === selectedId);

  const load = async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const { EquipmentAPI } = await import("../utils/assets-client.ts");
      setItems(await EquipmentAPI.list());
    } catch (err: unknown) {
      setLoadError(
        err instanceof Error ? err.message : "Failed to load equipment",
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const openSidebar = (id: string) => {
    setSelectedId(id);
    setIsSidebarOpen(true);
    setModal("none");
    setSidebarHistory([]);
  };
  const closeSidebar = () => {
    setIsSidebarOpen(false);
    setModal("none");
    setTimeout(() => {
      setSelectedId(null);
    }, 300);
  };
  const closeModal = () => {
    setModal("none");
    setMaintErrors({});
    setRetireErrors({});
  };

  const handleMaintenance = async (e: Event) => {
    e.preventDefault();
    const result = MaintenanceSchema.safeParse({
      date: fDate,
      description: fDesc,
      costDollars: fCost || undefined,
      technician: fTech || undefined,
    });
    if (!result.success) {
      setMaintErrors(extractErrors(result));
      return;
    }
    if (!selectedId) return;

    setIsMaintSubmitting(true);
    try {
      const { EquipmentAPI } = await import("../utils/assets-client.ts");
      await EquipmentAPI.logMaintenance(selectedId, result.data);
      setSidebarHistory([
        {
          date: result.data.date,
          description: result.data.description,
          costDollars: result.data.costDollars,
          technician: result.data.technician,
        },
        ...sidebarHistory,
      ]);
      // Update maintenanceCount in list
      setItems((prev) =>
        prev.map((eq) =>
          eq.id === selectedId
            ? { ...eq, maintenanceCount: eq.maintenanceCount + 1 }
            : eq
        )
      );
      closeModal();
      setFDate("");
      setFDesc("");
      setFCost("");
      setFTech("");
    } catch (err: unknown) {
      setMaintErrors({ _api: err instanceof Error ? err.message : "Failed" });
    } finally {
      setIsMaintSubmitting(false);
    }
  };

  const handleRetire = async (e: Event) => {
    e.preventDefault();
    const result = RetireSchema.safeParse({ reason: fReason });
    if (!result.success) {
      setRetireErrors(extractErrors(result));
      return;
    }
    if (!selectedId) return;

    setIsRetireSubmitting(true);
    try {
      const { EquipmentAPI } = await import("../utils/assets-client.ts");
      await EquipmentAPI.retire(selectedId, result.data.reason);
      setItems((prev) =>
        prev.map((eq) =>
          eq.id === selectedId ? { ...eq, status: "Retired" } : eq
        )
      );
      closeModal();
      setFReason("");
    } catch (err: unknown) {
      setRetireErrors({ _api: err instanceof Error ? err.message : "Failed" });
    } finally {
      setIsRetireSubmitting(false);
    }
  };

  const inputClass = (field: string, errs: FieldErrors) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 transition ${
      errs[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  if (isLoading) {
    return (
      <div class="flex items-center justify-center py-20 text-stone-400">
        <span class="animate-spin text-2xl mr-3">⟳</span>
        <p>Loading equipment…</p>
      </div>
    );
  }

  if (loadError) {
    return (
      <div class="bg-red-50 border border-red-200 rounded-xl p-6 text-center">
        <p class="text-red-700">⚠️ {loadError}</p>
        <button
          type="button"
          onClick={load}
          class="mt-3 text-sm text-red-600 underline hover:no-underline"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div class="relative flex min-h-[500px]">
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen ? "mr-[420px]" : ""
        }`}
      >
        {items.length === 0
          ? (
            <div class="flex flex-col items-center justify-center py-20 text-stone-400">
              <p class="text-5xl mb-4">🚜</p>
              <p class="text-lg font-semibold text-stone-600">
                No equipment registered yet
              </p>
              <p class="text-sm mt-1">
                Use the "+ Register Equipment" button to get started.
              </p>
            </div>
          )
          : (
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {items.map((eq) => {
                const isSelected = selectedId === eq.id;
                const variant = STATUS_VARIANT[eq.status] ?? "active";
                return (
                  <button
                    type="button"
                    key={eq.id}
                    onClick={() => openSidebar(eq.id)}
                    class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                      isSelected
                        ? "border-emerald-400 ring-2 ring-emerald-200"
                        : eq.status === "Retired"
                        ? "border-stone-200 opacity-60 hover:opacity-80"
                        : "border-stone-200 hover:border-emerald-200"
                    }`}
                  >
                    <div class="flex items-start justify-between mb-3">
                      <div>
                        <p class="text-base font-bold text-stone-800 leading-tight">
                          {eq.name}
                        </p>
                        <p class="text-xs text-stone-500 mt-0.5">
                          {eq.make} {eq.model}
                          {eq.year ? ` · ${eq.year}` : ""}
                        </p>
                      </div>
                      <StatusBadge
                        variant={variant}
                        label={`${STATUS_ICON[eq.status]} ${eq.status}`}
                      />
                    </div>
                    <div class="flex items-center justify-between pt-3 border-t border-stone-100 text-xs text-stone-400">
                      <span>
                        🔧 {eq.maintenanceCount}{" "}
                        maintenance record{eq.maintenanceCount !== 1 ? "s" : ""}
                      </span>
                      <span>📍 {eq.lat.toFixed(4)}, {eq.lng.toFixed(4)}</span>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
      </div>

      {/* Sidebar backdrop on mobile */}
      {isSidebarOpen && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30"
          onClick={closeSidebar}
        />
      )}

      {/* Slide-out Sidebar */}
      <aside
        class={`fixed top-0 right-0 h-full w-[420px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selected && (
          <div class="p-6">
            {/* Header */}
            <div class="flex items-start justify-between mb-6 pb-5 border-b border-stone-100">
              <div>
                <h3 class="text-xl font-bold text-stone-800">
                  {selected.name}
                </h3>
                <p class="text-sm text-stone-500 mt-0.5">
                  {selected.make} {selected.model}
                  {selected.year ? ` · ${selected.year}` : ""}
                </p>
                <div class="mt-2">
                  <StatusBadge
                    variant={STATUS_VARIANT[selected.status] ?? "active"}
                    label={`${STATUS_ICON[selected.status]} ${selected.status}`}
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

            {/* Stats */}
            <div class="grid grid-cols-2 gap-3 mb-6">
              <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                  Location
                </p>
                <p class="text-sm font-semibold text-stone-800 mt-1">
                  📍 {selected.lat.toFixed(4)}, {selected.lng.toFixed(4)}
                </p>
              </div>
              <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                  Maintenance
                </p>
                <p class="text-xl font-bold text-stone-800 mt-1">
                  {selected.maintenanceCount}
                </p>
              </div>
            </div>

            {/* Actions */}
            {selected.status !== "Retired" && (
              <div class="flex gap-2 mb-6">
                <button
                  type="button"
                  onClick={() => setModal("maintenance")}
                  class="flex-1 bg-amber-50 border border-amber-200 text-amber-800 font-semibold py-2 px-3 rounded-lg hover:bg-amber-100 transition text-sm cursor-pointer"
                >
                  🔧 Log Maintenance
                </button>
                <button
                  type="button"
                  onClick={() => setModal("retire")}
                  class="flex-1 bg-red-50 border border-red-200 text-red-700 font-semibold py-2 px-3 rounded-lg hover:bg-red-100 transition text-sm cursor-pointer"
                >
                  🛑 Retire
                </button>
              </div>
            )}

            {/* Maintenance History */}
            <div>
              <h4 class="text-sm font-bold text-stone-700 uppercase tracking-wider mb-3">
                Maintenance History
              </h4>
              {sidebarHistory.length === 0 && selected.maintenanceCount === 0
                ? (
                  <p class="text-sm text-stone-400 text-center py-6">
                    No maintenance records yet
                  </p>
                )
                : sidebarHistory.length === 0
                ? (
                  <p class="text-xs text-stone-400 italic text-center py-4">
                    {selected.maintenanceCount}{" "}
                    record{selected.maintenanceCount !== 1 ? "s" : ""}{" "}
                    on server — history loads after next action
                  </p>
                )
                : (
                  <div class="flex flex-col gap-3">
                    {sidebarHistory.map((entry, i) => (
                      <div
                        key={i}
                        class="bg-amber-50 border border-amber-100 rounded-xl p-4 animate-[slideDown_0.2s_ease-out]"
                      >
                        <div class="flex items-center justify-between mb-1">
                          <p class="text-xs font-semibold text-stone-700">
                            {entry.date}
                          </p>
                          {entry.costDollars !== undefined && (
                            <p class="text-xs text-emerald-700 font-mono font-semibold">
                              ${entry.costDollars.toFixed(2)}
                            </p>
                          )}
                        </div>
                        <p class="text-sm text-stone-700">
                          {entry.description}
                        </p>
                        {entry.technician && (
                          <p class="text-xs text-stone-500 mt-1">
                            👷 {entry.technician}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
            </div>
          </div>
        )}
      </aside>

      {/* ── Modals ─────────────────────────────────────────────── */}

      {/* Log Maintenance Modal */}
      {modal === "maintenance" && (
        <div
          class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
              <h3 class="text-lg font-bold text-stone-800">
                🔧 Log Maintenance
              </h3>
              <button
                type="button"
                onClick={closeModal}
                class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition cursor-pointer"
              >
                ✕
              </button>
            </div>
            <form onSubmit={handleMaintenance} class="p-6 flex flex-col gap-4">
              {maintErrors._api && (
                <div class="bg-red-50 border border-red-200 rounded-lg px-4 py-3">
                  <p class="text-sm text-red-700">⚠️ {maintErrors._api}</p>
                </div>
              )}
              <div>
                <label class="text-xs font-semibold text-stone-600 uppercase">
                  Date *
                </label>
                <input
                  type="date"
                  class={`mt-1 ${inputClass("date", maintErrors)}`}
                  value={fDate}
                  onInput={(e) =>
                    setFDate((e.target as HTMLInputElement).value)}
                />
                {maintErrors.date && (
                  <p class="text-xs text-red-500 mt-1">{maintErrors.date}</p>
                )}
              </div>
              <div>
                <label class="text-xs font-semibold text-stone-600 uppercase">
                  Description *
                </label>
                <textarea
                  rows={3}
                  class={`mt-1 ${inputClass("description", maintErrors)}`}
                  placeholder="What was done?"
                  value={fDesc}
                  onInput={(e) =>
                    setFDesc((e.target as HTMLTextAreaElement).value)}
                />
                {maintErrors.description && (
                  <p class="text-xs text-red-500 mt-1">
                    {maintErrors.description}
                  </p>
                )}
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Cost ($)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    class={`mt-1 ${inputClass("costDollars", maintErrors)}`}
                    placeholder="0.00"
                    value={fCost}
                    onInput={(e) =>
                      setFCost((e.target as HTMLInputElement).value)}
                  />
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Technician
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("technician", maintErrors)}`}
                    placeholder="Name or company"
                    value={fTech}
                    onInput={(e) =>
                      setFTech((e.target as HTMLInputElement).value)}
                  />
                </div>
              </div>
              <div class="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={closeModal}
                  class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isMaintSubmitting}
                  class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 disabled:opacity-50 transition cursor-pointer"
                >
                  {isMaintSubmitting ? "Saving…" : "Log Record"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Retire Modal */}
      {modal === "retire" && (
        <div
          class="fixed inset-0 bg-stone-900/60 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div class="bg-white rounded-2xl shadow-2xl w-full max-w-sm mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            <div class="px-6 py-4 border-b border-red-100 flex items-center justify-between bg-red-50">
              <h3 class="text-lg font-bold text-red-800">
                🛑 Retire Equipment
              </h3>
              <button
                type="button"
                onClick={closeModal}
                class="text-red-400 hover:text-red-600 hover:bg-red-100 rounded p-1 transition cursor-pointer"
              >
                ✕
              </button>
            </div>
            <form onSubmit={handleRetire} class="p-6 flex flex-col gap-4">
              <p class="text-sm text-stone-600">
                This will mark <strong>{selected?.name}</strong>{" "}
                as retired. This action cannot be undone.
              </p>
              {retireErrors._api && (
                <div class="bg-red-50 border border-red-200 rounded-lg px-4 py-2">
                  <p class="text-sm text-red-700">⚠️ {retireErrors._api}</p>
                </div>
              )}
              <div>
                <label class="text-xs font-semibold text-stone-600 uppercase">
                  Reason *
                </label>
                <textarea
                  rows={2}
                  class={`mt-1 ${inputClass("reason", retireErrors)}`}
                  placeholder="e.g. Beyond economic repair, sold, scrapped…"
                  value={fReason}
                  onInput={(e) =>
                    setFReason((e.target as HTMLTextAreaElement).value)}
                />
                {retireErrors.reason && (
                  <p class="text-xs text-red-500 mt-1">{retireErrors.reason}</p>
                )}
              </div>
              <div class="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={closeModal}
                  class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isRetireSubmitting}
                  class="bg-red-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-red-700 disabled:opacity-50 transition cursor-pointer"
                >
                  {isRetireSubmitting ? "Retiring…" : "Confirm Retire"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
