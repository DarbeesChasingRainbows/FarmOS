import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import { IoTAPI, ZoneSummaryDto } from "../../../utils/farmos-client.ts";
import ZoneCreationModal from "../../../islands/ZoneCreationModal.tsx";

export default define.page(async function IoTZonesDashboard() {
  let zones: ZoneSummaryDto[] = [];
  let error: string | null = null;
  
  try {
    zones = (await IoTAPI.getZones()) || [];
  } catch (err: any) {
    error = err.message || "Failed to fetch zones";
  }

  const getZoneTypeName = (type: number) => {
    const types = ["Greenhouse", "Field", "Barn", "Cellar", "Storage", "Other"];
    return types[type] || "Unknown";
  };

  return (
    <div class="p-8 max-w-7xl mx-auto">
      <Head>
        <title>IoT Zones — FarmOS</title>
      </Head>

      <div class="mb-8">
        <a href="/iot" class="text-emerald-600 hover:text-emerald-800 font-semibold mb-4 inline-block transition">&larr; Back to Devices</a>
        <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              IoT Zones
            </h1>
            <p class="text-stone-500 mt-1">
              Group and locate your devices hierarchically across the farm.
            </p>
          </div>
          
          <div class="flex gap-3">
            <ZoneCreationModal />
          </div>
        </div>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-50 text-red-700 rounded-xl border border-red-100">
          Failed to load zones: {error}
        </div>
      )}

      <div class="bg-white rounded-xl shadow-sm border border-stone-200 overflow-hidden">
        {zones.length === 0 ? (
          <div class="p-12 text-center text-stone-500">
            <p class="text-lg font-medium mb-2">No zones created</p>
            <p class="text-sm">Create your first zone to organize your network.</p>
          </div>
        ) : (
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-stone-50 border-b border-stone-200">
                <th class="p-4 font-semibold text-stone-600 text-sm">Name</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Zone Type</th>
                <th class="p-4 font-semibold text-stone-600 text-sm">Parent Zone</th>
                <th class="p-4 font-semibold text-stone-600 text-sm text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-stone-100">
              {zones.map(z => (
                <tr class="hover:bg-stone-50 transition group">
                  <td class="p-4">
                    <div class="font-bold text-stone-800">{z.name}</div>
                    <div class="text-xs text-stone-400 font-mono mt-0.5">{z.id}</div>
                  </td>
                  <td class="p-4 text-stone-600 text-sm">{getZoneTypeName(z.zoneType)}</td>
                  <td class="p-4 text-stone-500 text-sm">{z.parentZoneId || "—"}</td>
                  <td class="p-4 text-right">
                    <a href={`/iot/zones/${z.id}`} class="text-emerald-600 hover:text-emerald-800 font-semibold text-sm transition">
                      View Hub &rarr;
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
