import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  CultureSchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

const cultureTypes = [
  {
    value: 0,
    label: "Sourdough Starter",
    desc: "Wild yeast + lactobacillus for bread leavening",
  },
  {
    value: 1,
    label: "Kombucha SCOBY",
    desc: "Symbiotic colony of bacteria and yeast for tea fermentation",
  },
  {
    value: 2,
    label: "Milk Kefir Grains",
    desc: "Polysaccharide matrix of bacteria/yeast for milk fermentation",
  },
  {
    value: 3,
    label: "Other",
    desc: "Water kefir, tempeh, vinegar mother, etc.",
  },
];

export default function CreateCultureForm() {
  const isOpen = useSignal(false);
  const name = useSignal("");
  const type = useSignal("0");
  const origin = useSignal("");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const closeModal = () => {
    isOpen.value = false;
    name.value = "";
    origin.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = CultureSchema.safeParse({
      name: name.value,
      type: type.value,
      origin: origin.value,
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
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      await HearthAPI.createCulture(result.data);
      showToast(
        "success",
        "Culture created!",
        `"${name.value}" is now in your culture registry.`,
      );
      closeModal();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to create culture",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const selectedType = cultureTypes.find((t) => t.value === Number(type.value));

  return (
    <>
      <button
        onClick={() => isOpen.value = true}
        class="bg-emerald-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-emerald-700 transition shadow-sm flex items-center gap-2"
      >
        <span class="text-lg">+</span> Register Culture
      </button>

      {isOpen.value && (
        <div
          class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
          onClick={(e) => {
            if (e.target === e.currentTarget) closeModal();
          }}
        >
          <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
            {/* Header */}
            <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
              <div class="flex items-center gap-2">
                <h3 class="text-lg font-bold text-stone-800">
                  Register New Culture
                </h3>
                <Tooltip text="Cultures are living organisms you maintain (starters, SCOBYs, kefir grains). Register them here to track feeding schedules and lineage.">
                  <InfoIcon />
                </Tooltip>
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
                  label="Culture Name"
                  error={errors.value.name}
                  helpText="A memorable name like 'Gertrude' or 'SCOBY Prime'"
                  required
                >
                  <input
                    type="text"
                    class={inputClass("name")}
                    placeholder="e.g. Gertrude"
                    value={name.value}
                    onInput={(e) =>
                      name.value = (e.target as HTMLInputElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

                <FormField
                  label="Culture Type"
                  error={errors.value.type}
                  required
                >
                  <select
                    class={inputClass("type")}
                    value={type.value}
                    onChange={(e) =>
                      type.value = (e.target as HTMLSelectElement).value}
                    disabled={isSubmitting.value}
                  >
                    {cultureTypes.map((t) => (
                      <option value={t.value}>{t.label}</option>
                    ))}
                  </select>
                  {selectedType && (
                    <p class="text-xs text-stone-400 mt-1">
                      💡 {selectedType.desc}
                    </p>
                  )}
                </FormField>

                <FormField
                  label="Origin"
                  error={errors.value.origin}
                  helpText="Where did this culture come from?"
                  required
                >
                  <textarea
                    class={inputClass("origin")}
                    rows={2}
                    placeholder="e.g. San Francisco heritage starter, gifted by neighbor in 2019"
                    value={origin.value}
                    onInput={(e) =>
                      origin.value = (e.target as HTMLTextAreaElement).value}
                    disabled={isSubmitting.value}
                  />
                </FormField>

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
                    disabled={isSubmitting.value}
                    class="bg-emerald-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-emerald-700 transition disabled:opacity-50 shadow-sm"
                  >
                    {isSubmitting.value ? "Creating..." : "Register Culture"}
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
