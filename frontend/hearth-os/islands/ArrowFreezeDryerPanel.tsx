import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";
import {
  FreezeDryerAPI,
  type FreezeDryerBatchDto,
  HarvestRightAPI,
  type HarvestRightStatus,
} from "../utils/farmos-client.ts";

const PHASES = ["Loading", "Freezing", "PrimaryDrying", "SecondaryDrying", "Complete", "Aborted"];
const PHASE_COLORS: Record<string, string> = {
  Loading: "bg-stone-50 text-stone-600",
  Freezing: "bg-blue-50 text-blue-700",
  PrimaryDrying: "bg-amber-50 text-amber-700",
  SecondaryDrying: "bg-orange-50 text-orange-700",
  Complete: "bg-emerald-50 text-emerald-700",
  Aborted: "bg-red-50 text-red-700",
};

export default function ArrowFreezeDryerPanel() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      batches: [] as FreezeDryerBatchDto[],
      showForm: false,
      errors: {} as Record<string, string>,
      readingErrors: {} as Record<string, string>,
      selectedBatch: null as string | null,
      hrStatus: null as HarvestRightStatus | null,
      submitting: false,
    });

    async function refreshBatches() {
      try { state.batches = await FreezeDryerAPI.getBatches() ?? []; } catch { /* */ }
    }
    refreshBatches();
    async function fetchHRStatus() {
      try { state.hrStatus = await HarvestRightAPI.getStatus(); } catch { state.hrStatus = null; }
    }
    fetchHRStatus();
    const hrInterval = setInterval(fetchHRStatus, 30_000);

    const handleStartBatch = async (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      const batchCode = fd.get("batchCode") as string;
      const dryerId = fd.get("dryerId") as string;
      const productDescription = fd.get("productDescription") as string;
      const preDryWeight = fd.get("preDryWeight") as string;
      const errs: Record<string, string> = {};
      if (!batchCode) errs.batchCode = "Required";
      if (!dryerId) errs.dryerId = "Required";
      if (!productDescription) errs.productDescription = "Required";
      if (!preDryWeight) errs.preDryWeight = "Required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      state.submitting = true;
      try {
        await FreezeDryerAPI.startBatch({ batchCode, dryerId, productDescription, preDryWeight: Number(preDryWeight) });
        await refreshBatches();
        showToast("success", "Batch started", "Freeze-dryer batch is now tracking.");
        state.showForm = false;
        clearErrors(state.errors);
        form.reset();
      } catch { showToast("error", "Failed", "Could not start batch."); }
      finally { state.submitting = false; }
    };

    const handleRecordReading = async (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      const shelfTempF = fd.get("shelfTempF") as string;
      const vacuumMTorr = fd.get("vacuumMTorr") as string;
      const productTempF = fd.get("productTempF") as string;
      const errs: Record<string, string> = {};
      if (!shelfTempF) errs.shelfTempF = "Required";
      if (!vacuumMTorr) errs.vacuumMTorr = "Required";
      if (Object.keys(errs).length) { setErrors(state.readingErrors, errs); return; }
      if (!state.selectedBatch) return;
      state.submitting = true;
      try {
        await FreezeDryerAPI.recordReading(state.selectedBatch, {
          reading: { shelfTempF: Number(shelfTempF), vacuumMTorr: Number(vacuumMTorr), productTempF: productTempF ? Number(productTempF) : undefined },
        });
        showToast("success", "Reading recorded", "Freeze-dryer reading saved.");
        clearErrors(state.readingErrors);
        form.reset();
      } catch { showToast("error", "Failed", "Could not record reading."); }
      finally { state.submitting = false; }
    };

    html`
      <div>
        ${() => state.hrStatus ? html`
          <div class="${`mb-4 px-4 py-3 rounded-lg border flex items-center gap-3 ${state.hrStatus?.connected ? 'bg-emerald-50 border-emerald-200' : 'bg-stone-50 border-stone-200'}`}">
            <span class="${`inline-block w-2.5 h-2.5 rounded-full ${state.hrStatus?.connected ? 'bg-emerald-500 animate-pulse' : 'bg-stone-400'}`}"></span>
            <div class="flex-1">
              <span class="text-sm font-medium text-stone-700">${state.hrStatus?.connected ? "Connected to Harvest Right Cloud" : "Harvest Right Disconnected"}</span>
            </div>
          </div>
        ` : html`<span></span>`}

        <div class="flex justify-between items-center mb-6">
          <h2 class="text-xl font-bold text-stone-800">Freeze-Dryer Batches</h2>
          <button @click="${() => state.showForm = !state.showForm}" class="px-4 py-2 bg-stone-800 text-white rounded-lg text-sm font-medium hover:bg-stone-700 transition">
            ${() => state.showForm ? "Cancel" : "New Batch"}
          </button>
        </div>

        ${() => state.showForm ? html`
          <form @submit="${handleStartBatch}" class="bg-white rounded-lg border border-stone-200 p-6 mb-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-stone-700 mb-1">Batch Code</label>
                <input name="batchCode" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="FD-20260316-01">
                ${() => state.errors.batchCode ? html`<p class="text-red-500 text-xs mt-1">${state.errors.batchCode}</p>` : html`<span></span>`}
              </div>
              <div>
                <label class="block text-sm font-medium text-stone-700 mb-1">Dryer</label>
                <select name="dryerId" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm">
                  <option value="">Select dryer...</option>
                </select>
                ${() => state.errors.dryerId ? html`<p class="text-red-500 text-xs mt-1">${state.errors.dryerId}</p>` : html`<span></span>`}
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-stone-700 mb-1">Product Description</label>
              <input name="productDescription" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Beef jerky strips, Lion's Mane slices...">
              ${() => state.errors.productDescription ? html`<p class="text-red-500 text-xs mt-1">${state.errors.productDescription}</p>` : html`<span></span>`}
            </div>
            <div class="w-1/3">
              <label class="block text-sm font-medium text-stone-700 mb-1">Pre-Dry Weight (lbs)</label>
              <input name="preDryWeight" type="number" step="0.01" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm">
              ${() => state.errors.preDryWeight ? html`<p class="text-red-500 text-xs mt-1">${state.errors.preDryWeight}</p>` : html`<span></span>`}
            </div>
            <button type="submit" class="px-6 py-2 bg-stone-800 text-white rounded-lg text-sm font-medium hover:bg-stone-700 transition">${() => state.submitting ? "Starting..." : "Start Batch"}</button>
          </form>
        ` : html`<span></span>`}

        ${() => state.batches.length === 0 ? html`
          <div class="text-center py-12 text-stone-400">
            <p class="text-lg">No active freeze-dryer batches</p>
            <p class="text-sm mt-1">Start a new batch to begin tracking</p>
          </div>
        ` : html`
          <div class="space-y-4">
            ${() => state.batches.map(batch => html`
              <div class="bg-white rounded-lg border border-stone-200 p-4">
                <div class="flex justify-between items-center">
                  <div>
                    <span class="font-mono font-bold text-stone-800">${batch.batchCode}</span>
                    <span class="ml-3 text-sm text-stone-500">${batch.productDescription}</span>
                  </div>
                  <span class="${`px-3 py-1 rounded-full text-xs font-medium ${PHASE_COLORS[PHASES[batch.phase]] ?? 'bg-stone-50 text-stone-600'}`}">${PHASES[batch.phase]}</span>
                </div>
                <div class="mt-2 text-xs text-stone-400">
                  Pre-dry: ${batch.preDryWeight} lbs${batch.postDryWeight ? ` | Post-dry: ${batch.postDryWeight} lbs | Ratio: ${(batch.postDryWeight / batch.preDryWeight * 100).toFixed(1)}%` : ""}
                </div>
                ${() => state.selectedBatch === batch.id && batch.phase > 0 && batch.phase < 4 ? html`
                  <form @submit="${handleRecordReading}" class="mt-4 pt-4 border-t border-stone-100 grid grid-cols-3 gap-3">
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Shelf Temp (°F)</label>
                      <input name="shelfTempF" type="number" step="0.1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm">
                      ${() => state.readingErrors.shelfTempF ? html`<p class="text-red-500 text-xs">${state.readingErrors.shelfTempF}</p>` : html`<span></span>`}
                    </div>
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Vacuum (mTorr)</label>
                      <input name="vacuumMTorr" type="number" step="1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm">
                      ${() => state.readingErrors.vacuumMTorr ? html`<p class="text-red-500 text-xs">${state.readingErrors.vacuumMTorr}</p>` : html`<span></span>`}
                    </div>
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Product Temp (°F)</label>
                      <input name="productTempF" type="number" step="0.1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Optional">
                    </div>
                    <div class="col-span-3 flex gap-2">
                      <button type="submit" class="px-4 py-1.5 bg-stone-700 text-white rounded text-xs font-medium hover:bg-stone-600 transition">${() => state.submitting ? "Recording..." : "Record"}</button>
                    </div>
                  </form>
                ` : html`<span></span>`}
                ${batch.phase > 0 && batch.phase < 4 ? html`
                  <button @click="${() => state.selectedBatch = state.selectedBatch === batch.id ? null : batch.id}" class="mt-2 text-xs text-stone-500 hover:text-stone-700 underline">
                    ${() => state.selectedBatch === batch.id ? "Hide Reading Form" : "Record Reading"}
                  </button>
                ` : html`<span></span>`}
              </div>
            `.key(batch.id))}
          </div>
        `}
      </div>
    `(containerRef.current);

    return () => clearInterval(hrInterval);
  }, []);

  return <div ref={containerRef} />;
}
