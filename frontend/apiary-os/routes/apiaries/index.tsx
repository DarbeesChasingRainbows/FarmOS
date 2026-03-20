import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import CreateApiaryForm from "../../components/CreateApiaryForm.tsx";

export default define.page(function ApiariesPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Apiaries — Apiary OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Apiary Locations
            </h1>
            <Tooltip text="Group your hives by physical location. Each apiary represents a yard or site where hives are kept.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Manage yards and assign hives to locations.
          </p>
        </div>
      </div>

      <CreateApiaryForm />

      <div class="bg-stone-50 border border-stone-200 rounded-xl p-8 text-center text-stone-500">
        <p class="text-lg font-medium mb-2">No apiaries yet</p>
        <p class="text-sm">
          Create your first apiary location to start grouping hives by yard.
        </p>
      </div>
    </div>
  );
});
