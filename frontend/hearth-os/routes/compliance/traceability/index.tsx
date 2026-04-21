import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";

export default define.page(function TraceabilityPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Traceability & FSMA 204 — Hearth OS</title>
      </Head>

      <div class="mb-2">
        <a
          href="/compliance"
          class="text-orange-600 hover:text-orange-700 text-sm font-semibold transition"
        >
          &larr; Back to Compliance
        </a>
      </div>

      <div class="mb-8 flex justify-between items-end">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Traceability & FSMA 204
          </h1>
          <p class="text-stone-500 mt-1">
            Log Critical Tracking Events (CTEs) and export Key Data Elements
            (KDEs) to comply with FDA mandates.
          </p>
        </div>
      </div>

      <div class="rounded-lg border border-dashed border-stone-300 bg-white p-8 text-center">
        <p class="text-stone-600">Traceability dashboard coming soon.</p>
        <p class="text-stone-400 text-sm mt-2">
          CTE/KDE capture UI will be rebuilt as an Arrow.js island.
        </p>
      </div>
    </div>
  );
});
