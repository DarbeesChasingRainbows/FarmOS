import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowStatusBadge } from "../components/ArrowStatusBadge.ts";
import { showToast } from "../utils/toastState.ts";
import {
  ApiarySchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";
import type {
  ApiaryOverview,
  HiveSummary,
} from "../utils/farmos-client.ts";

export default function ArrowApiaryManager() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      apiaries: [] as ApiaryOverview[],
      hives: [] as HiveSummary[],
      loading: true,
      // Create modal
      showCreateModal: false,
      name: "",
      latitude: "",
      longitude: "",
      maxCapacity: "20",
      notes: "",
      submitting: false,
      errors: {} as FieldErrors,
    });

    const loadData = async () => {
      try {
        const { ApiaryReportsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        const [apiaries, hives] = await Promise.allSettled([
          ApiaryReportsAPI.getAllApiaries(),
          ApiaryReportsAPI.getAllHives(),
        ]);
        state.apiaries =
          apiaries.status === "fulfilled" ? apiaries.value : [];
        state.hives = hives.status === "fulfilled" ? hives.value : [];
      } catch {
        // silent
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const hivesForApiary = (apiaryId: string) =>
      state.hives.filter((h) => h.apiaryId === apiaryId);

    const handleCreate = async () => {
      const result = ApiarySchema.safeParse({
        name: state.name,
        latitude: state.latitude,
        longitude: state.longitude,
        maxCapacity: state.maxCapacity,
        notes: state.notes,
      });
      if (!result.success) {
        state.errors = extractErrors(result);
        showToast(
          "error",
          "Validation failed",
          "Please fix the highlighted fields.",
        );
        return;
      }
      state.submitting = true;
      state.errors = {};
      try {
        const { ApiaryLocationAPI } = await import(
          "../utils/farmos-client.ts"
        );
        await ApiaryLocationAPI.createApiary({
          name: result.data.name,
          position: {
            latitude: result.data.latitude,
            longitude: result.data.longitude,
          },
          maxCapacity: result.data.maxCapacity,
          notes: result.data.notes || undefined,
        });
        showToast(
          "success",
          "Apiary created!",
          result.data.name + " is now tracked.",
        );
        state.showCreateModal = false;
        state.name = "";
        state.latitude = "";
        state.longitude = "";
        state.notes = "";
        loadData();
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.submitting = false;
      }
    };

    const inputCls = (field: string) =>
      "w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition " +
      (state.errors[field] ? "border-red-400 bg-red-50" : "border-stone-300");

    // Apiary card
    const apiaryCard = (apiary: ApiaryOverview) => {
      const hives = hivesForApiary(apiary.id);
      const capacityPct = Math.min(
        (apiary.hiveCount / apiary.capacity) * 100,
        100,
      );
      const capacityColor =
        capacityPct > 80
          ? "bg-red-500"
          : capacityPct > 50
            ? "bg-amber-500"
            : "bg-emerald-500";

      return html`
        <div
          class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 hover:shadow-md transition-shadow"
        >
          <div class="flex items-start justify-between mb-4">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-lg">\uD83D\uDCCD</span>
                <h3 class="text-lg font-bold text-stone-800">
                  ${apiary.name}
                </h3>
              </div>
              <p class="text-xs text-stone-400 mt-0.5">
                ${apiary.status}
              </p>
            </div>
            ${ArrowStatusBadge({
              variant: apiary.status === "Active" ? "active" : "resting",
            })}
          </div>

          <!-- Capacity Bar -->
          <div class="mb-4">
            <div class="flex items-center justify-between text-xs text-stone-500 mb-1">
              <span>Capacity</span>
              <span class="font-bold">${apiary.hiveCount}/${apiary.capacity}</span>
            </div>
            <div class="bg-stone-100 rounded-full h-2">
              <div
                class="${capacityColor} h-2 rounded-full transition-all"
                style="width: ${capacityPct}%"
              ></div>
            </div>
          </div>

          <!-- Hive List -->
          ${hives.length > 0
            ? html`
                <div class="space-y-1.5">
                  ${hives.map(
                    (h) => html`
                      <div
                        class="flex items-center justify-between text-sm"
                      >
                        <span class="text-stone-700 font-medium"
                          >${h.name}</span
                        >
                        ${ArrowStatusBadge({
                          variant:
                            h.status === "Attention"
                              ? "attention"
                              : h.status === "Resting"
                                ? "resting"
                                : "active",
                        })}
                      </div>
                    `,
                  )}
                </div>
              `
            : html`<p class="text-xs text-stone-400 italic">
                No hives assigned
              </p>`}
        </div>
      `;
    };

    // Create modal
    const createModal = () => html`
      <div
        class="${() =>
          state.showCreateModal
            ? "fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
            : "hidden"}"
      >
        <div
          class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]"
        >
          <div
            class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50"
          >
            <h3 class="text-lg font-bold text-stone-800">
              Create Apiary Location
            </h3>
            <button
              type="button"
              @click="${() => {
                state.showCreateModal = false;
              }}"
              class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
            >
              \u2715
            </button>
          </div>
          <div class="p-6 flex flex-col gap-4">
            <div>
              <label class="text-sm font-medium text-stone-700"
                >Apiary Name *</label
              >
              <input
                type="text"
                class="${() => inputCls("name")}"
                placeholder="e.g. Home Yard"
                @input="${(e: Event) => {
                  state.name = (e.target as HTMLInputElement).value;
                }}"
              />
              ${() =>
                state.errors.name
                  ? html`<p class="text-xs text-red-600 mt-1">
                      ${state.errors.name}
                    </p>`
                  : html``}
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="text-sm font-medium text-stone-700"
                  >Latitude *</label
                >
                <input
                  type="number"
                  step="any"
                  class="${() => inputCls("latitude")}"
                  placeholder="38.897"
                  @input="${(e: Event) => {
                    state.latitude = (e.target as HTMLInputElement).value;
                  }}"
                />
              </div>
              <div>
                <label class="text-sm font-medium text-stone-700"
                  >Longitude *</label
                >
                <input
                  type="number"
                  step="any"
                  class="${() => inputCls("longitude")}"
                  placeholder="-77.037"
                  @input="${(e: Event) => {
                    state.longitude = (e.target as HTMLInputElement).value;
                  }}"
                />
              </div>
            </div>
            <div>
              <label class="text-sm font-medium text-stone-700"
                >Max Hive Capacity *</label
              >
              <input
                type="number"
                class="${() => inputCls("maxCapacity")}"
                value="20"
                @input="${(e: Event) => {
                  state.maxCapacity = (e.target as HTMLInputElement).value;
                }}"
              />
            </div>
            <div>
              <label class="text-sm font-medium text-stone-700"
                >Notes</label
              >
              <textarea
                class="${() => inputCls("notes")}"
                rows="2"
                placeholder="Access notes, sun exposure, forage..."
                @input="${(e: Event) => {
                  state.notes = (e.target as HTMLTextAreaElement).value;
                }}"
              ></textarea>
            </div>
            <div class="flex justify-end gap-3 mt-2">
              <button
                type="button"
                @click="${() => {
                  state.showCreateModal = false;
                }}"
                class="px-4 py-2 rounded-lg font-medium text-stone-600 hover:bg-stone-100 transition"
              >
                Cancel
              </button>
              <button
                type="button"
                @click="${handleCreate}"
                class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm"
              >
                ${() =>
                  state.submitting ? "Creating..." : "Create Apiary"}
              </button>
            </div>
          </div>
        </div>
      </div>
    `;

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1
              class="text-3xl font-extrabold text-stone-800 tracking-tight"
            >
              Apiary Locations
            </h1>
            <p class="text-stone-500 mt-1">
              Group hives by yard and track capacity.
            </p>
          </div>
          <button
            type="button"
            @click="${() => {
              state.showCreateModal = true;
            }}"
            class="bg-amber-600 text-white font-semibold py-2.5 px-5 rounded-xl hover:bg-amber-700 transition shadow-sm flex items-center gap-2 text-sm"
          >
            <span class="text-lg">+</span> New Apiary
          </button>
        </div>

        ${() =>
          state.loading
            ? html`
                <div class="flex items-center justify-center py-20">
                  <div
                    class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"
                  ></div>
                </div>
              `
            : state.apiaries.length === 0
              ? ArrowEmptyState({
                  icon: "\uD83D\uDCCD",
                  title: "No apiaries yet",
                  message:
                    "Create your first apiary location to start grouping hives by yard.",
                })
              : html`
                  <div
                    class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"
                  >
                    ${() => state.apiaries.map((a) => apiaryCard(a))}
                  </div>
                `}

        ${createModal}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
