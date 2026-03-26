import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import type { DeviceSummaryDto } from "../utils/farmos-client.ts";

const SENSOR_TYPES = ["Temperature", "Humidity", "Soil Moisture", "Light", "CO2", "pH"];
const STATUS_NAMES = ["Pending", "Active", "Offline", "Maintenance", "Decommissioned"];
const STATUS_STYLES: Record<number, { bg: string; text: string }> = {
  0: { bg: "bg-amber-100", text: "text-amber-800" },
  1: { bg: "bg-emerald-100", text: "text-emerald-800" },
  2: { bg: "bg-red-100", text: "text-red-800" },
  3: { bg: "bg-stone-100", text: "text-stone-800" },
  4: { bg: "bg-stone-200", text: "text-stone-500" },
};

export default function ArrowIoTDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      devices: [] as DeviceSummaryDto[],
      loading: true,
      error: null as string | null,
      showRegForm: false,
      regCode: "",
      regName: "",
      regSensor: "0",
      registering: false,
      regError: null as string | null,
    });

    const loadData = async () => {
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.devices = (await IoTAPI.getDevices()) ?? [];
      } catch (err: unknown) {
        state.error =
          err instanceof Error ? err.message : "Failed to load devices";
      } finally {
        state.loading = false;
      }
    };

    loadData();

    const handleRegister = async (e: Event) => {
      e.preventDefault();
      if (!state.regCode.trim() || !state.regName.trim()) return;
      state.registering = true;
      state.regError = null;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.registerDevice({
          deviceCode: state.regCode,
          name: state.regName,
          sensorType: Number(state.regSensor),
        });
        state.regCode = "";
        state.regName = "";
        state.regSensor = "0";
        state.showRegForm = false;
        state.loading = true;
        await loadData();
      } catch (err: unknown) {
        state.regError =
          err instanceof Error ? err.message : "Registration failed";
      } finally {
        state.registering = false;
      }
    };

    // KPI computed values
    const totalCount = () => state.devices.length;
    const activeCount = () =>
      state.devices.filter((d) => d.status === 1).length;
    const offlineCount = () =>
      state.devices.filter((d) => d.status === 2).length;
    const unassignedCount = () =>
      state.devices.filter((d) => !d.zoneId).length;

    const statusBadge = (status: number) => {
      const style = STATUS_STYLES[status] ?? STATUS_STYLES[0];
      const name = STATUS_NAMES[status] ?? "Unknown";
      return html`<span
        class="px-2.5 py-1 ${style.bg} ${style.text} rounded-full text-xs font-semibold"
        >${name}</span
      >`;
    };

    const deviceCard = (device: DeviceSummaryDto) => html`
      <a
        href="/iot/devices/${device.id}"
        class="block bg-white rounded-2xl border border-stone-200/60 shadow-sm p-5 hover:shadow-md transition-shadow group"
      >
        <div class="flex items-center justify-between mb-3">
          <h3
            class="text-sm font-bold text-stone-800 truncate group-hover:text-orange-700 transition-colors"
          >
            ${device.name}
          </h3>
          ${statusBadge(device.status)}
        </div>
        <p class="text-xs text-stone-400 font-mono mb-1">${device.deviceCode}</p>
        <p class="text-xs text-stone-500">
          ${SENSOR_TYPES[device.sensorType] ?? "Unknown"}
        </p>
      </a>
    `;

    const sensorOptions = SENSOR_TYPES.map(
      (name, i) =>
        html`<option value="${String(i)}">${name}</option>`,
    );

    const template = html`
      <div class="px-6 py-8 max-w-7xl mx-auto">
        <!-- Header -->
        <div
          class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-8 gap-4"
        >
          <div>
            <h1
              class="text-3xl font-extrabold text-stone-800 tracking-tight"
            >
              IoT Devices
            </h1>
            <p class="text-stone-500 mt-1">
              Manage your connected agricultural sensors and hardware.
            </p>
          </div>
          <div class="flex gap-3">
            <a
              href="/iot/zones"
              class="bg-white border border-stone-200 hover:bg-stone-50 text-stone-700 px-5 py-2.5 rounded-lg font-semibold shadow-sm transition"
              >Manage Zones</a
            >
            <button
              type="button"
              class="bg-orange-600 hover:bg-orange-700 text-white px-5 py-2.5 rounded-lg font-semibold shadow-sm transition"
              @click="${() => {
                state.showRegForm = !state.showRegForm;
                state.regError = null;
              }}"
            >
              ${() => (state.showRegForm ? "Cancel" : "Register Device")}
            </button>
          </div>
        </div>

        <!-- Inline Registration Form -->
        ${() =>
          state.showRegForm
            ? html`
                <form
                  class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-6 mb-6"
                  @submit="${handleRegister}"
                >
                  <h2
                    class="text-sm font-bold text-stone-800 uppercase tracking-wider mb-4"
                  >
                    Register New Device
                  </h2>
                  ${() =>
                    state.regError
                      ? html`<div
                          class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm"
                        >
                          ${state.regError}
                        </div>`
                      : html``}
                  <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-4">
                    <div>
                      <label
                        class="block text-xs font-semibold text-stone-600 mb-1"
                        >Device Code</label
                      >
                      <input
                        type="text"
                        placeholder="e.g. SENSOR-001"
                        class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none transition"
                        value="${() => state.regCode}"
                        @input="${(e: Event) => {
                          state.regCode = (e.target as HTMLInputElement).value;
                        }}"
                      />
                    </div>
                    <div>
                      <label
                        class="block text-xs font-semibold text-stone-600 mb-1"
                        >Name</label
                      >
                      <input
                        type="text"
                        placeholder="e.g. Greenhouse Temp"
                        class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none transition"
                        value="${() => state.regName}"
                        @input="${(e: Event) => {
                          state.regName = (e.target as HTMLInputElement).value;
                        }}"
                      />
                    </div>
                    <div>
                      <label
                        class="block text-xs font-semibold text-stone-600 mb-1"
                        >Sensor Type</label
                      >
                      <select
                        class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-orange-200 focus:border-orange-400 outline-none transition"
                        @change="${(e: Event) => {
                          state.regSensor = (e.target as HTMLSelectElement).value;
                        }}"
                      >
                        ${sensorOptions}
                      </select>
                    </div>
                  </div>
                  <button
                    type="submit"
                    class="bg-orange-600 hover:bg-orange-700 text-white px-6 py-2 rounded-lg font-semibold shadow-sm transition disabled:opacity-50"
                    disabled="${() => state.registering}"
                  >
                    ${() => (state.registering ? "Registering..." : "Register")}
                  </button>
                </form>
              `
            : html``}

        ${() =>
          state.loading
            ? html`
                <div class="flex items-center justify-center py-20">
                  <div
                    class="animate-spin w-8 h-8 border-4 border-stone-200 border-t-orange-500 rounded-full"
                  ></div>
                </div>
              `
            : html`
                <div>
                  ${() =>
                    state.error
                      ? html`<div
                          class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm"
                        >
                          ${state.error}
                        </div>`
                      : html``}

                  <!-- KPI Row -->
                  <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                    ${ArrowKPICard({
                      label: "Total Devices",
                      value: () => String(totalCount()),
                      icon: "\uD83D\uDCE1",
                      color: "sky",
                    })}
                    ${ArrowKPICard({
                      label: "Active",
                      value: () => String(activeCount()),
                      icon: "\u2705",
                      color: "emerald",
                    })}
                    ${ArrowKPICard({
                      label: "Offline",
                      value: () => String(offlineCount()),
                      icon: "\uD83D\uDEAB",
                      color: "red",
                    })}
                    ${ArrowKPICard({
                      label: "Unassigned",
                      value: () => String(unassignedCount()),
                      icon: "\uD83D\uDD17",
                      color: "amber",
                    })}
                  </div>

                  <!-- Device Grid -->
                  ${() =>
                    state.devices.length === 0
                      ? ArrowEmptyState({
                          icon: "\uD83D\uDCE1",
                          title: "No devices registered",
                          message:
                            "Register your first IoT device to start monitoring your farm.",
                        })
                      : html`
                          <div
                            class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4"
                          >
                            ${() =>
                              state.devices.map((device) =>
                                deviceCard(device),
                              )}
                          </div>
                        `}
                </div>
              `}
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
