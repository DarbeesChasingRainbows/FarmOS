import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
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

const MONTH_SHORT = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

const PRIORITY_STYLES: Record<
  string,
  { bg: string; text: string; dot: string; border: string }
> = {
  High: {
    bg: "bg-red-50",
    text: "text-red-700",
    dot: "bg-red-500",
    border: "border-red-200",
  },
  Medium: {
    bg: "bg-amber-50",
    text: "text-amber-700",
    dot: "bg-amber-500",
    border: "border-amber-200",
  },
  Low: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
    border: "border-emerald-200",
  },
};

const CATEGORY_ICONS: Record<string, string> = {
  Inspection: "\uD83D\uDD0D",
  Treatment: "\uD83D\uDC8A",
  Harvest: "\uD83C\uDF6F",
  Feeding: "\uD83E\uDD44",
  Equipment: "\uD83D\uDD27",
  General: "\uD83D\uDCCB",
};

export default function ArrowTaskCalendar() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const currentMonth = new Date().getMonth() + 1;

    const state = reactive({
      tasks: [] as SeasonalTask[],
      selectedMonth: currentMonth,
      loading: true,
      error: null as string | null,
    });

    const loadTasks = async (month?: number) => {
      state.loading = true;
      state.error = null;
      try {
        const { ApiaryReportsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        state.tasks = (await ApiaryReportsAPI.getCalendar(month)) ?? [];
      } catch (err: unknown) {
        state.error = err instanceof Error
          ? err.message
          : "Failed to load tasks";
      } finally {
        state.loading = false;
      }
    };

    loadTasks(currentMonth);

    const selectMonth = (month: number) => {
      state.selectedMonth = month;
      loadTasks(month);
    };

    const filteredTasks = () =>
      state.tasks.filter((t) => t.month === state.selectedMonth);

    const taskCount = () => filteredTasks().length;

    // Sort by priority: High first
    const sortedTasks = () => {
      const order: Record<string, number> = {
        High: 0,
        Medium: 1,
        Low: 2,
      };
      return [...filteredTasks()].sort(
        (a, b) => (order[a.priority] ?? 2) - (order[b.priority] ?? 2),
      );
    };

    // Month strip button
    const monthBtn = (index: number) => {
      const month = index + 1;
      const isCurrent = currentMonth === month;
      return html`
        <button
          type="button"
          @click="${() => selectMonth(month)}"
          class="${() => {
            if (state.selectedMonth === month) {
              return "bg-amber-500 text-white shadow-md";
            }
            if (isCurrent) {
              return "bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100";
            }
            return "bg-stone-100 text-stone-600 hover:bg-stone-200";
          }} px-3 py-2 rounded-xl text-sm font-semibold transition-all min-w-[48px] text-center"
        >
          ${MONTH_SHORT[index]}
        </button>
      `;
    };

    // Task card
    const taskCard = (task: SeasonalTask) => {
      const style = PRIORITY_STYLES[task.priority] || PRIORITY_STYLES.Low;
      const icon = CATEGORY_ICONS[task.category] || "\uD83D\uDCCB";
      return html`
        <div
          class="${style.bg} ${style
            .border} border rounded-2xl p-5 hover:shadow-sm transition-shadow"
        >
          <div class="flex items-start gap-3">
            <span class="text-xl flex-shrink-0 mt-0.5">${icon}</span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <h4 class="font-bold text-sm ${style.text}">
                  ${task.title}
                </h4>
                <span
                  class="w-2 h-2 rounded-full ${style.dot} flex-shrink-0"
                ></span>
              </div>
              <p class="text-xs text-stone-600 leading-relaxed">
                ${task.description}
              </p>
              <div class="flex items-center gap-2 mt-3">
                <span
                  class="text-xs bg-white/70 px-2 py-0.5 rounded-md font-medium text-stone-500 border border-stone-200/50"
                >
                  ${task.category}
                </span>
                <span class="text-xs font-semibold ${style.text}">
                  ${task.priority}
                </span>
              </div>
            </div>
          </div>
        </div>
      `;
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1
            class="text-3xl font-extrabold text-stone-800 tracking-tight"
          >
            Seasonal Calendar
          </h1>
          <p class="text-stone-500 mt-1">
            Monthly task templates for inspections, treatments, and harvest windows.
          </p>
        </header>

        <!-- Month Strip -->
        <div
          class="flex gap-2 mb-6 overflow-x-auto pb-2 scrollbar-thin"
        >
          ${Array.from({ length: 12 }, (_, i) => monthBtn(i))}
        </div>

        <!-- Selected Month Header -->
        <div class="flex items-center gap-3 mb-6">
          <span
            class="w-10 h-10 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center text-sm font-bold"
          >
            ${() => state.selectedMonth}
          </span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">
              ${() => MONTH_NAMES[state.selectedMonth - 1]}
            </h2>
            <p class="text-xs text-stone-400">
              ${taskCount} tasks scheduled
            </p>
          </div>
        </div>

        ${() =>
          state.error
            ? html`
              <div
                class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm"
              >
                ${state.error}
              </div>
            `
            : html`

            `} ${() =>
          state.loading
            ? html`
              <div class="flex items-center justify-center py-16">
                <div
                  class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-amber-500 rounded-full"
                >
                </div>
              </div>
            `
            : html`
              ${() =>
                sortedTasks().length === 0
                  ? html`
                    <div
                      class="bg-stone-50 border border-stone-200 rounded-2xl p-12 text-center"
                    >
                      <p
                        class="text-lg font-medium text-stone-600 mb-2"
                      >
                        No tasks for this month
                      </p>
                      <p class="text-sm text-stone-500">
                        Select a different month to see scheduled tasks.
                      </p>
                    </div>
                  `
                  : html`
                    <div
                      class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                    >
                      ${() => sortedTasks().map((task) => taskCard(task))}
                    </div>
                  `}
            `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
