import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import MushroomActionPanel from "../../islands/MushroomActionPanel.tsx";

// Mock data fetcher until projector is wired
export default define.page(async function MushroomBatchDetails(ctx) {
  const { id } = ctx.params;

  // In the future this will be a real API fetch
  // const batch = await fetchFarmOS(`/api/hearth/mushrooms/${id}`);

  const batch = {
    id,
    batchCode: id === "mb-lion-01" ? "LM-03-A" : "BO-03-B",
    species: id === "mb-lion-01" ? "Lion's Mane" : "Blue Oyster",
    substrateType: id === "mb-lion-01"
      ? "Hardwood Sawdust + Soy Hulls"
      : "Straw",
    phase: id === "mb-lion-01" ? 2 : 0, // 2=Fruiting, 0=Incubation
    currentFlushes: id === "mb-lion-01" ? 1 : 0,
    inoculatedAt: new Date(Date.now() - 21 * 24 * 3600 * 1000).toISOString(),
    environments: [],
    flushes: [],
    isContaminated: false,
    isCompleted: false,
  };

  return (
    <div class="p-8">
      <Head>
        <title>{batch.batchCode} — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <a
          href="/mushrooms"
          class="text-sm text-stone-500 hover:text-emerald-700 transition font-medium mb-4 inline-block"
        >
          ← Back to Mushrooms
        </a>
        <div class="flex items-center gap-4">
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            {batch.species}{" "}
            <span class="text-stone-400 font-medium">({batch.batchCode})</span>
          </h1>
          {batch.phase === 2 && (
            <span class="bg-emerald-100 text-emerald-800 border border-emerald-200 text-xs font-bold px-3 py-1 rounded-full">
              Fruiting Phase
            </span>
          )}
          {batch.phase === 0 && (
            <span class="bg-stone-100 text-stone-800 border border-stone-200 text-xs font-bold px-3 py-1 rounded-full">
              Incubation Phase
            </span>
          )}
        </div>
        <p class="text-stone-500 mt-2">
          Substrate: {batch.substrateType}
        </p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Column: Timeline / Data */}
        <div class="lg:col-span-2 space-y-6">
          <div class="bg-white rounded-xl border border-stone-200 shadow-sm p-6">
            <h2 class="text-lg font-bold text-stone-800 mb-4">
              Cultivation Log
            </h2>
            <div class="text-center py-10 text-stone-400 text-sm">
              No events recorded yet.
              <br />
              (Use the action panel to record temperature or flushes)
            </div>
          </div>
        </div>

        {/* Right Column: Actions */}
        <div class="space-y-6">
          <div class="bg-white rounded-xl border border-stone-200 shadow-sm overflow-hidden">
            <div class="bg-stone-50 px-5 py-4 border-b border-stone-200">
              <h3 class="font-bold text-stone-800">Block Actions</h3>
            </div>
            <div class="p-5">
              <MushroomActionPanel
                batchId={batch.id}
                currentPhase={batch.phase}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
});
