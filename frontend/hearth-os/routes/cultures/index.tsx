import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import CultureDetailPanel from "../../islands/CultureDetailPanel.tsx";

export default define.page(function CulturesPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Cultures — Hearth OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Living Cultures
            </h1>
            <Tooltip text="Living cultures are organisms you maintain — sourdough starters, kombucha SCOBYs, kefir grains. They need regular feeding to stay healthy.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Click any culture to view details, feed, or split it.
          </p>
        </div>
      </div>

      <CultureDetailPanel />
    </div>
  );
});
