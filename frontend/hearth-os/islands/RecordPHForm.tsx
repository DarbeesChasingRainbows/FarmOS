import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  extractErrors,
  type FieldErrors,
  PHRecordSchema,
} from "../utils/schemas.ts";

interface RecordPHFormProps {
  batchId: string;
  batchType: "sourdough" | "kombucha";
}

export default function RecordPHForm(
  { batchId, batchType }: RecordPHFormProps,
) {
  const isOpen = useSignal(false);
  const ph = useSignal("");
  const temp = useSignal("");
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const onSubmit = async (e: Event) => {
    e.preventDefault();

    const result = PHRecordSchema.safeParse({
      pH: ph.value,
      temperature: temp.value || undefined,
    });

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");

      if (batchType === "kombucha") {
        await HearthAPI.recordKombuchaPH(batchId, {
          pH: result.data.pH,
          temperature: result.data.temperature || 72,
        });
      } else {
        await HearthAPI.recordSourdoughCCP(batchId, {
          step: "BulkFerment",
          pH: result.data.pH,
          temperature: result.data.temperature || 75,
          measuredAt: new Date().toISOString(),
        });
      }

      showToast(
        "success",
        "pH recorded",
        `pH ${result.data.pH} saved for batch.`,
      );
      ph.value = "";
      temp.value = "";
      setTimeout(() => {
        isOpen.value = false;
      }, 1000);
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record pH",
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
        + Record pH
      </button>
    );
  }

  return (
    <form onSubmit={onSubmit} class="flex items-center gap-2">
      <div class="flex items-center gap-1">
        <input
          type="number"
          step="0.1"
          min="0"
          max="14"
          placeholder="pH"
          class={`w-16 px-2 py-1 border rounded text-xs focus:outline-none focus:ring-1 focus:ring-amber-500 ${
            errors.value.pH ? "border-red-400" : "border-stone-300"
          }`}
          value={ph.value}
          onInput={(e) => ph.value = (e.target as HTMLInputElement).value}
        />
        <Tooltip
          text={batchType === "kombucha"
            ? "Kombucha safe range: 2.5–3.5 pH. Below 2.5 is too acidic."
            : "Sourdough target: 3.5–4.5 pH during bulk ferment."}
        >
          <InfoIcon />
        </Tooltip>
      </div>
      <input
        type="number"
        placeholder="°F"
        class={`w-14 px-2 py-1 border rounded text-xs focus:outline-none focus:ring-1 focus:ring-amber-500 ${
          errors.value.temperature ? "border-red-400" : "border-stone-300"
        }`}
        value={temp.value}
        onInput={(e) => temp.value = (e.target as HTMLInputElement).value}
      />
      <button
        type="submit"
        disabled={isSubmitting.value}
        class="px-2 py-1 bg-amber-600 text-white text-xs rounded font-semibold hover:bg-amber-700 disabled:opacity-50"
      >
        {isSubmitting.value ? "..." : "Save"}
      </button>
      <button
        type="button"
        onClick={() => {
          isOpen.value = false;
          errors.value = {};
        }}
        class="text-stone-400 hover:text-stone-600 text-xs"
      >
        ✕
      </button>
    </form>
  );
}
