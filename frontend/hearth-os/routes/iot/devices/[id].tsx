import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import { IoTAPI, DeviceDetailDto, ZoneSummaryDto } from "../../../utils/farmos-client.ts";
import DeviceDecommissionButton from "../../../islands/DeviceDecommissionButton.tsx";
import DeviceZoneAssignmentButton from "../../../islands/DeviceZoneAssignmentButton.tsx";

export default define.page(async function DeviceDetail(ctx) {
  const { id } = ctx.params;
  let device: DeviceDetailDto | null = null;
  let zones: ZoneSummaryDto[] = [];
  let error: string | null = null;

  try {
    device = await IoTAPI.getDevice(id);
    zones = (await IoTAPI.getZones()) || [];
  } catch (err: any) {
    error = err.message || "Failed to fetch device";
  }

  if (error || !device) {
    return (
      <div class="p-8 max-w-4xl mx-auto">
        <h1 class="text-2xl font-bold text-red-700">Device Not Found</h1>
        <p class="text-stone-600 mt-2">{error || "The requested device could not be located."}</p>
        <a href="/iot" class="mt-4 inline-block text-amber-600 font-semibold hover:underline">← Back to Devices</a>
      </div>
    );
  }

  const getSensorTypeName = (type: number) => {
    const types = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
    return types[type] || "Unknown";
  };

  const getStatusBadge = (status: number) => {
    switch(status) {
      case 0: return <span class="px-3 py-1 bg-amber-100 text-amber-800 rounded-full text-sm font-semibold">Pending (Awaiting Data)</span>;
      case 1: return <span class="px-3 py-1 bg-emerald-100 text-emerald-800 rounded-full text-sm font-semibold">Active</span>;
      case 2: return <span class="px-3 py-1 bg-red-100 text-red-800 rounded-full text-sm font-semibold">Offline</span>;
      case 3: return <span class="px-3 py-1 bg-stone-100 text-stone-800 rounded-full text-sm font-semibold">Maintenance</span>;
      case 4: return <span class="px-3 py-1 bg-stone-200 text-stone-500 rounded-full text-sm font-semibold">Decommissioned</span>;
      default: return <span class="px-3 py-1 bg-stone-100 text-stone-600 rounded-full text-sm font-semibold">Unknown</span>;
    }
  };

  const currentZone = zones.find(z => z.id === device.zoneId);

  return (
    <div class="p-8 max-w-5xl mx-auto">
      <Head>
        <title>{device.name} — FarmOS IoT</title>
      </Head>

      <div class="mb-6">
        <a href="/iot" class="text-amber-600 hover:text-amber-800 font-semibold mb-4 inline-flex items-center gap-2 transition">
          &larr; Back to Devices
        </a>
      </div>

      <div class="bg-white rounded-2xl shadow-sm border border-stone-200 overflow-hidden mb-8">
        <div class="p-8 border-b border-stone-100 flex flex-col md:flex-row justify-between md:items-center gap-6 bg-gradient-to-br from-stone-50 to-white">
          <div>
            <div class="flex items-center gap-3 mb-2">
              {getStatusBadge(device.status)}
              <span class="text-sm font-mono text-stone-400">{device.deviceCode}</span>
            </div>
            <h1 class="text-3xl font-bold text-stone-800 tracking-tight">{device.name}</h1>
            <p class="text-stone-500 mt-1 flex items-center gap-2">
              <span class="inline-block w-2 h-2 rounded-full bg-emerald-400"></span>
              {getSensorTypeName(device.sensorType)} Sensor
            </p>
          </div>

          {device.status !== 4 && (
            <div class="flex gap-3">
              <DeviceDecommissionButton deviceId={device.id} disabled={device.status === 4} />
            </div>
          )}
        </div>

        <div class="p-8 grid grid-cols-1 md:grid-cols-2 gap-8">
          <section>
            <h3 class="text-lg font-bold text-stone-800 mb-4 border-b border-stone-100 pb-2">Location & Assignment</h3>
            <div class="space-y-4">
              <div class="flex justify-between items-center p-4 bg-stone-50 rounded-xl border border-stone-100">
                <div>
                  <p class="text-sm text-stone-500 font-medium">Assigned Zone</p>
                  <p class="text-lg font-semibold text-stone-800">{currentZone?.name || "Unassigned"}</p>
                </div>
                <DeviceZoneAssignmentButton deviceId={device.id} currentZoneId={device.zoneId || null} zones={zones} />
              </div>

              {(device.gridPos || device.geoPos) && (
                <div class="p-4 bg-stone-50 rounded-xl border border-stone-100">
                  <p class="text-sm text-stone-500 font-medium mb-1">Coordinates</p>
                  {device.gridPos && (
                    <p class="text-stone-700 font-mono text-sm">Grid: ({device.gridPos.x}, {device.gridPos.y}, {device.gridPos.z})</p>
                  )}
                  {device.geoPos && (
                    <p class="text-stone-700 font-mono text-sm">Geo: {device.geoPos.latitude.toFixed(6)}, {device.geoPos.longitude.toFixed(6)}</p>
                  )}
                </div>
              )}
            </div>
          </section>

          <section>
            <h3 class="text-lg font-bold text-stone-800 mb-4 border-b border-stone-100 pb-2">Device Info & Metadata</h3>
            
            <div class="space-y-3">
              <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                <span class="text-stone-500 text-sm font-medium">Device ID</span>
                <span class="col-span-2 text-stone-800 font-mono text-sm">{device.id}</span>
              </div>
              <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                <span class="text-stone-500 text-sm font-medium">MAC/Serial</span>
                <span class="col-span-2 text-stone-800 font-mono text-sm">{device.deviceCode}</span>
              </div>

              {device.metadata && Object.keys(device.metadata).length > 0 && (
                <div class="mt-4">
                  <span class="text-stone-500 text-sm font-medium block mb-2">Extended Attributes</span>
                  <div class="bg-stone-50 rounded-lg p-3 border border-stone-100">
                    {Object.entries(device.metadata).map(([k, v]) => (
                      <div class="flex justify-between py-1 text-sm">
                        <span class="text-stone-600">{k}</span>
                        <span class="font-medium text-stone-800">{v}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
});
