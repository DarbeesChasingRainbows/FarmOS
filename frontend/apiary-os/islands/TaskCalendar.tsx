import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import type { SeasonalTask } from "../utils/farmos-client.ts";

const MONTH_NAMES = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

const PRIORITY_STYLES: Record<
  string,
  { bg: string; text: string; dot: string }
> = {
  High: {
    bg: "bg-red-50 border-red-200",
    text: "text-red-700",
    dot: "bg-red-500",
  },
  Medium: {
    bg: "bg-amber-50 border-amber-200",
    text: "text-amber-700",
    dot: "bg-amber-500",
  },
  Low: {
    bg: "bg-emerald-50 border-emerald-200",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
};

const CATEGORY_ICONS: Record<string, string> = {
  Inspection: "🔍",
  Treatment: "💊",
  Harvest: "🍯",
  Feeding: "🥄",
  Equipment: "🔧",
  General: "📋",
};

export default function TaskCalendar() {
  const tasks = useSignal<SeasonalTask[]>([]);
  const selectedMonth = useSignal<number | null>(null);
  const loading = useSignal(false);
  const error = useSignal<string | null>(null);

  const loadTasks = async (month?: number) => {
    loading.value = true;
    error.value = null;
    try {
      const { ApiaryReportsAPI } = await import("../utils/farmos-client.ts");
      tasks.value = (await ApiaryReportsAPI.getCalendar(month)) ?? [];
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : "Failed to load tasks";
    } finally {
      loading.value = false;
    }
  };

  useEffect(() => {
    // Start with current month highlighted
    const now = new Date();
    selectedMonth.value = now.getMonth() + 1;
    loadTasks();
  }, []);

  const selectMonth = (month: number | null) => {
    selectedMonth.value = month;
    loadTasks(month ?? undefined);
  };

  const filteredTasks = selectedMonth.value
    ? tasks.value.filter((t) => t.month === selectedMonth.value)
    : tasks.value;

  // Group tasks by month for display
  const tasksByMonth = new Map<number, SeasonalTask[]>();
  for (const task of filteredTasks) {
    const list = tasksByMonth.get(task.month) ?? [];
    list.push(task);
    tasksByMonth.set(task.month, list);
  }

  return (
    <div>
      {/* Month Selector */}
      <div class="flex flex-wrap gap-2 mb-6">
        <button
          type="button"
          onClick={() => selectMonth(null)}
          class={`px-3 py-1.5 rounded-lg text-sm font-medium transition ${
            selectedMonth.value === null
              ? "bg-stone-800 text-white"
              : "bg-stone-100 text-stone-600 hover:bg-stone-200"
          }`}
        >
          All Months
        </button>
        {MONTH_NAMES.map((name, i) => {
          const month = i + 1;
          const isCurrentMonth = new Date().getMonth() + 1 === month;
          return (
            <button
              type="button"
              key={month}
              onClick={() => selectMonth(month)}
              class={`px-3 py-1.5 rounded-lg text-sm font-medium transition ${
                selectedMonth.value === month
                  ? "bg-amber-500 text-white"
                  : isCurrentMonth
                  ? "bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100"
                  : "bg-stone-100 text-stone-600 hover:bg-stone-200"
              }`}
            >
              {name.slice(0, 3)}
            </button>
          );
        })}
      </div>

      {/* Error */}
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

      {/* Tasks */}
      {!loading.value && (
        <div class="space-y-8">
          {Array.from(tasksByMonth.entries())
            .sort(([a], [b]) => a - b)
            .map(([month, monthTasks]) => (
              <div key={month}>
                <h3 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
                  <span class="w-8 h-8 rounded-full bg-amber-100 text-amber-700 flex items-center justify-center text-sm font-bold">
                    {month}
                  </span>
                  {MONTH_NAMES[month - 1]}
                </h3>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                  {monthTasks.map((task, i) => {
                    const style = PRIORITY_STYLES[task.priority] ??
                      PRIORITY_STYLES.Low;
                    const icon = CATEGORY_ICONS[task.category] ?? "📋";
                    return (
                      <div
                        key={i}
                        class={`rounded-xl border p-4 ${style.bg}`}
                      >
                        <div class="flex items-start gap-3">
                          <span class="text-xl flex-shrink-0 mt-0.5">
                            {icon}
                          </span>
                          <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 mb-1">
                              <h4 class={`font-bold text-sm ${style.text}`}>
                                {task.title}
                              </h4>
                              <span
                                class={`w-2 h-2 rounded-full ${style.dot} flex-shrink-0`}
                              />
                            </div>
                            <p class="text-xs text-stone-600 leading-relaxed">
                              {task.description}
                            </p>
                            <div class="flex items-center gap-2 mt-2">
                              <span class="text-xs bg-white/70 px-2 py-0.5 rounded-md font-medium text-stone-500">
                                {task.category}
                              </span>
                              <span
                                class={`text-xs font-semibold ${style.text}`}
                              >
                                {task.priority}
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}

          {filteredTasks.length === 0 && (
            <div class="bg-stone-50 border border-stone-200 rounded-xl p-12 text-center">
              <p class="text-lg font-medium text-stone-600 mb-2">
                No tasks for this period
              </p>
              <p class="text-sm text-stone-500">
                Select a different month or view all months.
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
