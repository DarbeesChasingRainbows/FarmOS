import { useState } from "preact/hooks";
import { IoTAPI } from "../utils/farmos-client.ts";

export default function ZoneCreationModal() {
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  // 0: Greenhouse, 1: Field, 2: Barn, 3: Cellar, 4: Storage, 5: Other
  const [zoneType, setZoneType] = useState<number>(0);

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await IoTAPI.createZone({
        name,
        zoneType: Number(zoneType),
        description
      });
      setIsOpen(false);
      setName("");
      setDescription("");
      setZoneType(0);
      if (typeof globalThis !== "undefined" && globalThis.location) {
        globalThis.location.reload();
      }
    } catch (err: any) {
      setError(err.message || "Failed to create zone");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        class="bg-emerald-600 hover:bg-emerald-700 text-white px-5 py-2.5 rounded-lg font-semibold shadow-sm transition"
      >
        + Create Zone
      </button>

      {isOpen && (
        <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div class="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
            <div class="px-6 py-4 border-b border-stone-100 flex justify-between items-center bg-stone-50">
              <h3 class="text-lg font-bold text-stone-800">Create IoT Zone</h3>
              <button 
                onClick={() => setIsOpen(false)}
                class="text-stone-400 hover:text-stone-600"
              >
                ✕
              </button>
            </div>
            
            <form onSubmit={handleSubmit} class="p-6">
              {error && (
                <div class="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm border border-red-100">
                  {error}
                </div>
              )}

              <div class="space-y-4">
                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">Zone Name</label>
                  <input
                    required
                    type="text"
                    value={name}
                    onInput={(e) => setName((e.target as HTMLInputElement).value)}
                    placeholder="e.g. North Greenhouse"
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">Zone Type</label>
                  <select
                    value={zoneType}
                    onChange={(e) => setZoneType(Number((e.target as HTMLSelectElement).value))}
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                  >
                    <option value={0}>Greenhouse</option>
                    <option value={1}>Field</option>
                    <option value={2}>Barn</option>
                    <option value={3}>Cellar</option>
                    <option value={4}>Storage</option>
                    <option value={5}>Other</option>
                  </select>
                </div>
                
                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">Description (Optional)</label>
                  <textarea
                    value={description}
                    onInput={(e) => setDescription((e.target as HTMLTextAreaElement).value)}
                    placeholder="Location details or purpose"
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-emerald-500 focus:ring-emerald-500"
                    rows={3}
                  />
                </div>
              </div>

              <div class="mt-8 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setIsOpen(false)}
                  class="px-4 py-2 text-stone-600 hover:text-stone-800 font-medium"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={loading}
                  class="bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-5 py-2 rounded-lg font-semibold shadow-sm transition"
                >
                  {loading ? "Creating..." : "Create Zone"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
