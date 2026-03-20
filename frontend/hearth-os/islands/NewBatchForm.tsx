import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  extractErrors,
  type FieldErrors,
  KombuchaBatchSchema,
  SourdoughBatchSchema,
} from "../utils/schemas.ts";

export default function NewBatchForm() {
  const isOpen = useSignal(false);
  const batchType = useSignal<"sourdough" | "kombucha">("sourdough");
  const batchCode = useSignal("");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  // Sourdough fields
  const flourGrams = useSignal("1000");
  const waterGrams = useSignal("750");
  const saltGrams = useSignal("20");

  // Kombucha fields
  const teaType = useSignal("Black");
  const sugarGrams = useSignal("200");

  const closeModal = () => {
    isOpen.value = false;
    batchCode.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();
    errors.value = {};

    if (batchType.value === "sourdough") {
      const result = SourdoughBatchSchema.safeParse({
        batchCode: batchCode.value,
        flourGrams: flourGrams.value,
        waterGrams: waterGrams.value,
        saltGrams: saltGrams.value,
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
    } else {
      const result = KombuchaBatchSchema.safeParse({
        batchCode: batchCode.value,
        teaType: teaType.value,
        sugarGrams: sugarGrams.value,
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
    }

    isSubmitting.value = true;

    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");

      if (batchType.value === "sourdough") {
        await HearthAPI.startSourdough({
          batchCode: batchCode.value,
          starterId: "00000000-0000-0000-0000-000000000000",
          ingredients: [
            {
              name: "Bread Flour",
              amount: {
                value: Number(flourGrams.value),
                unit: "grams",
                type: "weight",
              },
            },
            {
              name: "Water",
              amount: {
                value: Number(waterGrams.value),
                unit: "grams",
                type: "weight",
              },
            },
            {
              name: "Salt",
              amount: {
                value: Number(saltGrams.value),
                unit: "grams",
                type: "weight",
              },
            },
          ],
        });
      } else {
        await HearthAPI.startKombucha({
          batchCode: batchCode.value,
          scobyCultureId: "00000000-0000-0000-0000-000000000000",
          teaType: teaType.value,
          sugarGrams: Number(sugarGrams.value),
        });
      }

      showToast(
        "success",
        "Batch started!",
        `${
          batchType.value === "sourdough" ? "Sourdough" : "Kombucha"
        } batch "${batchCode.value}" is now active.`,
      );
      closeModal();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to start batch",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  return (
    <>
      <button
        onClick={() => isOpen.value = true}
        class="bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition shadow-sm flex items-center gap-2"
      >
        <span class="text-lg">+</span> New Batch
      </button>

      {isOpen.value && (
        <div
          class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div class="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            {/* Header */}
            <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
              <h3 class="text-lg font-bold text-stone-800">Start New Batch</h3>
              <button
                onClick={closeModal}
                class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
              >
                ✕
              </button>
            </div>

            <div class="p-6">
              {/* Type Toggle */}
              <div class="flex gap-2 mb-6">
                {(["sourdough", "kombucha"] as const).map((type) => (
                  <button
                    type="button"
                    onClick={() => {
                      batchType.value = type;
                      errors.value = {};
                    }}
                    class={`px-4 py-2 rounded-lg text-sm font-semibold transition ${
                      batchType.value === type
                        ? "bg-amber-600 text-white shadow-sm"
                        : "bg-stone-100 text-stone-600 hover:bg-stone-200"
                    }`}
                  >
                    {type === "sourdough" ? "🍞 Sourdough" : "🫖 Kombucha"}
                  </button>
                ))}
              </div>

              <form onSubmit={onSubmit} class="flex flex-col gap-4">
                <FormField
                  label="Batch Code"
                  error={errors.value.batchCode}
                  helpText="Unique identifier like SD-2024-03-A"
                  required
                >
                  <input
                    type="text"
                    class={inputClass("batchCode")}
                    placeholder={batchType.value === "sourdough"
                      ? "e.g. SD-2024-03-A"
                      : "e.g. KB-2024-03-A"}
                    value={batchCode.value}
                    onInput={(e) =>
                      batchCode.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                {batchType.value === "sourdough"
                  ? (
                    <div>
                      <div class="flex items-center gap-1 mb-3">
                        <span class="text-sm font-medium text-stone-700">
                          Ingredients
                        </span>
                        <Tooltip text="Standard hydration is 65-80%. Flour:Water ratio determines crumb structure.">
                          <InfoIcon />
                        </Tooltip>
                      </div>
                      <div class="grid grid-cols-3 gap-3">
                        <FormField
                          label="Flour (g)"
                          error={errors.value.flourGrams}
                          helpText="Bread flour preferred"
                        >
                          <input
                            type="number"
                            class={inputClass("flourGrams")}
                            value={flourGrams.value}
                            onInput={(e) =>
                              flourGrams.value =
                                (e.target as HTMLInputElement).value}
                          />
                        </FormField>
                        <FormField
                          label="Water (g)"
                          error={errors.value.waterGrams}
                          helpText="Filtered, room temp"
                        >
                          <input
                            type="number"
                            class={inputClass("waterGrams")}
                            value={waterGrams.value}
                            onInput={(e) =>
                              waterGrams.value =
                                (e.target as HTMLInputElement).value}
                          />
                        </FormField>
                        <FormField
                          label="Salt (g)"
                          error={errors.value.saltGrams}
                          helpText="~2% of flour weight"
                        >
                          <input
                            type="number"
                            class={inputClass("saltGrams")}
                            value={saltGrams.value}
                            onInput={(e) =>
                              saltGrams.value =
                                (e.target as HTMLInputElement).value}
                          />
                        </FormField>
                      </div>
                    </div>
                  )
                  : (
                    <div class="grid grid-cols-2 gap-3">
                      <FormField
                        label="Tea Type"
                        error={errors.value.teaType}
                        helpText="Black tea produces stronger kombucha"
                      >
                        <select
                          class={inputClass("teaType")}
                          value={teaType.value}
                          onChange={(e) =>
                            teaType.value =
                              (e.target as HTMLSelectElement).value}
                        >
                          <option>Black</option>
                          <option>Green</option>
                          <option>Oolong</option>
                          <option>White</option>
                        </select>
                      </FormField>
                      <FormField
                        label="Sugar (g)"
                        error={errors.value.sugarGrams}
                        helpText="SCOBY ferments sugar into acids"
                      >
                        <input
                          type="number"
                          class={inputClass("sugarGrams")}
                          value={sugarGrams.value}
                          onInput={(e) =>
                            sugarGrams.value =
                              (e.target as HTMLInputElement).value}
                        />
                      </FormField>
                    </div>
                  )}

                <div class="flex justify-end gap-3 mt-2">
                  <button
                    type="button"
                    onClick={closeModal}
                    disabled={isSubmitting.value}
                    class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting.value || !batchCode.value}
                    class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm"
                  >
                    {isSubmitting.value
                      ? "Starting..."
                      : `Start ${
                        batchType.value === "sourdough"
                          ? "Sourdough"
                          : "Kombucha"
                      }`}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
