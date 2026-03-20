import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import EquipmentPanel from "../../islands/EquipmentPanel.tsx";
import SanitationLog from "../../islands/SanitationLog.tsx";
import StaffCertifications from "../../islands/StaffCertifications.tsx";
import DeliveryLog from "../../islands/DeliveryLog.tsx";

export default define.page(function CompliancePage() {
  return (
    <div class="p-8">
      <Head>
        <title>Compliance — Hearth OS</title>
        <style
          dangerouslySetInnerHTML={{
            __html: `
          @media print {
            body { background: white !important; margin: 0; padding: 0; color: black !important; }
            .p-8 { padding: 0 !important; }
            button, nav, .no-print { display: none !important; }
            section { page-break-inside: avoid; margin-bottom: 2rem !important; }
            h1, h2, h3, h4, h5, p, span, div { color: black !important; }
            .bg-white, .bg-stone-50, .bg-emerald-50, .bg-red-50 { background: white !important; border: 1px solid #ccc !important; box-shadow: none !important; color: black !important; }
            .shadow-sm, .shadow-md, .shadow-2xl { shadow: none !important; }
            @page { margin: 0.5in; }
            * { transition: none !important; }
          }
        `,
          }}
        />
      </Head>

      <div class="mb-8 flex justify-between items-end">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Kitchen Compliance
          </h1>
          <p class="text-stone-500 mt-1 print:hidden">
            Food safety, sanitation, certifications, and cold chain tracking.
          </p>
        </div>
        <button
          type="button"
          // @ts-ignore: inline JS for server component
          onclick="window.print()"
          class="no-print bg-stone-800 hover:bg-stone-900 text-white text-sm font-bold py-2.5 px-5 rounded-lg shadow-md transition flex items-center gap-2"
        >
          🖨️ Print for Inspector
        </button>
      </div>

      {/* Traceability & FSMA 204 */}
      <section class="mb-12">
        <div class="flex items-center gap-3 mb-5">
          <span class="text-2xl">🔗</span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">
              Traceability & FSMA 204
            </h2>
            <p class="text-sm text-stone-500">
              Track Key Data Elements (KDEs) across Critical Tracking Events (CTEs) for 24-Hour FDA compliance.
            </p>
          </div>
        </div>
        <div class="bg-white border text-center p-8 rounded-xl shadow-sm">
           <p class="text-stone-600 mb-4">View the Traceability Engine to log Receiving, Transformation, and Shipping events, or generate your 24-Hour Audit report.</p>
           <a href="/compliance/traceability" class="inline-block px-5 py-2.5 bg-emerald-600 font-semibold text-white rounded-lg hover:bg-emerald-700 transition">Open Traceability Dashboard</a>
        </div>
      </section>

      {/* Temperature Monitoring */}
      <section class="mb-12">
        <div class="flex items-center gap-3 mb-5">
          <span class="text-2xl">🌡️</span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">
              Equipment Temperatures
            </h2>
            <p class="text-sm text-stone-500">
              FDA Food Code: Fridge ≤41°F · Freezer ≤0°F · Hot-hold ≥140°F
            </p>
          </div>
        </div>
        <EquipmentPanel />
      </section>

      {/* Sanitation */}
      <section class="mb-12">
        <div class="flex items-center gap-3 mb-5">
          <span class="text-2xl">🧹</span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">Sanitation Log</h2>
            <p class="text-sm text-stone-500">
              Cleaning tasks, frequency schedules, and sign-off records.
            </p>
          </div>
        </div>
        <SanitationLog />
      </section>

      {/* Staff Certifications */}
      <section class="mb-12">
        <div class="flex items-center gap-3 mb-5">
          <span class="text-2xl">🪪</span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">
              Staff Certifications
            </h2>
            <p class="text-sm text-stone-500">
              Food handler and manager certification expiry tracking.
            </p>
          </div>
        </div>
        <StaffCertifications />
      </section>

      {/* Delivery Receiving */}
      <section class="mb-12">
        <div class="flex items-center gap-3 mb-5">
          <span class="text-2xl">📦</span>
          <div>
            <h2 class="text-xl font-bold text-stone-800">
              Delivery Receiving Log
            </h2>
            <p class="text-sm text-stone-500">
              Supplier deliveries with arrival temperature and acceptance
              decisions.
            </p>
          </div>
        </div>
        <DeliveryLog />
      </section>
    </div>
  );
});
