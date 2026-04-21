import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import type {
  ColonySurvivalReport,
  MiteTrendPoint,
  WeatherCorrelation,
  YieldReport,
} from "../utils/farmos-client.ts";

type Tab = "mites" | "yield" | "survival" | "weather";

export default function ReportsDashboard() {
  const activeTab = useSignal<Tab>("mites");
  const loading = useSignal(false);
  const error = useSignal<string | null>(null);

  // Data signals
  const miteTrends = useSignal<MiteTrendPoint[]>([]);
  const yieldReport = useSignal<YieldReport | null>(null);
  const survivalReport = useSignal<ColonySurvivalReport | null>(null);
  const weatherData = useSignal<WeatherCorrelation[]>([]);

  const loadData = async (tab: Tab) => {
    loading.value = true;
    error.value = null;
    try {
      const { ApiaryReportsAPI } = await import("../utils/farmos-client.ts");
      switch (tab) {
        case "mites":
          miteTrends.value = (await ApiaryReportsAPI.getMiteTrends()) ?? [];
          break;
        case "yield":
          yieldReport.value = await ApiaryReportsAPI.getYieldReport();
          break;
        case "survival":
          survivalReport.value = await ApiaryReportsAPI.getSurvivalReport();
          break;
        case "weather":
          weatherData.value = (await ApiaryReportsAPI.getWeatherCorrelations()) ?? [];
          break;
      }
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : "Failed to load data";
    } finally {
      loading.value = false;
    }
  };

  useEffect(() => {
    loadData(activeTab.value);
  }, []);

  const switchTab = (tab: Tab) => {
    activeTab.value = tab;
    loadData(tab);
  };

  const tabs: { key: Tab; label: string; icon: string }[] = [
    { key: "mites", label: "Mite Trends", icon: "🔬" },
    { key: "yield", label: "Yield Report", icon: "🍯" },
    { key: "survival", label: "Colony Survival", icon: "📈" },
    { key: "weather", label: "Weather Correlation", icon: "🌤️" },
  ];

  return (
    <div>
      {/* Tab Bar */}
      <div class="flex gap-1 bg-stone-100 rounded-xl p-1 mb-6">
        {tabs.map((tab) => (
          <button
            type="button"
            key={tab.key}
            onClick={() => switchTab(tab.key)}
            class={`flex-1 py-2.5 px-4 rounded-lg text-sm font-semibold transition flex items-center justify-center gap-2 ${
              activeTab.value === tab.key
                ? "bg-white text-stone-800 shadow-sm"
                : "text-stone-500 hover:text-stone-700"
            }`}
          >
            <span>{tab.icon}</span>
            {tab.label}
          </button>
        ))}
      </div>

      {/* Error Banner */}
      {error.value && (
        <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">
          {error.value}
        </div>
      )}

      {/* Loading */}
      {loading.value && (
        <div class="flex items-center justify-center py-16">
          <div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full" />
        </div>
      )}

      {/* Content */}
      {!loading.value && (
        <div>
          {activeTab.value === "mites" && (
            <MiteTrendsView data={miteTrends.value} />
          )}
          {activeTab.value === "yield" && (
            <YieldView data={yieldReport.value} />
          )}
          {activeTab.value === "survival" && (
            <SurvivalView data={survivalReport.value} />
          )}
          {activeTab.value === "weather" && (
            <WeatherView data={weatherData.value} />
          )}
        </div>
      )}
    </div>
  );
}

// ─── Mite Trends ──────────────────────────────────────────────────────

