import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { clearErrors, setErrors } from "../utils/schemas.ts";

const HAZARD_TYPES = ["Biological", "Chemical", "Physical", "Regulatory"];

interface CCPDefinition {
  product: string;
  ccpName: string;
  hazardType: number;
  criticalLimitExpression: string;
  monitoringProcedure: string;
  defaultCorrectiveAction: string;
}

export default function ArrowHACCPPlanBuilder(_props: { planId?: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      definitions: [] as CCPDefinition[],
      showForm: false,
      errors: {} as Record<string, string>,
    });

    const handleAdd = (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      const errs: Record<string, string> = {};
      const product = fd.get("product") as string;
      const ccpName = fd.get("ccpName") as string;
      const criticalLimit = fd.get("criticalLimitExpression") as string;
      const monitoring = fd.get("monitoringProcedure") as string;
      const corrective = fd.get("defaultCorrectiveAction") as string;
      const hazardType = Number(fd.get("hazardType") ?? 0);
      if (!product) errs.product = "Required";
      if (!ccpName) errs.ccpName = "Required";
      if (!criticalLimit) errs.criticalLimitExpression = "Required";
      if (!monitoring) errs.monitoringProcedure = "Required";
      if (!corrective) errs.defaultCorrectiveAction = "Required";
      if (Object.keys(errs).length) { setErrors(state.errors, errs); return; }
      state.definitions = [...state.definitions, { product, ccpName, hazardType, criticalLimitExpression: criticalLimit, monitoringProcedure: monitoring, defaultCorrectiveAction: corrective }];
      showToast("success", "CCP added", "Definition added to plan.");
      state.showForm = false;
      clearErrors(state.errors);
      form.reset();
    };

    const handleRemove = (product: string, ccpName: string) => {
      state.definitions = state.definitions.filter(d => !(d.product === product && d.ccpName === ccpName));
      showToast("success", "CCP removed", "Definition removed from plan.");
    };

    const errMsg = (field: string) =>
      state.errors[field] ? `<p class="text-red-500 text-xs">${state.errors[field]}</p>` : "";

    html`
      <div>
        <div class="flex justify-between items-center mb-4">
          <h3 class="text-lg font-bold text-stone-800">CCP Definitions</h3>
          <button
            @click="${() => state.showForm = !state.showForm}"
            class="px-3 py-1.5 bg-stone-800 text-white rounded text-xs font-medium hover:bg-stone-700 transition"
          >${() => state.showForm ? "Cancel" : "Add CCP"}</button>
        </div>

        ${() => state.showForm ? html`
          <form @submit="${handleAdd}" class="bg-stone-50 rounded-lg border border-stone-200 p-4 mb-4 space-y-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Product</label>
                <input name="product" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Sourdough, Kombucha, Beef...">
                ${() => errMsg("product")}
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">CCP Name</label>
                <input name="ccpName" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Internal Bake Temp, pH Level...">
                ${() => errMsg("ccpName")}
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Hazard Type</label>
                <select name="hazardType" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm">
                  ${HAZARD_TYPES.map((t, i) => html`<option value="${i}">${t}</option>`)}
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-stone-600 mb-1">Critical Limit</label>
                <input name="criticalLimitExpression" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="≥ 190°F, ≤ 4.2 pH...">
                ${() => errMsg("criticalLimitExpression")}
              </div>
            </div>
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">Monitoring Procedure</label>
              <input name="monitoringProcedure" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Probe thermometer at center of product, every batch">
              ${() => errMsg("monitoringProcedure")}
            </div>
            <div>
              <label class="block text-xs font-medium text-stone-600 mb-1">Default Corrective Action</label>
              <input name="defaultCorrectiveAction" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Return to oven until temp reaches target">
              ${() => errMsg("defaultCorrectiveAction")}
            </div>
            <button type="submit" class="px-4 py-1.5 bg-stone-800 text-white rounded text-xs font-medium hover:bg-stone-700 transition">Add Definition</button>
          </form>
        ` : html`<span></span>`}

        ${() => state.definitions.length > 0 ? html`
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
                ${() => state.definitions.map((d, i) => html`
                  <tr class="border-t border-stone-100">
                    <td class="px-4 py-2 font-medium">${d.product}</td>
                    <td class="px-4 py-2">${d.ccpName}</td>
                    <td class="px-4 py-2"><span class="px-2 py-0.5 bg-stone-100 text-stone-700 rounded text-xs">${HAZARD_TYPES[d.hazardType]}</span></td>
                    <td class="px-4 py-2 font-mono text-xs">${d.criticalLimitExpression}</td>
                    <td class="px-4 py-2 text-right">
                      <button @click="${() => handleRemove(d.product, d.ccpName)}" class="text-red-500 hover:text-red-700 text-xs">Remove</button>
                    </td>
                  </tr>
                `.key(String(i)))}
              </tbody>
            </table>
          </div>
        ` : html`<span></span>`}
      </div>
    `(containerRef.current);
  }, []);

  return <div ref={containerRef} />;
}
