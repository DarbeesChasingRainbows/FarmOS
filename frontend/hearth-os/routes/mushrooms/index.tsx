import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowMushroomBatchList from "../../islands/ArrowMushroomBatchList.tsx";

export default define.page(function MushroomsPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Mushrooms — Hearth OS</title>
      </Head>

      <div class="mb-8 flex justify-between items-end">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            Mushroom Cultivation
          </h1>
          <p class="text-stone-500 mt-1">
            Track incubation, pinning, and fruiting phases for all fungi blocks.
          </p>
        </div>
        <a
          href="/mushrooms/new"
          class="bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-2.5 px-5 rounded-lg shadow-sm transition"
        >
          + Start New Batch
        </a>
      </div>

      <div class="bg-white rounded-xl border border-stone-200 shadow-sm p-6 mb-8">
        <ArrowMushroomBatchList />
      </div>
    </div>
  );
});
