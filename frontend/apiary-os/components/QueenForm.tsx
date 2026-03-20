import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import FormField from "../components/FormField.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import {
  extractErrors,
  type FieldErrors,
  QueenLostSchema,
  QueenSchema,
} from "../utils/schemas.ts";

const markedColors = [
  { value: 0, label: "White", css: "bg-white border-stone-300", years: "1, 6" },
  { value: 1, label: "Yellow", css: "bg-yellow-300 border-yellow-400", years: "2, 7" },
  { value: 2, label: "Red", css: "bg-red-400 border-red-500", years: "3, 8" },
  { value: 3, label: "Green", css: "bg-green-400 border-green-500", years: "4, 9" },
  { value: 4, label: "Blue", css: "bg-blue-400 border-blue-500", years: "5, 0" },
];

const queenOrigins = [
  { value: 0, label: "Purchased" },
  { value: 1, label: "Raised" },
  { value: 2, label: "Swarm" },
];

interface QueenFormProps {
  hiveId: string;
  hiveName: string;
  hasQueen: boolean;
}

export default function QueenForm(
  { hiveId, hiveName, hasQueen }: QueenFormProps,
) {
  const mode = useSignal<"closed" | "introduce" | "replace" | "lost">("closed");
  const color = useSignal("");
  const origin = useSignal("0");
  const introducedDate = useSignal(new Date().toISOString().split("T")[0]);
  const breed = useSignal("");
  const notes = useSignal("");
  const reason = useSignal("");
  const lostDate = useSignal(new Date().toISOString().split("T")[0]);
  const isSubmitting = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
      errors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  const resetForm = () => {
    mode.value = "closed";
    color.value = "";
    origin.value = "0";
    breed.value = "";
    notes.value = "";
    reason.value = "";
    errors.value = {};
  };

  const onSubmitQueen = async (e: Event) => {
    e.preventDefault();
    const result = QueenSchema.safeParse({
      color: color.value !== "" ? color.value : undefined,
      origin: origin.value,
      introducedDate: introducedDate.value,
      breed: breed.value,
      notes: notes.value,
    });

    if (!result.success) {
      errors.value = extractErrors(result);
      showToast("error", "Validation failed", "Please fix the highlighted fields.");
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      const queenData = {
        color: result.data.color,
        origin: result.data.origin,
        introducedDate: result.data.introducedDate,
        breed: result.data.breed || undefined,
        notes: result.data.notes || undefined,
      };

      if (mode.value === "introduce") {
        await ApiaryAPI.introduceQueen(hiveId, { queen: queenData });
        showToast("success", "Queen introduced", `Queen added to ${hiveName}.`);
      } else {
        await ApiaryAPI.replaceQueen(hiveId, {
          newQueen: queenData,
          reason: reason.value,
        });
        showToast("success", "Queen replaced", `New queen in ${hiveName}.`);
      }
      resetForm();
    } catch (err: unknown) {
      showToast("error", "Failed", err instanceof Error ? err.message : "Unknown error");
    } finally {
      isSubmitting.value = false;
    }
  };

  const onSubmitLost = async (e: Event) => {
    e.preventDefault();
    const result = QueenLostSchema.safeParse({
      reason: reason.value,
      date: lostDate.value,
    });

    if (!result.success) {
      errors.value = extractErrors(result);
      showToast("error", "Validation failed", "Please fix the highlighted fields.");
      return;
    }

    isSubmitting.value = true;
    errors.value = {};

    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.markQueenLost(hiveId, result.data);
      showToast("success", "Queen marked lost", `${hiveName} is now queenless.`);
      resetForm();
    } catch (err: unknown) {
      showToast("error", "Failed", err instanceof Error ? err.message : "Unknown error");
    } finally {
      isSubmitting.value = false;
    }
  };

  if (mode.value === "closed") {
    return (
      <div class="flex gap-2 flex-wrap">
        {!hasQueen
          ? (
            <button
              onClick={() => mode.value = "introduce"}
              class="text-xs text-amber-600 hover:text-amber-800 font-semibold transition"
            >
              👑 Introduce Queen
            </button>
          )
          : (
            <>
              <button
                onClick={() => mode.value = "replace"}
                class="text-xs text-amber-600 hover:text-amber-800 font-semibold transition"
              >
                🔄 Replace Queen
              </button>
              <button
                onClick={() => mode.value = "lost"}
                class="text-xs text-red-600 hover:text-red-800 font-semibold transition"
              >
                ⚠ Queen Lost
              </button>
            </>
          )}
      </div>
    );
  }

  if (mode.value === "lost") {
    return (
      <div class="mt-4 pt-4 border-t border-stone-100">
        <h4 class="text-sm font-bold text-stone-700 mb-3">
          Mark Queen Lost — {hiveName}
        </h4>
        <form onSubmit={onSubmitLost} class="flex flex-col gap-3">
          <FormField label="Reason" error={errors.value.reason} required>
            <input
              type="text"
              class={inputClass("reason")}
              placeholder="e.g. Swarmed, supersedure, dead"
              value={reason.value}
              onInput={(e) => reason.value = (e.target as HTMLInputElement).value}
            />
          </FormField>

          <FormField label="Date" error={errors.value.date} required>
            <input
              type="date"
              class={inputClass("date")}
              value={lostDate.value}
              onInput={(e) => lostDate.value = (e.target as HTMLInputElement).value}
            />
          </FormField>

          <div class="flex gap-2 mt-1">
            <button
              type="submit"
              disabled={isSubmitting.value}
              class="flex-1 bg-red-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-red-700 transition disabled:opacity-50 shadow-sm text-sm"
            >
              {isSubmitting.value ? "Saving..." : "Confirm Queen Lost"}
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

  // Introduce or Replace form
  return (
    <div class="mt-4 pt-4 border-t border-stone-100">
      <h4 class="text-sm font-bold text-stone-700 mb-3">
        {mode.value === "introduce" ? "Introduce" : "Replace"} Queen — {hiveName}
      </h4>
      <form onSubmit={onSubmitQueen} class="flex flex-col gap-3">
        <div class="flex flex-col gap-1">
          <div class="flex items-center gap-1">
            <label class="text-sm font-medium text-stone-700">
              Marked Color
            </label>
            <Tooltip text="International queen marking: White (1/6), Yellow (2/7), Red (3/8), Green (4/9), Blue (5/0)">
              <InfoIcon />
            </Tooltip>
          </div>
          <div class="flex gap-2 mt-1">
            {markedColors.map((c) => (
              <button
                type="button"
                key={c.value}
                onClick={() =>
                  color.value = color.value === String(c.value)
                    ? ""
                    : String(c.value)}
                class={`w-8 h-8 rounded-full border-2 transition ${c.css} ${
                  color.value === String(c.value)
                    ? "ring-2 ring-amber-500 ring-offset-1"
                    : ""
                }`}
                title={`${c.label} (years ending ${c.years})`}
              />
            ))}
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <FormField label="Origin" error={errors.value.origin} required>
            <select
              class={inputClass("origin")}
              value={origin.value}
              onChange={(e) =>
                origin.value = (e.target as HTMLSelectElement).value}
            >
              {queenOrigins.map((o) => (
                <option value={o.value}>{o.label}</option>
              ))}
            </select>
          </FormField>

          <FormField
            label="Introduced Date"
            error={errors.value.introducedDate}
            required
          >
            <input
              type="date"
              class={inputClass("introducedDate")}
              value={introducedDate.value}
              onInput={(e) =>
                introducedDate.value = (e.target as HTMLInputElement).value}
            />
          </FormField>
        </div>

        <FormField label="Breed / Line" error={errors.value.breed}>
          <input
            type="text"
            class={inputClass("breed")}
            placeholder="e.g. Italian, Carniolan, VSH"
            value={breed.value}
            onInput={(e) => breed.value = (e.target as HTMLInputElement).value}
          />
        </FormField>

        {mode.value === "replace" && (
          <FormField label="Replacement Reason" error={errors.value.reason}>
            <input
              type="text"
              class={inputClass("reason")}
              placeholder="e.g. Poor laying pattern, aggressive"
              value={reason.value}
              onInput={(e) =>
                reason.value = (e.target as HTMLInputElement).value}
            />
          </FormField>
        )}

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
            {isSubmitting.value
              ? "Saving..."
              : mode.value === "introduce"
              ? "Introduce Queen"
              : "Replace Queen"}
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
