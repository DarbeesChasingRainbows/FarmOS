import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import {
  extractErrors,
  FeedingSchema,
  type FieldErrors,
} from "../utils/schemas.ts";

const feedTypes = [
  {
    value: 0,
    label: "Sugar Syrup",
    desc: "1:1 (spring) or 2:1 (fall) sugar water",
  },
  {
    value: 1,
    label: "Fondant",
    desc: "Solid sugar block for winter emergency feeding",
  },
  {
    value: 2,
    label: "Pollen Patty",
    desc: "Protein supplement for brood rearing",
  },
  { value: 3, label: "Other", desc: "Honey frames, dry sugar, etc." },
];

interface FeedHiveFormProps {
  hiveId: string;
  hiveName: string;
}

export default function FeedHiveForm(
  { hiveId, hiveName }: FeedHiveFormProps,
) {
  const isOpen = useSignal(false);
  const feedType = useSignal("0");
  const amountValue = useSignal("");
  const amountUnit = useSignal("lbs");
  const concentration = useSignal("");
  const date = useSignal(new Date().toISOString().split("T")[0]);
  const notes = useSignal("");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const resetForm = () => {
    isOpen.value = false;
    amountValue.value = "";
    concentration.value = "";
    notes.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = FeedingSchema.safeParse({
      feedType: feedType.value,
      amountValue: amountValue.value,
      amountUnit: amountUnit.value,
      concentration: concentration.value,
      date: date.value,
      notes: notes.value,
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
      await ApiaryAPI.feedHive(hiveId, {
        data: {
          feedType: result.data.feedType,
          amount: {
            value: result.data.amountValue,
            unit: result.data.amountUnit,
            measure: "weight",
          },
          concentration: result.data.concentration || undefined,
          date: result.data.date,
          notes: result.data.notes || undefined,
        },
      });
      showToast(
        "success",
        "Feeding recorded",
        `${hiveName} fed ${result.data.amountValue} ${result.data.amountUnit} of ${
          feedTypes[result.data.feedType].label
        }.`,
      );
      resetForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record feeding",
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
        🍯 Record Feeding
      </button>
    );
  }

  const showConcentration = Number(feedType.value) === 0; // Only for Sugar Syrup

  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        Record Feeding — {hiveName}
      </h4>
      <form onSubmit={onSubmit} class="flex flex-col gap-3">
        <div class="grid grid-cols-2 gap-3">
          <FormField label="Feed Type" error={errors.value.feedType} required>
            <select
              class={inputClass("feedType")}
              value={feedType.value}
              onChange={(e) =>
                feedType.value = (e.target as HTMLSelectElement).value}
            >
              {feedTypes.map((t) => <option value={t.value}>{t.label}</option>)}
            </select>
            <p class="text-xs text-stone-400 mt-1">
              {feedTypes[Number(feedType.value)]?.desc}
            </p>
          </FormField>

          <FormField label="Date" error={errors.value.date} required>
            <input
              type="date"
              class={inputClass("date")}
              value={date.value}
              onInput={(e) => date.value = (e.target as HTMLInputElement).value}
            />
          </FormField>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FormField label="Amount" error={errors.value.amountValue} required>
            <div class="flex gap-2">
              <input
                type="number"
                step="0.1"
                class={inputClass("amountValue")}
                placeholder="e.g. 2.5"
                value={amountValue.value}
                onInput={(e) =>
                  amountValue.value = (e.target as HTMLInputElement).value}
              />
              <select
                class="px-2 py-2 border rounded-lg text-sm border-stone-300 focus:outline-none focus:ring-2 focus:ring-amber-500"
                value={amountUnit.value}
                onChange={(e) =>
                  amountUnit.value = (e.target as HTMLSelectElement).value}
              >
                <option value="lbs">lbs</option>
                <option value="kg">kg</option>
                <option value="qt">qt</option>
                <option value="L">L</option>
              </select>
            </div>
          </FormField>

          {showConcentration && (
            <FormField
              label="Concentration"
              error={errors.value.concentration}
              helpText="Sugar-to-water ratio"
            >
              <select
                class={inputClass("concentration")}
                value={concentration.value}
                onChange={(e) =>
                  concentration.value = (e.target as HTMLSelectElement).value}
              >
                <option value="">Select...</option>
                <option value="1:1">1:1 — Spring stimulative</option>
                <option value="2:1">2:1 — Fall stores building</option>
              </select>
            </FormField>
          )}
        </div>

        <FormField label="Notes" error={errors.value.notes}>
          <textarea
            class={inputClass("notes")}
            rows={2}
            placeholder="Optional notes..."
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
            {isSubmitting.value ? "Saving..." : "Save Feeding"}
          </button>
          <button
            type="button"
            onClick={resetForm}
            class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
