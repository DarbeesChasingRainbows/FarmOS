import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import FinancialDashboard from "../../islands/FinancialDashboard.tsx";

export default define.page(function FinancialsPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Financials — Apiary OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Financial Tracking
            </h1>
            <Tooltip text="Track apiary expenses (treatments, feed, equipment) and revenue (honey, wax, nucs) to understand profitability per hive.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Expenses, revenue, and profit/loss for your apiary operations.
          </p>
        </div>
      </div>

      <FinancialDashboard />
    </div>
  );
});
