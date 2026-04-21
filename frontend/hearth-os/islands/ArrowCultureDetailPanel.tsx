import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowStatusBadge } from "../components/ArrowStatusBadge.ts";
import { ArrowInfoIcon, ArrowTooltip } from "../components/ArrowTooltip.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import { showToast } from "../utils/toastState.ts";
import { extractErrors, FeedCultureSchema } from "../utils/schemas.ts";

interface Culture {
  id: string;
  name: string;
  type: string;
  typeLabel: string;
  origin: string;
  lastFed: string;
  feedIntervalHours: number;
  status: "active" | "attention" | "idle";
  feedCount: number;
  age: string;
  children: number;
}

const typeDescriptions: Record<string, string> = {
  SourdoughStarter:
    "Wild yeast + lactobacillus. Feed with flour + water every 12–24h. Discard half before feeding to maintain balance.",
  KombuchaSCOBY:
    "Symbiotic colony of bacteria and yeast. Feed by starting a new batch of sweet tea. SCOBY grows a new layer each batch.",
  MilkKefir:
    "Polysaccharide matrix of bacteria and yeast. Ferments milk in 24h. Strain grains, add fresh milk, repeat.",
};

// ── Inline feeding timer (pure Arrow template, no Preact hooks) ──
function renderFeedingTimer(
  cultureId: string,
  cultureName: string,
  lastFedISO: string,
  intervalHours: number,
) {
  const timerState = reactive({
    timeRemaining: "",
    isOverdue: false,
    isFeeding: false,
    confirmOpen: false,
  });

  const updateTimer = () => {
    const lastFed = new Date(lastFedISO).getTime();
    const nextFeed = lastFed + intervalHours * 60 * 60 * 1000;
    const now = Date.now();
    const diff = nextFeed - now;
    if (diff <= 0) {
      const overdue = Math.abs(diff);
      const hours = Math.floor(overdue / (1000 * 60 * 60));
      const mins = Math.floor((overdue % (1000 * 60 * 60)) / (1000 * 60));
      timerState.timeRemaining = `${hours}h ${mins}m overdue`;
      timerState.isOverdue = true;
    } else {
      const hours = Math.floor(diff / (1000 * 60 * 60));
      const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
      timerState.timeRemaining = `${hours}h ${mins}m`;
      timerState.isOverdue = false;
    }
  };
  updateTimer();
  setInterval(updateTimer, 60000);

  const doFeed = async () => {
    timerState.confirmOpen = false;
    timerState.isFeeding = true;
    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      await HearthAPI.feedCulture(cultureId, {
        flourGrams: 100,
        waterGrams: 100,
        notes: "Routine feeding from HearthOS",
      });
      showToast(
        "success",
        `${cultureName} fed!`,
        `100g flour + 100g water. Next feeding in ${intervalHours}h.`,
      );
    } catch (err: unknown) {
      showToast(
        "error",
        "Feeding failed",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      timerState.isFeeding = false;
    }
  };

  return html`
    <div>
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-1">
          <div>
            <p class="text-xs text-stone-400 uppercase tracking-wider font-medium">
              Next Feed
            </p>
            <p class="${() =>
              `text-sm font-bold mt-0.5 ${
                timerState.isOverdue ? "text-red-600" : "text-stone-800"
              }`}">${() => timerState.timeRemaining}</p>
          </div>
          ${ArrowTooltip({
            text:
              `This culture needs feeding every ${intervalHours}h. Regular feeding maintains yeast/bacteria balance.`,
            children: ArrowInfoIcon(),
          })}
        </div>
        <button
          @click="${() => timerState.confirmOpen = true}"
          disabled="${() => timerState.isFeeding}"
          class="${() =>
            `px-3 py-1.5 text-xs font-semibold rounded-lg transition shadow-sm ${
              timerState.isOverdue
                ? "bg-red-600 text-white hover:bg-red-700"
                : "bg-stone-100 text-stone-700 hover:bg-stone-200"
            } disabled:opacity-50`}"
        >
          ${() => timerState.isFeeding ? "Feeding..." : "Feed Now"}
        </button>
      </div>
      ${() =>
        timerState.confirmOpen
          ? html`
            <div class="mt-3 p-3 bg-amber-50 rounded-lg border border-amber-200">
              <p class="text-xs text-amber-800 mb-2 font-medium">
                Feed ${cultureName} with 100g flour + 100g water?
              </p>
              <div class="flex gap-2">
                <button
                  @click="${doFeed}"
                  class="px-3 py-1 text-xs font-semibold bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition"
                >
                  Feed Now
                </button>
                <button
                  @click="${() => timerState.confirmOpen = false}"
                  class="px-3 py-1 text-xs text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                >
                  Cancel
                </button>
              </div>
            </div>
          `
          : ""}
    </div>
  `;
}

