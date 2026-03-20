import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import { type FieldErrors } from "../utils/schemas.ts";

const hiveTypes = [
  { value: 0, label: "Langstroth" },
  { value: 1, label: "Top Bar" },
  { value: 2, label: "Warré" },
];

interface SplitColonyFormProps {
  hiveId: string;
  hiveName: string;
}

export default function SplitColonyForm(
  { hiveId, hiveName }: SplitColonyFormProps,
) {
  const isOpen = useSignal(false);
  const newName = useSignal("");
  const newType = useSignal("0");
  const latitude = useSignal("");
  const longitude = useSignal("");
  const date = useSignal(new Date().toISOString().split("T")[0]);
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const resetForm = () => {
    isOpen.value = false;
    newName.value = "";
    latitude.value = "";
    longitude.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    if (!newName.value.trim()) {
      errors.value = { newName: "Name required" };
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.splitColony(hiveId, {
        newHiveName: newName.value,
        newHiveType: Number(newType.value),
        newPosition: {
          latitude: Number(latitude.value) || 0,
          longitude: Number(longitude.value) || 0,
        },
        date: date.value,
      });
      showToast(
        "success",
        "Colony split",
        `New hive "${newName.value}" created from ${hiveName}.`,
      );
      resetForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to split colony",
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
        ✂️ Split Colony
      </button>
    );
  }

  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        Split Colony — {hiveName}
      </h4>
      <form onSubmit={onSubmit} class="flex flex-col gap-3">
        <div class="grid grid-cols-2 gap-3">
          <FormField label="New Hive Name" error={errors.value.newName} required>
            <input
              type="text"
              class={inputClass("newName")}
              placeholder="e.g. Hive Delta"
              value={newName.value}
              onInput={(e) =>
                newName.value = (e.target as HTMLInputElement).value}
            />
          </FormField>

          <FormField label="Hive Type" error={errors.value.newType} required>
            <select
              class={inputClass("newType")}
              value={newType.value}
              onChange={(e) =>
                newType.value = (e.target as HTMLSelectElement).value}
            >
              {hiveTypes.map((t) => (
                <option value={t.value}>{t.label}</option>
              ))}
            </select>
          </FormField>
        </div>

        <FormField label="Date" error={errors.value.date} required>
          <input
            type="date"
            class={inputClass("date")}
            value={date.value}
            onInput={(e) => date.value = (e.target as HTMLInputElement).value}
          />
        </FormField>

        <div class="flex gap-2 mt-1">
          <button
            type="submit"
            disabled={isSubmitting.value}
            class="flex-1 bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm text-sm"
          >
            {isSubmitting.value ? "Splitting..." : "Split Colony"}
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
