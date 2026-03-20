import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  extractErrors,
  type FieldErrors,
  HiveSchema,
} from "../utils/schemas.ts";

const hiveTypes = [
  {
    value: 0,
    label: "Langstroth",
    desc:
      "Most common type. Stacked boxes with removable frames. Best for honey production.",
  },
  {
    value: 1,
    label: "Top Bar",
    desc:
      "Horizontal hive with bars instead of frames. Low-cost, bee-friendly.",
  },
  {
    value: 2,
    label: "Warré",
    desc: "Vertical top-bar hive. Minimal intervention, mimics natural cavity.",
  },
];

export default function CreateHiveForm() {
  const isOpen = useSignal(false);
  const name = useSignal("");
  const location = useSignal("");
  const hiveType = useSignal("0");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const closeModal = () => {
    isOpen.value = false;
    name.value = "";
    location.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = HiveSchema.safeParse({
      name: name.value,
      location: location.value,
      hiveType: hiveType.value,
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
      await ApiaryAPI.createHive(result.data);
      showToast(
        "success",
        "Hive registered!",
        `"${name.value}" at ${location.value} is now tracked.`,
      );
      closeModal();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to create hive",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const selectedType = hiveTypes.find((t) =>
    t.value === Number(hiveType.value)
  );

  return (
    <>
      <button
        onClick={() => isOpen.value = true}
        class="bg-emerald-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-emerald-700 transition shadow-sm mb-6 flex items-center gap-2"
      >
        <span class="text-xl">+</span> Add New Hive
      </button>

      {isOpen.value && (
        <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]">
          <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
              <div class="flex items-center gap-2">
                <h3 class="text-lg font-bold text-stone-800">
                  Register New Hive
                </h3>
              </div>
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
                  label="Hive Name"
                  error={errors.value.name}
                  helpText="NATO alphabet or custom: Alpha, Bravo, etc."
                  required
                >
                  <input
                    type="text"
                    class={inputClass("name")}
                    placeholder="e.g. Hive Alpha"
                    value={name.value}
                    onInput={(e) =>
                      name.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <FormField
                  label="Location"
                  error={errors.value.location}
                  helpText="Where on the property is this hive?"
                  required
                >
                  <input
                    type="text"
                    class={inputClass("location")}
                    placeholder="e.g. South Orchard"
                    value={location.value}
                    onInput={(e) =>
                      location.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <FormField
                  label="Hive Type"
                  error={errors.value.hiveType}
                  required
                >
                  <select
                    class={inputClass("hiveType")}
                    value={hiveType.value}
                    onChange={(e) =>
                      hiveType.value = (e.target as HTMLSelectElement).value}
                    disabled={isSubmitting.value}
                  >
                    {hiveTypes.map((t) => (
                      <option value={t.value}>{t.label}</option>
                    ))}
                  </select>
                  {selectedType && (
                    <p class="text-xs text-stone-400 mt-1">
                      💡 {selectedType.desc}
                    </p>
                  )}
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
                    {isSubmitting.value ? "Registering..." : "Register Hive"}
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
