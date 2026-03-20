import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import ReportsDashboard from "../../islands/ReportsDashboard.tsx";

export default define.page(function ReportsPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Reports — Apiary OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Reports & Analytics
            </h1>
            <Tooltip text="Analyze mite trends, honey yields, colony survival rates, and weather correlations across your apiary.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Data-driven insights from your hive inspections and harvests.
          </p>
        </div>
      </div>

      <ReportsDashboard />
    </div>
  );
});
