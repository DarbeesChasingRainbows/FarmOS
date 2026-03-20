import { useState } from "preact/hooks";
import { IoTAPI } from "../utils/farmos-client.ts";

export default function DeviceDecommissionButton({ deviceId, disabled }: { deviceId: string, disabled: boolean }) {
  if (disabled) return null;
  
  const [isOpen, setIsOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleDecommission = async () => {
    if (!reason.trim()) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await IoTAPI.decommissionDevice(deviceId, { deviceId, reason });
      if (typeof globalThis !== "undefined" && globalThis.location) {
        globalThis.location.reload();
      }
    } catch (err: any) {
      setError(err.message || "Failed to decommission device");
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <button 
        type="button"
        onClick={() => setIsOpen(true)}
        class="bg-stone-100 border border-stone-200 hover:bg-red-50 hover:text-red-700 hover:border-red-200 px-5 py-2.5 rounded-lg text-stone-600 font-semibold shadow-sm transition"
      >
        Decommission
      </button>

      {isOpen && (
        <dialog class="modal modal-open">
          <div class="modal-box border-t-4 border-red-500">
            <h3 class="font-bold text-lg text-red-600 mb-4">Decommission Device</h3>
            <p class="text-sm text-stone-600 mb-4">
              Are you sure you want to decommission this device? This action indicates the device is permanently removed from service.
            </p>
            
            <input 
              type="text" 
              placeholder="Reason (e.g. Broken, Upgraded)"
              class="input input-bordered w-full mb-4 focus:outline-none focus:border-red-400 focus:ring-1 focus:ring-red-400"
              value={reason}
              onInput={(e) => setReason((e.target as HTMLInputElement).value)}
            />

            {error && (
              <div class="alert alert-error mb-4 rounded-lg bg-red-50 text-red-700 border border-red-200">
                <span class="text-xs font-semibold">{error}</span>
              </div>
            )}

            <div class="modal-action">
              <button 
                type="button" 
                class="btn bg-white border-stone-200 text-stone-700 hover:bg-stone-50" 
                disabled={isSubmitting}
                onClick={() => setIsOpen(false)}
              >
                Cancel
              </button>
              <button 
                type="button" 
                class="btn btn-error text-white"
                disabled={isSubmitting || !reason.trim()}
                onClick={handleDecommission}
              >
                {isSubmitting ? <span class="loading loading-spinner loading-sm"></span> : "Decommission"}
              </button>
            </div>
          </div>
          <form method="dialog" class="modal-backdrop bg-stone-900/40">
            <button onClick={() => setIsOpen(false)}>close</button>
          </form>
        </dialog>
      )}
    </>
  );
}
