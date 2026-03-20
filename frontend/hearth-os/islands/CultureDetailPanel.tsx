import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import FeedingTimer from "./FeedingTimer.tsx";
import ConfirmDialog from "./ConfirmDialog.tsx";
import CreateCultureForm from "./CreateCultureForm.tsx";
import FormField from "../components/FormField.tsx";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  FeedCultureSchema,
  type FieldErrors,
} from "../utils/schemas.ts";

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

export default function CultureDetailPanel() {
  const cultures = useSignal<Culture[]>([
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
  ]);

  const selectedId = useSignal<string | null>(null);
  const isSidebarOpen = useSignal(false);
  const showSplitConfirm = useSignal(false);
  const splitName = useSignal("");
  const showFeedForm = useSignal(false);
  const feedFlour = useSignal("100");
  const feedWater = useSignal("100");
  const feedNotes = useSignal("");
  const feedErrors = useSignal<FieldErrors>({});
  const isSubmitting = useSignal(false);

  const selectedCulture = cultures.value.find((c) => c.id === selectedId.value);

  const openSidebar = (id: string) => {
    selectedId.value = id;
    showFeedForm.value = false;
    showSplitConfirm.value = false;
    isSidebarOpen.value = true;
  };

  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null;
    }, 300);
  };

  const handleFeed = async (culture: Culture) => {
    const result = FeedCultureSchema.safeParse({
      flourGrams: feedFlour.value,
      waterGrams: feedWater.value,
      notes: feedNotes.value || undefined,
    });
    if (!result.success) {
      feedErrors.value = extractErrors(result);
      return;
    }
    isSubmitting.value = true;
    feedErrors.value = {};
    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      await HearthAPI.feedCulture(culture.id, result.data);
      showToast(
        "success",
        `${culture.name} fed!`,
        `${feedFlour.value}g flour + ${feedWater.value}g water.`,
      );
      showFeedForm.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Feeding failed",
        err instanceof Error ? err.message : "Unknown error",
      );
    } finally {
      isSubmitting.value = false;
    }
  };

  const handleSplit = async (culture: Culture) => {
    if (!splitName.value.trim()) return;
    showSplitConfirm.value = false;
    try {
      const { HearthAPI } = await import("../utils/farmos-client.ts");
      await HearthAPI.splitCulture(culture.id, { newName: splitName.value });
      showToast(
        "success",
        "Culture split!",
        `New culture "${splitName.value}" created from ${culture.name}.`,
      );
      splitName.value = "";
    } catch (err: unknown) {
      showToast(
        "error",
        "Split failed",
        err instanceof Error ? err.message : "Unknown error",
      );
    }
  };

  const inputClass = (field: string) =>
    `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 transition ${
      feedErrors.value[field] ? "border-red-400 bg-red-50" : "border-stone-300"
    }`;

  return (
    <div class="relative flex min-h-[500px]">
      {/* Main Grid */}
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[420px]" : ""
        }`}
      >
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {cultures.value.map((culture) => {
            const isSelected = selectedId.value === culture.id;
            return (
              <button
                type="button"
                onClick={() => openSidebar(culture.id)}
                class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition flex flex-col text-left w-full cursor-pointer ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <div class="flex items-start justify-between mb-3">
                  <div>
                    <h3 class="text-lg font-bold text-stone-800">
                      {culture.name}
                    </h3>
                    <p class="text-xs text-stone-400 mt-0.5">
                      {culture.typeLabel}
                    </p>
                  </div>
                  <StatusBadge variant={culture.status} />
                </div>
                <p class="text-sm text-stone-500 mb-3 flex-1">
                  {culture.origin}
                </p>
                <div class="pt-3 border-t border-stone-100 flex items-center justify-between">
                  <span class="text-xs text-stone-400">
                    Age: {culture.age} · Fed {culture.feedCount}x
                  </span>
                  <span class="text-xs text-stone-400">View Details →</span>
                </div>
              </button>
            );
          })}
        </div>

        {/* Register Culture Modal Trigger */}
        <CreateCultureForm />
      </div>

      {/* Mobile Backdrop */}
      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30 transition-opacity"
          onClick={closeSidebar}
        />
      )}

      {/* Slide-out Sidebar */}
      <aside
        class={`fixed top-0 right-0 h-full w-[420px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selectedCulture && (
          <div class="p-6 h-full flex flex-col">
            {/* Header */}
            <div class="flex items-start justify-between mb-6 pb-4 border-b border-stone-100">
              <div>
                <div class="flex items-center gap-2 mb-2">
                  <h3 class="text-2xl font-bold text-stone-800">
                    {selectedCulture.name}
                  </h3>
                  <StatusBadge variant={selectedCulture.status} />
                </div>
                <p class="text-sm text-stone-500">
                  {selectedCulture.typeLabel} · {selectedCulture.origin}
                </p>
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>

            <div class="flex-1">
              {/* Type Description */}
              <div class="bg-amber-50 rounded-lg p-4 mb-5 border border-amber-100">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-sm font-semibold text-amber-800">
                    About {selectedCulture.typeLabel}
                  </span>
                  <Tooltip text="Each culture type has different feeding needs and behaviors.">
                    <InfoIcon />
                  </Tooltip>
                </div>
                <p class="text-xs text-amber-700 leading-relaxed">
                  {typeDescriptions[selectedCulture.type]}
                </p>
              </div>

              {/* Stats */}
              <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-3">
                Vital Stats
              </h4>
              <div class="grid grid-cols-2 gap-3 mb-5">
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-bold text-stone-700">
                    {selectedCulture.age}
                  </p>
                  <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                    Age
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-bold text-stone-700">
                    {selectedCulture.feedCount}
                  </p>
                  <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                    Total Feedings
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-bold text-stone-700">
                    {selectedCulture.children}
                  </p>
                  <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                    Splits
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-bold text-stone-700">
                    {selectedCulture.feedIntervalHours}h
                  </p>
                  <p class="text-xs text-stone-500 mt-1 uppercase tracking-wide">
                    Feed Interval
                  </p>
                </div>
              </div>

              {/* Feeding Timer */}
              <div class="mb-5 p-4 bg-stone-50 rounded-lg">
                <FeedingTimer
                  cultureId={selectedCulture.id}
                  cultureName={selectedCulture.name}
                  lastFedISO={selectedCulture.lastFed}
                  intervalHours={selectedCulture.feedIntervalHours}
                />
              </div>

              {/* Custom Feed Form */}
              {showFeedForm.value && (
                <div class="mb-5 p-4 bg-stone-50 rounded-lg border border-stone-200">
                  <h4 class="text-sm font-bold text-stone-700 mb-3">
                    Custom Feeding
                  </h4>
                  <div class="grid grid-cols-3 gap-3 mb-3">
                    <FormField
                      label="Flour (g)"
                      error={feedErrors.value.flourGrams}
                    >
                      <input
                        type="number"
                        class={inputClass("flourGrams")}
                        value={feedFlour.value}
                        onInput={(e) =>
                          feedFlour.value =
                            (e.target as HTMLInputElement).value}
                      />
                    </FormField>
                    <FormField
                      label="Water (g)"
                      error={feedErrors.value.waterGrams}
                    >
                      <input
                        type="number"
                        class={inputClass("waterGrams")}
                        value={feedWater.value}
                        onInput={(e) =>
                          feedWater.value =
                            (e.target as HTMLInputElement).value}
                      />
                    </FormField>
                    <FormField label="Notes" error={feedErrors.value.notes}>
                      <input
                        type="text"
                        class={inputClass("notes")}
                        placeholder="Optional"
                        value={feedNotes.value}
                        onInput={(e) =>
                          feedNotes.value =
                            (e.target as HTMLInputElement).value}
                      />
                    </FormField>
                  </div>
                  <div class="flex gap-2">
                    <button
                      onClick={() => handleFeed(selectedCulture)}
                      disabled={isSubmitting.value}
                      class="px-3 py-1.5 bg-amber-600 text-white text-xs font-semibold rounded-lg hover:bg-amber-700 disabled:opacity-50 transition"
                    >
                      {isSubmitting.value
                        ? "Feeding..."
                        : "Feed with Custom Amounts"}
                    </button>
                    <button
                      onClick={() => showFeedForm.value = false}
                      class="px-3 py-1.5 text-xs text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}

              {/* Split Confirmation */}
              {showSplitConfirm.value && (
                <div class="mb-5 p-4 bg-blue-50 rounded-lg border border-blue-200">
                  <h4 class="text-sm font-bold text-blue-800 mb-2">
                    Split {selectedCulture.name}
                  </h4>
                  <p class="text-xs text-blue-700 mb-3">
                    Creates a new culture descended from{" "}
                    {selectedCulture.name}. Parent continues unchanged.
                  </p>
                  <div class="flex gap-2">
                    <input
                      type="text"
                      placeholder="New culture name"
                      class="px-3 py-1.5 border border-blue-300 rounded-lg text-sm flex-1 focus:outline-none focus:ring-2 focus:ring-blue-400"
                      value={splitName.value}
                      onInput={(e) =>
                        splitName.value = (e.target as HTMLInputElement).value}
                    />
                    <button
                      onClick={() => handleSplit(selectedCulture)}
                      disabled={!splitName.value.trim()}
                      class="px-3 py-1.5 text-xs font-semibold bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
                    >
                      Split
                    </button>
                    <button
                      onClick={() => showSplitConfirm.value = false}
                      class="px-3 py-1.5 text-xs text-stone-600 bg-stone-100 rounded-lg hover:bg-stone-200 transition"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Quick Actions */}
            <div class="pt-4 border-t border-stone-100 mt-auto">
              <p class="text-xs text-stone-500 uppercase tracking-wider mb-2 font-semibold">
                Quick Actions
              </p>
              <div class="flex flex-col gap-2">
                {!showFeedForm.value && (
                  <button
                    onClick={() => showFeedForm.value = true}
                    class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-800 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2"
                  >
                    🥄 Custom Feed
                  </button>
                )}
                <button
                  onClick={() => showSplitConfirm.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition border border-blue-100 flex items-center justify-center gap-2"
                >
                  ✂️ Split Culture
                </button>
              </div>
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
