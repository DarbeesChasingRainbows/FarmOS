import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import type {
  ExpenseEntry,
  FinancialSummary,
  RevenueEntry,
} from "../utils/farmos-client.ts";

export default function ArrowFinancialDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      summary: null as FinancialSummary | null,
      expenses: [] as ExpenseEntry[],
      revenue: [] as RevenueEntry[],
      loading: true,
    });

    const loadData = async () => {
      try {
        const { ApiaryFinancialsAPI } = await import(
          "../utils/farmos-client.ts"
        );
        const [summary, expenses, revenue] = await Promise.allSettled([
          ApiaryFinancialsAPI.getSummary(),
          ApiaryFinancialsAPI.getExpenses(),
          ApiaryFinancialsAPI.getRevenue(),
        ]);
        state.summary = summary.status === "fulfilled" ? summary.value : null;
        state.expenses = expenses.status === "fulfilled" ? (expenses.value ?? []) : [];
        state.revenue = revenue.status === "fulfilled" ? (revenue.value ?? []) : [];
      } catch {
        // silent
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const fmt = (n: number) => "$" + n.toFixed(2);

    // Group expenses by category
    const expenseBreakdown = () => {
      const groups: Record<string, number> = {};
      for (const e of state.expenses) {
        groups[e.category] = (groups[e.category] || 0) + e.amount;
      }
      return Object.entries(groups).sort(([, a], [, b]) => b - a);
    };

    // Group revenue by product
    const revenueBreakdown = () => {
      const groups: Record<string, number> = {};
      for (const r of state.revenue) {
        groups[r.product] = (groups[r.product] || 0) + r.amount;
      }
      return Object.entries(groups).sort(([, a], [, b]) => b - a);
    };

    // Combined transaction list
    const allTransactions = () => {
      type Tx = {
        date: string;
        description: string;
        amount: number;
        category: string;
        type: "expense" | "revenue";
      };
      const txs: Tx[] = [
        ...state.expenses.map((e) => ({
          date: e.date,
          description: e.description,
          amount: -e.amount,
          category: e.category,
          type: "expense" as const,
        })),
        ...state.revenue.map((r) => ({
          date: r.date,
          description: r.description,
          amount: r.amount,
          category: r.product,
          type: "revenue" as const,
        })),
      ];
      txs.sort(
        (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
      );
      return txs.slice(0, 10);
    };

    // Horizontal bar chart
    const barChart = (
      entries: [string, number][],
      color: string,
    ) => {
      const total = entries.reduce((s, [, v]) => s + v, 0);
      return html`
        <div class="space-y-2">
          ${entries.map(([name, val]) => {
            const pct = total > 0 ? Math.round((val / total) * 100) : 0;
            return html`
              <div>
                <div
                  class="flex items-center justify-between text-xs text-stone-600 mb-0.5"
                >
                  <span>${name}</span>
                  <span class="font-bold">${fmt(val)} (${pct}%)</span>
                </div>
                <div class="bg-stone-100 rounded-full h-2">
                  <div
                    class="${color} h-2 rounded-full transition-all"
                    style="width: ${pct}%"
                  >
                  </div>
                </div>
              </div>
            `;
          })}
        </div>
      `;
    };

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <header class="mb-8">
          <h1
            class="text-3xl font-extrabold text-stone-800 tracking-tight"
          >
            Financial Tracking
          </h1>
          <p class="text-stone-500 mt-1">
            Expenses, revenue, and profitability.
          </p>
        </header>

        ${() =>
          state.loading
            ? html`
              <div class="flex items-center justify-center py-20">
                <div
                  class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-emerald-500 rounded-full"
                >
                </div>
              </div>
            `
            : html`
              <div>
                <!-- KPI Row -->
                <div
                  class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6"
                >
                  ${ArrowKPICard({
                    label: "Total Expenses",
                    value: () =>
                      state.summary
                        ? fmt(state.summary.totalExpenses)
                        : "\u2014",
                    icon: "\uD83D\uDCB8",
                    color: "red",
                  })} ${ArrowKPICard({
                    label: "Total Revenue",
                    value: () =>
                      state.summary
                        ? fmt(state.summary.totalRevenue)
                        : "\u2014",
                    icon: "\uD83D\uDCB0",
                    color: "emerald",
                  })} ${ArrowKPICard({
                    label: "Net Profit",
                    value: () =>
                      state.summary
                        ? (state.summary.netProfit >= 0 ? "+" : "") +
                          fmt(state.summary.netProfit)
                        : "\u2014",
                    icon: "\uD83D\uDCCA",
                    color: state.summary && state.summary.netProfit >= 0
                      ? "emerald"
                      : "red",
                  })}
                </div>

                <!-- Breakdown Row -->
                <div
                  class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6"
                >
                  <div
                    class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                  >
                    <h2
                      class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                    >
                      Expense Breakdown
                    </h2>
                    ${() => {
                      const entries = expenseBreakdown();
                      return entries.length === 0
                        ? html`
                          <p class="text-sm text-stone-400">
                            No expenses recorded.
                          </p>
                        `
                        : barChart(entries, "bg-red-400");
                    }}
                  </div>
                  <div
                    class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                  >
                    <h2
                      class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                    >
                      Revenue by Product
                    </h2>
                    ${() => {
                      const entries = revenueBreakdown();
                      return entries.length === 0
                        ? html`
                          <p class="text-sm text-stone-400">
                            No revenue recorded.
                          </p>
                        `
                        : barChart(entries, "bg-emerald-400");
                    }}
                  </div>
                </div>

                <!-- Combined Transactions -->
                <div
                  class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6"
                >
                  <h2
                    class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                  >
                    Recent Transactions
                  </h2>
                  ${() => {
                    const txs = allTransactions();
                    return txs.length === 0
                      ? html`
                        <p class="text-sm text-stone-400">
                          No transactions yet.
                        </p>
                      `
                      : html`
                        <div class="overflow-x-auto">
                          <table class="w-full text-sm">
                            <thead>
                              <tr class="border-b border-stone-100">
                                <th
                                  class="text-left py-2 text-xs text-stone-500 font-medium"
                                >
                                  Date
                                </th>
                                <th
                                  class="text-left py-2 text-xs text-stone-500 font-medium"
                                >
                                  Category
                                </th>
                                <th
                                  class="text-left py-2 text-xs text-stone-500 font-medium"
                                >
                                  Description
                                </th>
                                <th
                                  class="text-right py-2 text-xs text-stone-500 font-medium"
                                >
                                  Amount
                                </th>
                              </tr>
                            </thead>
                            <tbody>
                              ${txs.map(
                                (tx) =>
                                  html`
                                    <tr
                                      class="border-b border-stone-50 hover:bg-stone-50"
                                    >
                                      <td
                                        class="py-2.5 text-stone-700"
                                      >
                                        ${tx.date}
                                      </td>
                                      <td class="py-2.5">
                                        <span
                                          class="${tx.type ===
                                              "revenue"
                                            ? "bg-emerald-50 text-emerald-700"
                                            : "bg-red-50 text-red-700"} text-xs px-2 py-0.5 rounded-md font-medium"
                                        >${tx.category}</span>
                                      </td>
                                      <td
                                        class="py-2.5 text-stone-700"
                                      >
                                        ${tx.description}
                                      </td>
                                      <td
                                        class="py-2.5 text-right font-bold ${tx
                                            .amount >=
                                            0
                                          ? "text-emerald-600"
                                          : "text-red-600"}"
                                      >
                                        ${tx.amount >= 0 ? "+" : ""}${fmt(
                                          Math.abs(tx.amount),
                                        )}
                                      </td>
                                    </tr>
                                  `,
                              )}
                            </tbody>
                          </table>
                        </div>
                      `;
                  }}
                </div>
              </div>
            `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
