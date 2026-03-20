import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";
import {
  CCPDefinitionSchema,
  extractErrors,
  type FieldErrors,
} from "../utils/schemas.ts";

const HAZARD_TYPES = ["Biological", "Chemical", "Physical", "Regulatory"];

interface CCPDefinition {
  product: string;
  ccpName: string;
  hazardType: number;
  criticalLimitExpression: string;
  monitoringProcedure: string;
  defaultCorrectiveAction: string;
}

export default function HACCPPlanBuilder(props: { planId?: string }) {
  const definitions = useSignal<CCPDefinition[]>([]);
  const showForm = useSignal(false);
  const errors = useSignal<FieldErrors>({});

  function handleAdd(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = CCPDefinitionSchema.safeParse(data);

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    // TODO: Call HACCPAPI.addCCPDefinition() and refresh
    const parsed = result.data;
    definitions.value = [...definitions.value, parsed as CCPDefinition];
    showToast("CCP definition added", "success");
    showForm.value = false;
    errors.value = {};
    form.reset();
  }

  function handleRemove(product: string, ccpName: string) {
    // TODO: Call HACCPAPI.removeCCPDefinition()
    definitions.value = definitions.value.filter(
      (d) => !(d.product === product && d.ccpName === ccpName),
    );
    showToast("CCP definition removed", "success");
  }

  return (
    <div>
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-bold text-stone-800">CCP Definitions</h3>
        <button
          onClick={() => (showForm.value = !showForm.value)}
          class="px-3 py-1.5 bg-stone-800 text-white rounded text-xs font-medium hover:bg-stone-700"
        >
          {showForm.value ? "Cancel" : "Add CCP"}
        </button>
      </div>

      {showForm.value && (
        <form onSubmit={handleAdd} class="bg-stone-50 rounded-lg border border-stone-200 p-4 mb-4 space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">Product</label>
              <input name="product" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Sourdough, Kombucha, Beef..." />
              {errors.value.product && <p class="text-red-500 text-xs">{errors.value.product}</p>}
            </div>
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">CCP Name</label>
              <input name="ccpName" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Internal Bake Temp, pH Level..." />
              {errors.value.ccpName && <p class="text-red-500 text-xs">{errors.value.ccpName}</p>}
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">Hazard Type</label>
              <select name="hazardType" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm">
                {HAZARD_TYPES.map((t, i) => <option key={i} value={i}>{t}</option>)}
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">Critical Limit</label>
              <input name="criticalLimitExpression" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="&ge; 190°F, &le; 4.2 pH..." />
              {errors.value.criticalLimitExpression && <p class="text-red-500 text-xs">{errors.value.criticalLimitExpression}</p>}
            </div>
          </div>
          <div>
            <label class="block text-xs font-medium text-stone-600 mb-1">Monitoring Procedure</label>
            <input name="monitoringProcedure" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Probe thermometer at center of product, every batch" />
            {errors.value.monitoringProcedure && <p class="text-red-500 text-xs">{errors.value.monitoringProcedure}</p>}
          </div>
          <div>
            <label class="block text-xs font-medium text-stone-600 mb-1">Default Corrective Action</label>
            <input name="defaultCorrectiveAction" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Return to oven until temp reaches target" />
            {errors.value.defaultCorrectiveAction && <p class="text-red-500 text-xs">{errors.value.defaultCorrectiveAction}</p>}
          </div>
          <button type="submit" class="px-4 py-1.5 bg-stone-800 text-white rounded text-xs font-medium hover:bg-stone-700">
            Add Definition
          </button>
        </form>
      )}

      {definitions.value.length > 0 && (
        <div class="bg-white rounded-lg border border-stone-200 overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-stone-50">
              <tr>
                <th class="text-left px-4 py-2 font-semibold text-stone-600">Product</th>
                <th class="text-left px-4 py-2 font-semibold text-stone-600">CCP</th>
                <th class="text-left px-4 py-2 font-semibold text-stone-600">Hazard</th>
                <th class="text-left px-4 py-2 font-semibold text-stone-600">Limit</th>
                <th class="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {definitions.value.map((d, i) => (
                <tr key={i} class="border-t border-stone-100">
                  <td class="px-4 py-2 font-medium">{d.product}</td>
                  <td class="px-4 py-2">{d.ccpName}</td>
                  <td class="px-4 py-2">
                    <span class="px-2 py-0.5 bg-stone-100 text-stone-700 rounded text-xs">
                      {HAZARD_TYPES[d.hazardType]}
                    </span>
                  </td>
                  <td class="px-4 py-2 font-mono text-xs">{d.criticalLimitExpression}</td>
                  <td class="px-4 py-2 text-right">
                    <button
                      onClick={() => handleRemove(d.product, d.ccpName)}
                      class="text-red-500 hover:text-red-700 text-xs"
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
