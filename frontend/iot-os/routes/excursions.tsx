import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import { IoTAPI, type ActiveExcursion } from "../utils/iot-client.ts";
import ExcursionList from "../islands/ExcursionList.tsx";

export default define.page(async function ExcursionsPage() {
  let excursions: ActiveExcursion[] = [];
  let error: string | null = null;

  try {
    excursions = (await IoTAPI.getActiveExcursions()) || [];
  } catch (err: unknown) {
    error = err instanceof Error ? err.message : "Failed to fetch excursions";
  }

  return (
    <div class="p-6 md:p-8 max-w-7xl mx-auto">
      <Head>
        <title>Active Alerts — FarmOS IoT</title>
      </Head>

      <div class="mb-8">
        <h1 class="text-3xl font-black text-amber-50 tracking-tight">
          🚨 Active Alerts
        </h1>
        <p class="text-stone-400 mt-1 text-sm">
          Excursions requiring immediate attention
        </p>
      </div>

      {error && (
        <div class="mb-6 p-4 bg-red-900/40 text-red-300 rounded-xl border border-red-700/50 text-sm font-semibold">
          {error}
        </div>
      )}

      <ExcursionList initialExcursions={excursions} />
    </div>
  );
});
