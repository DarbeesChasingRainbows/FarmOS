import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  extractErrors,
  type FieldErrors,
  HiveInspectionSchema,
} from "../utils/schemas.ts";

interface InspectHiveFormProps {
  hiveId: string;
  hiveName: string;
}

export default function InspectHiveForm(
  { hiveId, hiveName }: InspectHiveFormProps,
) {
  const isOpen = useSignal(false);
  const queenSeen = useSignal(true);
  const broodPattern = useSignal("");
  const temperament = useSignal("");
  const mitesPerHundred = useSignal("");
  const notes = useSignal("");
  const date = useSignal(new Date().toISOString().split("T")[0]);
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = HiveInspectionSchema.safeParse({
      queenSeen: queenSeen.value,
      broodPattern: broodPattern.value,
      temperament: temperament.value,
      mitesPerHundred: mitesPerHundred.value,
      notes: notes.value,
      date: date.value,
    });

    if (!result.success) {
      errors.value = extractErrors(result);
      showToast(
        "error",
        "Validation failed",
        "Please fix the highlighted fields.",
      );
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.inspectHive(hiveId, result.data);
      showToast(
        "success",
        "Inspection recorded",
        `${hiveName} inspection logged for ${date.value}.`,
      );
      broodPattern.value = "";
      temperament.value = "";
      mitesPerHundred.value = "";
      notes.value = "";
      isOpen.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record inspection",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  if (!isOpen.value) {
    return (
      <button
        onClick={() => isOpen.value = true}
        class="text-xs text-amber-600 hover:text-amber-800 font-semibold transition"
      >
        📋 Record Inspection
      </button>
    );
  }

  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        Inspection — {hiveName}
      </h4>
      <form onSubmit={onSubmit} class="flex flex-col gap-3">
        <div class="grid grid-cols-2 gap-3">
          <FormField label="Date" error={errors.value.date} required>
            <input
              type="date"
              class={inputClass("date")}
              value={date.value}
              onInput={(e) => date.value = (e.target as HTMLInputElement).value}
            />
          </FormField>

          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-1">
              <label class="text-sm font-medium text-stone-700">
                Queen Seen
              </label>
              <Tooltip text="Did you visually confirm the queen? If not, look for fresh eggs (laid in past 3 days) as evidence of her presence.">
                <InfoIcon />
              </Tooltip>
            </div>
            <div class="flex gap-2 mt-1">
              <button
                type="button"
                onClick={() => queenSeen.value = true}
                class={`px-3 py-1.5 text-xs rounded-lg font-medium transition ${
                  queenSeen.value
                    ? "bg-emerald-100 text-emerald-800 border border-emerald-300"
                    : "bg-stone-100 text-stone-500"
                }`}
              >
                ✓ Yes
              </button>
              <button
                type="button"
                onClick={() => queenSeen.value = false}
                class={`px-3 py-1.5 text-xs rounded-lg font-medium transition ${
                  !queenSeen.value
                    ? "bg-red-100 text-red-800 border border-red-300"
                    : "bg-stone-100 text-stone-500"
                }`}
              >
                ✕ No
              </button>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FormField
            label="Brood Pattern"
            error={errors.value.broodPattern}
            required
          >
            <select
              class={inputClass("broodPattern")}
              value={broodPattern.value}
              onChange={(e) =>
                broodPattern.value = (e.target as HTMLSelectElement).value}
            >
              <option value="">Select...</option>
              <option value="Solid">Solid — excellent queen</option>
              <option value="Spotty">Spotty — possible issues</option>
              <option value="Drone Heavy">Drone Heavy — laying workers?</option>
              <option value="No Brood">No Brood — emergency</option>
            </select>
          </FormField>

          <FormField
            label="Temperament"
            error={errors.value.temperament}
            required
          >
            <select
              class={inputClass("temperament")}
              value={temperament.value}
              onChange={(e) =>
                temperament.value = (e.target as HTMLSelectElement).value}
            >
              <option value="">Select...</option>
              <option value="Calm">Calm — easy to work</option>
              <option value="Nervous">Nervous — running on frames</option>
              <option value="Defensive">Defensive — stinging</option>
              <option value="Aggressive">Aggressive — unsafe</option>
            </select>
          </FormField>
        </div>

        <FormField
          label="Varroa Mites per 100 bees"
          error={errors.value.mitesPerHundred}
          required
        >
          <div class="flex items-center gap-2">
            <input
              type="number"
              step="0.1"
              class={inputClass("mitesPerHundred")}
              placeholder="e.g. 2.5"
              value={mitesPerHundred.value}
              onInput={(e) =>
                mitesPerHundred.value = (e.target as HTMLInputElement).value}
            />
            <Tooltip
              text="Treatment threshold: >3 mites/100 bees requires immediate treatment. 1-3 is watchable. <1 is excellent."
              position="bottom"
            >
              <InfoIcon />
            </Tooltip>
          </div>
        </FormField>

        <FormField
          label="Notes"
          error={errors.value.notes}
          helpText="Honey stores, swarm cells, disease signs, etc."
        >
          <textarea
            class={inputClass("notes")}
            rows={2}
            placeholder="Optional observations..."
            value={notes.value}
            onInput={(e) =>
              notes.value = (e.target as HTMLTextAreaElement).value}
          />
        </FormField>

        <div class="flex gap-2 mt-1">
          <button
            type="submit"
            disabled={isSubmitting.value}
            class="flex-1 bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm text-sm"
          >
            {isSubmitting.value ? "Saving..." : "Save Inspection"}
          </button>
          <button
            type="button"
            onClick={() => {
              isOpen.value = false;
              errors.value = {};
            }}
            class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
