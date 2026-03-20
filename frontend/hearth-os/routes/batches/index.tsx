import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import BatchDetailPanel from "../../islands/BatchDetailPanel.tsx";

export default define.page(function BatchesList() {
  return (
    <div class="p-8">
      <Head>
        <title>Batches — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          Fermentation Batches
        </h1>
        <p class="text-stone-500 mt-1">
          Click any batch to view details, record pH, or advance the phase.
        </p>
      </div>

      <BatchDetailPanel />
    </div>
  );
});
