import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { IoTAPI } from "../utils/farmos-client.ts";

export default function ArrowZoneCreationModal() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = '';

    const state = reactive({
      isOpen: false,
      loading: false,
      error: null as string | null,
      name: "",
      description: "",
      zoneType: 0
    });

    const handleSubmit = async (e: Event) => {
      e.preventDefault();
      state.loading = true;
      state.error = null;
      try {
        await IoTAPI.createZone({
          name: state.name,
          zoneType: Number(state.zoneType),
          description: state.description
        });
        state.isOpen = false;
        state.name = "";
        state.description = "";
        state.zoneType = 0;
        if (typeof globalThis !== "undefined" && globalThis.location) {
          globalThis.location.reload();
        }
      } catch (err: any) {
        state.error = err.message || "Failed to create zone";
      } finally {
        state.loading = false;
      }
    };

    const template = html`
      <div>
        <button
          @click="${() => state.isOpen = true}"
          class="bg-emerald-600 hover:bg-emerald-700 text-white px-5 py-2.5 rounded-lg font-semibold shadow-sm transition"
        >
          + Create Zone
        </button>

        ${() => {
          if (!state.isOpen) return '';
          return html`
            <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
              <div class="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
                <div class="px-6 py-4 border-b border-stone-100 flex justify-between items-center bg-stone-50">
                  <h3 class="text-lg font-bold text-stone-800">Create IoT Zone</h3>
                  <button @click="${() => state.isOpen = false}" class="text-stone-400 hover:text-stone-600">✕</button>
                </div>
                
                <form @submit="${handleSubmit}" class="p-6">
                  ${() => state.error ? html`<div class="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm border border-red-100">${state.error}</div>` : ''}

                  <div class="space-y-4">
                    <div>
                      <label class="block text-sm font-medium text-stone-700 mb-1">Zone Name</label>
                      <input
                        required
                        type="text"
                        value="${() => state.name}"
                        @input="${(e: Event) => state.name = (e.target as HTMLInputElement).value}"
                        placeholder="e.g. North Greenhouse"
                        class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                      />
                    </div>

                    <div>
                      <label class="block text-sm font-medium text-stone-700 mb-1">Zone Type</label>
                      <select
                        value="${() => state.zoneType}"
                        @change="${(e: Event) => state.zoneType = Number((e.target as HTMLSelectElement).value)}"
                        class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                      >
                        <option value="0">Greenhouse</option>
                        <option value="1">Field</option>
                        <option value="2">Barn</option>
                        <option value="3">Cellar</option>
                        <option value="4">Storage</option>
                        <option value="5">Other</option>
                      </select>
                    </div>
                    
                    <div>
                      <label class="block text-sm font-medium text-stone-700 mb-1">Description (Optional)</label>
                      <textarea
                        value="${() => state.description}"
                        @input="${(e: Event) => state.description = (e.target as HTMLTextAreaElement).value}"
                        placeholder="Location details or purpose"
                        class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                        rows="3"
                      ></textarea>
                    </div>
                  </div>

                  <div class="mt-8 flex justify-end gap-3">
                    <button type="button" @click="${() => state.isOpen = false}" class="px-4 py-2 text-stone-600 hover:text-stone-800 font-medium">Cancel</button>
                    <button
                      type="submit"
                      disabled="${() => state.loading}"
                      class="bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-5 py-2 rounded-lg font-semibold shadow-sm transition"
                    >
                      ${() => state.loading ? "Creating..." : "Create Zone"}
                    </button>
                  </div>
                </form>
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