export default function ArrowCultureDetailPanel() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      cultures: [
        {
          id: "c1-gertrude",
          name: "Gertrude",
          type: "SourdoughStarter",
          typeLabel: "Sourdough Starter",
          origin: "San Francisco heritage, est. 2019",
          lastFed: new Date(Date.now() - 20 * 60 * 60 * 1000).toISOString(),
          feedIntervalHours: 24,
          status: "active",
          feedCount: 342,
          age: "5 years",
          children: 2,
        },
        {
          id: "c2-scoby-prime",
          name: "SCOBY Prime",
          type: "KombuchaSCOBY",
          typeLabel: "Kombucha SCOBY",
          origin: "Split from neighbor's culture, 2023",
          lastFed: new Date(Date.now() - 6 * 24 * 60 * 60 * 1000).toISOString(),
          feedIntervalHours: 168,
          status: "active",
          feedCount: 28,
          age: "1.5 years",
          children: 1,
        },
        {
          id: "c3-kefir",
          name: "Kefir Grains",
          type: "MilkKefir",
          typeLabel: "Milk Kefir Grains",
          origin: "Purchased from Cultures for Health",
          lastFed: new Date(Date.now() - 26 * 60 * 60 * 1000).toISOString(),
          feedIntervalHours: 24,
          status: "attention",
          feedCount: 180,
          age: "6 months",
          children: 0,
        },
      ] as Culture[],
      selectedId: null as string | null,
      isSidebarOpen: false,
      showSplitConfirm: false,
      splitName: "",
      showFeedForm: false,
      feedFlour: "100",
      feedWater: "100",
      feedNotes: "",
      feedErrors: {} as Record<string, string>,
      isSubmitting: false,
    });

    const openSidebar = (id: string) => {
      state.selectedId = id;
      state.showFeedForm = false;
      state.showSplitConfirm = false;
      state.isSidebarOpen = true;
    };

    const closeSidebar = () => {
      state.isSidebarOpen = false;
      setTimeout(() => {
        state.selectedId = null;
      }, 300);
    };

    const handleFeed = async (culture: Culture) => {
      const result = FeedCultureSchema.safeParse({
        flourGrams: state.feedFlour,
        waterGrams: state.feedWater,
        notes: state.feedNotes || undefined,
      });
      if (!result.success) {
        state.feedErrors = extractErrors(result);
        return;
      }
      state.isSubmitting = true;
      state.feedErrors = {};
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.feedCulture(culture.id, result.data);
        showToast(
          "success",
          `${culture.name} fed!`,
          `${state.feedFlour}g flour + ${state.feedWater}g water.`,
        );
        state.showFeedForm = false;
      } catch (err: unknown) {
        showToast(
          "error",
          "Feeding failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.isSubmitting = false;
      }
    };

    const handleSplit = async (culture: Culture) => {
      if (!state.splitName.trim()) return;
      state.showSplitConfirm = false;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.splitCulture(culture.id, { newName: state.splitName });
        showToast(
          "success",
          "Culture split!",
          `New culture "${state.splitName}" created from ${culture.name}.`,
        );
        state.splitName = "";
      } catch (err: unknown) {
        showToast(
          "error",
          "Split failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      }
    };

    const inputClass = (field: string) => () =>
      `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
        (state.feedErrors as Record<string, string>)[field]
          ? "border-red-400 bg-red-50"
          : "border-stone-300"
      }`;

    const template = html`
      <div class="relative flex min-h-[500px]">
        <!-- Main Grid -->
        <div class="${() =>
          `flex-1 transition-all duration-300 ${
            state.isSidebarOpen ? "mr-[420px]" : ""
          }`}">
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            ${() =>
              state.cultures.map((culture) => {
                const isSelected = state.selectedId === culture.id;
                return html`
                  <button
                    type="button"
                    @click="${() => openSidebar(culture.id)}"
                    class="${`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition flex flex-col text-left w-full cursor-pointer ${
                      isSelected
                        ? "border-amber-400 ring-2 ring-amber-200"
                        : "border-stone-200 hover:border-amber-200"
                    }`}"
                  >
                    <div class="flex items-start justify-between mb-3">
                      <div>
                        <h3 class="text-lg font-bold text-stone-800">${culture
                          .name}</h3>
                        <p class="text-xs text-stone-400 mt-0.5">${culture
                          .typeLabel}</p>
                      </div>
                      ${ArrowStatusBadge({ variant: culture.status })}
                    </div>
                    <p class="text-sm text-stone-500 mb-3 flex-1">${culture
                      .origin}</p>
                    <div class="pt-3 border-t border-stone-100 flex items-center justify-between">
                      <span class="text-xs text-stone-400"
                      >Age: ${culture.age} · Fed ${culture.feedCount}x</span>
                      <span class="text-xs text-stone-400">View Details →</span>
                    </div>
                  </button>
                `;
              })}
          </div>
        </div>

        <!-- Mobile Backdrop -->
        ${() => {
          if (!state.isSidebarOpen) return "";
          return html`
            <div
              class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30 transition-opacity"
              @click="${closeSidebar}"
            >
            </div>
          `;
        }}

        <!-- Slide-out Sidebar -->
        <aside class="${() =>
          `fixed top-0 right-0 h-full w-[420px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
            state.isSidebarOpen ? "translate-x-0" : "translate-x-full"
          }`}">
          ${() => {
            const selectedCulture = state.cultures.find((c) =>
              c.id === state.selectedId
            );
            if (!selectedCulture) return "";

            return html`
              <div class="p-6 h-full flex flex-col">
                <!-- Header -->
                <div
                  class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100"
                >
                  <div>
                    <div class="flex items-center gap-2 mb-2">
                      <h3 class="text-2xl font-bold text-stone-800">${selectedCulture
                        .name}</h3>
                      ${ArrowStatusBadge({ variant: selectedCulture.status })}
                    </div>
                    <p class="text-sm text-stone-500">${selectedCulture
                      .typeLabel} · ${selectedCulture.origin}</p>
                  </div>
                  <button
                    @click="${closeSidebar}"
                    class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
                  >
                    ✕
                  </button>
                </div>

                <div class="flex-1">
                  <!-- Type Description -->
                  <div class="bg-amber-50 rounded-lg p-4 mb-5 border border-amber-100">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="text-sm font-semibold text-amber-800"
                      >About ${selectedCulture.typeLabel}</span>
                      ${ArrowTooltip({
                        text:
                          "Each culture type has different feeding needs and behaviors.",
                        children: ArrowInfoIcon(),
                      })}
                    </div>
                    <p class="text-xs text-amber-700 leading-relaxed">${typeDescriptions[
                      selectedCulture.type
                    ]}</p>
                  </div>

                  <!-- Stats -->
                  <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-3">
                    Vital Stats
                  </h4>
                  <div class="grid grid-cols-2 gap-3 mb-5">
                    <div
                      class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100"
                    >
                      <p class="text-2xl font-bold text-stone-700">${selectedCulture
                        .age}</p>
                      <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">Age</p>
                    </div>
                    <div
                      class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100"
                    >
                      <p class="text-2xl font-bold text-stone-700">${selectedCulture
                        .feedCount}</p>
                      <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                        Total Feedings
                      </p>
                    </div>
                    <div
                      class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100"
                    >
                      <p class="text-2xl font-bold text-stone-700">${selectedCulture
                        .children}</p>
                      <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                        Splits
                      </p>
                    </div>
                    <div
                      class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100"
                    >
                      <p class="text-2xl font-bold text-stone-700">${selectedCulture
                        .feedIntervalHours}h</p>
                      <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                        Feed Interval
                      </p>
                    </div>
                  </div>

                  <!-- Inline Feeding Timer (pure Arrow, no Preact hooks) -->
                  <div class="mb-5 p-4 bg-stone-50 rounded-lg">
                    ${renderFeedingTimer(
                      selectedCulture.id,
                      selectedCulture.name,
                      selectedCulture.lastFed,
                      selectedCulture.feedIntervalHours,
                    )}
                  </div>

                  <!-- Custom Feed Form -->
                  ${state.showFeedForm
                    ? html`
                      <div class="mb-5 p-4 bg-stone-50 rounded-lg border border-stone-200">
                        <h4 class="text-sm font-bold text-stone-700 mb-3">Custom Feeding</h4>
                        <div class="grid grid-cols-3 gap-3 mb-3">
                          ${ArrowFormField({
                            label: "Flour (g)",
                            error: () => state.feedErrors.flourGrams,
                            children: html`
                              <input
                                type="number"
                                class="${inputClass("flourGrams")}"
                                value="${() => state.feedFlour}"
                                @input="${(e: Event) =>
                                  state.feedFlour =
                                    (e.target as HTMLInputElement).value}"
                              />
                            `,
                          })} ${ArrowFormField({
                            label: "Water (g)",
                            error: () => state.feedErrors.waterGrams,
                            children: html`
                              <input
                                type="number"
                                class="${inputClass("waterGrams")}"
                                value="${() => state.feedWater}"
                                @input="${(e: Event) =>
                                  state.feedWater =
                                    (e.target as HTMLInputElement).value}"
                              />
                            `,
                          })} ${ArrowFormField({
                            label: "Notes",
                            error: () => state.feedErrors.notes,
                            children: html`
                              <input
                                type="text"
                                class="${inputClass("notes")}"
                                placeholder="Optional"
                                value="${() => state.feedNotes}"
                                @input="${(e: Event) =>
                                  state.feedNotes =
                                    (e.target as HTMLInputElement).value}"
                              />
                            `,
                          })}
                        </div>
                        <div class="flex gap-2">
                          <button
                            @click="${() => handleFeed(selectedCulture)}"
                            disabled="${() => state.isSubmitting}"
                            class="px-3 py-1.5 bg-amber-600 text-white text-xs font-semibold rounded-lg hover:bg-amber-700 disabled:opacity-50 transition"
                          >
                            ${() =>
                              state.isSubmitting
                                ? "Feeding..."
                                : "Feed with Custom Amounts"}
                          </button>
                          <button
                            @click="${() => state.showFeedForm = false}"
                            class="px-3 py-1.5 text-xs text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    `
                    : ""}

                  <!-- Split Confirmation -->
                  ${state.showSplitConfirm
                    ? html`
                      <div class="mb-5 p-4 bg-blue-50 rounded-lg border border-blue-200">
                        <h4 class="text-sm font-bold text-blue-800 mb-2">Split ${selectedCulture
                          .name}</h4>
                        <p class="text-xs text-blue-700 mb-3">
                          Creates a new culture descended from ${selectedCulture
                            .name}. Parent continues
                          unchanged.
                        </p>
                        <div class="flex gap-2">
                          <input
                            type="text"
                            placeholder="New culture name"
                            class="px-3 py-1.5 border border-blue-300 rounded-lg text-sm flex-1 focus:outline-none focus:ring-2 focus:ring-blue-400"
                            value="${() => state.splitName}"
                            @input="${(e: Event) =>
                              state.splitName =
                                (e.target as HTMLInputElement).value}"
                          />
                          <button
                            @click="${() => handleSplit(selectedCulture)}"
                            disabled="${() => !state.splitName.trim()}"
                            class="px-3 py-1.5 text-xs font-semibold bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
                          >
                            Split
                          </button>
                          <button
                            @click="${() => state.showSplitConfirm = false}"
                            class="px-3 py-1.5 text-xs text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                          >
                            Cancel
                          </button>
                        </div>
                      </div>
                    `
                    : ""}
                </div>

                <!-- Quick Actions -->
                <div class="pt-4 border-t border-stone-100 mt-auto">
                  <p
                    class="text-xs text-stone-500 uppercase tracking-wider mb-2 font-semibold"
                  >
                    Quick Actions
                  </p>
                  <div class="flex flex-col gap-2">
                    ${!state.showFeedForm
                      ? html`
                        <button
                          @click="${() => state.showFeedForm = true}"
                          class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-800 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2"
                        >
                          🥄 Custom Feed
                        </button>
                      `
                      : ""}
                    <button
                      @click="${() => state.showSplitConfirm = true}"
                      class="w-full py-2.5 text-sm font-bold rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition border border-blue-100 flex items-center justify-center gap-2"
                    >
                      ✂️ Split Culture
                    </button>
                  </div>
                </div>
              </div>
            `;
          }}
        </aside>
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
