import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

const CTE_LABELS = ["Receiving", "Transformation", "Shipping"];

interface CAPARecord {
  id: string;
  description: string;
  deviationSource: string;
  relatedCTE?: number;
  status: "Open" | "InProgress" | "Closed" | "Verified";
  openedAt: string;
  closedAt?: string;
}

export default function ArrowCAPADashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      records: [] as CAPARecord[],
      showForm: false,
      closingId: null as string | null,
      errors: {} as Record<string, string>,
      closeErrors: {} as Record<string, string>,
    });

    const statusBadge = (status: string) => {
      const cls = status === "Open"
        ? "bg-red-50 text-red-700"
        : status === "InProgress"
        ? "bg-amber-50 text-amber-700"
        : "bg-emerald-50 text-emerald-700";
      return `<span class="px-3 py-1 rounded-full text-xs font-medium ${cls}">${status}</span>`;
    };

    const handleOpen = (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      const desc = fd.get("description") as string;
      const src = fd.get("deviationSource") as string;
      const cte = fd.get("relatedCTE") as string;
      const errs: Record<string, string> = {};
      if (!desc || desc.length < 5) errs.description = "Description required (min 5 chars)";
      if (!src) errs.deviationSource = "Source is required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      showToast("success", "CAPA opened", "Corrective action record created.");
      state.showForm = false;
      clearErrors(state.errors);
      form.reset();
    };

    const handleClose = (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      const resolution = fd.get("resolution") as string;
      const verifiedBy = fd.get("verifiedBy") as string;
      const errs: Record<string, string> = {};
      if (!resolution) errs.resolution = "Resolution is required";
      if (!verifiedBy) errs.verifiedBy = "Verified by is required";
      if (Object.keys(errs).length) { setErrors(state.closeErrors, errs); return; }
      showToast("success", "CAPA closed", "Corrective action resolved.");
      state.closingId = null;
      clearErrors(state.closeErrors);
      form.reset();
    };

    const errMsg = (bag: Record<string, string>, field: string) =>
      bag[field] ? `<p class="text-red-500 text-xs mt-1">${bag[field]}</p>` : "";

    html`
      <div>
        <div class="flex justify-between items-center mb-6">
          <div>
            <h2 class="text-xl font-bold text-stone-800">CAPA Tracking</h2>
            <p class="text-sm text-stone-500">Corrective and Preventive Actions</p>
          </div>
          <button
            @click="${() => state.showForm = !state.showForm}"
            class="px-4 py-2 bg-red-700 text-white rounded-lg text-sm font-medium hover:bg-red-600 transition"
          >${() => state.showForm ? "Cancel" : "Open CAPA"}</button>
        </div>

        ${() => state.showForm ? html`
          <form @submit="${handleOpen}" class="bg-white rounded-lg border border-red-200 p-6 mb-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-stone-700 mb-1">Deviation Description</label>
              <textarea name="description" rows="3" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Describe the deviation that triggered this CAPA..."></textarea>
              ${() => errMsg(state.errors, "description")}
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-stone-700 mb-1">Deviation Source</label>
                <input name="deviationSource" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="CCP Reading, Audit Finding...">
                ${() => errMsg(state.errors, "deviationSource")}
              </div>
              <div>
                <label class="block text-sm font-medium text-stone-700 mb-1">Related CTE (optional)</label>
                <select name="relatedCTE" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm">
                  <option value="">None</option>
                  <option value="0">Receiving</option>
                  <option value="1">Transformation</option>
                  <option value="2">Shipping</option>
                </select>
              </div>
            </div>
            <button type="submit" class="px-6 py-2 bg-red-700 text-white rounded-lg text-sm font-medium hover:bg-red-600 transition">Open CAPA</button>
          </form>
        ` : html`<span></span>`}

        ${() => state.records.length === 0
          ? html`
            <div class="text-center py-12 text-stone-400">
              <p class="text-lg">No open CAPAs</p>
              <p class="text-sm mt-1">All corrective actions resolved</p>
            </div>`
          : html`
            <div class="space-y-3">
              ${() => state.records.map((r) => html`
                <div class="${`bg-white rounded-lg border p-4 ${r.status === 'Open' ? 'border-red-200' : 'border-stone-200'}`}">
                  <div class="flex justify-between items-start">
                    <div>
                      <p class="font-medium text-stone-800">${r.description}</p>
                      <p class="text-xs text-stone-500 mt-1">Source: ${r.deviationSource}${r.relatedCTE !== undefined ? ` | CTE: ${CTE_LABELS[r.relatedCTE]}` : ""}</p>
                    </div>
                    ${statusBadge(r.status)}
                  </div>
                  ${r.status === "Open" && state.closingId === r.id ? html`
                    <form @submit="${handleClose}" class="mt-4 pt-4 border-t border-stone-100 space-y-3">
                      <div>
                        <label class="block text-xs font-medium text-stone-600 mb-1">Resolution</label>
                        <textarea name="resolution" rows="2" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm"></textarea>
                        ${() => errMsg(state.closeErrors, "resolution")}
                      </div>
                      <div>
                        <label class="block text-xs font-medium text-stone-600 mb-1">Verified By</label>
                        <input name="verifiedBy" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm">
                        ${() => errMsg(state.closeErrors, "verifiedBy")}
                      </div>
                      <button type="submit" class="px-4 py-1.5 bg-emerald-700 text-white rounded text-xs font-medium">Close CAPA</button>
                    </form>
                  ` : html`<span></span>`}
                  ${r.status === "Open" && state.closingId !== r.id ? html`
                    <button @click="${() => state.closingId = r.id}" class="mt-2 text-xs text-red-600 hover:text-red-800 underline">Close this CAPA</button>
                  ` : html`<span></span>`}
                </div>
              `.key(r.id))}
            </div>`
        }
      </div>
    `(containerRef.current);
  }, []);

  return <div ref={containerRef} />;
}
