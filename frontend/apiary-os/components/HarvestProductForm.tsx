import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import { type FieldErrors } from "../utils/schemas.ts";

const productTypes = [
  { value: 0, label: "Honey", unit: "lbs" },
  { value: 1, label: "Wax", unit: "lbs" },
  { value: 2, label: "Pollen", unit: "oz" },
  { value: 3, label: "Propolis", unit: "oz" },
  { value: 4, label: "Royal Jelly", unit: "g" },
  { value: 5, label: "Nuc Sale", unit: "frames" },
];

interface HarvestProductFormProps {
  hiveId: string;
  hiveName: string;
}

export default function HarvestProductForm(
  { hiveId, hiveName }: HarvestProductFormProps,
) {
  const isOpen = useSignal(false);
  const product = useSignal("0");
  const yieldValue = useSignal("");
  const yieldUnit = useSignal("lbs");
  const method = useSignal("");
  const moisturePercent = useSignal("");
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
    yieldValue.value = "";
    method.value = "";
    moisturePercent.value = "";
    notes.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    if (!yieldValue.value || Number(yieldValue.value) <= 0) {
      errors.value = { yieldValue: "Must be positive" };
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.harvestProduct(hiveId, {
        data: {
          product: Number(product.value),
          yield: {
            value: Number(yieldValue.value),
            unit: yieldUnit.value,
            measure: "weight",
          },
          date: date.value,
          method: method.value || undefined,
          notes: notes.value || undefined,
          moisturePercent: moisturePercent.value
            ? Number(moisturePercent.value)
            : undefined,
        },
      });
      const productLabel =
        productTypes[Number(product.value)]?.label ?? "Product";
      showToast(
        "success",
        "Harvest recorded",
        `${yieldValue.value} ${yieldUnit.value} of ${productLabel} from ${hiveName}.`,
      );
      resetForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record harvest",
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
        🏺 Harvest Product
      </button>
    );
  }

  const selectedProduct = Number(product.value);
  const showMoisture = selectedProduct === 0; // Only for Honey

  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        Harvest Product — {hiveName}
      </h4>
      <form onSubmit={onSubmit} class="flex flex-col gap-3">
        <div class="grid grid-cols-2 gap-3">
          <FormField label="Product" error={errors.value.product} required>
            <select
              class={inputClass("product")}
              value={product.value}
              onChange={(e) => {
                product.value = (e.target as HTMLSelectElement).value;
                yieldUnit.value =
                  productTypes[Number((e.target as HTMLSelectElement).value)]
                    ?.unit ?? "lbs";
              }}
            >
              {productTypes.map((t) => (
                <option value={t.value}>{t.label}</option>
              ))}
            </select>
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
          <FormField label="Yield" error={errors.value.yieldValue} required>
            <div class="flex gap-2">
              <input
                type="number"
                step="0.1"
                class={inputClass("yieldValue")}
                placeholder="e.g. 30"
                value={yieldValue.value}
                onInput={(e) =>
                  yieldValue.value = (e.target as HTMLInputElement).value}
              />
              <span class="flex items-center text-sm text-stone-500 min-w-[3rem]">
                {yieldUnit.value}
              </span>
            </div>
          </FormField>

          {showMoisture && (
            <FormField
              label="Moisture %"
              error={errors.value.moisturePercent}
              helpText="Target: <18.6% for stable honey"
            >
              <input
                type="number"
                step="0.1"
                class={inputClass("moisturePercent")}
                placeholder="e.g. 17.5"
                value={moisturePercent.value}
                onInput={(e) =>
                  moisturePercent.value = (e.target as HTMLInputElement).value}
              />
            </FormField>
          )}
        </div>

        <FormField label="Method" error={errors.value.method}>
          <input
            type="text"
            class={inputClass("method")}
            placeholder="e.g. Crush & strain, extractor, cut comb"
            value={method.value}
            onInput={(e) => method.value = (e.target as HTMLInputElement).value}
          />
        </FormField>

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
            {isSubmitting.value ? "Saving..." : "Record Harvest"}
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
