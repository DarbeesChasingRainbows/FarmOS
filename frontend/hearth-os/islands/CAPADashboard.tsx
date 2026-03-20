import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import {
  CAPACloseSchema,
  CAPASchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

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

export default function CAPADashboard() {
  const records = useSignal<CAPARecord[]>([]);
  const showForm = useSignal(false);
  const errors = useSignal<FieldErrors>({});
  const closingId = useSignal<string | null>(null);
  const closeErrors = useSignal<FieldErrors>({});

  function handleOpen(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = CAPASchema.safeParse(data);

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    // TODO: Call HACCPAPI.openCAPA() and refresh list
    showToast("CAPA opened", "success");
    showForm.value = false;
    errors.value = {};
    form.reset();
  }

  function handleClose(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = CAPACloseSchema.safeParse(data);

    if (!result.success) {
      closeErrors.value = extractErrors(result);
      return;
    }

    // TODO: Call HACCPAPI.closeCAPA() and refresh list
    showToast("CAPA closed", "success");
    closingId.value = null;
    closeErrors.value = {};
    form.reset();
  }

  return (
    <div>
      <div class="flex justify-between items-center mb-6">
        <div>
          <h2 class="text-xl font-bold text-stone-800">CAPA Tracking</h2>
          <p class="text-sm text-stone-500">Corrective and Preventive Actions</p>
        </div>
        <button
          onClick={() => (showForm.value = !showForm.value)}
          class="px-4 py-2 bg-red-700 text-white rounded-lg text-sm font-medium hover:bg-red-600"
        >
          {showForm.value ? "Cancel" : "Open CAPA"}
        </button>
      </div>

      {showForm.value && (
        <form onSubmit={handleOpen} class="bg-white rounded-lg border border-red-200 p-6 mb-6 space-y-4">
          <div>
            <label class="block text-sm font-medium text-stone-700 mb-1">Deviation Description</label>
            <textarea name="description" rows={3} class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Describe the deviation that triggered this CAPA..." />
            {errors.value.description && <p class="text-red-500 text-xs mt-1">{errors.value.description}</p>}
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-stone-700 mb-1">Deviation Source</label>
              <input name="deviationSource" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="CCP Reading, Audit Finding, Customer Complaint..." />
              {errors.value.deviationSource && <p class="text-red-500 text-xs mt-1">{errors.value.deviationSource}</p>}
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
          <button type="submit" class="px-6 py-2 bg-red-700 text-white rounded-lg text-sm font-medium hover:bg-red-600">
            Open CAPA
          </button>
        </form>
      )}

      {records.value.length === 0
        ? (
          <div class="text-center py-12 text-stone-400">
            <p class="text-lg">No open CAPAs</p>
            <p class="text-sm mt-1">All corrective actions resolved</p>
          </div>
        )
        : (
          <div class="space-y-3">
            {records.value.map((record) => (
              <div key={record.id} class={`bg-white rounded-lg border p-4 ${
                record.status === "Open" ? "border-red-200" : "border-stone-200"
              }`}>
                <div class="flex justify-between items-start">
                  <div>
                    <p class="font-medium text-stone-800">{record.description}</p>
                    <p class="text-xs text-stone-500 mt-1">
                      Source: {record.deviationSource}
                      {record.relatedCTE !== undefined && ` | CTE: ${CTE_LABELS[record.relatedCTE]}`}
                    </p>
                  </div>
                  <span class={`px-3 py-1 rounded-full text-xs font-medium ${
                    record.status === "Open" ? "bg-red-50 text-red-700" :
                    record.status === "InProgress" ? "bg-amber-50 text-amber-700" :
                    "bg-emerald-50 text-emerald-700"
                  }`}>
                    {record.status}
                  </span>
                </div>

                {record.status === "Open" && closingId.value === record.id && (
                  <form onSubmit={handleClose} class="mt-4 pt-4 border-t border-stone-100 space-y-3">
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Resolution</label>
                      <textarea name="resolution" rows={2} class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" />
                      {closeErrors.value.resolution && <p class="text-red-500 text-xs">{closeErrors.value.resolution}</p>}
                    </div>
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Verified By</label>
                      <input name="verifiedBy" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" />
                      {closeErrors.value.verifiedBy && <p class="text-red-500 text-xs">{closeErrors.value.verifiedBy}</p>}
                    </div>
                    <button type="submit" class="px-4 py-1.5 bg-emerald-700 text-white rounded text-xs font-medium">Close CAPA</button>
                  </form>
                )}

                {record.status === "Open" && closingId.value !== record.id && (
                  <button
                    onClick={() => (closingId.value = record.id)}
                    class="mt-2 text-xs text-red-600 hover:text-red-800 underline"
                  >
                    Close this CAPA
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
    </div>
  );
}
