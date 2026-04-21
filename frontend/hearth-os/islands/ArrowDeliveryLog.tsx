import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

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

const formatRelative = (iso: string) => {
  const h = Math.round((Date.now() - new Date(iso).getTime()) / 3_600_000);
  if (h < 1) return "< 1h ago";
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
};

const MOCK_DELIVERIES: Delivery[] = [
  { id: "del-1", supplier: "Fresh Valley Farms", items: "Mixed produce (40 lbs)", arrivalTempF: 38, accepted: true, receivedBy: "Maria", receivedAt: new Date(Date.now() - 2 * 3_600_000).toISOString() },
  { id: "del-2", supplier: "Pacific Seafood Co.", items: "Salmon fillets, Halibut", arrivalTempF: 46, accepted: false, receivedBy: "James", receivedAt: new Date(Date.now() - 5 * 3_600_000).toISOString(), notes: "Arrived over 41°F — rejected per cold chain policy" },
  { id: "del-3", supplier: "Heritage Dairy", items: "Whole milk (10 gal), Heavy cream (5 gal)", arrivalTempF: 40, accepted: true, receivedBy: "Sam", receivedAt: new Date(Date.now() - 26 * 3_600_000).toISOString() },
];

export default function ArrowDeliveryLog() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      deliveries: [...MOCK_DELIVERIES] as Delivery[],
      showForm: false,
      selectedId: null as string | null,
      sidebarOpen: false,
      fSupplier: "",
      fItems: "",
      fArrivalTemp: "",
      fReceivedBy: "",
      fAccepted: true,
      fNotes: "",
      errors: {} as Record<string, string>,
      submitting: false,
    });

    const selected = () => state.deliveries.find(d => d.id === state.selectedId);
    const openSidebar = (id: string) => { state.selectedId = id; state.sidebarOpen = true; };
    const closeSidebar = () => { state.sidebarOpen = false; setTimeout(() => { state.selectedId = null; }, 300); };
    const closeForm = () => { state.showForm = false; clearErrors(state.errors); };

    const handleLog = async (e: Event) => {
      e.preventDefault();
      const errs: Record<string, string> = {};
      if (!state.fSupplier) errs.supplier = "Required";
      if (!state.fItems) errs.items = "Required";
      if (!state.fArrivalTemp) errs.arrivalTempF = "Required";
      if (!state.fReceivedBy) errs.receivedBy = "Required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      state.submitting = true;
      try {
        const { KitchenAPI } = await import("../utils/farmos-client.ts");
        await KitchenAPI.logDelivery({
          supplier: state.fSupplier,
          items: state.fItems,
          arrivalTempF: Number(state.fArrivalTemp),
          receivedBy: state.fReceivedBy,
          accepted: state.fAccepted,
          notes: state.fNotes || undefined,
        });
        showToast("success", "Delivery logged!", `${state.fSupplier} delivery ${state.fAccepted ? "accepted" : "rejected"}.`);
        closeForm();
      } catch (err: unknown) {
        showToast("error", "Failed to save", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.submitting = false;
      }
    };

    html`
      <div class="relative flex min-h-[400px]">
        <div class="${() => `flex-1 transition-all duration-300 ${state.sidebarOpen ? 'mr-[400px]' : ''}`}">
          <div class="flex flex-col gap-3 mb-6">
            ${() => state.deliveries.map(d => html`
              <button type="button" @click="${() => openSidebar(d.id)}"
                class="${`bg-white rounded-xl border shadow-sm px-5 py-4 hover:shadow-md transition text-left cursor-pointer w-full flex items-center gap-4 ${
                  state.selectedId === d.id ? 'border-amber-400 ring-2 ring-amber-200'
                  : !d.accepted ? 'border-red-200 bg-red-50/20'
                  : 'border-stone-200 hover:border-amber-200'
                }`}">
                <span class="text-2xl">${d.accepted ? "✅" : "❌"}</span>
                <div class="flex-1">
                  <p class="text-base font-bold text-stone-800">${d.supplier}</p>
                  <p class="text-xs text-stone-500 mt-0.5">${d.items}</p>
                </div>
                <div class="text-right shrink-0">
                  <p class="${`text-sm font-mono font-bold ${d.arrivalTempF > 41 ? 'text-red-600' : 'text-emerald-600'}`}">${d.arrivalTempF}°F</p>
                  <span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${d.accepted ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'}`}">${d.accepted ? "Accepted" : "Rejected"}</span>
                  <p class="text-xs text-stone-400 mt-1">${formatRelative(d.receivedAt)}</p>
                </div>
              </button>
            `.key(d.id))}
          </div>

          <button @click="${() => state.showForm = true}" class="bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition shadow-sm flex items-center gap-2">
            <span>+</span> Log Delivery
          </button>

          ${() => state.showForm ? html`
            <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50" @click="${(e: Event) => { if (e.target === e.currentTarget) closeForm(); }}">
              <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden">
                <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
                  <h3 class="text-lg font-bold text-stone-800">Log Delivery</h3>
                  <button @click="${closeForm}" class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition">✕</button>
                </div>
                <form @submit="${handleLog}" class="p-6 flex flex-col gap-4">
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Supplier</label>
                    <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Supplier name"
                      @input="${(e: Event) => state.fSupplier = (e?.target as HTMLInputElement)?.value ?? ''}">
                    ${() => state.errors.supplier ? html`<p class="text-xs text-red-500 mt-1">${state.errors.supplier}</p>` : html`<span></span>`}
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Items Received</label>
                    <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g. Salmon fillets, 20 lbs"
                      @input="${(e: Event) => state.fItems = (e?.target as HTMLInputElement)?.value ?? ''}">
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">Arrival Temp (°F)</label>
                      <input type="number" step="0.1" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="e.g. 38"
                        @input="${(e: Event) => state.fArrivalTemp = (e?.target as HTMLInputElement)?.value ?? ''}">
                      ${() => state.errors.arrivalTempF ? html`<p class="text-xs text-red-500 mt-1">${state.errors.arrivalTempF}</p>` : html`<span></span>`}
                    </div>
                    <div>
                      <label class="text-xs font-semibold text-stone-600 uppercase">Received By</label>
                      <input type="text" class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Staff name"
                        @input="${(e: Event) => state.fReceivedBy = (e?.target as HTMLInputElement)?.value ?? ''}">
                    </div>
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Decision</label>
                    <div class="flex gap-3 mt-2">
                      <button type="button" @click="${() => state.fAccepted = true}"
                        class="${() => `flex-1 py-2 text-sm font-bold rounded-lg border transition ${state.fAccepted ? 'bg-emerald-600 text-white border-emerald-600' : 'text-stone-600 border-stone-300 hover:bg-stone-50'}`}">✅ Accept</button>
                      <button type="button" @click="${() => state.fAccepted = false}"
                        class="${() => `flex-1 py-2 text-sm font-bold rounded-lg border transition ${!state.fAccepted ? 'bg-red-600 text-white border-red-600' : 'text-stone-600 border-stone-300 hover:bg-stone-50'}`}">❌ Reject</button>
                    </div>
                  </div>
                  <div>
                    <label class="text-xs font-semibold text-stone-600 uppercase">Notes (optional)</label>
                    <textarea class="mt-1 w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" rows="2" placeholder="Reason for rejection, condition notes..."
                      @input="${(e: Event) => state.fNotes = (e?.target as HTMLTextAreaElement)?.value ?? ''}"></textarea>
                  </div>
                  <div class="flex justify-end gap-3">
                    <button type="button" @click="${closeForm}" class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition">Cancel</button>
                    <button type="submit" class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 transition">${() => state.submitting ? "Saving..." : "Log Delivery"}</button>
                  </div>
                </form>
              </div>
            </div>
          ` : html`<span></span>`}
        </div>

        ${() => state.sidebarOpen ? html`<div class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30" @click="${closeSidebar}"></div>` : html`<span></span>`}

        <aside class="${() => `fixed top-0 right-0 h-full w-[380px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${state.sidebarOpen ? 'translate-x-0' : 'translate-x-full'}`}">
          ${() => {
            const d = selected();
            if (!d) return html`<span></span>`;
            return html`
              <div class="p-6">
                <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
                  <div>
                    <h3 class="text-xl font-bold text-stone-800">${d.supplier}</h3>
                    <p class="text-sm text-stone-500 mt-1">${formatRelative(d.receivedAt)} · Received by ${d.receivedBy}</p>
                    <div class="mt-2">
                      <span class="${`px-2 py-0.5 rounded-full text-xs font-medium ${d.accepted ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'}`}">${d.accepted ? "Accepted" : "Rejected"}</span>
                    </div>
                  </div>
                  <button @click="${closeSidebar}" class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition">✕</button>
                </div>
                <div class="flex flex-col gap-4">
                  <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                    <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Items</p>
                    <p class="text-base font-semibold text-stone-800 mt-1">${d.items}</p>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div class="${`rounded-xl p-4 border ${d.arrivalTempF > 41 ? 'bg-red-50 border-red-200' : 'bg-emerald-50 border-emerald-200'}`}">
                      <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Arrival Temp</p>
                      <p class="${`text-2xl font-mono font-bold mt-1 ${d.arrivalTempF > 41 ? 'text-red-600' : 'text-emerald-600'}`}">${d.arrivalTempF}°F</p>
                      ${d.arrivalTempF > 41 ? html`<p class="text-xs text-red-600 mt-1">Above 41°F safe limit</p>` : html`<span></span>`}
                    </div>
                    <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                      <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">Decision</p>
                      <p class="${`text-lg font-bold mt-1 ${d.accepted ? 'text-emerald-600' : 'text-red-600'}`}">${d.accepted ? "✅ Accepted" : "❌ Rejected"}</p>
                    </div>
                  </div>
                  ${d.notes ? html`
                    <div class="bg-amber-50 rounded-xl p-4 border border-amber-100">
                      <p class="text-xs text-amber-700 font-medium">📝 ${d.notes}</p>
                    </div>
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
