import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { ArrowTooltip, ArrowInfoIcon } from "../components/ArrowTooltip.ts";
import { extractErrors, type FieldErrors, PHRecordSchema } from "../utils/schemas.ts";

export interface ArrowRecordPHFormProps {
  batchId: string;
  batchType: "sourdough" | "kombucha";
}

export default function ArrowRecordPHForm({ batchId, batchType }: ArrowRecordPHFormProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = '';

    const state = reactive({
      isOpen: false,
      ph: "",
      temp: "",
      isSubmitting: false,
      errors: {} as FieldErrors
    });

    const onSubmit = async (e: Event) => {
      e.preventDefault();

      const result = PHRecordSchema.safeParse({
        pH: state.ph,
        temperature: state.temp || undefined,
      });

      if (!result.success) {
        state.errors = extractErrors(result);
        return;
      }

      state.isSubmitting = true;
      state.errors = {};

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

        showToast("success", "pH recorded", `pH ${result.data.pH} saved for batch.`);
        state.ph = "";
        state.temp = "";
        setTimeout(() => {
          state.isOpen = false;
        }, 1000);
      } catch (err: unknown) {
        showToast("error", "Failed to record pH", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.isSubmitting = false;
      }
    };

    const template = html`
      ${() => {
        if (!state.isOpen) {
          return html`
            <button
              @click="${() => state.isOpen = true}"
              class="text-xs text-amber-600 hover:text-amber-800 font-semibold transition"
            >
              + Record pH
            </button>
          `;
        }

        return html`
          <form @submit="${onSubmit}" class="flex items-center gap-2">
            <div class="flex items-center gap-1">
              <input
                type="number"
                step="0.1"
                min="0"
                max="14"
                placeholder="pH"
                class="w-16 px-2 py-1 border rounded text-xs focus:outline-none focus:ring-1 focus:ring-amber-500 ${() => state.errors.pH ? 'border-red-400' : 'border-stone-300'}"
                value="${() => state.ph}"
                @input="${(e: Event) => state.ph = (e.target as HTMLInputElement).value}"
              />
              ${ArrowTooltip({
                text: batchType === "kombucha" ? "Kombucha safe range: 2.5–3.5 pH. Below 2.5 is too acidic." : "Sourdough target: 3.5–4.5 pH during bulk ferment.",
                children: ArrowInfoIcon()
              })}
            </div>
            <input
              type="number"
              placeholder="°F"
              class="w-14 px-2 py-1 border rounded text-xs focus:outline-none focus:ring-1 focus:ring-amber-500 ${() => state.errors.temperature ? 'border-red-400' : 'border-stone-300'}"
              value="${() => state.temp}"
              @input="${(e: Event) => state.temp = (e.target as HTMLInputElement).value}"
            />
            <button
              type="submit"
              disabled="${() => state.isSubmitting}"
              class="px-2 py-1 bg-amber-600 text-white text-xs rounded font-semibold hover:bg-amber-700 disabled:opacity-50"
            >
              ${() => state.isSubmitting ? "..." : "Save"}
            </button>
            <button
              type="button"
              @click="${() => { state.isOpen = false; state.errors = {}; }}"
              class="text-stone-400 hover:text-stone-600 text-xs"
            >
              ✕
            </button>
          </form>
        `;
      }}
    `;

    template(containerRef.current);
  }, [batchId, batchType]);

  return <div ref={containerRef}></div>;
}
