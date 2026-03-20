import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NewMushroomBatchForm from "../../islands/NewMushroomBatchForm.tsx";

export default define.page(function NewMushroomPage() {
  return (
    <div class="p-8">
      <Head>
        <title>New Mushroom Block — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <a
          href="/mushrooms"
          class="text-sm text-stone-500 hover:text-emerald-700 transition font-medium mb-4 inline-block"
        >
          ← Back to Mushrooms
        </a>
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          Inoculate Substrate
        </h1>
        <p class="text-stone-500 mt-1">
          Start tracking a new block through incubation and fruiting phases.
        </p>
      </div>

      <NewMushroomBatchForm />
    </div>
  );
});
