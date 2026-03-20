import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import { showToast } from "../utils/toastState.ts";
import {
  DeliverySchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

interface Delivery {
  id: string;
  supplier: string;
  items: string;
  arrivalTempF: number;
  accepted: boolean;
  receivedBy: string;
  receivedAt: string;
  notes?: string;
}

export default function DeliveryLog() {
  const deliveries = useSignal<Delivery[]>([
    {
      id: "del-1",
      supplier: "Fresh Valley Farms",
      items: "Mixed produce (40 lbs)",
      arrivalTempF: 38,
      accepted: true,
      receivedBy: "Maria",
      receivedAt: new Date(Date.now() - 2 * 3_600_000).toISOString(),
    },
    {
      id: "del-2",
      supplier: "Pacific Seafood Co.",
      items: "Salmon fillets, Halibut",
      arrivalTempF: 46,
      accepted: false,
      receivedBy: "James",
      receivedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(),
      notes: "Arrived over 41°F — rejected per cold chain policy",
    },
    {
      id: "del-3",
      supplier: "Heritage Dairy",
      items: "Whole milk (10 gal), Heavy cream (5 gal)",
      arrivalTempF: 40,
      accepted: true,
      receivedBy: "Sam",
      receivedAt: new Date(Date.now() - 26 * 3_600_000).toISOString(),
    },
  ]);

  const showLogForm = useSignal(false);
  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);

  const fSupplier = useSignal("");
  const fItems = useSignal("");
  const fArrivalTemp = useSignal("");
  const fReceivedBy = useSignal("");
  const fAccepted = useSignal(true);
  const fNotes = useSignal("");
  const formErrors = useSignal<FieldErrors>({});
  const isSubmitting = useSignal(false);

  const selected = deliveries.value.find((d) => d.id === selectedId.value);

  const openSidebar = (id: string) => {
    selectedId.value = id;
    isSidebarOpen.value = true;
  };
  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null;
    }, 300);
  };
  const closeForm = () => {
    showLogForm.value = false;
    formErrors.value = {};
  };

  const tempStatus = (
    tempF: number,
    accepted: boolean,
  ): "active" | "attention" | "idle" => {
    if (!accepted) return "attention";
    if (tempF > 41) return "attention";
    return "active";
  };

  const handleLog = async (e: Event) => {
    e.preventDefault();
    const result = DeliverySchema.safeParse({
      supplier: fSupplier.value,
      items: fItems.value,
      arrivalTempF: fArrivalTemp.value,
      receivedBy: fReceivedBy.value,
      accepted: fAccepted.value,
      notes: fNotes.value || undefined,
    });
    if (!result.success) {
      formErrors.value = extractErrors(result);
      return;
    }
    isSubmitting.value = true;
    try {
      const { KitchenAPI } = await import("../utils/farmos-client.ts");
      await KitchenAPI.logDelivery(result.data);
      showToast(
        "success",
        "Delivery logged!",
        `${fSupplier.value} delivery ${
          fAccepted.value ? "accepted" : "rejected"
        }.`,
      );
      closeForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to save",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const formatRelative = (iso: string) => {
    const h = Math.round((Date.now() - new Date(iso).getTime()) / 3_600_000);
    if (h < 1) return "< 1h ago";
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  };

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
      formErrors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  return (
    <div class="relative flex min-h-[400px]">
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[400px]" : ""
        }`}
      >
        <div class="flex flex-col gap-3 mb-6">
          {deliveries.value.map((d) => {
            const status = tempStatus(d.arrivalTempF, d.accepted);
            const isSelected = selectedId.value === d.id;
            return (
              <button
                type="button"
                onClick={() => openSidebar(d.id)}
                class={`bg-white rounded-xl border shadow-sm px-5 py-4 hover:shadow-md transition text-left cursor-pointer w-full flex items-center gap-4 ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : !d.accepted
                    ? "border-red-200 bg-red-50/20"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <span class="text-2xl">{d.accepted ? "✅" : "❌"}</span>
                <div class="flex-1">
                  <p class="text-base font-bold text-stone-800">{d.supplier}</p>
                  <p class="text-xs text-stone-500 mt-0.5">{d.items}</p>
                </div>
                <div class="text-right shrink-0">
                  <p
                    class={`text-sm font-mono font-bold ${
                      d.arrivalTempF > 41 ? "text-red-600" : "text-emerald-600"
                    }`}
                  >
                    {d.arrivalTempF}°F
                  </p>
                  <StatusBadge
                    variant={status}
                    label={d.accepted ? "Accepted" : "Rejected"}
                  />
                  <p class="text-xs text-stone-400 mt-1">
                    {formatRelative(d.receivedAt)}
                  </p>
                </div>
              </button>
            );
          })}
        </div>

        <button
          onClick={() => showLogForm.value = true}
          class="bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition shadow-sm flex items-center gap-2"
        >
          <span>+</span> Log Delivery
        </button>

        {/* Log Delivery Modal */}
        {showLogForm.value && (
          <div
            class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
            onClick={(e) => {
              if (e.target === e.currentTarget) closeForm();
            }}
          >
            <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
              <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
                <h3 class="text-lg font-bold text-stone-800">Log Delivery</h3>
                <button
                  onClick={closeForm}
                  class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
                >
                  ✕
                </button>
              </div>
              <form onSubmit={handleLog} class="p-6 flex flex-col gap-4">
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Supplier
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("supplier")}`}
                    placeholder="Supplier name"
                    value={fSupplier.value}
                    onInput={(e) =>
                      fSupplier.value = (e.target as HTMLInputElement).value}
                  />
                  {formErrors.value.supplier && (
                    <p class="text-xs text-red-500 mt-1">
                      {formErrors.value.supplier}
                    </p>
                  )}
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Items Received
                  </label>
                  <input
                    type="text"
                    class={`mt-1 ${inputClass("items")}`}
                    placeholder="e.g. Salmon fillets, 20 lbs"
                    value={fItems.value}
                    onInput={(e) =>
                      fItems.value = (e.target as HTMLInputElement).value}
                  />
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">
                      Arrival Temp (°F)
                    </label>
                    <input
                      type="number"
                      step="0.1"
                      class={`mt-1 ${inputClass("arrivalTempF")}`}
                      placeholder="e.g. 38"
                      value={fArrivalTemp.value}
                      onInput={(e) =>
                        fArrivalTemp.value =
                          (e.target as HTMLInputElement).value}
                    />
                    {formErrors.value.arrivalTempF && (
                      <p class="text-xs text-red-500 mt-1">
                        {formErrors.value.arrivalTempF}
                      </p>
                    )}
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">
                      Received By
                    </label>
                    <input
                      type="text"
                      class={`mt-1 ${inputClass("receivedBy")}`}
                      placeholder="Staff name"
                      value={fReceivedBy.value}
                      onInput={(e) =>
                        fReceivedBy.value =
                          (e.target as HTMLInputElement).value}
                    />
                  </div>
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Decision
                  </label>
                  <div class="flex gap-3 mt-2">
                    <button
                      type="button"
                      onClick={() => fAccepted.value = true}
                      class={`flex-1 py-2 text-sm font-bold rounded-lg border transition ${
                        fAccepted.value
                          ? "bg-emerald-600 text-white border-emerald-600"
                          : "text-stone-600 border-stone-300 hover:bg-stone-50"
                      }`}
                    >
                      ✅ Accept
                    </button>
                    <button
                      type="button"
                      onClick={() => fAccepted.value = false}
                      class={`flex-1 py-2 text-sm font-bold rounded-lg border transition ${
                        !fAccepted.value
                          ? "bg-red-600 text-white border-red-600"
                          : "text-stone-600 border-stone-300 hover:bg-stone-50"
                      }`}
                    >
                      ❌ Reject
                    </button>
                  </div>
                </div>
                <div>
                  <label class="text-xs font-semibold text-stone-600 uppercase">
                    Notes (optional)
                  </label>
                  <textarea
                    class={`mt-1 ${inputClass("notes")}`}
                    rows={2}
                    placeholder="Reason for rejection, condition notes..."
                    value={fNotes.value}
                    onInput={(e) =>
                      fNotes.value = (e.target as HTMLTextAreaElement).value}
                  />
                </div>
                <div class="flex justify-end gap-3">
                  <button
                    type="button"
                    onClick={closeForm}
                    class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting.value}
                    class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 disabled:opacity-50 transition"
                  >
                    {isSubmitting.value ? "Saving..." : "Log Delivery"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
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
          <div class="p-6">
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <h3 class="text-xl font-bold text-stone-800">
                  {selected.supplier}
                </h3>
                <p class="text-sm text-stone-500 mt-1">
                  {formatRelative(selected.receivedAt)} · Received by{" "}
                  {selected.receivedBy}
                </p>
                <div class="mt-2">
                  <StatusBadge
                    variant={tempStatus(
                      selected.arrivalTempF,
                      selected.accepted,
                    )}
                    label={selected.accepted ? "Accepted" : "Rejected"}
                  />
                </div>
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>
            <div class="flex flex-col gap-4">
              <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                  Items
                </p>
                <p class="text-base font-semibold text-stone-800 mt-1">
                  {selected.items}
                </p>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div
                  class={`rounded-xl p-4 border ${
                    selected.arrivalTempF > 41
                      ? "bg-red-50 border-red-200"
                      : "bg-emerald-50 border-emerald-200"
                  }`}
                >
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Arrival Temp
                  </p>
                  <p
                    class={`text-2xl font-mono font-bold mt-1 ${
                      selected.arrivalTempF > 41
                        ? "text-red-600"
                        : "text-emerald-600"
                    }`}
                  >
                    {selected.arrivalTempF}°F
                  </p>
                  {selected.arrivalTempF > 41 && (
                    <p class="text-xs text-red-600 mt-1">
                      Above 41°F safe limit
                    </p>
                  )}
                </div>
                <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Decision
                  </p>
                  <p
                    class={`text-lg font-bold mt-1 ${
                      selected.accepted ? "text-emerald-600" : "text-red-600"
                    }`}
                  >
                    {selected.accepted ? "✅ Accepted" : "❌ Rejected"}
                  </p>
                </div>
              </div>
              {selected.notes && (
                <div class="bg-amber-50 rounded-xl p-4 border border-amber-100">
                  <p class="text-xs text-amber-700 font-medium">
                    📝 {selected.notes}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
