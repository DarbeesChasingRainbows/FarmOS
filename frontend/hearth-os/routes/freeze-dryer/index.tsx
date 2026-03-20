import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import FreezeDryerPanel from "../../islands/FreezeDryerPanel.tsx";

export default define.page(function FreezeDryerPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Freeze-Dryer — Hearth OS</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
          Freeze-Dryer Management
        </h1>
        <p class="text-stone-500 mt-1">
          Track Harvest Right freeze-dryer cycles — vacuum pressure, shelf
          temperature, batch weights, and phase progression.
        </p>
      </div>

      <FreezeDryerPanel />
    </div>
  );
});
