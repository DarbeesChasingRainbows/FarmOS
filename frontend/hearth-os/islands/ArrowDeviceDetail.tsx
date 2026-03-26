import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowConfirmDialog } from "../components/ArrowConfirmDialog.ts";
import type { DeviceDetailDto, ZoneSummaryDto } from "../utils/farmos-client.ts";

const SENSOR_TYPES = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];

const STATUS_STYLES: Record<number, { badge: string; label: string }> = {
  0: { badge: "bg-amber-100 text-amber-800 border-amber-200", label: "Pending" },
  1: { badge: "bg-emerald-100 text-emerald-800 border-emerald-200", label: "Active" },
  2: { badge: "bg-red-100 text-red-800 border-red-200", label: "Offline" },
  3: { badge: "bg-stone-100 text-stone-700 border-stone-200", label: "Maintenance" },
  4: { badge: "bg-stone-200 text-stone-500 border-stone-300", label: "Decommissioned" },
};

export interface ArrowDeviceDetailProps {
  deviceId: string;
}

export default function ArrowDeviceDetail({ deviceId }: ArrowDeviceDetailProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      device: null as DeviceDetailDto | null,
      zones: [] as ZoneSummaryDto[],
      loading: true,
      error: "",
      selectedZoneId: "",
      assigning: false,
      assignError: "",
      showDecommissionDialog: false,
      decommissioning: false,
    });

    const fetchData = async () => {
      state.loading = true;
      state.error = "";
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        const [deviceResult, zonesResult] = await Promise.allSettled([
          IoTAPI.getDevice(deviceId),
          IoTAPI.getZones(),
        ]);

        if (deviceResult.status === "fulfilled") {
          state.device = deviceResult.value;
          state.selectedZoneId = deviceResult.value.zoneId || "";
        } else {
          state.error = "Failed to load device details";
        }

        if (zonesResult.status === "fulfilled") {
          state.zones = zonesResult.value || [];
        }
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to load data";
      } finally {
        state.loading = false;
      }
    };

    const handleAssignZone = async () => {
      if (!state.selectedZoneId || !state.device) return;
      state.assigning = true;
      state.assignError = "";
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.assignDeviceToZone(deviceId, {
          deviceId,
          zoneId: state.selectedZoneId,
        });
        await fetchData();
      } catch (err: unknown) {
        state.assignError = err instanceof Error ? err.message : "Failed to assign zone";
      } finally {
        state.assigning = false;
      }
    };

    const handleDecommission = async () => {
      state.decommissioning = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.decommissionDevice(deviceId, {
          deviceId,
          reason: "Decommissioned by user",
        });
        globalThis.location.href = "/iot";
      } catch (err: unknown) {
        state.error = err instanceof Error ? err.message : "Failed to decommission device";
        state.showDecommissionDialog = false;
      } finally {
        state.decommissioning = false;
      }
    };

    const sensorTypeName = (type: number) => SENSOR_TYPES[type] || "Unknown";

    const template = html`
      <div class="p-8 max-w-5xl mx-auto">
        <!-- Back link -->
        <div class="mb-6">
          <a href="/iot" class="text-emerald-600 hover:text-emerald-800 font-semibold inline-block transition">
            &larr; Back to Devices
          </a>
        </div>

        <!-- Error -->
        ${() => state.error
          ? html`<div class="mb-6 p-4 bg-red-50 text-red-700 rounded-xl border border-red-100">${() => state.error}</div>`
          : html``}

        <!-- Loading -->
        ${() => state.loading
          ? html`<div class="text-center py-12 text-stone-400">Loading device...</div>`
          : html``}

        <!-- Device detail -->
        ${() => {
          if (state.loading || !state.device) return html``;
          const device = state.device;
          const s = STATUS_STYLES[device.status] || STATUS_STYLES[3];

          return html`
            <!-- Header card -->
            <div class="bg-white rounded-2xl border border-stone-200 shadow-sm overflow-hidden mb-8">
              <div class="p-8 border-b border-stone-100 flex flex-col md:flex-row justify-between md:items-center gap-6 bg-gradient-to-br from-stone-50 to-white">
                <div>
                  <div class="flex items-center gap-3 mb-2">
                    <span class="${`text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full border ${s.badge}`}">
                      ${s.label}
                    </span>
                    <span class="text-sm font-mono text-stone-400">${device.deviceCode}</span>
                  </div>
                  <h1 class="text-3xl font-bold text-stone-800 tracking-tight">${device.name}</h1>
                  <p class="text-stone-500 mt-1 flex items-center gap-2">
                    <span class="inline-block w-2 h-2 rounded-full bg-emerald-400"></span>
                    ${sensorTypeName(device.sensorType)} Sensor
                  </p>
                </div>
                ${device.status !== 4
                  ? html`
                    <button
                      type="button"
                      @click="${() => { state.showDecommissionDialog = true; }}"
                      class="px-4 py-2 bg-red-50 text-red-700 font-semibold rounded-lg hover:bg-red-100 transition text-sm border border-red-200"
                    >
                      Decommission
                    </button>
                  `
                  : html``}
              </div>
            </div>

            <!-- 2-column grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-8 mb-8">
              <!-- Zone Assignment -->
              <div class="bg-white rounded-2xl border border-stone-200 shadow-sm p-6">
                <h3 class="text-lg font-bold text-stone-800 mb-4 border-b border-stone-100 pb-2">Zone Assignment</h3>
                <div class="space-y-4">
                  <div class="flex items-center gap-3">
                    <select
                      @change="${(e: Event) => { state.selectedZoneId = (e.target as HTMLSelectElement).value; }}"
                      class="flex-1 px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-emerald-200 focus:border-emerald-400 outline-none transition bg-white"
                    >
                      <option value="">Unassigned</option>
                      ${state.zones.map((z: ZoneSummaryDto) =>
                        html`<option value="${z.id}" selected="${z.id === state.selectedZoneId}">${z.name}</option>`
                      )}
                    </select>
                    <button
                      type="button"
                      @click="${handleAssignZone}"
                      disabled="${() => state.assigning || !state.selectedZoneId}"
                      class="px-4 py-2 bg-emerald-600 text-white font-semibold rounded-lg hover:bg-emerald-700 transition text-sm disabled:opacity-50 shadow-sm"
                    >
                      ${() => state.assigning ? "Assigning..." : "Assign"}
                    </button>
                  </div>
                  ${() => state.assignError
                    ? html`<p class="text-xs text-red-600 font-medium">${() => state.assignError}</p>`
                    : html``}
                </div>
              </div>

              <!-- Device Info -->
              <div class="bg-white rounded-2xl border border-stone-200 shadow-sm p-6">
                <h3 class="text-lg font-bold text-stone-800 mb-4 border-b border-stone-100 pb-2">Device Info</h3>
                <div class="space-y-3">
                  <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                    <span class="text-stone-500 text-sm font-medium">Sensor Type</span>
                    <span class="col-span-2 text-stone-800 text-sm">${sensorTypeName(device.sensorType)}</span>
                  </div>
                  <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                    <span class="text-stone-500 text-sm font-medium">Device ID</span>
                    <span class="col-span-2 text-stone-800 font-mono text-sm">${device.id}</span>
                  </div>
                  ${device.gridPos
                    ? html`
                      <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                        <span class="text-stone-500 text-sm font-medium">Grid Position</span>
                        <span class="col-span-2 text-stone-800 font-mono text-sm">(${device.gridPos.x}, ${device.gridPos.y}, ${device.gridPos.z})</span>
                      </div>
                    `
                    : html``}
                  ${device.geoPos
                    ? html`
                      <div class="grid grid-cols-3 gap-4 border-b border-stone-100 py-2">
                        <span class="text-stone-500 text-sm font-medium">Geo Position</span>
                        <span class="col-span-2 text-stone-800 font-mono text-sm">${device.geoPos.latitude.toFixed(6)}, ${device.geoPos.longitude.toFixed(6)}</span>
                      </div>
                    `
                    : html``}
                </div>
              </div>
            </div>

            <!-- Metadata section -->
            ${device.metadata && Object.keys(device.metadata).length > 0
              ? html`
                <div class="bg-white rounded-2xl border border-stone-200 shadow-sm p-6 mb-8">
                  <h3 class="text-lg font-bold text-stone-800 mb-4 border-b border-stone-100 pb-2">Metadata</h3>
                  <div class="bg-stone-50 rounded-lg p-4 border border-stone-100">
                    ${Object.entries(device.metadata).map(([k, v]) => html`
                      <div class="flex justify-between py-2 border-b border-stone-100 last:border-0 text-sm">
                        <span class="text-stone-600 font-medium">${k}</span>
                        <span class="text-stone-800 font-mono">${v}</span>
                      </div>
                    `)}
                  </div>
                </div>
              `
              : html``}

            <!-- Decommission confirm dialog -->
            ${ArrowConfirmDialog({
              isOpen: () => state.showDecommissionDialog,
              title: "Decommission Device",
              message: "Are you sure you want to decommission this device? This action cannot be undone.",
              confirmLabel: "Decommission",
              danger: true,
              onConfirm: handleDecommission,
              onCancel: () => { state.showDecommissionDialog = false; },
            })}
          `;
        }}
      </div>
    `;

    template(containerRef.current);
    fetchData();
  }, [deviceId]);

  return <div ref={containerRef}></div>;
}
