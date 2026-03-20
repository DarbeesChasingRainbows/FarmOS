import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { showToast } from "../utils/toastState.ts";
import {
  extractErrors,
  type FieldErrors,
  FreezeDryerBatchSchema,
  FreezeDryerReadingSchema,
} from "../utils/schemas.ts";
import {
  FreezeDryerAPI,
  HarvestRightAPI,
  type FreezeDryerBatchDto,
  type HarvestRightStatus,
} from "../utils/farmos-client.ts";
import { freezeDryerTelemetry } from "../utils/connectionState.ts";

const PHASES = ["Loading", "Freezing", "PrimaryDrying", "SecondaryDrying", "Complete", "Aborted"];

const PHASE_COLORS: Record<string, string> = {
  Loading: "bg-stone-50 text-stone-600",
  Freezing: "bg-blue-50 text-blue-700",
  PrimaryDrying: "bg-amber-50 text-amber-700",
  SecondaryDrying: "bg-orange-50 text-orange-700",
  Complete: "bg-emerald-50 text-emerald-700",
  Aborted: "bg-red-50 text-red-700",
};

const ALERT_COLORS: Record<string, string> = {
  Safe: "border-emerald-200 bg-emerald-50",
  Warning: "border-amber-200 bg-amber-50",
  Critical: "border-red-200 bg-red-50",
};

