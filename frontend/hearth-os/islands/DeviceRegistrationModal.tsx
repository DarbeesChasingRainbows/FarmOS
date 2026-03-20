import { useState } from "preact/hooks";
import { IoTAPI, DeviceSummaryDto } from "../utils/farmos-client.ts";

export default function DeviceRegistrationModal() {
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [deviceCode, setDeviceCode] = useState("");
  const [name, setName] = useState("");
  // 0: Temperature, 1: Humidity, 2: SoilMoisture, 3: Light, 4: Co2, 5: Ph
  const [sensorType, setSensorType] = useState<number>(0);

  const handleSubmit = async (e: Event) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await IoTAPI.registerDevice({
        deviceCode,
        name,
        sensorType: Number(sensorType)
      });
      setIsOpen(false);
      setDeviceCode("");
      setName("");
      setSensorType(0);
      if (typeof globalThis !== "undefined" && globalThis.location) {
        globalThis.location.reload();
      }
    } catch (err: any) {
      setError(err.message || "Failed to register device");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        class="bg-amber-600 hover:bg-amber-700 text-white px-5 py-2.5 rounded-lg font-semibold shadow-sm transition"
      >
        + Register Device
      </button>

      {isOpen && (
        <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div class="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
            <div class="px-6 py-4 border-b border-stone-100 flex justify-between items-center bg-stone-50">
              <h3 class="text-lg font-bold text-stone-800">Register IoT Device</h3>
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
                  <label class="block text-sm font-medium text-stone-700 mb-1">Device Code (MAC/Serial)</label>
                  <input
                    required
                    type="text"
                    value={deviceCode}
                    onInput={(e) => setDeviceCode((e.target as HTMLInputElement).value)}
                    placeholder="e.g. 00:1B:44:11:3A:B7"
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-amber-500 focus:ring-amber-500"
                  />
                </div>
                
                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">Display Name</label>
                  <input
                    required
                    type="text"
                    value={name}
                    onInput={(e) => setName((e.target as HTMLInputElement).value)}
                    placeholder="e.g. Greenhouse Temp Sensor 1"
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-amber-500 focus:ring-amber-500"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-stone-700 mb-1">Sensor Type</label>
                  <select
                    value={sensorType}
                    onChange={(e) => setSensorType(Number((e.target as HTMLSelectElement).value))}
                    class="w-full border-stone-200 rounded-lg shadow-sm focus:border-amber-500 focus:ring-amber-500"
                  >
                    <option value={0}>Temperature</option>
                    <option value={1}>Humidity</option>
                    <option value={2}>Soil Moisture</option>
                    <option value={3}>Light Level</option>
                    <option value={4}>CO2</option>
                    <option value={5}>pH</option>
                  </select>
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
                  class="bg-amber-600 hover:bg-amber-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-5 py-2 rounded-lg font-semibold shadow-sm transition"
                >
                  {loading ? "Registering..." : "Register Device"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
