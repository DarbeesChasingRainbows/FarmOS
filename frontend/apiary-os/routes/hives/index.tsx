import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import HiveDetailPanel from "../../islands/HiveDetailPanel.tsx";

export default define.page(function HivesPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Hives — Hearth OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Apiaries
            </h1>
            <Tooltip text="Manage your bee colonies. Regular inspections (every 7–10 days in season) track queen health, mite loads, and honey production.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Click any hive to inspect, treat, or harvest.
          </p>
        </div>
      </div>

      <HiveDetailPanel />
    </div>
  );
});
