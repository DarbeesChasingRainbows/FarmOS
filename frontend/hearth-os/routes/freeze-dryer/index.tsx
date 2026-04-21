import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowFreezeDryerPanel from "../../islands/ArrowFreezeDryerPanel.tsx";

export default define.page(function FreezeDryerPage() {
  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>Freeze-Dryer — Hearth OS</title>
      </Head>
      <header class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          Freeze-Dryer Management
        </h1>
        <p class="text-stone-500 mt-1">
          Track Harvest Right cycles — vacuum, shelf temperature, batch weights,
          and phase progression.
        </p>
      </header>

      <ArrowFreezeDryerPanel />
    </div>
  );
});
