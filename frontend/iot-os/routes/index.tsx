import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import { IoTAPI, type ZoneSummaryDto, ZONE_TYPE_NAMES } from "../utils/iot-client.ts";
import ZoneGridDashboard from "../islands/ZoneGridDashboard.tsx";

export default define.page(async function ZoneDashboard() {
  let zones: ZoneSummaryDto[] = [];
  let error: string | null = null;

  try {
    zones = (await IoTAPI.getZones()) || [];
  } catch (err: unknown) {
    error = err instanceof Error ? err.message : "Failed to fetch zones";
  }

  return (
    <div class="p-6 md:p-8 max-w-7xl mx-auto">
      <Head>
        <title>Sensor Grid — FarmOS IoT</title>
      </Head>

      <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
        <div>
          <h1 class="text-3xl font-black text-amber-50 tracking-tight">
            Sensor Grid
          </h1>
          <p class="text-stone-400 mt-1 text-sm">
            Live zone monitoring — tap a zone for details
          </p>
        </div>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-900/40 text-red-300 rounded-xl border border-red-700/50 text-sm font-semibold">
          {error}
        </div>
      )}

      <ZoneGridDashboard initialZones={zones} />
    </div>
  );
});
