import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import { IoTAPI, ZoneDetailDto } from "../../../utils/farmos-client.ts";
import ZoneArchiveButton from "../../../islands/ZoneArchiveButton.tsx";

export default define.page(async function ZoneDetail(ctx) {
  const { id } = ctx.params;
  let zone: ZoneDetailDto | null = null;
  let error: string | null = null;

  try {
    zone = await IoTAPI.getZone(id);
  } catch (err: any) {
    error = err.message || "Failed to fetch zone";
  }

  if (error || !zone) {
    return (
      <div class="p-8 max-w-4xl mx-auto">
        <h1 class="text-2xl font-bold text-red-700">Zone Not Found</h1>
        <p class="text-stone-600 mt-2">{error || "The requested zone could not be located."}</p>
        <a href="/iot/zones" class="mt-4 inline-block text-emerald-600 font-semibold hover:underline">← Back to Zones</a>
      </div>
    );
  }

  const getZoneTypeName = (type: number) => {
    const types = ["Greenhouse", "Field", "Barn", "Cellar", "Storage", "Other"];
    return types[type] || "Unknown";
  };

  const getSensorTypeName = (type: number) => {
    const types = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
    return types[type] || "Unknown";
  };

  const getStatusBadge = (status: number) => {
    switch(status) {
      case 0: return <span class="px-2.5 py-1 bg-amber-100 text-amber-800 rounded-full text-xs font-semibold">Pending (Awaiting Data)</span>;
      case 1: return <span class="px-2.5 py-1 bg-emerald-100 text-emerald-800 rounded-full text-xs font-semibold">Active</span>;
      case 2: return <span class="px-2.5 py-1 bg-red-100 text-red-800 rounded-full text-xs font-semibold">Offline</span>;
      case 3: return <span class="px-2.5 py-1 bg-stone-100 text-stone-800 rounded-full text-xs font-semibold">Maintenance</span>;
      case 4: return <span class="px-2.5 py-1 bg-stone-200 text-stone-500 rounded-full text-xs font-semibold">Decommissioned</span>;
      default: return <span class="px-2.5 py-1 bg-stone-100 text-stone-600 rounded-full text-xs font-semibold">Unknown</span>;
    }
  };

  return (
    <div class="p-8 max-w-6xl mx-auto">
      <Head>
        <title>{zone.name} — FarmOS Zones</title>
      </Head>

      <div class="mb-6">
        <a href="/iot/zones" class="text-emerald-600 hover:text-emerald-800 font-semibold mb-4 inline-flex items-center gap-2 transition">
          &larr; Back to Zones
        </a>
      </div>

      <div class="bg-white rounded-2xl shadow-sm border border-stone-200 overflow-hidden mb-8">
        <div class="p-8 border-b border-stone-100 flex flex-col md:flex-row justify-between md:items-center gap-6 bg-gradient-to-br from-stone-50 to-white">
          <div>
            <div class="flex items-center gap-3 mb-2">
              <span class="px-3 py-1 bg-emerald-100 text-emerald-800 rounded-full text-sm font-semibold">
                {getZoneTypeName(zone.zoneType)}
              </span>
              {zone.isArchived && (
                <span class="px-3 py-1 bg-stone-200 text-stone-500 rounded-full text-sm font-semibold">Archived</span>
              )}
            </div>
            <h1 class="text-3xl font-bold text-stone-800 tracking-tight">{zone.name}</h1>
            {zone.description && (
              <p class="text-stone-500 mt-2 max-w-2xl">{zone.description}</p>
            )}
          </div>

          {!zone.isArchived && (
            <div class="flex gap-3">
              <ZoneArchiveButton zoneId={zone.id} isArchived={zone.isArchived} />
            </div>
          )}
        </div>

        <div class="p-8">
          <h3 class="text-xl font-bold text-stone-800 mb-6 flex items-center gap-2">
            <span class="w-2 h-6 rounded bg-emerald-500 inline-block"></span>
            Devices in Zone
          </h3>
          
          {zone.devices && zone.devices.length > 0 ? (
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {zone.devices.map(d => (
                <a href={`/iot/devices/${d.id}`} class="group p-5 bg-white border border-stone-200 hover:border-emerald-300 shadow-sm hover:shadow-md rounded-xl transition cursor-pointer">
                  <div class="flex justify-between items-start mb-3">
                    {getStatusBadge(d.status)}
                    <span class="text-xs font-mono text-stone-400 group-hover:text-emerald-600 transition">{d.deviceCode}</span>
                  </div>
                  <h4 class="text-lg font-bold text-stone-800 group-hover:text-emerald-700 transition">{d.name}</h4>
                  <p class="text-stone-500 text-sm mt-1 flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 inline-block"></span>
                    {getSensorTypeName(d.sensorType)}
                  </p>
                </a>
              ))}
            </div>
          ) : (
            <div class="p-10 border border-dashed border-stone-300 rounded-xl text-center text-stone-500 bg-stone-50">
              <p class="font-medium text-lg">No devices assigned</p>
              <p class="text-sm mt-1">Assign devices to this zone from their detail pages to track telemetry spatially.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
});
