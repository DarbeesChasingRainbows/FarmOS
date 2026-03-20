import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import { IoTAPI, type DeviceSummaryDto, SENSOR_TYPE_NAMES } from "../utils/iot-client.ts";

export default define.page(async function DevicesPage() {
  let devices: DeviceSummaryDto[] = [];
  let error: string | null = null;

  try {
    devices = (await IoTAPI.getDevices()) || [];
  } catch (err: unknown) {
    error = err instanceof Error ? err.message : "Failed to fetch devices";
  }

  const getStatusBadge = (status: number) => {
    const styles: Record<number, string> = {
      0: "bg-amber-900/50 text-amber-300 border-amber-600/30",
      1: "bg-emerald-900/50 text-emerald-300 border-emerald-600/30",
      2: "bg-red-900/50 text-red-300 border-red-600/30",
      3: "bg-stone-700/50 text-stone-300 border-stone-500/30",
      4: "bg-stone-800/50 text-stone-500 border-stone-600/30",
    };
    const labels: Record<number, string> = {
      0: "Pending", 1: "Active", 2: "Offline", 3: "Maintenance", 4: "Decommissioned",
    };
    return (
      <span class={`px-3 py-1.5 rounded-lg text-xs font-bold border ${styles[status] || styles[0]}`}>
        {labels[status] || "Unknown"}
      </span>
    );
  };

  return (
    <div class="p-6 md:p-8 max-w-7xl mx-auto">
      <Head>
        <title>Devices — FarmOS IoT</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-black text-amber-50 tracking-tight">
          📟 Devices
        </h1>
        <p class="text-stone-400 mt-1 text-sm">
          Registered IoT sensors and hardware
        </p>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-900/40 text-red-300 rounded-xl border border-red-700/50 text-sm font-semibold">
          {error}
        </div>
      )}

      <div class="bg-stone-800/60 rounded-2xl border border-stone-700/50 overflow-hidden">
        {devices.length === 0 ? (
          <div class="p-12 text-center text-stone-500">
            <p class="text-lg font-bold mb-2">No devices registered</p>
            <p class="text-sm">Register sensors via the IoT API to start monitoring.</p>
          </div>
        ) : (
          <div class="divide-y divide-stone-700/30">
            {devices.map((d) => (
              <div class="flex items-center justify-between px-6 py-5 hover:bg-stone-700/20 transition">
                <div class="flex items-center gap-4">
                  <div class="w-12 h-12 rounded-xl bg-stone-700/50 flex items-center justify-center text-xl">
                    {d.sensorType === 0 ? "🌡️" : d.sensorType === 1 ? "💧" : d.sensorType === 5 ? "⚗️" : "📊"}
                  </div>
                  <div>
                    <div class="font-bold text-amber-50 text-base">{d.name}</div>
                    <div class="text-xs text-stone-400 font-mono mt-0.5">{d.deviceCode}</div>
                  </div>
                </div>
                <div class="flex items-center gap-4">
                  <span class="text-sm text-stone-400 hidden md:block">
                    {SENSOR_TYPE_NAMES[d.sensorType] || "Unknown"}
                  </span>
                  {getStatusBadge(d.status)}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
});