function MiteTrendsView({ data }: { data: MiteTrendPoint[] }) {
  if (data.length === 0) {
    return (
      <EmptyState
        title="No mite data yet"
        message="Mite counts will appear here after you log inspections with mite wash or sugar roll counts."
      />
    );
  }

  // Group by hive for simple table display
  const byHive = new Map<string, MiteTrendPoint[]>();
  for (const point of data) {
    const list = byHive.get(point.hiveId) ?? [];
    list.push(point);
    byHive.set(point.hiveId, list);
  }

  return (
    <div class="space-y-6">
      {/* Summary bar chart (CSS-only) */}
      <div class="bg-white rounded-xl border border-stone-200 p-6">
        <h3 class="text-lg font-bold text-stone-800 mb-4">
          Latest Mite Counts by Hive
        </h3>
        <div class="space-y-3">
          {Array.from(byHive.entries()).map(([hiveId, points]) => {
            const latest = points[points.length - 1];
            const mites = latest.miteCount;
            const barColor = mites <= 1
              ? "bg-emerald-500"
              : mites <= 3
              ? "bg-amber-500"
              : "bg-red-500";
            const barWidth = Math.min(mites / 10 * 100, 100);
            return (
              <div key={hiveId}>
                <div class="flex justify-between text-sm mb-1">
                  <span class="font-medium text-stone-700">
                    {latest.hiveName || hiveId}
                  </span>
                  <span class="font-bold">{mites}/100 bees</span>
                </div>
                <div class="w-full bg-stone-100 rounded-full h-3">
                  <div
                    class={`${barColor} h-3 rounded-full transition-all`}
                    style={{ width: `${barWidth}%` }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Data table */}
      <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-stone-50 border-b border-stone-200">
            <tr>
              <th class="text-left px-4 py-3 font-semibold text-stone-600">
                Date
              </th>
              <th class="text-left px-4 py-3 font-semibold text-stone-600">
                Hive
              </th>
              <th class="text-right px-4 py-3 font-semibold text-stone-600">
                Mites/100
              </th>
              <th class="text-right px-4 py-3 font-semibold text-stone-600">
                Status
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-stone-100">
            {data.slice(-20).reverse().map((point, i) => {
              const level = point.miteCount <= 1
                ? "Low"
                : point.miteCount <= 3
                ? "Moderate"
                : "High";
              const levelColor = point.miteCount <= 1
                ? "text-emerald-600"
                : point.miteCount <= 3
                ? "text-amber-600"
                : "text-red-600";
              return (
                <tr key={i} class="hover:bg-stone-50">
                  <td class="px-4 py-3 text-stone-700">{point.date}</td>
                  <td class="px-4 py-3 text-stone-700 font-medium">
                    {point.hiveName || point.hiveId}
                  </td>
                  <td class="px-4 py-3 text-right font-bold">
                    {point.miteCount}
                  </td>
                  <td
                    class={`px-4 py-3 text-right font-semibold ${levelColor}`}
                  >
                    {level}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ─── Yield Report ─────────────────────────────────────────────────────

function YieldView({ data }: { data: YieldReport | null }) {
  if (!data) {
    return (
      <EmptyState
        title="No harvest data yet"
        message="Yield reports will populate after you record honey or product harvests."
      />
    );
  }

  return (
    <div class="space-y-6">
      {/* Summary Cards */}
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <StatCard
          label="Total Honey"
          value={`${data.totalHoneyLbs.toFixed(1)} lbs`}
          icon="🍯"
          color="amber"
        />
        <StatCard
          label="Total Wax"
          value={`${data.totalWaxLbs.toFixed(1)} lbs`}
          icon="🕯️"
          color="yellow"
        />
        <StatCard
          label="Harvests"
          value={String(data.harvestCount)}
          icon="📦"
          color="stone"
        />
        <StatCard
          label="Year"
          value={String(data.year)}
          icon="📅"
          color="violet"
        />
      </div>

      {/* By-Product Breakdown */}
      {data.byProduct && Object.keys(data.byProduct).length > 0 && (
        <div class="bg-white rounded-xl border border-stone-200 p-6">
          <h3 class="text-lg font-bold text-stone-800 mb-4">
            Yield by Product
          </h3>
          <div class="space-y-3">
            {Object.entries(data.byProduct).map(([product, amount]) => (
              <div
                key={product}
                class="flex items-center justify-between py-2 border-b border-stone-50 last:border-0"
              >
                <span class="font-medium text-stone-700 capitalize">
                  {product}
                </span>
                <span class="font-bold text-stone-800">
                  {amount.toFixed(1)} lbs
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Colony Survival ──────────────────────────────────────────────────

function SurvivalView({ data }: { data: ColonySurvivalReport | null }) {
  if (!data) {
    return (
      <EmptyState
        title="No colony data yet"
        message="Survival statistics require at least one created hive."
      />
    );
  }

  const rate = data.survivalRate * 100;
  const rateColor = rate >= 80
    ? "text-emerald-600"
    : rate >= 50
    ? "text-amber-600"
    : "text-red-600";

  return (
    <div class="space-y-6">
      {/* Big Survival Rate */}
      <div class="bg-white rounded-xl border border-stone-200 p-8 text-center">
        <p class={`text-6xl font-extrabold ${rateColor}`}>{rate.toFixed(0)}%</p>
        <p class="text-stone-500 mt-2 text-lg">Colony Survival Rate</p>
        <p class="text-stone-400 text-sm mt-1">
          {data.currentlyActive} active of {data.totalCreated} total colonies
        </p>
      </div>

      {/* Breakdown Cards */}
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          label="Total Created"
          value={String(data.totalCreated)}
          icon="🏠"
          color="stone"
        />
        <StatCard
          label="Active"
          value={String(data.currentlyActive)}
          icon="✅"
          color="emerald"
        />
        <StatCard
          label="Dead"
          value={String(data.dead)}
          icon="💀"
          color="red"
        />
        <StatCard
          label="Swarmed"
          value={String(data.swarmed)}
          icon="🐝"
          color="amber"
        />
      </div>
    </div>
  );
}

// ─── Weather Correlation ──────────────────────────────────────────────

function WeatherView({ data }: { data: WeatherCorrelation[] }) {
  if (data.length === 0) {
    return (
      <EmptyState
        title="No weather data yet"
        message="Weather correlations appear when inspections include weather snapshots. Configure a weather API integration to enable this feature."
      />
    );
  }

  return (
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <div class="p-4 border-b border-stone-200">
        <h3 class="text-lg font-bold text-stone-800">
          Weather × Inspection Correlation
        </h3>
        <p class="text-sm text-stone-500">
          How temperature and humidity relate to mite counts and honey frames.
        </p>
      </div>
      <table class="w-full text-sm">
        <thead class="bg-stone-50 border-b border-stone-200">
          <tr>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Date
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Temp (°F)
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Humidity
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Mites/100
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Honey Frames
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-stone-100">
          {data.map((row, i) => (
            <tr key={i} class="hover:bg-stone-50">
              <td class="px-4 py-3 text-stone-700">{row.date}</td>
              <td class="px-4 py-3 text-right">{row.tempF}°</td>
              <td class="px-4 py-3 text-right">{row.humidity}%</td>
              <td class="px-4 py-3 text-right font-bold">
                {row.miteCount ?? "—"}
              </td>
              <td class="px-4 py-3 text-right font-bold">{row.honeyFrames}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─── Shared Components ────────────────────────────────────────────────

function StatCard(
  { label, value, icon, color }: {
    label: string;
    value: string;
    icon: string;
    color: string;
  },
) {
  const bgMap: Record<string, string> = {
    amber: "bg-amber-50 border-amber-100",
    yellow: "bg-yellow-50 border-yellow-100",
    emerald: "bg-emerald-50 border-emerald-100",
    red: "bg-red-50 border-red-100",
    stone: "bg-stone-50 border-stone-100",
    violet: "bg-violet-50 border-violet-100",
  };

  return (
    <div
      class={`rounded-xl border p-5 text-center ${bgMap[color] ?? bgMap.stone}`}
    >
      <p class="text-2xl mb-1">{icon}</p>
      <p class="text-2xl font-extrabold text-stone-800">{value}</p>
      <p class="text-xs text-stone-500 uppercase tracking-wider mt-1 font-semibold">
        {label}
      </p>
    </div>
  );
}

function EmptyState({ title, message }: { title: string; message: string }) {
  return (
    <div class="bg-stone-50 border border-stone-200 rounded-xl p-12 text-center">
      <p class="text-lg font-medium text-stone-600 mb-2">{title}</p>
      <p class="text-sm text-stone-500 max-w-md mx-auto">{message}</p>
    </div>
  );
}
