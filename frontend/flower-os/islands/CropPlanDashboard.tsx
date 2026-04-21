import { useSignal } from "@preact/signals";
import type {
  CropPlanDetail,
  CropPlanSummary,
} from "../utils/farmos-client.ts";

const CHANNEL_LABELS = [
  "Farmers Market",
  "CSA",
  "Wholesale",
  "Wedding",
  "Direct Sale",
];

export default function CropPlanDashboard() {
  const plans = useSignal<CropPlanSummary[]>([]);
  const selectedPlan = useSignal<CropPlanDetail | null>(null);
  const loading = useSignal(true);
  const error = useSignal("");
  const showCreateForm = useSignal(false);

  // Create form
  const newPlanName = useSignal("");
  const newSeason = useSignal("Spring");
  const newYear = useSignal(new Date().getFullYear());

  const loadPlans = async () => {
    loading.value = true;
    error.value = "";
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const result = await FloraAPI.getCropPlans();
      plans.value = result ?? [];
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load plans";
    } finally {
      loading.value = false;
    }
  };

  const selectPlan = async (id: string) => {
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const detail = await FloraAPI.getCropPlan(id);
      selectedPlan.value = detail;
    } catch (err) {
      error.value = err instanceof Error ? err.message : "Failed to load plan";
    }
  };

  const createPlan = async () => {
    if (!newPlanName.value.trim()) return;
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      await FloraAPI.createCropPlan({
        seasonYear: newYear.value,
        seasonName: newSeason.value,
        planName: newPlanName.value,
      });
      showCreateForm.value = false;
      newPlanName.value = "";
      await loadPlans();
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to create plan";
    }
  };

  if (loading.value && plans.value.length === 0) {
    loadPlans();
  }

  return (
    <div>
      {error.value && (
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error.value}
          <button
            onClick={() => (error.value = "")}
            class="ml-2 text-red-500 hover:text-red-700"
          >
            ✕
          </button>
        </div>
      )}

      <div class="flex gap-6">
        {/* Plan List */}
        <div class="w-80 flex-shrink-0">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide">
              Plans
            </h3>
            <button
              onClick={() => (showCreateForm.value = !showCreateForm.value)}
              class="px-3 py-1.5 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              + New Plan
            </button>
          </div>

          {showCreateForm.value && (
            <div class="mb-4 p-4 bg-white rounded-xl border border-stone-200 space-y-3">
              <input
                type="text"
                placeholder="Plan name"
                value={newPlanName.value}
                onInput={(
                  e,
                ) => (newPlanName.value = (e.target as HTMLInputElement).value)}
                class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
              <div class="flex gap-2">
                <select
                  value={newSeason.value}
                  onChange={(
                    e,
                  ) => (newSeason.value =
                    (e.target as HTMLSelectElement).value)}
                  class="flex-1 px-3 py-2 text-sm border border-stone-300 rounded-lg"
                >
                  <option value="Spring">Spring</option>
                  <option value="Summer">Summer</option>
                  <option value="Fall">Fall</option>
                  <option value="Year-Round">Year-Round</option>
                </select>
                <input
                  type="number"
                  value={newYear.value}
                  onInput={(
                    e,
                  ) => (newYear.value = +(e.target as HTMLInputElement).value)}
                  class="w-24 px-3 py-2 text-sm border border-stone-300 rounded-lg"
                />
              </div>
              <div class="flex gap-2">
                <button
                  onClick={createPlan}
                  class="flex-1 px-3 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Create
                </button>
                <button
                  onClick={() => (showCreateForm.value = false)}
                  class="px-3 py-2 text-sm text-stone-500 hover:text-stone-700"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {loading.value
            ? (
              <div class="text-center py-8 text-stone-400">
                Loading plans...
              </div>
            )
            : plans.value.length === 0
            ? (
              <div class="text-center py-8 text-stone-400">
                <p class="text-lg">No crop plans yet</p>
                <p class="text-sm mt-1">
                  Create a seasonal plan to track profitability.
                </p>
              </div>
            )
            : (
              <div class="space-y-2">
                {plans.value.map((plan) => {
                  const profit = plan.totalRevenue - plan.totalCosts;
                  return (
                    <button
                      key={plan.id}
                      onClick={() => selectPlan(plan.id)}
                      class={`w-full text-left p-4 rounded-xl border transition-all duration-150 ${
                        selectedPlan.value?.id === plan.id
                          ? "bg-blue-50 border-blue-300 shadow-sm"
                          : "bg-white border-stone-200 hover:border-blue-200 hover:shadow-sm"
                      }`}
                    >
                      <div class="font-semibold text-stone-800">
                        {plan.planName}
                      </div>
                      <div class="text-sm text-stone-500">
                        {plan.seasonName} {plan.seasonYear}
                      </div>
                      <div class="flex justify-between mt-2 text-xs">
                        <span class="text-stone-400">
                          {plan.bedCount} beds · {plan.totalStemsHarvested}{" "}
                          stems
                        </span>
                        <span
                          class={profit >= 0
                            ? "text-emerald-600 font-medium"
                            : "text-red-600 font-medium"}
                        >
                          ${profit.toFixed(0)}
                        </span>
                      </div>
                    </button>
                  );
                })}
              </div>
            )}
        </div>

        {/* Plan Detail */}
        <div class="flex-1">
          {selectedPlan.value
            ? <PlanDetailView plan={selectedPlan.value} />
            : (
              <div class="flex items-center justify-center h-64 text-stone-400 bg-white rounded-xl border border-dashed border-stone-300">
                <p>Select a plan to view financial details</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

function PlanDetailView({ plan }: { plan: CropPlanDetail }) {
  const profit = plan.totalRevenue - plan.totalCosts;

  return (
    <div
      class="bg-white rounded-xl border border-stone-200 p-6"
      style="animation: slideIn 0.2s ease-out"
    >
      <h2 class="text-2xl font-bold text-stone-800">{plan.planName}</h2>
      <p class="text-stone-500 text-sm mb-6">
        {plan.seasonName} {plan.seasonYear}
      </p>

      {/* Financial Summary */}
      <div class="grid grid-cols-4 gap-4 mb-6">
        <FinCard
          label="Revenue"
          value={`$${plan.totalRevenue.toFixed(0)}`}
          color="text-emerald-600"
        />
        <FinCard
          label="Costs"
          value={`$${plan.totalCosts.toFixed(0)}`}
          color="text-red-600"
        />
        <FinCard
          label="Profit"
          value={`$${profit.toFixed(0)}`}
          color={profit >= 0 ? "text-emerald-600" : "text-red-600"}
        />
        <FinCard
          label="Stems"
          value={plan.totalStemsHarvested.toString()}
          color="text-stone-800"
        />
      </div>

      {/* Bed Assignments */}
      {plan.bedAssignments.length > 0 && (
        <div class="mb-6">
          <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-3">
            Bed Assignments
          </h3>
          <div class="space-y-2">
            {plan.bedAssignments.map((a, i) => (
              <div
                key={i}
                class="flex items-center justify-between p-3 bg-stone-50 rounded-lg border border-stone-100"
              >
                <span class="text-stone-700">{a.species} '{a.cultivar}'</span>
                <span class="text-sm text-stone-500">
                  {a.plannedSuccessions} successions
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Revenue by Channel */}
      {plan.revenues.length > 0 && (
        <div>
          <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-3">
            Revenue by Channel
          </h3>
          <div class="space-y-2">
            {plan.revenues.map((r, i) => (
              <div
                key={i}
                class="flex items-center justify-between p-3 bg-stone-50 rounded-lg border border-stone-100"
              >
                <span class="text-stone-700">
                  {CHANNEL_LABELS[r.channel] ?? "Unknown"}
                </span>
                <span class="font-semibold text-emerald-600">
                  ${r.amount.toFixed(2)}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function FinCard(
  { label, value, color }: { label: string; value: string; color: string },
) {
  return (
    <div class="p-4 bg-stone-50 rounded-lg border border-stone-100 text-center">
      <div class="text-xs text-stone-400">{label}</div>
      <div class={`text-xl font-bold mt-1 ${color}`}>{value}</div>
    </div>
  );
}
