import { useState } from "preact/hooks";
import { IoTAPI } from "../utils/farmos-client.ts";

export default function ZoneArchiveButton({ zoneId, isArchived }: { zoneId: string, isArchived: boolean }) {
  if (isArchived) return null;

  const [isOpen, setIsOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleArchive = async () => {
    if (!reason.trim()) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await IoTAPI.archiveZone(zoneId, { zoneId, reason });
      if (typeof globalThis !== "undefined" && globalThis.location) {
        globalThis.location.href = "/iot/zones";
      }
    } catch (err: any) {
      setError(err.message || "Failed to archive zone");
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <button 
        type="button"
        onClick={() => setIsOpen(true)}
        class="bg-stone-100 border border-stone-200 hover:bg-red-50 hover:text-red-700 hover:border-red-200 px-5 py-2.5 rounded-lg text-stone-700 font-semibold shadow-sm transition"
      >
        Delete Zone
      </button>

      {isOpen && (
        <dialog class="modal modal-open">
          <div class="modal-box border-t-4 border-red-500">
            <h3 class="font-bold text-lg mb-4 text-red-600">Delete Zone</h3>
            <p class="text-sm text-stone-600 mb-4">Are you sure you want to delete this zone? Please provide a reason below.</p>
            
            <input 
              type="text" 
              placeholder="e.g. End of season, dismantled"
              class="input input-bordered w-full mb-4 focus:outline-none focus:border-stone-400 focus:ring-1 focus:ring-stone-400"
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
                onClick={handleArchive}
              >
                {isSubmitting ? <span class="loading loading-spinner loading-sm"></span> : "Delete Zone"}
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
