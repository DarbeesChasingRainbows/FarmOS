import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowBatchDetailPanel from "../../islands/ArrowBatchDetailPanel.tsx";
import ArrowNewBatchForm from "../../islands/ArrowNewBatchForm.tsx";
import { FermentationAPI } from "../../utils/farmos-client.ts";

export default define.page(async function BatchesList() {
  const batches = (await FermentationAPI.getActiveMonitoring().catch(() => [])) ?? [];

  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Batches — Hearth OS</title>
      </Head>
      <header class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">Fermentation Batches</h1>
          <p class="text-stone-500 mt-1">Click any batch to view details, record pH, or advance the phase.</p>
        </div>
        <ArrowNewBatchForm />
      </header>
      <ArrowBatchDetailPanel initialBatches={batches} />
    </div>
  );
});
