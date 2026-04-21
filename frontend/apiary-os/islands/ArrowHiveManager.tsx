import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowStatusBadge } from "../components/ArrowStatusBadge.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowConfirmDialog } from "../components/ArrowConfirmDialog.ts";
import type { HiveSummary } from "../utils/farmos-client.ts";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  type FieldErrors,
  HiveInspectionSchema,
  HiveSchema,
} from "../utils/schemas.ts";

type FilterType = "all" | "active" | "attention" | "resting";
type HiveHealth = "active" | "attention" | "resting";

interface Hive {
  id: string;
  name: string;
  location: string;
  type: string;
  queenStatus: string;
  lastInspection: string;
  mites: number;
  health: HiveHealth;
  honeySupers: number;
}

export default function ArrowHiveManager() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const mapHive = (h: HiveSummary): Hive => ({
      id: h.id,
      name: h.name,
      location: h.apiaryId || "Unassigned",
      type: h.type || "Langstroth",
      queenStatus: h.queenStatus || "Unknown",
      lastInspection: h.lastInspection || "Never",
      mites: h.miteCount || 0,
      health: h.status === "Attention" || (h.miteCount && h.miteCount > 3)
        ? "attention"
        : h.status === "Resting"
        ? "resting"
        : "active",
      honeySupers: h.honeySupers || 0,
    });

    const state = reactive({
      hives: [] as Hive[],
      filter: "all" as FilterType,
      selectedId: null as string | null,
      sidebarOpen: false,
      loading: true,
      // Create hive modal
      showCreateModal: false,
      createName: "",
      createLocation: "",
      createType: "0",
      createSubmitting: false,
      createErrors: {} as FieldErrors,
      // Inspect form
      showInspectForm: false,
      inspectDate: new Date().toISOString().split("T")[0],
      inspectQueenSeen: true,
      inspectBrood: "",
      inspectTemperament: "",
      inspectMites: "",
      inspectNotes: "",
      inspectSubmitting: false,
      inspectErrors: {} as FieldErrors,
      // Treat form
      showTreatForm: false,
      treatName: "",
      treatMethod: "",
      treatSubmitting: false,
      // Harvest confirm
      showHarvestConfirm: false,
      // Treat confirm
      showTreatConfirm: false,
    });

    const loadHives = async () => {
      try {
        const { ApiaryReportsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        const hives = await ApiaryReportsAPI.getAllHives();
        state.hives = (hives ?? []).map(mapHive);
      } catch {
        // Use demo data on API failure
        state.hives = [
          {
            id: "h1",
            name: "Hive Alpha",
            location: "South Orchard",
            type: "Langstroth",
            queenStatus: "Present",
            lastInspection: "Mar 15, 2026",
            mites: 1.2,
            health: "active",
            honeySupers: 2,
          },
          {
            id: "h2",
            name: "Hive Bravo",
            location: "North Meadow",
            type: "Langstroth",
            queenStatus: "Present",
            lastInspection: "Mar 15, 2026",
            mites: 0.8,
            health: "active",
            honeySupers: 3,
          },
          {
            id: "h3",
            name: "Hive Charlie",
            location: "Garden Wall",
            type: "Top Bar",
            queenStatus: "Present",
            lastInspection: "Mar 10, 2026",
            mites: 3.1,
            health: "attention",
            honeySupers: 1,
          },
          {
            id: "h4",
            name: "Hive Delta",
            location: "South Orchard",
            type: "Langstroth",
            queenStatus: "Unknown",
            lastInspection: "Mar 5, 2026",
            mites: 2.4,
            health: "active",
            honeySupers: 2,
          },
          {
            id: "h5",
            name: "Hive Echo",
            location: "West Pasture",
            type: "Warr\u00E9",
            queenStatus: "Present",
            lastInspection: "Mar 15, 2026",
            mites: 0.5,
            health: "active",
            honeySupers: 1,
          },
          {
            id: "h6",
            name: "Hive Foxtrot",
            location: "Barn Shelter",
            type: "Langstroth",
            queenStatus: "Absent",
            lastInspection: "Mar 15, 2026",
            mites: 0,
            health: "resting",
            honeySupers: 0,
          },
        ];
      } finally {
        state.loading = false;
      }
    };

    loadHives();

    // Helpers
    const miteColor = (m: number) =>
      m <= 1 ? "text-emerald-600" : m <= 3 ? "text-amber-600" : "text-red-600";
    const miteBarColor = (m: number) =>
      m <= 1 ? "bg-emerald-500" : m <= 3 ? "bg-amber-500" : "bg-red-500";
    const queenColor = (s: string) =>
      s === "Present"
        ? "text-emerald-600"
        : s === "Unknown"
        ? "text-amber-600"
        : "text-red-600";

    const filteredHives = () => {
      if (state.filter === "all") return state.hives;
      return state.hives.filter((h) => h.health === state.filter);
    };

    const countByHealth = (h: HiveHealth) =>
      state.hives.filter((hive) => hive.health === h).length;

    const selectedHive = () =>
      state.hives.find((h) => h.id === state.selectedId) || null;

    const openSidebar = (id: string) => {
      state.selectedId = id;
      state.sidebarOpen = true;
      state.showInspectForm = false;
      state.showTreatForm = false;
    };

    const closeSidebar = () => {
      state.sidebarOpen = false;
      state.selectedId = null;
    };

    // Input class helper
    const inputCls = (field: string, errors: FieldErrors) =>
      "w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition " +
      (errors[field] ? "border-red-400 bg-red-50" : "border-stone-300");

    // --- Create Hive ---
    const handleCreateHive = async () => {
      const result = HiveSchema.safeParse({
        name: state.createName,
        location: state.createLocation,
        hiveType: state.createType,
      });
      if (!result.success) {
        state.createErrors = extractErrors(result);
        showToast(
          "error",
          "Validation failed",
          "Please fix the highlighted fields.",
        );
        return;
      }
      state.createSubmitting = true;
      state.createErrors = {};
      try {
        const { ApiaryAPI } = await import("../utils/farmos-client.ts");
        await ApiaryAPI.createHive(result.data);
        showToast(
          "success",
          "Hive registered!",
          result.data.name + " is now tracked.",
        );
        state.showCreateModal = false;
        state.createName = "";
        state.createLocation = "";
        loadHives();
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed to create hive",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.createSubmitting = false;
      }
    };

    // --- Inspect Hive ---
    const handleInspect = async () => {
      const hive = selectedHive();
      if (!hive) return;
      const result = HiveInspectionSchema.safeParse({
        queenSeen: state.inspectQueenSeen,
        broodPattern: state.inspectBrood,
        temperament: state.inspectTemperament,
        mitesPerHundred: state.inspectMites,
        notes: state.inspectNotes,
        date: state.inspectDate,
      });
      if (!result.success) {
        state.inspectErrors = extractErrors(result);
        showToast(
          "error",
          "Validation failed",
          "Please fix the highlighted fields.",
        );
        return;
      }
      state.inspectSubmitting = true;
      state.inspectErrors = {};
      try {
        const { ApiaryAPI } = await import("../utils/farmos-client.ts");
        await ApiaryAPI.inspectHive(hive.id, result.data);
        showToast(
          "success",
          "Inspection recorded",
          hive.name + " inspection logged.",
        );
        state.showInspectForm = false;
        state.inspectBrood = "";
        state.inspectTemperament = "";
        state.inspectMites = "";
        state.inspectNotes = "";
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed to record inspection",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.inspectSubmitting = false;
      }
    };

    // --- Treat Hive ---
    const handleTreat = async () => {
      const hive = selectedHive();
      if (!hive || !state.treatName || !state.treatMethod) return;
      state.treatSubmitting = true;
      try {
        const { ApiaryAPI } = await import("../utils/farmos-client.ts");
        await ApiaryAPI.treatHive(hive.id, {
          treatment: state.treatName,
          method: state.treatMethod,
          date: new Date().toISOString().split("T")[0],
        });
        showToast(
          "success",
          "Treatment recorded",
          state.treatName + " applied to " + hive.name + ".",
        );
        state.showTreatForm = false;
        state.showTreatConfirm = false;
        state.treatName = "";
        state.treatMethod = "";
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.treatSubmitting = false;
      }
    };

    // --- Harvest ---
    const handleHarvest = async () => {
      const hive = selectedHive();
      if (!hive) return;
      try {
        const { ApiaryAPI } = await import("../utils/farmos-client.ts");
        await ApiaryAPI.harvestHoney(hive.id, {
          supers: 1,
          estimatedYield: { value: 30, unit: "lbs", type: "weight" },
          date: new Date().toISOString().split("T")[0],
        });
        showToast(
          "success",
          "Harvest recorded!",
          "Honey harvested from " + hive.name + ".",
        );
        state.showHarvestConfirm = false;
      } catch (err: unknown) {
        showToast(
          "error",
          "Failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      }
    };

    // --- Filter Button ---
    const filterBtn = (
      label: string,
      filter: FilterType,
      count: () => number,
    ) =>
      html`
        <button
          type="button"
          @click="${() => {
            state.filter = filter;
          }}"
          class="${() =>
            state.filter === filter
              ? "bg-amber-500 text-white"
              : "bg-stone-100 text-stone-600 hover:bg-stone-200"} px-3 py-1.5 rounded-lg text-sm font-medium transition flex items-center gap-1.5"
        >
          ${label}
          <span class="${() =>
            state.filter === filter
              ? "bg-amber-600"
              : "bg-stone-200"} text-xs px-1.5 py-0.5 rounded-full font-bold">
            ${count}
          </span>
        </button>
      `;

    // --- Hive Card ---
    const hiveCard = (hive: Hive) => {
      const miteBarWidth = Math.min((hive.mites / 10) * 100, 100);
      return html`
        <button
          type="button"
          @click="${() => openSidebar(hive.id)}"
          class="${() =>
            state.selectedId === hive.id
              ? "border-amber-400 ring-2 ring-amber-200/50"
              : "border-stone-200/60 hover:border-amber-200"} bg-white rounded-2xl border shadow-sm p-5 hover:shadow-md transition-all text-left cursor-pointer w-full"
        >
          <div class="flex items-start justify-between mb-3">
            <div>
              <h3 class="text-base font-bold text-stone-800">${hive.name}</h3>
              <p class="text-xs text-stone-400 mt-0.5">
                ${hive.location} \\u00B7 ${hive.type}
              </p>
            </div>
            ${ArrowStatusBadge({ variant: hive.health })}
          </div>
          <div class="grid grid-cols-2 gap-3 mt-3 pt-3 border-t border-stone-100">
            <div>
              <p
                class="text-[10px] text-stone-400 uppercase tracking-wider font-medium"
              >
                Queen
              </p>
              <p class="text-sm font-semibold mt-0.5 ${queenColor(
                hive.queenStatus,
              )}">${hive.queenStatus}</p>
            </div>
            <div>
              <p
                class="text-[10px] text-stone-400 uppercase tracking-wider font-medium"
              >
                Mites/100
              </p>
              <p class="text-sm font-semibold mt-0.5 ${miteColor(
                hive.mites,
              )}">${hive.mites}</p>
            </div>
          </div>
          <div class="mt-3 bg-stone-100 rounded-full h-1.5">
            <div
              class="${miteBarColor(
                hive.mites,
              )} h-1.5 rounded-full transition-all"
              style="width: ${miteBarWidth}%"
            >
            </div>
          </div>
          <div class="flex items-center justify-between mt-2">
            <span class="text-[10px] text-stone-400">${hive
              .honeySupers} supers</span>
            <span class="text-[10px] text-stone-400">${hive
              .lastInspection}</span>
          </div>
        </button>
      `;
    };

    // --- Sidebar Detail ---
    const sidebarContent = () => {
      const hive = selectedHive();
      if (!hive) {
        return html`

        `;
      }

      return html`
        <div class="p-6 h-full flex flex-col">
          <!-- Header -->
          <div
            class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100"
          >
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span class="text-xl">\\uD83D\\uDC1D</span>
                <h3 class="text-xl font-bold text-stone-800">${hive.name}</h3>
              </div>
              <div class="flex items-center gap-2">
                ${ArrowStatusBadge({ variant: hive.health })}
                <span class="text-sm text-stone-500"
                >${hive.location} \\u00B7 ${hive.type}</span>
              </div>
            </div>
            <button
              type="button"
              @click="${closeSidebar}"
              class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
            >
              \\u2715
            </button>
          </div>

          <!-- Vitals Grid -->
          <div class="flex-1">
            <h4 class="text-xs font-bold text-stone-800 uppercase tracking-wider mb-3">
              Vital Statistics
            </h4>
            <div class="grid grid-cols-2 gap-3 mb-6">
              <div
                class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
              >
                <p class="text-xl font-bold ${queenColor(
                  hive.queenStatus,
                )}">${hive.queenStatus}</p>
                <p class="text-[10px] text-stone-500 uppercase tracking-wide mt-1">
                  Queen Status
                </p>
              </div>
              <div
                class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
              >
                <p class="text-xl font-bold ${miteColor(hive.mites)}">${hive
                  .mites}</p>
                <p class="text-[10px] text-stone-500 uppercase tracking-wide mt-1">
                  Mites/100
                </p>
              </div>
              <div
                class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
              >
                <p class="text-xl font-bold text-stone-700">${hive
                  .honeySupers}</p>
                <p class="text-[10px] text-stone-500 uppercase tracking-wide mt-1">
                  Honey Supers
                </p>
              </div>
              <div
                class="bg-stone-50 rounded-xl p-3 text-center border border-stone-100"
              >
                <p class="text-sm font-bold text-stone-700 mt-1">${hive
                  .lastInspection}</p>
                <p class="text-[10px] text-stone-500 uppercase tracking-wide mt-1">
                  Last Inspected
                </p>
              </div>
            </div>

            <!-- Inspection Form (collapsible) -->
            ${() =>
              state.showInspectForm
                ? html`
                  <div class="mb-4 p-4 bg-amber-50 rounded-xl border border-amber-200">
                    <h4 class="text-sm font-bold text-amber-800 mb-3">Log Inspection</h4>
                    <div class="flex flex-col gap-3">
                      <div class="grid grid-cols-2 gap-3">
                        <div>
                          <label class="text-xs font-medium text-stone-700">Date *</label>
                          <input
                            type="date"
                            class="${inputCls("date", state.inspectErrors)}"
                            value="${state.inspectDate}"
                            @input="${(e: Event) => {
                              state.inspectDate =
                                (e.target as HTMLInputElement).value;
                            }}"
                          />
                        </div>
                        <div>
                          <label class="text-xs font-medium text-stone-700">Queen Seen</label>
                          <div class="flex gap-2 mt-1">
                            <button
                              type="button"
                              @click="${() => {
                                state.inspectQueenSeen = true;
                              }}"
                              class="${() =>
                                state.inspectQueenSeen
                                  ? "bg-emerald-100 text-emerald-800 border-emerald-300"
                                  : "bg-stone-100 text-stone-500"} px-3 py-1 text-xs rounded-lg font-medium border transition"
                            >
                              Yes
                            </button>
                            <button
                              type="button"
                              @click="${() => {
                                state.inspectQueenSeen = false;
                              }}"
                              class="${() =>
                                !state.inspectQueenSeen
                                  ? "bg-red-100 text-red-800 border-red-300"
                                  : "bg-stone-100 text-stone-500"} px-3 py-1 text-xs rounded-lg font-medium border transition"
                            >
                              No
                            </button>
                          </div>
                        </div>
                      </div>
                      <div class="grid grid-cols-2 gap-3">
                        <div>
                          <label class="text-xs font-medium text-stone-700"
                          >Brood Pattern *</label>
                          <select class="${inputCls(
                            "broodPattern",
                            state.inspectErrors,
                          )}" @change="${(e: Event) => {
                            state.inspectBrood =
                              (e.target as HTMLSelectElement).value;
                          }}">
                            <option value="">Select...</option>
                            <option value="Solid">Solid</option>
                            <option value="Spotty">Spotty</option>
                            <option value="Drone Heavy">Drone Heavy</option>
                            <option value="No Brood">No Brood</option>
                          </select>
                        </div>
                        <div>
                          <label class="text-xs font-medium text-stone-700">Temperament *</label>
                          <select class="${inputCls(
                            "temperament",
                            state.inspectErrors,
                          )}" @change="${(e: Event) => {
                            state.inspectTemperament =
                              (e.target as HTMLSelectElement).value;
                          }}">
                            <option value="">Select...</option>
                            <option value="Calm">Calm</option>
                            <option value="Nervous">Nervous</option>
                            <option value="Defensive">Defensive</option>
                            <option value="Aggressive">Aggressive</option>
                          </select>
                        </div>
                      </div>
                      <div>
                        <label class="text-xs font-medium text-stone-700"
                        >Mites per 100 bees *</label>
                        <input
                          type="number"
                          step="0.1"
                          class="${inputCls(
                            "mitesPerHundred",
                            state.inspectErrors,
                          )}"
                          placeholder="e.g. 2.5"
                          @input="${(e: Event) => {
                            state.inspectMites =
                              (e.target as HTMLInputElement).value;
                          }}"
                        />
                      </div>
                      <div>
                        <label class="text-xs font-medium text-stone-700">Notes</label>
                        <textarea
                          class="${inputCls("notes", state.inspectErrors)}"
                          rows="2"
                          placeholder="Optional observations..."
                          @input="${(e: Event) => {
                            state.inspectNotes =
                              (e.target as HTMLTextAreaElement).value;
                          }}"
                        ></textarea>
                      </div>
                      <div class="flex gap-2">
                        <button
                          type="button"
                          @click="${handleInspect}"
                          class="flex-1 bg-amber-600 text-white font-semibold py-2 px-4 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm text-sm"
                        >
                          ${() =>
                            state.inspectSubmitting
                              ? "Saving..."
                              : "Save Inspection"}
                        </button>
                        <button
                          type="button"
                          @click="${() => {
                            state.showInspectForm = false;
                          }}"
                          class="px-4 py-2 text-sm font-medium text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  </div>
                `
                : html`

                `}

            <!-- Treatment Form (collapsible) -->
            ${() =>
              state.showTreatForm
                ? html`
                  <div class="mb-4 p-4 bg-red-50 rounded-xl border border-red-200">
                    <h4 class="text-sm font-bold text-red-800 mb-3">Record Treatment</h4>
                    <div class="flex flex-col gap-3">
                      <div>
                        <label class="text-xs font-semibold text-stone-700 uppercase"
                        >Treatment</label>
                        <select
                          class="w-full px-3 py-2 border border-stone-300 bg-white rounded-lg text-sm mt-1 focus:ring-2 focus:ring-amber-500 outline-none"
                          @change="${(e: Event) => {
                            state.treatName =
                              (e.target as HTMLSelectElement).value;
                          }}"
                        >
                          <option value="">Select...</option>
                          <option value="Oxalic Acid">Oxalic Acid</option>
                          <option value="Formic Acid">Formic Acid</option>
                          <option value="Thymol">Thymol (ApiGuard)</option>
                          <option value="Apistan">Apistan Strips</option>
                          <option value="HopGuard">HopGuard III</option>
                        </select>
                      </div>
                      <div>
                        <label class="text-xs font-semibold text-stone-700 uppercase"
                        >Method</label>
                        <select
                          class="w-full px-3 py-2 border border-stone-300 bg-white rounded-lg text-sm mt-1 focus:ring-2 focus:ring-amber-500 outline-none"
                          @change="${(e: Event) => {
                            state.treatMethod =
                              (e.target as HTMLSelectElement).value;
                          }}"
                        >
                          <option value="">Select...</option>
                          <option value="Dribble">Dribble</option>
                          <option value="Vaporize">Vaporize</option>
                          <option value="Strip">Strip</option>
                          <option value="Pad">Pad</option>
                        </select>
                      </div>
                      <div class="flex gap-2">
                        <button
                          type="button"
                          @click="${() => {
                            state.showTreatConfirm = true;
                          }}"
                          class="flex-1 py-2 text-sm font-bold bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 transition"
                        >
                          Apply
                        </button>
                        <button
                          type="button"
                          @click="${() => {
                            state.showTreatForm = false;
                          }}"
                          class="flex-1 py-2 text-sm font-bold text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition"
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  </div>
                `
                : html`

                `}
          </div>

          <!-- Quick Actions -->
          <div class="pt-4 border-t border-stone-100 mt-auto">
            <p
              class="text-[10px] text-stone-500 uppercase tracking-wider mb-2 font-semibold"
            >
              Quick Actions
            </p>
            <div class="flex flex-col gap-2">
              ${() =>
                !state.showInspectForm
                  ? html`
                    <button
                      type="button"
                      @click="${() => {
                        state.showInspectForm = true;
                        state.showTreatForm = false;
                      }}"
                      class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-700 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2"
                    >
                      <span>\\uD83D\\uDCCB</span> Log Inspection
                    </button>
                  `
                  : html`

                  `} ${() =>
                !state.showTreatForm
                  ? html`
                    <button
                      type="button"
                      @click="${() => {
                        state.showTreatForm = true;
                        state.showInspectForm = false;
                      }}"
                      class="w-full py-2.5 text-sm font-bold rounded-lg bg-red-50 text-red-700 hover:bg-red-100 transition border border-red-100 flex items-center justify-center gap-2"
                    >
                      <span>\\uD83D\\uDC8A</span> Record Treatment
                    </button>
                  `
                  : html`

                  `}
              <button
                type="button"
                @click="${() => {
                  state.showHarvestConfirm = true;
                }}"
                class="w-full py-2.5 text-sm font-bold rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 transition border border-emerald-100 flex items-center justify-center gap-2"
              >
                <span>\\uD83C\\uDF6F</span> Record Harvest
              </button>
            </div>
          </div>
        </div>
      `;
    };

    // --- Create Hive Modal ---
    const createModal = () =>
      html`
        <div class="${() =>
          state.showCreateModal
            ? "fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50 animate-[fadeIn_0.2s_ease-out]"
            : "hidden"}">
          <div
            class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 overflow-hidden animate-[scaleIn_0.2s_ease-out]"
          >
            <div
              class="px-6 py-4 border-b border-stone-100 flex items-center justify-between bg-stone-50"
            >
              <h3 class="text-lg font-bold text-stone-800">Register New Hive</h3>
              <button
                type="button"
                @click="${() => {
                  state.showCreateModal = false;
                }}"
                class="text-stone-400 hover:text-stone-600 hover:bg-stone-200 rounded p-1 transition"
              >
                \\u2715
              </button>
            </div>
            <div class="p-6 flex flex-col gap-4">
              <div>
                <label class="text-sm font-medium text-stone-700">Hive Name *</label>
                <input
                  type="text"
                  class="${() => inputCls("name", state.createErrors)}"
                  placeholder="e.g. Hive Alpha"
                  @input="${(e: Event) => {
                    state.createName = (e.target as HTMLInputElement).value;
                  }}"
                />
                ${() =>
                  state.createErrors.name
                    ? html`
                      <p class="text-xs text-red-600 mt-1">${state.createErrors
                        .name}</p>
                    `
                    : html`

                    `}
              </div>
              <div>
                <label class="text-sm font-medium text-stone-700">Location *</label>
                <input
                  type="text"
                  class="${() => inputCls("location", state.createErrors)}"
                  placeholder="e.g. South Orchard"
                  @input="${(e: Event) => {
                    state.createLocation = (e.target as HTMLInputElement).value;
                  }}"
                />
                ${() =>
                  state.createErrors.location
                    ? html`
                      <p class="text-xs text-red-600 mt-1">${state.createErrors
                        .location}</p>
                    `
                    : html`

                    `}
              </div>
              <div>
                <label class="text-sm font-medium text-stone-700">Hive Type *</label>
                <select class="${() =>
                  inputCls("hiveType", state.createErrors)}" @change="${(
                  e: Event,
                ) => {
                  state.createType = (e.target as HTMLSelectElement).value;
                }}">
                  <option value="0">Langstroth</option>
                  <option value="1">Top Bar</option>
                  <option value="2">Warr\\u00E9</option>
                </select>
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
                  @click="${handleCreateHive}"
                  class="bg-amber-600 text-white font-semibold py-2 px-6 rounded-lg hover:bg-amber-700 transition disabled:opacity-50 shadow-sm"
                >
                  ${() =>
                    state.createSubmitting ? "Registering..." : "Register Hive"}
                </button>
              </div>
            </div>
          </div>
        </div>
      `;

    // --- Main Template ---
    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <div class="flex items-center justify-between mb-6">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Hives
            </h1>
            <p class="text-stone-500 mt-1">
              Click any hive to inspect, treat, or harvest.
            </p>
          </div>
          <button
            type="button"
            @click="${() => {
              state.showCreateModal = true;
            }}"
            class="bg-amber-600 text-white font-semibold py-2.5 px-5 rounded-xl hover:bg-amber-700 transition shadow-sm flex items-center gap-2 text-sm"
          >
            <span class="text-lg">+</span> New Hive
          </button>
        </div>

        <!-- Filter Bar -->
        <div class="flex flex-wrap gap-2 mb-6">
          ${filterBtn("All", "all", () => state.hives.length)} ${filterBtn(
            "Active",
            "active",
            () => countByHealth("active"),
          )} ${filterBtn(
            "Attention",
            "attention",
            () => countByHealth("attention"),
          )} ${filterBtn("Resting", "resting", () => countByHealth("resting"))}
        </div>

        ${() =>
          state.loading
            ? html`
              <div class="flex items-center justify-center py-20">
                <div
                  class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"
                >
                </div>
              </div>
            `
            : html`
              <div class="relative flex min-h-[500px]">
                <!-- Grid -->
                <div class="${() =>
                  state.sidebarOpen
                    ? "flex-1 mr-[420px] transition-all duration-300"
                    : "flex-1 transition-all duration-300"}">
                  ${() =>
                    filteredHives().length === 0
                      ? ArrowEmptyState({
                        icon: "\uD83D\uDC1D",
                        title: "No hives match this filter",
                        message: "Try a different filter or create a new hive.",
                      })
                      : html`
                        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                          ${() => filteredHives().map((h) => hiveCard(h))}
                        </div>
                      `}
                </div>

                <!-- Sidebar -->
                <aside
                  class="${() =>
                    state.sidebarOpen
                      ? "translate-x-0"
                      : "translate-x-full"} fixed top-0 right-0 h-full w-[400px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto"
                >
                  ${sidebarContent}
                </aside>
              </div>
            `} ${createModal} ${ArrowConfirmDialog({
            isOpen: () => state.showTreatConfirm,
            title: () => "Confirm Treatment",
            message: () => {
              const h = selectedHive();
              return "Apply " + state.treatName + " to " +
                (h ? h.name : "hive") + "?";
            },
            onConfirm: handleTreat,
            onCancel: () => {
              state.showTreatConfirm = false;
            },
            danger: true,
          })} ${ArrowConfirmDialog({
            isOpen: () => state.showHarvestConfirm,
            title: () => "Confirm Harvest",
            message: () => {
              const h = selectedHive();
              return "Record honey harvest for " + (h ? h.name : "hive") + "?";
            },
            onConfirm: handleHarvest,
            onCancel: () => {
              state.showHarvestConfirm = false;
            },
          })}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
