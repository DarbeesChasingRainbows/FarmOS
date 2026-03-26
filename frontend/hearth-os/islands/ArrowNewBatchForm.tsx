import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import { extractErrors, GenericBatchSchema } from "../utils/schemas.ts";

// ── All supported batch types with icons, default labels, and code prefixes ─
const batchTypes = [
  { id: "sourdough",   icon: "🍞", label: "Sourdough",   prefix: "SD" },
  { id: "kombucha",    icon: "🫖", label: "Kombucha",    prefix: "KB" },
  { id: "kimchi",      icon: "🥬", label: "Kimchi",      prefix: "KM" },
  { id: "sauerkraut",  icon: "🥒", label: "Sauerkraut",  prefix: "SK" },
  { id: "jun",         icon: "🍵", label: "Jun",         prefix: "JN" },
  { id: "miso",        icon: "🫘", label: "Miso",        prefix: "MS" },
  { id: "hot-sauce",   icon: "🌶️", label: "Hot Sauce",   prefix: "HS" },
  { id: "pickles",     icon: "🥒", label: "Pickles",     prefix: "PK" },
  { id: "yogurt",      icon: "🥛", label: "Yogurt",      prefix: "YG" },
  { id: "tempeh",      icon: "🫘", label: "Tempeh",      prefix: "TP" },
  { id: "vinegar",     icon: "🫙", label: "Vinegar",     prefix: "VN" },
  { id: "other",       icon: "🧪", label: "Other",       prefix: "OT" },
];

