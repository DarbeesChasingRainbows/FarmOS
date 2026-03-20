import { useSignal } from "@preact/signals";
import StatusBadge from "../components/StatusBadge.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";
import InspectHiveForm from "../components/InspectHiveForm.tsx";
import CreateHiveForm from "../components/CreateHiveForm.tsx";
import ConfirmDialog from "../components/ConfirmDialog.tsx";
import { showToast } from "../utils/toastState.ts";

interface Hive {
  id: string;
  name: string;
  location: string;
  type: string;
  queenStatus: string;
  lastInspection: string;
  mites: number;
  health: "active" | "attention" | "fermenting" | "resting";
  honeySupers: number;
  lastHarvest: string;
  lastTreatment: string;
}

export default function HiveDetailPanel() {
  const hives = useSignal<Hive[]>([
    {
      id: "h1",
      name: "Hive Alpha",
      location: "South Orchard",
      type: "Langstroth",
      queenStatus: "Present",
      lastInspection: "Feb 27, 2024",
      mites: 1.2,
      health: "active",
      honeySupers: 2,
      lastHarvest: "Oct 2023",
      lastTreatment: "Dec 2023",
    },
    {
      id: "h2",
      name: "Hive Bravo",
      location: "North Meadow Edge",
      type: "Langstroth",
      queenStatus: "Present",
      lastInspection: "Feb 27, 2024",
      mites: 0.8,
      health: "active",
      honeySupers: 3,
      lastHarvest: "Sep 2023",
      lastTreatment: "Nov 2023",
    },
    {
      id: "h3",
      name: "Hive Charlie",
      location: "Garden Wall",
      type: "Top Bar",
      queenStatus: "Present",
      lastInspection: "Feb 20, 2024",
      mites: 3.1,
      health: "attention",
      honeySupers: 1,
      lastHarvest: "Aug 2023",
      lastTreatment: "Jan 2024",
    },
    {
      id: "h4",
      name: "Hive Delta",
      location: "South Orchard",
      type: "Langstroth",
      queenStatus: "Unknown",
      lastInspection: "Feb 15, 2024",
      mites: 2.4,
      health: "fermenting",
      honeySupers: 2,
      lastHarvest: "Oct 2023",
      lastTreatment: "Dec 2023",
    },
    {
      id: "h5",
      name: "Hive Echo",
      location: "West Pasture",
      type: "Warré",
      queenStatus: "Present",
      lastInspection: "Feb 27, 2024",
      mites: 0.5,
      health: "active",
      honeySupers: 1,
      lastHarvest: "Sep 2023",
      lastTreatment: "Never",
    },
    {
      id: "h6",
      name: "Hive Foxtrot",
      location: "Barn Shelter",
      type: "Langstroth",
      queenStatus: "Absent",
      lastInspection: "Feb 27, 2024",
      mites: 0,
      health: "resting",
      honeySupers: 0,
      lastHarvest: "N/A",
      lastTreatment: "N/A",
    },
  ]);

  const selectedId = useSignal<string | null>(null);
  const showTreatForm = useSignal(false);
  const showHarvestForm = useSignal(false);
  const treatmentName = useSignal("");
  const treatmentMethod = useSignal("");
  const showTreatConfirm = useSignal(false);
  const showHarvestConfirm = useSignal(false);

  // Overlay state indicator
  const isSidebarOpen = useSignal(false);

  const selectedHive = hives.value.find((h) => h.id === selectedId.value);

  const miteColor = (mites: number) => {
    if (mites <= 1) return "text-emerald-600";
    if (mites <= 3) return "text-amber-600";
    return "text-red-600";
  };

  const queenColor = (status: string) => {
    if (status === "Present") return "text-emerald-600";
    if (status === "Unknown") return "text-amber-600";
    return "text-red-600";
  };

  const openSidebar = (id: string) => {
    selectedId.value = id;
    showTreatForm.value = false;
    showHarvestForm.value = false;
    isSidebarOpen.value = true;
  };

  const closeSidebar = () => {
    isSidebarOpen.value = false;
    setTimeout(() => {
      selectedId.value = null; // delay clearing the hive until slide-out animation finishes
    }, 300);
  };

  const handleTreat = async () => {
    if (!selectedHive || !treatmentName.value || !treatmentMethod.value) return;
    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.treatHive(selectedHive.id, {
        treatment: treatmentName.value,
        method: treatmentMethod.value,
        date: new Date().toISOString().split("T")[0],
      });
      showToast(
        "success",
        "Treatment recorded",
        `${treatmentName.value} applied to ${selectedHive.name}.`,
      );
      showTreatForm.value = false;
      treatmentName.value = "";
      treatmentMethod.value = "";
      showTreatConfirm.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record treatment",
        err instanceof Error ? err.message : "Unknown error",
      );
    }
  };

  const handleHarvest = async () => {
    if (!selectedHive) return;
    try {
      const { ApiaryAPI } = await import("../utils/farmos-client.ts");
      await ApiaryAPI.harvestHoney(selectedHive.id, {
        supers: 1,
        estimatedYield: { value: 30, unit: "lbs", type: "weight" },
        date: new Date().toISOString().split("T")[0],
      });
      showToast(
        "success",
        "Harvest recorded!",
        `Honey harvested from ${selectedHive.name}.`,
      );
      showHarvestConfirm.value = false;
    } catch (err: unknown) {
      showToast(
        "error",
        "Failed to record harvest",
        err instanceof Error ? err.message : "Unknown error",
      );
    }
  };

  return (
    <div class="relative flex min-h-[500px]">
      {/* Main Grid Section */}
      <div
        class={`flex-1 transition-all duration-300 ${
          isSidebarOpen.value ? "mr-[420px]" : ""
        }`}
      >
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {hives.value.map((hive) => {
            const isSelected = selectedId.value === hive.id;
            return (
              <button
                type="button"
                onClick={() => openSidebar(hive.id)}
                class={`bg-white rounded-xl border shadow-sm p-5 hover:shadow-md transition text-left cursor-pointer w-full ${
                  isSelected
                    ? "border-amber-400 ring-2 ring-amber-200"
                    : "border-stone-200 hover:border-amber-200"
                }`}
              >
                <div class="flex items-start justify-between mb-3">
                  <div>
                    <h3 class="text-lg font-bold text-stone-800">
                      {hive.name}
                    </h3>
                    <p class="text-xs text-stone-400 mt-0.5">
                      {hive.location} · {hive.type}
                    </p>
                  </div>
                  <StatusBadge variant={hive.health} />
                </div>
                <div class="grid grid-cols-2 gap-3 mt-4 pt-3 border-t border-stone-100">
                  <div>
                    <p class="text-xs text-stone-400 uppercase tracking-wider font-medium">
                      Queen
                    </p>
                    <p
                      class={`text-sm font-semibold mt-0.5 ${
                        queenColor(hive.queenStatus)
                      }`}
                    >
                      {hive.queenStatus}
                    </p>
                  </div>
                  <div>
                    <p class="text-xs text-stone-400 uppercase tracking-wider font-medium">
                      Mites/100
                    </p>
                    <p
                      class={`text-sm font-semibold mt-0.5 ${
                        miteColor(hive.mites)
                      }`}
                    >
                      {hive.mites}
                    </p>
                  </div>
                </div>
              </button>
            );
          })}
        </div>

        {/* Create Hive Modal (Already converted by previous conversation) */}
        <CreateHiveForm />
      </div>

      {/* Backdrop for mobile */}
      {isSidebarOpen.value && (
        <div
          class="md:hidden fixed inset-0 bg-stone-900/20 backdrop-blur-sm z-30 transition-opacity"
          onClick={closeSidebar}
        />
      )}

      {/* Slide-out Sidebar Panel */}
      <aside
        class={`fixed top-0 right-0 h-full w-[400px] bg-white border-l border-stone-200 shadow-2xl z-40 transform transition-transform duration-300 ease-in-out overflow-y-auto ${
          isSidebarOpen.value ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selectedHive && (
          <div class="p-6 h-full flex flex-col">
            <div class="flex items-start justify-between mb-8 pb-4 border-b border-stone-100">
              <div>
                <div class="flex items-center gap-2 mb-2">
                  <span class="text-2xl">🐝</span>
                  <h3 class="text-2xl font-bold text-stone-800">
                    {selectedHive.name}
                  </h3>
                </div>
                <div class="flex items-center gap-3">
                  <StatusBadge variant={selectedHive.health} />
                  <span class="text-sm text-stone-500">
                    {selectedHive.location} · {selectedHive.type}
                  </span>
                </div>
              </div>
              <button
                onClick={closeSidebar}
                class="text-stone-400 hover:text-stone-700 bg-stone-50 hover:bg-stone-100 rounded-full p-2 transition"
              >
                ✕
              </button>
            </div>

            <div class="flex-1">
              <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">
                Vital Statistics
              </h4>

              {/* Stats Grid */}
              <div class="grid grid-cols-2 gap-4 mb-8">
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p
                    class={`text-2xl font-bold ${
                      queenColor(selectedHive.queenStatus)
                    }`}
                  >
                    {selectedHive.queenStatus}
                  </p>
                  <div class="flex items-center justify-center gap-1 mt-1">
                    <p class="text-xs text-stone-500 uppercase tracking-wide">
                      Queen Status
                    </p>
                    <Tooltip
                      text="A queenless hive will decline. 'Unknown' = not seen but eggs may be present. 'Absent' = no queen or evidence found."
                      position="left"
                    >
                      <InfoIcon />
                    </Tooltip>
                  </div>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p
                    class={`text-2xl font-bold ${
                      miteColor(selectedHive.mites)
                    }`}
                  >
                    {selectedHive.mites}
                  </p>
                  <div class="flex items-center justify-center gap-1 mt-1">
                    <p class="text-xs text-stone-500 uppercase tracking-wide">
                      Mites/100
                    </p>
                    <Tooltip
                      text="≤1 = excellent. 1–3 = monitor. >3 = treat immediately with oxalic acid or formic acid."
                      position="left"
                    >
                      <InfoIcon />
                    </Tooltip>
                  </div>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-2xl font-bold text-stone-700">
                    {selectedHive.honeySupers}
                  </p>
                  <div class="flex items-center justify-center gap-1 mt-1">
                    <p class="text-xs text-stone-500 uppercase tracking-wide">
                      Honey Supers
                    </p>
                    <Tooltip
                      text="Number of honey supers currently on the hive. Each super holds ~30 lbs of honey when full."
                      position="left"
                    >
                      <InfoIcon />
                    </Tooltip>
                  </div>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 text-center border border-stone-100">
                  <p class="text-lg font-bold text-stone-700 mt-1">
                    {selectedHive.lastInspection.split(",")[0]}
                  </p>
                  <p class="text-xs text-stone-500 uppercase tracking-wide mt-1">
                    Last Inspected
                  </p>
                </div>
              </div>

              <h4 class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4">
                Activity History
              </h4>

              {/* History */}
              <div class="grid grid-cols-2 gap-4 mb-8">
                <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Last Harvest
                  </p>
                  <p class="text-base font-semibold text-stone-800 mt-1">
                    {selectedHive.lastHarvest}
                  </p>
                </div>
                <div class="bg-stone-50 rounded-xl p-4 border border-stone-100">
                  <p class="text-xs text-stone-500 uppercase tracking-wider font-medium">
                    Last Treatment
                  </p>
                  <p class="text-base font-semibold text-stone-800 mt-1">
                    {selectedHive.lastTreatment}
                  </p>
                </div>
              </div>

              {/* Inspection Form (inline) */}
              <div class="mb-4">
                <InspectHiveForm
                  hiveId={selectedHive.id}
                  hiveName={selectedHive.name}
                />
              </div>

              {/* Treatment Form */}
              {showTreatForm.value && (
                <div class="mb-4 p-5 bg-red-50 rounded-xl border border-red-200">
                  <h4 class="text-sm font-bold text-red-800 mb-3">
                    Record Treatment
                  </h4>
                  <div class="flex flex-col gap-3 mb-4">
                    <div>
                      <label class="text-xs font-semibold text-stone-700 uppercase">
                        Treatment
                      </label>
                      <select
                        class="w-full px-3 py-2 border border-stone-300 bg-white rounded-lg text-sm mt-1 focus:ring-2 focus:ring-amber-500 outline-none"
                        value={treatmentName.value}
                        onChange={(e) =>
                          treatmentName.value =
                            (e.target as HTMLSelectElement).value}
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
                      <label class="text-xs font-semibold text-stone-700 uppercase">
                        Method
                      </label>
                      <select
                        class="w-full px-3 py-2 border border-stone-300 bg-white rounded-lg text-sm mt-1 focus:ring-2 focus:ring-amber-500 outline-none"
                        value={treatmentMethod.value}
                        onChange={(e) =>
                          treatmentMethod.value =
                            (e.target as HTMLSelectElement).value}
                      >
                        <option value="">Select...</option>
                        <option value="Dribble">Dribble</option>
                        <option value="Vaporize">Vaporize</option>
                        <option value="Strip">Strip</option>
                        <option value="Pad">Pad</option>
                      </select>
                    </div>
                  </div>
                  <div class="flex gap-2">
                    <button
                      type="button"
                      onClick={() => showTreatConfirm.value = true}
                      disabled={!treatmentName.value || !treatmentMethod.value}
                      class="flex-1 py-2 text-sm font-bold bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 transition"
                    >
                      Apply
                    </button>
                    <button
                      type="button"
                      onClick={() => showTreatForm.value = false}
                      class="flex-1 py-2 text-sm font-bold text-stone-600 bg-stone-200 rounded-lg hover:bg-stone-300 transition"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Actions Bar fixed to bottom of sidebar optionally if needed, but it works fine inline */}
            <div class="pt-4 border-t border-stone-100 mt-auto">
              <p class="text-xs text-stone-500 uppercase tracking-wider mb-2 font-semibold">
                Quick Actions
              </p>
              <div class="flex flex-col gap-2">
                {!showTreatForm.value && (
                  <button
                    type="button"
                    onClick={() => showTreatForm.value = true}
                    class="w-full py-2.5 text-sm font-bold rounded-lg bg-red-50 text-red-700 hover:bg-red-100 hover:text-red-800 transition border border-red-100 flex items-center justify-center gap-2"
                  >
                    <span>💊</span> Record Treatment
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => showHarvestConfirm.value = true}
                  class="w-full py-2.5 text-sm font-bold rounded-lg bg-amber-50 text-amber-800 hover:bg-amber-100 transition border border-amber-100 flex items-center justify-center gap-2"
                >
                  <span>🍯</span> Record Harvest
                </button>
              </div>
            </div>
          </div>
        )}
      </aside>

      {/* Confirmation Dialogs */}
      <ConfirmDialog
        open={showTreatConfirm.value}
        title="Confirm Treatment"
        message={`Are you sure you want to log a ${treatmentName.value} treatment for ${selectedHive?.name}?`}
        onConfirm={handleTreat}
        onCancel={() => showTreatConfirm.value = false}
      />
      <ConfirmDialog
        open={showHarvestConfirm.value}
        title="Confirm Harvest"
        message={`Are you sure you want to record a honey harvest for ${selectedHive?.name}?`}
        onConfirm={handleHarvest}
        onCancel={() => showHarvestConfirm.value = false}
      />
    </div>
  );
}