export default function FreezeDryerPanel() {
  const batches = useSignal<FreezeDryerBatchDto[]>([]);
  const showForm = useSignal(false);
  const errors = useSignal<FieldErrors>({});
  const selectedBatch = useSignal<string | null>(null);
  const readingErrors = useSignal<FieldErrors>({});
  const hrStatus = useSignal<HarvestRightStatus | null>(null);
  const submitting = useSignal(false);

  async function refreshBatches() {
    try {
      batches.value = await FreezeDryerAPI.getBatches();
    } catch {
      // Batch list unavailable
    }
  }

  // Fetch batches on mount
  useEffect(() => {
    refreshBatches();
  }, []);

  // Poll Harvest Right cloud connection status
  useEffect(() => {
    async function fetchStatus() {
      try {
        hrStatus.value = await HarvestRightAPI.getStatus();
      } catch {
        hrStatus.value = null;
      }
    }
    fetchStatus();
    const interval = setInterval(fetchStatus, 30_000);
    return () => clearInterval(interval);
  }, []);

  // Auto-refresh batch list when live telemetry indicates phase changes
  useEffect(() => {
    const telem = freezeDryerTelemetry.value;
    if (!telem) return;
    // Refresh when phase changes or new batches might have been auto-created
    refreshBatches();
  }, [freezeDryerTelemetry.value?.phase, freezeDryerTelemetry.value?.batchId]);

  async function handleStartBatch(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = FreezeDryerBatchSchema.safeParse(data);

    if (!result.success) {
      errors.value = extractErrors(result);
      return;
    }

    submitting.value = true;
    try {
      await FreezeDryerAPI.startBatch({
        batchCode: result.data.batchCode,
        dryerId: result.data.dryerId,
        productDescription: result.data.productDescription,
        preDryWeight: Number(result.data.preDryWeight),
      });
      await refreshBatches();
      showToast("Freeze-dryer batch started", "success");
      showForm.value = false;
      errors.value = {};
      form.reset();
    } catch (err) {
      showToast("Failed to start batch", "error");
    } finally {
      submitting.value = false;
    }
  }

  async function handleRecordReading(e: Event) {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);
    const result = FreezeDryerReadingSchema.safeParse(data);

    if (!result.success) {
      readingErrors.value = extractErrors(result);
      return;
    }

    if (!selectedBatch.value) return;

    submitting.value = true;
    try {
      await FreezeDryerAPI.recordReading(selectedBatch.value, {
        reading: {
          shelfTempF: Number(result.data.shelfTempF),
          vacuumMTorr: Number(result.data.vacuumMTorr),
          productTempF: result.data.productTempF ? Number(result.data.productTempF) : undefined,
        },
      });
      showToast("Reading recorded", "success");
      readingErrors.value = {};
      form.reset();
    } catch (err) {
      showToast("Failed to record reading", "error");
    } finally {
      submitting.value = false;
    }
  }

  const telem = freezeDryerTelemetry.value;

  return (
    <div>
      {/* Harvest Right Cloud Connection Status */}
      {hrStatus.value && (
        <div class={`mb-4 px-4 py-3 rounded-lg border flex items-center gap-3 ${
          hrStatus.value.connected
            ? "bg-emerald-50 border-emerald-200"
            : "bg-stone-50 border-stone-200"
        }`}>
          <span class={`inline-block w-2.5 h-2.5 rounded-full ${
            hrStatus.value.connected ? "bg-emerald-500 animate-pulse" : "bg-stone-400"
          }`} />
          <div class="flex-1">
            <span class="text-sm font-medium text-stone-700">
              {hrStatus.value.connected ? "Connected to Harvest Right Cloud" : "Harvest Right Disconnected"}
            </span>
            {hrStatus.value.dryers.length > 0 && (
              <span class="ml-3 text-xs text-stone-500">
                {hrStatus.value.dryers.map(d => d.name).join(", ")}
              </span>
            )}
          </div>
          {hrStatus.value.lastTelemetryAt && (
            <span class="text-xs text-stone-400">
              Last data: {new Date(hrStatus.value.lastTelemetryAt).toLocaleTimeString()}
            </span>
          )}
        </div>
      )}

      {/* Live Telemetry Card */}
      {telem && (
        <div class={`mb-4 px-4 py-3 rounded-lg border ${
          ALERT_COLORS[telem.alertLevel ?? "Safe"] ?? ALERT_COLORS.Safe
        }`}>
          <div class="flex items-center justify-between mb-2">
            <span class="text-sm font-semibold text-stone-800">Live Telemetry</span>
            <span class={`px-2 py-0.5 rounded-full text-xs font-medium ${
              PHASE_COLORS[telem.phase] ?? "bg-stone-50 text-stone-600"
            }`}>
              {telem.phase}
            </span>
          </div>
          <div class="grid grid-cols-3 gap-4 text-center">
            <div>
              <div class="text-lg font-mono font-bold text-stone-800">{telem.temperatureF.toFixed(1)}°F</div>
              <div class="text-xs text-stone-500">Shelf Temp</div>
            </div>
            <div>
              <div class="text-lg font-mono font-bold text-stone-800">{telem.vacuumMTorr.toFixed(0)} mT</div>
              <div class="text-xs text-stone-500">Vacuum</div>
            </div>
            <div>
              <div class="text-lg font-mono font-bold text-stone-800">{telem.progressPercent.toFixed(1)}%</div>
              <div class="text-xs text-stone-500">Progress</div>
            </div>
          </div>
          {telem.alertMessage && telem.alertLevel !== "Safe" && (
            <div class="mt-2 text-xs font-medium text-stone-700">{telem.alertMessage}</div>
          )}
          <div class="mt-1 text-xs text-stone-400 text-right">
            {new Date(telem.timestamp).toLocaleTimeString()}
          </div>
        </div>
      )}

      <div class="flex justify-between items-center mb-6">
        <h2 class="text-xl font-bold text-stone-800">Freeze-Dryer Batches</h2>
        <button
          onClick={() => (showForm.value = !showForm.value)}
          class="px-4 py-2 bg-stone-800 text-white rounded-lg text-sm font-medium hover:bg-stone-700"
        >
          {showForm.value ? "Cancel" : "New Batch"}
        </button>
      </div>

      {showForm.value && (
        <form onSubmit={handleStartBatch} class="bg-white rounded-lg border border-stone-200 p-6 mb-6 space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-stone-700 mb-1">Batch Code</label>
              <input name="batchCode" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="FD-20260316-01" />
              {errors.value.batchCode && <p class="text-red-500 text-xs mt-1">{errors.value.batchCode}</p>}
            </div>
            <div>
              <label class="block text-sm font-medium text-stone-700 mb-1">Dryer</label>
              <select name="dryerId" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm">
                <option value="">Select dryer...</option>
                {hrStatus.value?.dryers.map(d => (
                  <option key={d.dryerId} value={String(d.dryerId)}>{d.name} ({d.serial})</option>
                ))}
              </select>
              {errors.value.dryerId && <p class="text-red-500 text-xs mt-1">{errors.value.dryerId}</p>}
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-stone-700 mb-1">Product Description</label>
            <input name="productDescription" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" placeholder="Beef jerky strips, Lion's Mane slices..." />
            {errors.value.productDescription && <p class="text-red-500 text-xs mt-1">{errors.value.productDescription}</p>}
          </div>
          <div class="w-1/3">
            <label class="block text-sm font-medium text-stone-700 mb-1">Pre-Dry Weight (lbs)</label>
            <input name="preDryWeight" type="number" step="0.01" class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm" />
            {errors.value.preDryWeight && <p class="text-red-500 text-xs mt-1">{errors.value.preDryWeight}</p>}
          </div>
          <button type="submit" disabled={submitting.value} class="px-6 py-2 bg-stone-800 text-white rounded-lg text-sm font-medium hover:bg-stone-700 disabled:opacity-50">
            {submitting.value ? "Starting..." : "Start Batch"}
          </button>
        </form>
      )}

      {/* Batch List */}
      {batches.value.length === 0
        ? (
          <div class="text-center py-12 text-stone-400">
            <p class="text-lg">No active freeze-dryer batches</p>
            <p class="text-sm mt-1">Start a new batch to begin tracking</p>
          </div>
        )
        : (
          <div class="space-y-4">
            {batches.value.map((batch) => (
              <div key={batch.id} class="bg-white rounded-lg border border-stone-200 p-4">
                <div class="flex justify-between items-center">
                  <div>
                    <span class="font-mono font-bold text-stone-800">{batch.batchCode}</span>
                    <span class="ml-3 text-sm text-stone-500">{batch.productDescription}</span>
                  </div>
                  <span class={`px-3 py-1 rounded-full text-xs font-medium ${
                    PHASE_COLORS[PHASES[batch.phase]] ?? "bg-stone-50 text-stone-600"
                  }`}>
                    {PHASES[batch.phase]}
                  </span>
                </div>
                <div class="mt-2 text-xs text-stone-400">
                  Pre-dry: {batch.preDryWeight} lbs
                  {batch.postDryWeight && ` | Post-dry: ${batch.postDryWeight} lbs | Ratio: ${(batch.postDryWeight / batch.preDryWeight * 100).toFixed(1)}%`}
                </div>

                {/* Reading Form (collapsed by default) */}
                {selectedBatch.value === batch.id && batch.phase > 0 && batch.phase < 4 && (
                  <form onSubmit={handleRecordReading} class="mt-4 pt-4 border-t border-stone-100 grid grid-cols-3 gap-3">
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Shelf Temp (°F)</label>
                      <input name="shelfTempF" type="number" step="0.1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" />
                      {readingErrors.value.shelfTempF && <p class="text-red-500 text-xs">{readingErrors.value.shelfTempF}</p>}
                    </div>
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Vacuum (mTorr)</label>
                      <input name="vacuumMTorr" type="number" step="1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" />
                      {readingErrors.value.vacuumMTorr && <p class="text-red-500 text-xs">{readingErrors.value.vacuumMTorr}</p>}
                    </div>
                    <div>
                      <label class="block text-xs font-medium text-stone-600 mb-1">Product Temp (°F)</label>
                      <input name="productTempF" type="number" step="0.1" class="w-full px-2 py-1.5 border border-stone-300 rounded text-sm" placeholder="Optional" />
                    </div>
                    <div class="col-span-3 flex gap-2">
                      <button type="submit" disabled={submitting.value} class="px-4 py-1.5 bg-stone-700 text-white rounded text-xs font-medium hover:bg-stone-600 disabled:opacity-50">
                        {submitting.value ? "Recording..." : "Record"}
                      </button>
                    </div>
                  </form>
                )}

                {batch.phase > 0 && batch.phase < 4 && (
                  <button
                    onClick={() => (selectedBatch.value = selectedBatch.value === batch.id ? null : batch.id)}
                    class="mt-2 text-xs text-stone-500 hover:text-stone-700 underline"
                  >
                    {selectedBatch.value === batch.id ? "Hide Reading Form" : "Record Reading"}
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
    </div>
  );
}
