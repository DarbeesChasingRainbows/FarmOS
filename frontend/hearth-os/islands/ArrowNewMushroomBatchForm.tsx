import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { MushroomAPI } from "../utils/farmos-client.ts";
import { extractErrors, type FieldErrors, MushroomBatchSchema } from "../utils/schemas.ts";
import { showToast } from "../utils/toastState.ts";

export default function ArrowNewMushroomBatchForm() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = '';

    const state = reactive({
      isSubmitting: false,
      errors: {} as FieldErrors,
      species: "",
      batchCode: "",
      substrate: ""
    });

    const handleSubmit = async (e: Event) => {
      e.preventDefault();
      state.errors = {};

      const result = MushroomBatchSchema.safeParse({
        species: state.species,
        batchCode: state.batchCode,
        substrateType: state.substrate,
      });

      if (!result.success) {
        state.errors = extractErrors(result);
        return;
      }

      state.isSubmitting = true;
      try {
        await MushroomAPI.startBatch({
          batchCode: state.batchCode,
          species: state.species,
          substrateType: state.substrate,
          inoculatedAt: new Date().toISOString(),
        });
        showToast("success", "Mushroom block inoculated successfully!");
        globalThis.location.href = `/mushrooms/${state.batchCode}`;
      } catch (err: unknown) {
        showToast("error", err instanceof Error ? err.message : "Failed to start batch");
      } finally {
        state.isSubmitting = false;
      }
    };

    const inputClass = (field: string) => () => `
      block w-full rounded-lg border-0 py-2.5 px-3 text-stone-900 shadow-sm ring-1 ring-inset focus:ring-2 focus:ring-inset sm:text-sm sm:leading-6 
      ${(state.errors as any)[field] ? "ring-red-300 focus:ring-red-500" : "ring-stone-300 focus:ring-emerald-600"}
    `;

    const template = html`
      <form @submit="${handleSubmit}" class="space-y-6 max-w-xl">
        <div class="bg-white px-6 py-8 rounded-xl border border-stone-200 shadow-sm space-y-6">
          <div>
            <h3 class="text-lg font-bold text-stone-900 mb-1">Mushroom Species</h3>
            <p class="text-sm text-stone-500 mb-4">What variety are you cultivating?</p>
            <input
              type="text"
              class="${inputClass("species")}"
              placeholder="e.g. Blue Oyster, Lion's Mane"
              value="${() => state.species}"
              @input="${(e: Event) => state.species = (e.target as HTMLInputElement).value}"
            />
            ${() => state.errors.species ? html`<p class="mt-2 text-sm text-red-600">${state.errors.species}</p>` : ''}
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium leading-6 text-stone-900 mb-1">Batch Code</label>
              <input
                type="text"
                class="${inputClass("batchCode")}"
                placeholder="e.g. BO-24-01"
                value="${() => state.batchCode}"
                @input="${(e: Event) => state.batchCode = (e.target as HTMLInputElement).value.toUpperCase()}"
              />
              ${() => state.errors.batchCode ? html`<p class="mt-2 text-sm text-red-600">${state.errors.batchCode}</p>` : ''}
            </div>

            <div>
              <label class="block text-sm font-medium leading-6 text-stone-900 mb-1">Substrate Type</label>
              <input
                type="text"
                class="${inputClass("substrateType")}"
                placeholder="e.g. Hardwood Sawdust, Straw"
                value="${() => state.substrate}"
                @input="${(e: Event) => state.substrate = (e.target as HTMLInputElement).value}"
              />
              ${() => state.errors.substrateType ? html`<p class="mt-2 text-sm text-red-600">${state.errors.substrateType}</p>` : ''}
            </div>
          </div>
        </div>

        <div class="flex items-center justify-end gap-x-6">
          <a href="/mushrooms" class="text-sm font-semibold leading-6 text-stone-900 hover:text-stone-600">Cancel</a>
          <button
            type="submit"
            disabled="${() => state.isSubmitting}"
            class="rounded-md bg-emerald-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-emerald-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-600 disabled:opacity-50 transition"
          >
            ${() => state.isSubmitting ? "Saving..." : "Inoculate Block"}
          </button>
        </div>
      </form>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
