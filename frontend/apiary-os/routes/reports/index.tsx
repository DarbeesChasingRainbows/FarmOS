import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowReportsDashboard from "../../islands/ArrowReportsDashboard.tsx";

export default define.page(function ReportsPage() {
  return (
    <div>
      <Head>
        <title>Reports — Apiary OS</title>
      </Head>
      <ArrowReportsDashboard />
    </div>
  );
});
