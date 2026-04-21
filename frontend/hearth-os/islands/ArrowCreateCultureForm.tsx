import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import { ArrowInfoIcon, ArrowTooltip } from "../components/ArrowTooltip.ts";
import {
  clearErrors,
  CultureSchema,
  extractErrors,
  type FieldErrors,
  setErrors,
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

export default function ArrowCreateCultureForm() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      isOpen: false,
      name: "",
      type: "0",
      origin: "",
      isSubmitting: false,
      errors: {} as FieldErrors,
    });

    const closeModal = () => {
      state.isOpen = false;
      state.name = "";
      state.origin = "";
      clearErrors(state.errors);
    };

    const onSubmit = async (e: Event) => {
      e.preventDefault();

      const result = CultureSchema.safeParse({
        name: state.name,
        type: state.type,
        origin: state.origin,
      });

      if (!result.success) {
        setErrors(state.errors, extractErrors(result));
        showToast(
          "error",
          "Validation failed",
          "Please fix the highlighted fields.",
        );
        return;
      }

      state.isSubmitting = true;
      clearErrors(state.errors);

      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.createCulture(result.data);
        showToast(
          "success",
          "Culture created!",
          `"${state.name}" is now in your culture registry.`,
        );
        closeModal();
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed to create culture",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.isSubmitting = false;
      }
    };

    const inputClass = (field: string) => () =>
      `w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition ${
        (state.errors as any)[field]
          ? "border-red-400 bg-red-50"
          : "border-stone-300"
      }`;

    const template = html`
      <div>
        <button
          @click="${() => state.isOpen = true}"
          class="bg-emerald-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-emerald-700 transition shadow-sm flex items-center gap-2"
        >
          <span class="text-lg">+</span> Register Culture
        </button>

        ${() => {
          if (!state.isOpen) return "";

          return html`
            <div
              class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
              @click="${(e: Event) => {
                if (e.target === e.currentTarget) closeModal();
              }}"
            >
              <div
                class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]"
              >
                <!-- Header -->
                <div
                  class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50"
                >
                  <div class="flex items-center gap-2">
                    <h3 class="text-lg font-bold text-stone-800">Register New Culture</h3>
                    ${ArrowTooltip({
                      text:
                        "Cultures are living organisms you maintain (starters, SCOBYs, kefir grains). Register them here to track feeding schedules and lineage.",
                      children: ArrowInfoIcon(),
                    })}
                  </div>
                  <button
                    @click="${closeModal}"
                    class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
                  >
                    ✕
                  </button>
                </div>

                <div class="p-6">
                  <form @submit="${onSubmit}" class="flex flex-col gap-4">
                    ${ArrowFormField({
                      label: "Culture Name",
                      error: () => state.errors.name,
                      helpText:
                        "A memorable name like 'Gertrude' or 'SCOBY Prime'",
                      required: true,
                      children: html`
                        <input
                          type="text"
                          class="${inputClass("name")}"
                          placeholder="e.g. Gertrude"
                          value="${() => state.name}"
                          @input="${(e: Event) =>
                            state.name = (e.target as HTMLInputElement).value}"
                          disabled="${() => state.isSubmitting}"
                        />
                      `,
                    })} ${ArrowFormField({
                      label: "Culture Type",
                      error: () => state.errors.type,
                      required: true,
                      children: html`
                        <select
                          class="${inputClass("type")}"
                          value="${() => state.type}"
                          @change="${(e: Event) =>
                            state.type = (e.target as HTMLSelectElement).value}"
                          disabled="${() => state.isSubmitting}"
                        >
                          ${cultureTypes.map((t) =>
                            html`
                              <option value="${t.value}">${t.label}</option>
                            `
                          )}
                        </select>
                        ${() => {
                          const selectedType = cultureTypes.find((t) =>
                            t.value === Number(state.type)
                          );
                          return selectedType
                            ? html`
                              <p class="text-xs text-stone-400 mt-1">💡 ${selectedType
                                .desc}</p>
                            `
                            : "";
                        }}
                      `,
                    })} ${ArrowFormField({
                      label: "Origin",
                      error: () => state.errors.origin,
                      helpText: "Where did this culture come from?",
                      required: true,
                      children: html`
                        <textarea
                          class="${inputClass("origin")}"
                          rows="2"
                          placeholder="e.g. San Francisco heritage starter, gifted by neighbor in 2019"
                          value="${() => state.origin}"
                          @input="${(e: Event) =>
                            state.origin =
                              (e.target as HTMLTextAreaElement).value}"
                          disabled="${() => state.isSubmitting}"
                        ></textarea>
                      `,
                    })}

                    <div class="flex justify-end gap-3 mt-2">
                      <button
                        type="button"
                        @click="${closeModal}"
                        disabled="${() => state.isSubmitting}"
                        class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        disabled="${() => state.isSubmitting}"
                        class="bg-emerald-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-emerald-700 transition disabled:opacity-50 shadow-sm"
                      >
                        ${() =>
                          state.isSubmitting
                            ? "Creating..."
                            : "Register Culture"}
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
