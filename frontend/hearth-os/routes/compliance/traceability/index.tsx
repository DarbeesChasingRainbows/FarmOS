import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import TraceabilityDashboard from "../../../islands/TraceabilityDashboard.tsx";

export default define.page(function TraceabilityPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Traceability & FSMA 204 — Hearth OS</title>
      </Head>

      <div class="mb-8 flex justify-between items-end">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Traceability & FSMA 204
          </h1>
          <p class="text-stone-500 mt-1">
            Log Critical Tracking Events (CTEs) and export Key Data Elements (KDEs) to comply with FDA mandates.
          </p>
        </div>
      </div>

      <TraceabilityDashboard />
    </div>
  );
});
