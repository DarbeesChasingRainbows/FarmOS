import { useSignal } from "@preact/signals";
import { showToast } from "../utils/toastState.ts";

type EventType = "Receiving" | "Transformation" | "Shipping";

export default function TraceabilityDashboard() {
  const activeTab = useSignal<EventType>("Receiving");
  const isExporting = useSignal(false);

  const handleExport = async () => {
    isExporting.value = true;
    try {
      const response = await fetch("http://localhost:5000/api/hearth/compliance/traceability/audit-report");
      if (!response.ok) throw new Error("Failed to generate report");
      
      const blob = await response.blob();
      const url = globalThis.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `FSMA_24H_Audit_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(a);
      a.click();
      globalThis.URL.revokeObjectURL(url);
      a.remove();
      
      showToast("success", "Audit Exported", "24-Hour Audit Report successfully downloaded.");
    } catch (_e) {
      showToast("error", "Export Failed", "Failed to download audit report.");
    } finally {
      isExporting.value = false;
    }
  };

  const logEvent = async (e: Event) => {
    e.preventDefault();
    const fd = new FormData(e.target as HTMLFormElement);
    const payload: Record<string, unknown> = Object.fromEntries(fd.entries());
    
    // Convert string inputs to correct types (amount to number)
    payload.amount = { value: Number(payload.amountValue), unit: payload.amountUnit, measure: "Weight/Volume" };
    delete payload.amountValue;
    delete payload.amountUnit;
    payload.timestamp = new Date().toISOString();
    
    // Category mapping (defaulting to generic if not mapped to strict Enum in backend)
    payload.category = Number(payload.category); 
    
    const endpoint = activeTab.value.toLowerCase();
    
    try {
      const res = await fetch(`http://localhost:5000/api/hearth/compliance/traceability/${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
      
      if (!res.ok) throw new Error("Failed to log event");
      showToast("success", "Event Logged", `Successfully logged ${activeTab.value} event!`);
      (e.target as HTMLFormElement).reset();
    } catch (_err) {
      showToast("error", "Log Failed", "Failed to log traceability event.");
    }
  };

  return (
    <div class="space-y-6">
      {/* 24-Hour Audit Banner */}
      <div class="bg-indigo-50 border border-indigo-100 p-6 rounded-xl flex items-center justify-between shadow-sm">
        <div>
          <h2 class="text-xl font-bold text-indigo-900 mb-1">24-Hour Audit Ready</h2>
          <p class="text-indigo-700 text-sm">
            Generate your electronic, sortable spreadsheet dynamically compiling ingredient KDEs and CTEs for inspectors.
          </p>
        </div>
        <button
          type="button"
          onClick={handleExport}
          disabled={isExporting.value}
          class="flex items-center gap-2 px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-lg shadow-md transition disabled:opacity-50"
        >
          {isExporting.value ? "Generating CSV..." : "📥 Download 24-Hour CSV Audit"}
        </button>
      </div>

      {/* CTE Logging Forms */}
      <div class="bg-white border rounded-xl overflow-hidden shadow-sm">
        <div class="flex border-b bg-stone-50">
          {(["Receiving", "Transformation", "Shipping"] as const).map(tab => (
            <button
              type="button"
              onClick={() => activeTab.value = tab}
              class={`flex-1 py-4 font-semibold text-center transition-colors ${activeTab.value === tab ? "bg-white text-emerald-700 border-b-2 border-emerald-500" : "text-stone-500 hover:text-stone-700 hover:bg-stone-100"}`}
            >
              {tab}
            </button>
          ))}
        </div>

        <div class="p-8">
           <form onSubmit={logEvent} class="max-w-xl mx-auto space-y-5">
              <div class="grid grid-cols-2 gap-4">
                 <div>
                    <label class="block text-sm font-semibold text-stone-700 mb-1">Product Category</label>
                    <select name="category" required class="w-full px-4 py-2 bg-stone-50 border text-stone-800 border-stone-200 rounded-lg focus:ring-2 focus:ring-emerald-500">
                        {/* 0: Mushroom, 1: Jun, 2: Kombucha, 3: Sourdough, 4: Beef, 5: Wheat, 6: Ingredients, 7: Other */}
                        <option value="6">Raw Ingredient</option>
                        <option value="5">Heritage Wheat</option>
                        <option value="4">Dexter Beef</option>
                        <option value="3">Sourdough</option>
                        <option value="0">Mushroom</option>
                        <option value="1">Jun</option>
                        <option value="2">Kombucha</option>
                    </select>
                 </div>
                 <div>
                    <label class="block text-sm font-semibold text-stone-700 mb-1">Description</label>
                    <input type="text" name="description" required placeholder="e.g. Red Fife Flour" class="w-full px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
                 </div>
              </div>

              <div class="grid grid-cols-2 gap-4">
                 <div class="col-span-1">
                    <label class="block text-sm font-semibold text-stone-700 mb-1">Quantity</label>
                    <div class="flex gap-2">
                       <input type="number" step="0.01" name="amountValue" required class="w-2/3 px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
                       <select name="amountUnit" class="w-1/3 px-3 py-2 border text-stone-800 bg-stone-50 rounded-lg">
                          <option value="lbs">lbs</option>
                          <option value="kg">kg</option>
                          <option value="gal">gal</option>
                          <option value="ea">ea</option>
                       </select>
                    </div>
                 </div>
                 
                 {activeTab.value === "Receiving" && (
                     <div class="col-span-1">
                        <label class="block text-sm font-semibold text-stone-700 mb-1">Supplier / Origin</label>
                        <input type="text" name="sourceSupplier" required placeholder="FieldOps Zone 1" class="w-full px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
                     </div>
                 )}
                 {activeTab.value === "Transformation" && (
                     <div class="col-span-1">
                        <label class="block text-sm font-semibold text-stone-700 mb-1">Source Lot ID</label>
                        <input type="text" name="sourceLotId" required placeholder="Trace back to supplier lot" class="w-full px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
                     </div>
                 )}
                 {activeTab.value === "Shipping" && (
                     <div class="col-span-1">
                        <label class="block text-sm font-semibold text-stone-700 mb-1">Destination</label>
                        <input type="text" name="destination" required placeholder="B2C EdgePortal / Syndicate" class="w-full px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
                     </div>
                 )}
              </div>

              <div>
                  <label class="block text-sm font-semibold text-stone-700 mb-1">Generated Lot ID</label>
                  <input type="text" name={activeTab.value === "Transformation" ? "newLotId" : "lotId"} required placeholder="MUSH-20261014-01" class="w-full px-4 py-2 border text-stone-800 bg-stone-50 rounded-lg focus:ring-2 focus:ring-emerald-500" />
              </div>

              <div class="pt-4 mt-6 border-t border-stone-100 flex justify-end">
                 <button type="submit" class="px-6 py-2.5 bg-stone-800 text-white font-bold rounded-lg shadow-sm hover:bg-stone-900 transition">
                    Log {activeTab.value} Event
                 </button>
              </div>
           </form>
        </div>
      </div>
    </div>
  );
}
