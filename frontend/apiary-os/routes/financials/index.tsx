import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowFinancialDashboard from "../../islands/ArrowFinancialDashboard.tsx";

export default define.page(function FinancialsPage() {
  return (
    <div>
      <Head>
        <title>Financials — Apiary OS</title>
      </Head>
      <ArrowFinancialDashboard />
    </div>
  );
});
