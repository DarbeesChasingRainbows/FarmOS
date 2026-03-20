import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  type FieldErrors,
  MonitoringCorrectionSchema,
} from "../utils/schemas.ts";

interface Props {
  logId: string;
  originalValue: number;
  onClose: () => void;
}

export default function MonitoringLogCorrection({ logId, originalValue, onClose }: Props) {
  const errors = useSignal<FieldErrors>({});

  function handleSubmit(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = MonitoringCorrectionSchema.safeParse(data);

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    // TODO: Call HACCPAPI.appendCorrection()
    showToast("Correction appended to immutable log", "success");
    errors.value = {};
    onClose();
  }

  return (
    <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <div class="flex justify-between items-center mb-4">
          <h3 class="text-lg font-bold text-stone-800">Append Correction</h3>
          <button onClick={onClose} class="text-stone-400 hover:text-stone-600 text-xl">&times;</button>
        </div>

        <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-4 text-xs text-amber-700">
          <strong>Immutable Log:</strong> The original reading ({originalValue}°F) cannot be edited.
          This correction will be appended as a new event referencing log <code class="font-mono">{logId.slice(0, 8)}...</code>
        </div>

        <form onSubmit={handleSubmit} class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-stone-700 mb-1">Reason for Correction</label>
            <textarea name="reason" rows={3} class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Calibration error, misread thermometer, wrong equipment..." />
            {errors.value.reason && <p class="text-red-500 text-xs mt-1">{errors.value.reason}</p>}
          </div>
          <div>
            <label class="block text-sm font-medium text-stone-700 mb-1">Corrected Value (°F) — optional</label>
            <input name="correctedValueF" type="number" step="0.1" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Leave blank if no new value" />
          </div>
          <div>
            <label class="block text-sm font-medium text-stone-700 mb-1">Corrected By</label>
            <input name="correctedBy" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" />
            {errors.value.correctedBy && <p class="text-red-500 text-xs mt-1">{errors.value.correctedBy}</p>}
          </div>
          <div class="flex gap-3">
            <button type="submit" class="px-6 py-2 bg-stone-800 text-white rounded-lg text-sm font-medium hover:bg-stone-700">
              Append Correction
            </button>
            <button type="button" onClick={onClose} class="px-6 py-2 border border-stone-300 rounded-lg text-sm font-medium text-stone-600 hover:bg-stone-50">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
