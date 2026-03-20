import { useState } from "preact/hooks";
import { IoTAPI, ZoneSummaryDto } from "../utils/farmos-client.ts";

export default function DeviceZoneAssignmentButton({ 
  deviceId, 
  currentZoneId, 
  zones 
}: { 
  deviceId: string, 
  currentZoneId: string | null, 
  zones: ZoneSummaryDto[] 
}) {
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!isEditing && !errorMsg) {
    return (
      <button 
        type="button"
        class="text-amber-600 text-sm font-semibold hover:text-amber-800"
        onClick={() => setIsEditing(true)}
      >
        Change
      </button>
    );
  }

  return (
    <>
      {isEditing && (
        <div class="flex items-center gap-2">
          <select 
            class="select select-bordered select-sm bg-white text-stone-700 font-semibold focus:outline-none focus:border-amber-400 focus:ring-1 focus:ring-amber-400"
            disabled={isSaving}
            onChange={(e) => {
              const newZoneId = (e.target as HTMLSelectElement).value;
              setIsSaving(true);
              
              const promise = newZoneId === "" 
                ? IoTAPI.unassignDeviceFromZone(deviceId)
                : IoTAPI.assignDeviceToZone(deviceId, { deviceId, zoneId: newZoneId });
                
              promise.then(() => {
                if (typeof globalThis !== "undefined" && globalThis.location) {
                  globalThis.location.reload();
                }
              }).catch((err: any) => {
                setErrorMsg(err.message || "Failed to assign zone");
                setIsSaving(false);
                setIsEditing(false);
              });
            }}
          >
            <option value="">-- Unassigned --</option>
            {zones.map(z => (
              <option key={z.id} value={z.id} selected={z.id === currentZoneId}>
                {z.name}
              </option>
            ))}
          </select>
          <button 
            type="button" 
            class="text-stone-400 hover:text-stone-600 text-sm font-semibold p-1"
            onClick={() => setIsEditing(false)}
            disabled={isSaving}
          >
            Cancel
          </button>
        </div>
      )}

      {errorMsg && (
        <dialog class="modal modal-open">
          <div class="modal-box border-t-4 border-error">
            <h3 class="font-bold text-lg text-error">Assignment Failed</h3>
            <p class="py-4 text-sm text-stone-600">{errorMsg}</p>
            <div class="modal-action">
              <button 
                class="btn bg-white border-stone-200 hover:bg-stone-50 text-stone-700" 
                onClick={() => {
                  setErrorMsg(null);
                  setIsEditing(true);
                }}
              >
                Close
              </button>
            </div>
          </div>
          <form method="dialog" class="modal-backdrop bg-stone-900/40">
            <button onClick={() => {
              setErrorMsg(null);
              setIsEditing(true);
            }}>close</button>
          </form>
        </dialog>
      )}
    </>
  );
}
