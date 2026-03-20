import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import {
  ApiarySchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

export default function CreateApiaryForm() {
  const isOpen = useSignal(false);
  const name = useSignal("");
  const latitude = useSignal("");
  const longitude = useSignal("");
  const maxCapacity = useSignal("20");
  const notes = useSignal("");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const closeModal = () => {
    isOpen.value = false;
    name.value = "";
    latitude.value = "";
    longitude.value = "";
    notes.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = ApiarySchema.safeParse({
      name: name.value,
      latitude: latitude.value,
      longitude: longitude.value,
      maxCapacity: maxCapacity.value,
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
      const { ApiaryLocationAPI } = await import("../utils/farmos-client.ts");
      await ApiaryLocationAPI.createApiary({
        name: result.data.name,
        position: {
          latitude: result.data.latitude,
          longitude: result.data.longitude,
        },
        maxCapacity: result.data.maxCapacity,
        notes: result.data.notes || undefined,
      });
      showToast(
        "success",
        "Apiary created!",
        `"${name.value}" is now tracked as a location.`,
      );
      closeModal();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to create apiary",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  return (
    <>
      <button
        onClick={() => isOpen.value = true}
        class="bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition shadow-sm mb-6 flex items-center gap-2"
      >
        <span class="text-xl">+</span> Add Apiary Location
      </button>

      {isOpen.value && (
        <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]">
          <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
              <h3 class="text-lg font-bold text-stone-800">
                Create Apiary Location
              </h3>
              <button
                onClick={closeModal}
                class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
              >
                ✕
              </button>
            </div>

            <div class="p-6">
              <form onSubmit={onSubmit} class="flex flex-col gap-4">
                <FormField
                  label="Apiary Name"
                  error={errors.value.name}
                  helpText="e.g. Home Yard, Orchard Site, South Field"
                  required
                >
                  <input
                    type="text"
                    class={inputClass("name")}
                    placeholder="e.g. Home Yard"
                    value={name.value}
                    onInput={(e) =>
                      name.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <div class="grid grid-cols-2 gap-3">
                  <FormField
                    label="Latitude"
                    error={errors.value.latitude}
                    required
                  >
                    <input
                      type="number"
                      step="any"
                      class={inputClass("latitude")}
                      placeholder="e.g. 38.897"
                      value={latitude.value}
                      onInput={(e) =>
                        latitude.value = (e.target as HTMLInputElement).value}
                      disabled={isSubmitting.value}
                    />
                  </FormField>

                  <FormField
                    label="Longitude"
                    error={errors.value.longitude}
                    required
                  >
                    <input
                      type="number"
                      step="any"
                      class={inputClass("longitude")}
                      placeholder="e.g. -77.037"
                      value={longitude.value}
                      onInput={(e) =>
                        longitude.value = (e.target as HTMLInputElement).value}
                      disabled={isSubmitting.value}
                    />
                  </FormField>
                </div>

                <FormField
                  label="Max Hive Capacity"
                  error={errors.value.maxCapacity}
                  helpText="Maximum number of hives this location can hold"
                  required
                >
                  <input
                    type="number"
                    class={inputClass("maxCapacity")}
                    value={maxCapacity.value}
                    onInput={(e) =>
                      maxCapacity.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <FormField
                  label="Notes"
                  error={errors.value.notes}
                  helpText="Access notes, sun exposure, nearby forage, etc."
                >
                  <textarea
                    class={inputClass("notes")}
                    rows={2}
                    placeholder="Optional notes..."
                    value={notes.value}
                    onInput={(e) =>
                      notes.value = (e.target as HTMLTextAreaElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <div class="flex justify-end gap-3 mt-4">
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
                    disabled={isSubmitting.value}
                    class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm"
                  >
                    {isSubmitting.value ? "Creating..." : "Create Apiary"}
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