export default function ArrowNewBatchForm() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = '';

    const now = new Date();
    const dateStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;

    const state = reactive({
      isOpen: false,
      batchType: "sourdough",
      batchCode: "",
      notes: "",
      isSubmitting: false,
      errors: {} as Record<string, string>,
    });

    // Auto-generate batch code when type changes
    const autoCode = () => {
      const bt = batchTypes.find(t => t.id === state.batchType);
      return `${bt?.prefix ?? "XX"}-${dateStr}`;
    };

    const closeModal = () => {
      state.isOpen = false;
      state.batchCode = "";
      state.notes = "";
      (state as Record<string, unknown>).errors = {};
    };

    const onSubmit = async (e: Event) => {
      e.preventDefault();
      (state as Record<string, unknown>).errors = {};

      const code = state.batchCode || autoCode();
      const result = GenericBatchSchema.safeParse({
        batchCode: code,
        productType: state.batchType,
        notes: state.notes || undefined,
      });

      if (!result.success) {
        (state as Record<string, unknown>).errors = extractErrors(result);
        showToast("error", "Validation failed", "Please fix the highlighted fields.");
        return;
      }

      state.isSubmitting = true;

      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        const bt = batchTypes.find(t => t.id === state.batchType);

        // Route to existing endpoints for sourdough/kombucha, generic for others
        if (state.batchType === "sourdough") {
          await HearthAPI.startSourdough({
            batchCode: code,
            starterId: "00000000-0000-0000-0000-000000000000",
            ingredients: [
              { name: "Bread Flour", amount: { value: 1000, unit: "grams", type: "weight" } },
              { name: "Water", amount: { value: 750, unit: "grams", type: "weight" } },
              { name: "Salt", amount: { value: 20, unit: "grams", type: "weight" } },
            ],
          });
        } else if (state.batchType === "kombucha") {
          await HearthAPI.startKombucha({
            batchCode: code,
            scobyCultureId: "00000000-0000-0000-0000-000000000000",
            teaType: "Black",
            sugarGrams: 200,
          });
        } else {
          // Generic batch — uses sourdough endpoint as the universal batch creator
          // The backend treats productType as the differentiator
          await HearthAPI.startSourdough({
            batchCode: code,
            starterId: "00000000-0000-0000-0000-000000000000",
            ingredients: [
              { name: bt?.label ?? state.batchType, amount: { value: 1, unit: "batch", type: "count" } },
            ],
          });
        }

        showToast("success", "Batch started!", `${bt?.label ?? state.batchType} batch "${code}" is now active.`);
        closeModal();
      } catch (err: unknown) {
        showToast("error", "Failed to start batch", err instanceof Error ? err.message : "Unknown error");
      } finally {
        state.isSubmitting = false;
      }
    };

    const inputClass = (field: string) => () => 
      `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition text-sm ${
        (state.errors as Record<string, string>)[field] ? "border-red-400 bg-red-50" : "border-stone-300"
      }`;

    const template = html`
      <div>
        <button
          @click="${() => state.isOpen = true}"
          class="bg-amber-600 text-white font-semibold py-2.5 px-5 rounded-lg hover:bg-amber-700 transition shadow-sm flex items-center gap-2"
        >
          <span class="text-lg">+</span> New Batch
        </button>

        ${() => {
          if (!state.isOpen) return '';
          return html`
            <div
              class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
              @click="${(e: Event) => { if (e.target === e.currentTarget) closeModal(); }}"
            >
              <div class="bg-white rounded-xl shadow-xl w-full max-w-xl mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]">
                <!-- Header -->
                <div class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50">
                  <h3 class="text-lg font-bold text-stone-800">Start New Batch</h3>
                  <button @click="${closeModal}" class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded-full p-1.5 transition">✕</button>
                </div>

                <div class="p-6">
                  <!-- Batch Type Grid (Hick's Law: scannable grid, not long dropdown) -->
                  <p class="text-sm font-semibold text-stone-700 mb-3">What are you fermenting?</p>
                  <div class="grid grid-cols-4 gap-2 mb-6">
                    ${batchTypes.map(bt => html`
                      <button
                        type="button"
                        @click="${() => { (state as Record<string, unknown>).batchType = bt.id; (state as Record<string, unknown>).errors = {}; state.batchCode = ''; }}"
                        class="${() => `flex flex-col items-center gap-1 py-3 px-2 rounded-xl border text-sm font-medium transition ${
                          state.batchType === bt.id
                            ? "bg-amber-50 border-amber-300 text-amber-800 shadow-sm"
                            : "bg-white border-stone-200 text-stone-600 hover:bg-stone-50 hover:border-stone-300"
                        }`}"
                      >
                        <span class="text-xl">${bt.icon}</span>
                        <span class="text-xs leading-tight">${bt.label}</span>
                      </button>
                    `)}
                  </div>

                  <form @submit="${onSubmit}" class="flex flex-col gap-4">
                    ${ArrowFormField({
                      label: "Batch Code",
                      error: () => state.errors.batchCode,
                      helpText: "Leave blank to auto-generate",
                      children: html`
                        <input
                          type="text"
                          class="${inputClass("batchCode")}"
                          placeholder="${() => autoCode()}"
                          value="${() => state.batchCode}"
                          @input="${(e: Event | undefined) => { if (e?.target) state.batchCode = (e.target as HTMLInputElement).value; }}"
                          disabled="${() => state.isSubmitting}"
                        />
                      `
                    })}

                    ${ArrowFormField({
                      label: "Notes",
                      error: () => state.errors.notes,
                      helpText: "Optional — describe ingredients, source, etc.",
                      children: html`
                        <textarea
                          class="${() => `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition text-sm resize-none ${(state.errors as Record<string, string>).notes ? "border-red-400 bg-red-50" : "border-stone-300"}`}"
                          rows="2"
                          placeholder="e.g. Napa cabbage, gochugaru, garlic, ginger"
                          @input="${(e: Event | undefined) => { if (e?.target) state.notes = (e.target as HTMLTextAreaElement).value; }}"
                          disabled="${() => state.isSubmitting}"
                        ></textarea>
                      `
                    })}

                    <div class="flex justify-end gap-3 mt-2">
                      <button
                        type="button"
                        @click="${closeModal}"
                        disabled="${() => state.isSubmitting}"
                        class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
                      >Cancel</button>
                      <button
                        type="submit"
                        disabled="${() => state.isSubmitting}"
                        class="bg-amber-600 text-white font-semibold py-2.5 px-6 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm"
                      >
                        ${() => {
                          const bt = batchTypes.find(t => t.id === state.batchType);
                          return state.isSubmitting ? "Starting..." : `Start ${bt?.label ?? "Batch"}`;
                        }}
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            </div>
          `;
        }}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
