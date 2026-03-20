import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import { IoTAPI, DeviceSummaryDto } from "../../utils/farmos-client.ts";
import DeviceRegistrationModal from "../../islands/DeviceRegistrationModal.tsx";

export default define.page(async function IoTDashboard() {
  let devices: DeviceSummaryDto[] = [];
  let error: string | null = null;
  
  try {
    devices = (await IoTAPI.getDevices()) || [];
  } catch (err: any) {
    error = err.message || "Failed to fetch devices";
  }

  const getSensorTypeName = (type: number) => {
    const types = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
    return types[type] || "Unknown";
  };

  const getStatusBadge = (status: number) => {
    switch(status) {
      case 0: return <span class="px-2.5 py-1 bg-amber-100 text-amber-800 rounded-full text-xs font-semibold">Pending</span>;
      case 1: return <span class="px-2.5 py-1 bg-emerald-100 text-emerald-800 rounded-full text-xs font-semibold">Active</span>;
      case 2: return <span class="px-2.5 py-1 bg-red-100 text-red-800 rounded-full text-xs font-semibold">Offline</span>;
      case 3: return <span class="px-2.5 py-1 bg-stone-100 text-stone-800 rounded-full text-xs font-semibold">Maintenance</span>;
      case 4: return <span class="px-2.5 py-1 bg-stone-200 text-stone-500 rounded-full text-xs font-semibold">Decommissioned</span>;
      default: return <span class="px-2.5 py-1 bg-stone-100 text-stone-600 rounded-full text-xs font-semibold">Unknown</span>;
    }
  };

  return (
    <div class="p-8 max-w-7xl mx-auto">
      <Head>
        <title>IoT Devices — FarmOS</title>
      </Head>

      <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            IoT Devices
          </h1>
          <p class="text-stone-500 mt-1">
            Manage your connected agricultural sensors and hardware.
          </p>
        </div>
        
        <div class="flex gap-3">
          <a href="/iot/zones" class="bg-white border border-stone-200 hover:bg-stone-50 text-stone-700 px-5 py-2.5 rounded-lg font-semibold shadow-sm transition">
            Manage Zones
          </a>
          <DeviceRegistrationModal />
        </div>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-50 text-red-700 rounded-xl border border-red-100">
          Failed to load devices: {error}
        </div>
      )}

      <div class="bg-white rounded-xl shadow-sm border border-stone-200 overflow-hidden">
        {devices.length === 0 ? (
          <div class="p-12 text-center text-stone-500">
            <p class="text-lg font-medium mb-2">No devices registered</p>
            <p class="text-sm">Register your first IoT device to start monitoring your farm.</p>
          </div>
        ) : (
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-stone-50 border-b border-stone-200">
                <th class="p-4 font-semibold text-stone-600 text-sm">Name</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Code / MAC</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Sensor Type</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Status</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Zone</th>
                <th class="p-4 font-semibold text-stone-600 text-sm text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-stone-100">
              {devices.map(d => (
                <tr class="hover:bg-stone-50 transition group">
                  <td class="p-4">
                    <div class="font-bold text-stone-800">{d.name}</div>
                    <div class="text-xs text-stone-400 font-mono mt-0.5">{d.id}</div>
                  </td>
                  <td class="p-4 text-stone-600 font-mono text-sm">{d.deviceCode}</td>
                  <td class="p-4 text-stone-600 text-sm">{getSensorTypeName(d.sensorType)}</td>
                  <td class="p-4">{getStatusBadge(d.status)}</td>
                  <td class="p-4 text-stone-500 text-sm">{d.zoneId ? "Assigned" : "Unassigned"}</td>
                  <td class="p-4 text-right">
                    <a href={`/iot/devices/${d.id}`} class="text-amber-600 hover:text-amber-800 font-semibold text-sm transition">
                      View Details &rarr;
                    </a>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
});
