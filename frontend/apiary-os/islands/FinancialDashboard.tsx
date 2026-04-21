import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import type {
  ExpenseEntry,
  FinancialSummary,
  RevenueEntry,
} from "../utils/farmos-client.ts";

type Tab = "summary" | "expenses" | "revenue";

export default function FinancialDashboard() {
  const activeTab = useSignal<Tab>("summary");
  const loading = useSignal(false);
  const error = useSignal<string | null>(null);

  const summary = useSignal<FinancialSummary | null>(null);
  const expenses = useSignal<ExpenseEntry[]>([]);
  const revenue = useSignal<RevenueEntry[]>([]);

  const loadData = async (tab: Tab) => {
    loading.value = true;
    error.value = null;
    try {
      const { ApiaryFinancialsAPI } = await import("../utils/farmos-client.ts");
      switch (tab) {
        case "summary":
          summary.value = await ApiaryFinancialsAPI.getSummary();
          break;
        case "expenses":
          expenses.value = (await ApiaryFinancialsAPI.getExpenses()) ?? [];
          break;
        case "revenue":
          revenue.value = (await ApiaryFinancialsAPI.getRevenue()) ?? [];
          break;
      }
    } catch (err: unknown) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to load financial data";
    } finally {
      loading.value = false;
    }
  };

  useEffect(() => {
    loadData("summary");
  }, []);

  const switchTab = (tab: Tab) => {
    activeTab.value = tab;
    loadData(tab);
  };

  const tabs: { key: Tab; label: string; icon: string }[] = [
    { key: "summary", label: "Profit & Loss", icon: "📊" },
    { key: "expenses", label: "Expenses", icon: "💸" },
    { key: "revenue", label: "Revenue", icon: "💰" },
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

      {error.value && (
        <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">
          {error.value}
        </div>
      )}

      {loading.value && (
        <div class="flex items-center justify-center py-16">
          <div class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-emerald-500 rounded-full" />
        </div>
      )}

      {!loading.value && (
        <div>
          {activeTab.value === "summary" && (
            <SummaryView data={summary.value} />
          )}
          {activeTab.value === "expenses" && (
            <ExpensesView data={expenses.value} />
          )}
          {activeTab.value === "revenue" && (
            <RevenueView data={revenue.value} />
          )}
        </div>
      )}
    </div>
  );
}

function SummaryView({ data }: { data: FinancialSummary | null }) {
  if (!data) {
    return (
      <div class="bg-stone-50 border border-stone-200 rounded-xl p-12 text-center">
        <p class="text-lg font-medium text-stone-600 mb-2">
          No financial data yet
        </p>
        <p class="text-sm text-stone-500">
          Financial tracking integrates with the Ledger bounded context. Record
          expenses and revenue to see your profit/loss here.
        </p>
      </div>
    );
  }

  const profitColor = data.netProfit >= 0 ? "text-emerald-600" : "text-red-600";

  return (
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="bg-red-50 border border-red-100 rounded-xl p-6 text-center">
        <p class="text-3xl font-extrabold text-red-700">
          ${data.totalExpenses.toFixed(2)}
        </p>
        <p class="text-sm text-red-600 font-semibold mt-1 uppercase tracking-wider">
          Total Expenses
        </p>
      </div>
      <div class="bg-emerald-50 border border-emerald-100 rounded-xl p-6 text-center">
        <p class="text-3xl font-extrabold text-emerald-700">
          ${data.totalRevenue.toFixed(2)}
        </p>
        <p class="text-sm text-emerald-600 font-semibold mt-1 uppercase tracking-wider">
          Total Revenue
        </p>
      </div>
      <div class="bg-white border border-stone-200 rounded-xl p-6 text-center">
        <p class={`text-3xl font-extrabold ${profitColor}`}>
          {data.netProfit >= 0 ? "+" : ""}${data.netProfit.toFixed(2)}
        </p>
        <p class="text-sm text-stone-600 font-semibold mt-1 uppercase tracking-wider">
          Net Profit
        </p>
      </div>
    </div>
  );
}

function ExpensesView({ data }: { data: ExpenseEntry[] }) {
  if (data.length === 0) {
    return (
      <div class="bg-stone-50 border border-stone-200 rounded-xl p-12 text-center">
        <p class="text-lg font-medium text-stone-600 mb-2">
          No expenses recorded
        </p>
        <p class="text-sm text-stone-500">
          Expenses from treatments, equipment, and feed will appear here.
        </p>
      </div>
    );
  }

  return (
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-stone-50 border-b border-stone-200">
          <tr>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Date
            </th>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Category
            </th>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Description
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Amount
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-stone-100">
          {data.map((entry, i) => (
            <tr key={i} class="hover:bg-stone-50">
              <td class="px-4 py-3 text-stone-700">{entry.date}</td>
              <td class="px-4 py-3">
                <span class="bg-red-50 text-red-700 text-xs px-2 py-0.5 rounded-md font-medium">
                  {entry.category}
                </span>
              </td>
              <td class="px-4 py-3 text-stone-700">{entry.description}</td>
              <td class="px-4 py-3 text-right font-bold text-red-600">
                ${entry.amount.toFixed(2)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RevenueView({ data }: { data: RevenueEntry[] }) {
  if (data.length === 0) {
    return (
      <div class="bg-stone-50 border border-stone-200 rounded-xl p-12 text-center">
        <p class="text-lg font-medium text-stone-600 mb-2">
          No revenue recorded
        </p>
        <p class="text-sm text-stone-500">
          Revenue from honey, wax, and nuc sales will appear here.
        </p>
      </div>
    );
  }

  return (
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-stone-50 border-b border-stone-200">
          <tr>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Date
            </th>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Product
            </th>
            <th class="text-left px-4 py-3 font-semibold text-stone-600">
              Description
            </th>
            <th class="text-right px-4 py-3 font-semibold text-stone-600">
              Amount
            </th>
          </tr>
        </thead>
        <tbody class="divide-y divide-stone-100">
          {data.map((entry, i) => (
            <tr key={i} class="hover:bg-stone-50">
              <td class="px-4 py-3 text-stone-700">{entry.date}</td>
              <td class="px-4 py-3">
                <span class="bg-emerald-50 text-emerald-700 text-xs px-2 py-0.5 rounded-md font-medium">
                  {entry.product}
                </span>
              </td>
              <td class="px-4 py-3 text-stone-700">{entry.description}</td>
              <td class="px-4 py-3 text-right font-bold text-emerald-600">
                +${entry.amount.toFixed(2)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
