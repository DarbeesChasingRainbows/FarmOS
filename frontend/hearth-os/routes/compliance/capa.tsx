import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import CAPADashboard from "../../islands/CAPADashboard.tsx";

export default define.page(function CAPAPage() {
  return (
    <div class="p-8">
      <Head>
        <title>CAPA Tracking — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          CAPA Tracking
        </h1>
        <p class="text-stone-500 mt-1">
          Corrective and Preventive Actions — track deviations, enforce
          resolution, and verify closure for GDA inspection readiness.
        </p>
      </div>

      <CAPADashboard />
    </div>
  );
});
