import { useSignal } from "@preact/signals";
import { MushroomAPI } from "../utils/farmos-client.ts";
import {
  extractErrors,
  FieldErrors,
  MushroomBatchSchema,
} from "../utils/schemas.ts";
import { showToast } from "../utils/toastState.ts";

export default function NewMushroomBatchForm() {
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const species = useSignal("");
  const batchCode = useSignal("");
  const substrate = useSignal("");

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    errors.value = {};

    // Validate
    const result = MushroomBatchSchema.safeParse({
      species: species.value,
      batchCode: batchCode.value,
      substrateType: substrate.value,
    });

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    // Submit
    isSubmitting.value = true;
    try {
      await MushroomAPI.startBatch({
        batchCode: batchCode.value,
        species: species.value,
        substrateType: substrate.value,
        inoculatedAt: new Date().toISOString(),
      });

      showToast("success", "Mushroom block inoculated successfully!");

      // Navigate to details or reset
      window.location.href = `/mushrooms/${batchCode.value}`;
    } catch (err: unknown) {
      showToast(
        "error",
        err instanceof Error ? err.message : "Failed to start batch",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const inputClass = (field: string) => `
    block w-full rounded-lg border-0 py-2.5 px-3 text-stone-900 shadow-sm ring-1 ring-inset focus:ring-2 focus:ring-inset sm:text-sm sm:leading-6
    ${
    errors.value[field]
      ? "ring-red-300 focus:ring-red-500"
      : "ring-stone-300 focus:ring-emerald-600"
  }
  `;

  return (
    <form onSubmit={handleSubmit} class="space-y-6 max-w-xl">
      <div class="bg-white px-6 py-8 rounded-xl border border-stone-200 shadow-sm space-y-6">
        <div>
          <h3 class="text-lg font-bold text-stone-900 mb-1">
            Mushroom Species
          </h3>
          <p class="text-sm text-stone-500 mb-4">
            What variety are you cultivating?
          </p>

          <input
            type="text"
            class={inputClass("species")}
            placeholder="e.g. Blue Oyster, Lion's Mane"
            value={species.value}
            onInput={(e) =>
              species.value = (e.target as HTMLInputElement).value}
          />
          {errors.value.species && (
            <p class="mt-2 text-sm text-red-600">{errors.value.species}</p>
          )}
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium leading-6 text-stone-900 mb-1">
              Batch Code
            </label>
            <input
              type="text"
              class={inputClass("batchCode")}
              placeholder="e.g. BO-24-01"
              value={batchCode.value}
              onInput={(e) =>
                batchCode.value = (e.target as HTMLInputElement).value
                  .toUpperCase()}
            />
            {errors.value.batchCode && (
              <p class="mt-2 text-sm text-red-600">{errors.value.batchCode}</p>
            )}
          </div>

          <div>
            <label class="block text-sm font-medium leading-6 text-stone-900 mb-1">
              Substrate Type
            </label>
            <input
              type="text"
              class={inputClass("substrateType")}
              placeholder="e.g. Hardwood Sawdust, Straw"
              value={substrate.value}
              onInput={(e) =>
                substrate.value = (e.target as HTMLInputElement).value}
            />
            {errors.value.substrateType && (
              <p class="mt-2 text-sm text-red-600">
                {errors.value.substrateType}
              </p>
            )}
          </div>
        </div>
      </div>

      <div class="flex items-center justify-end gap-x-6">
        <a
          href="/mushrooms"
          class="text-sm font-semibold leading-6 text-stone-900 hover:text-stone-600"
        >
          Cancel
        </a>
        <button
          type="submit"
          disabled={isSubmitting.value}
          class="rounded-md bg-emerald-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-600 disabled:opacity-50 transition"
        >
          {isSubmitting.value ? "Saving..." : "Inoculate Block"}
        </button>
      </div>
    </form>
  );
}
