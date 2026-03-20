import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import { type FieldErrors } from "../utils/schemas.ts";

interface MergeColoniesFormProps {
  hiveId: string;
  hiveName: string;
}

export default function MergeColoniesForm(
  { hiveId, hiveName }: MergeColoniesFormProps,
) {
  const isOpen = useSignal(false);
  const absorbedHiveId = useSignal("");
  const date = useSignal(new Date().toISOString().split("T")[0]);
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const resetForm = () => {
    isOpen.value = false;
    absorbedHiveId.value = "";
    errors.value = {};
  };

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    if (!absorbedHiveId.value.trim()) {
      errors.value = { absorbedHiveId: "Hive ID required" };
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.mergeColonies(hiveId, {
        absorbedHiveId: absorbedHiveId.value,
        date: date.value,
      });
      showToast(
        "success",
        "Colonies merged",
        `Weak colony absorbed into ${hiveName}.`,
      );
      resetForm();
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to merge",
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
        class="text-xs text-teal-600 hover:text-teal-800 font-semibold transition"
      >
        🔗 Merge Colony
      </button>
    );
  }

  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        Merge Into {hiveName}
      </h4>
      <p class="text-xs text-stone-500 mb-3">
        The weak colony will be absorbed and marked as dead.
      </p>
      <form onSubmit={onSubmit} class="flex flex-col gap-3">
        <FormField
          label="Weak Hive ID"
          error={errors.value.absorbedHiveId}
          helpText="The hive being absorbed into this one"
          required
        >
          <input
            type="text"
            class={inputClass("absorbedHiveId")}
            placeholder="Paste hive ID..."
            value={absorbedHiveId.value}
            onInput={(e) =>
              absorbedHiveId.value = (e.target as HTMLInputElement).value}
          />
        </FormField>

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
            class="flex-1 bg-teal-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-teal-700 transition disabled:opacity-50 shadow-sm text-sm"
          >
            {isSubmitting.value ? "Merging..." : "Merge Colonies"}
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
