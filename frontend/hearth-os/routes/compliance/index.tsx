import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowComplianceHub from "../../islands/ArrowComplianceHub.tsx";
import ArrowEquipmentPanel from "../../islands/ArrowEquipmentPanel.tsx";
import ArrowSanitationLog from "../../islands/ArrowSanitationLog.tsx";
import ArrowStaffCertifications from "../../islands/ArrowStaffCertifications.tsx";
import ArrowDeliveryLog from "../../islands/ArrowDeliveryLog.tsx";

export default define.page(function CompliancePage() {
  return (
    <div>
      <Head>
        <title>Compliance — Hearth OS</title>
        <style
          dangerouslySetInnerHTML={{
            __html: `
          @media print {
            body { background: white !important; margin: 0; padding: 0; color: black !important; }
            button, nav, .no-print { display: none !important; }
            section { page-break-inside: avoid; margin-bottom: 2rem !important; }
            h1, h2, h3, h4, h5, p, span, div { color: black !important; }
            .bg-white, .bg-stone-50, .bg-emerald-50, .bg-red-50 { background: white !important; border: 1px solid #ccc !important; box-shadow: none !important; }
            @page { margin: 0.5in; }
            * { transition: none !important; }
          }
        `,
          }}
        />
      </Head>

      <ArrowComplianceHub />

      <div class="px-6 max-w-7xl mx-auto space-y-8 pb-8">
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">{"\u{1F321}\u{FE0F}"}</span>
            <h2 class="text-lg font-bold text-stone-800">
              Equipment Temperatures
            </h2>
          </div>
          <ArrowEquipmentPanel />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">{"\u{1F9F9}"}</span>
            <h2 class="text-lg font-bold text-stone-800">Sanitation Log</h2>
          </div>
          <ArrowSanitationLog />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">{"\u{1FAAA}"}</span>
            <h2 class="text-lg font-bold text-stone-800">
              Staff Certifications
            </h2>
          </div>
          <ArrowStaffCertifications />
        </section>
        <section>
          <div class="flex items-center gap-3 mb-4">
            <span class="text-xl">{"\u{1F4E6}"}</span>
            <h2 class="text-lg font-bold text-stone-800">
              Delivery Receiving Log
            </h2>
          </div>
          <ArrowDeliveryLog />
        </section>
      </div>
    </div>
  );
});
