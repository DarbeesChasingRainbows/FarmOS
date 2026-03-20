import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import { IoTAPI, type ComplianceReport } from "../utils/iot-client.ts";
import ComplianceGauge from "../islands/ComplianceGauge.tsx";

export default define.page(async function CompliancePage() {
  let reports: ComplianceReport[] = [];
  let error: string | null = null;

  try {
    reports = (await IoTAPI.getComplianceReport()) || [];
  } catch (err: unknown) {
    error = err instanceof Error ? err.message : "Failed to fetch compliance data";
  }

  return (
    <div class="p-6 md:p-8 max-w-7xl mx-auto">
      <Head>
        <title>Compliance — FarmOS IoT</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-black text-amber-50 tracking-tight">
          ✅ Compliance
        </h1>
        <p class="text-stone-400 mt-1 text-sm">
          Zone compliance against FDA/GDA threshold rules
        </p>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-900/40 text-red-300 rounded-xl border border-red-700/50 text-sm font-semibold">
          {error}
        </div>
      )}

      {reports.length === 0 ? (
        <div class="bg-stone-800/60 rounded-2xl border border-stone-700/50 p-12 text-center text-stone-500">
          <p class="text-lg font-bold mb-2">No compliance data yet</p>
          <p class="text-sm">Sensor data will populate compliance reports automatically.</p>
        </div>
      ) : (
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {reports.map((r) => (
            <ComplianceGauge report={r} />
          ))}
        </div>
      )}
    </div>
  );
});
