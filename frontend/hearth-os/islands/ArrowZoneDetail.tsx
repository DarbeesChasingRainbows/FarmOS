import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowConfirmDialog } from "../components/ArrowConfirmDialog.ts";
import type {
  DeviceSummaryDto,
  ZoneDetailDto,
} from "../utils/farmos-client.ts";

const ZONE_TYPES = [
  "Greenhouse",
  "Field",
  "Barn",
  "Cellar",
  "Storage",
  "Other",
];

const STATUS_STYLES: Record<number, { badge: string; label: string }> = {
  0: {
    badge: "bg-amber-100 text-amber-800 border-amber-200",
    label: "Pending",
  },
  1: {
    badge: "bg-emerald-100 text-emerald-800 border-emerald-200",
    label: "Active",
  },
  2: { badge: "bg-red-100 text-red-800 border-red-200", label: "Offline" },
  3: {
    badge: "bg-stone-100 text-stone-700 border-stone-200",
    label: "Maintenance",
  },
  4: {
    badge: "bg-stone-200 text-stone-500 border-stone-300",
    label: "Decommissioned",
  },
};

export interface ArrowZoneDetailProps {
  zoneId: string;
}

export default function ArrowZoneDetail({ zoneId }: ArrowZoneDetailProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      zone: null as ZoneDetailDto | null,
      loading: true,
      error: "",
      showArchiveDialog: false,
      archiving: false,
    });

    const fetchZone = async () => {
      state.loading = true;
      state.error = "";
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.zone = await IoTAPI.getZone(zoneId);
      } catch (err: unknown) {
        state.error = err instanceof Error
          ? err.message
          : "Failed to load zone";
      } finally {
        state.loading = false;
      }
    };

    const handleArchive = async () => {
      state.archiving = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.archiveZone(zoneId, {
          zoneId,
          reason: "Archived by user",
        });
        globalThis.location.href = "/iot/zones";
      } catch (err: unknown) {
        state.error = err instanceof Error
          ? err.message
          : "Failed to archive zone";
        state.showArchiveDialog = false;
      } finally {
        state.archiving = false;
      }
    };

    const zoneTypeName = (type: number) => ZONE_TYPES[type] || "Unknown";

    const zoneTypeColor = (type: number) => {
      const colors = [
        "bg-emerald-100 text-emerald-800 border-emerald-200",
        "bg-amber-100 text-amber-800 border-amber-200",
        "bg-orange-100 text-orange-800 border-orange-200",
        "bg-violet-100 text-violet-800 border-violet-200",
        "bg-sky-100 text-sky-800 border-sky-200",
        "bg-stone-100 text-stone-700 border-stone-200",
      ];
      return colors[type] || colors[5];
    };

    const deviceCard = (device: DeviceSummaryDto) => {
      const s = STATUS_STYLES[device.status] || STATUS_STYLES[3];
      return html`
        <a
          href="${`/iot/devices/${device.id}`}"
          class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-5 hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 block"
        >
          <div class="flex items-center justify-between mb-2">
            <h4 class="font-bold text-stone-800 text-sm truncate">${device
              .name}</h4>
            <span class="${`text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full border ${s.badge}`}">
              ${s.label}
            </span>
          </div>
          <p class="text-xs text-stone-400 font-mono">${device.deviceCode}</p>
        </a>
      `;
    };

    const template = html`
      <div class="p-8 max-w-5xl mx-auto">
        <!-- Back link -->
        <div class="mb-6">
          <a
            href="/iot/zones"
            class="text-emerald-600 hover:text-emerald-800 font-semibold inline-block transition"
          >
            &larr; Back to Zones
          </a>
        </div>

        <!-- Error -->
        ${() =>
          state.error
            ? html`
              <div class="mb-6 p-4 bg-red-50 text-red-700 rounded-xl border border-red-100">
                ${() => state.error}
              </div>
            `
            : html`

            `}

        <!-- Loading -->
        ${() =>
          state.loading
            ? html`
              <div class="text-center py-12 text-stone-400">Loading zone...</div>
            `
            : html`

            `}

        <!-- Zone detail -->
        ${() => {
          if (state.loading || !state.zone) {
            return html`

            `;
          }
          const zone = state.zone;

          return html`
            <!-- Header card -->
            <div
              class="bg-white rounded-2xl border border-stone-200 shadow-sm overflow-hidden mb-8"
            >
              <div
                class="p-8 border-b border-stone-100 flex flex-col md:flex-row justify-between md:items-center gap-6 bg-gradient-to-br from-stone-50 to-white"
              >
                <div>
                  <div class="flex items-center gap-3 mb-2">
                    <span class="${`text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full border ${
                      zoneTypeColor(zone.zoneType)
                    }`}">
                      ${zoneTypeName(zone.zoneType)}
                    </span>
                    ${zone.isArchived
                      ? html`
                        <span
                          class="text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full border bg-stone-200 text-stone-500 border-stone-300"
                        >Archived</span>
                      `
                      : html`

                      `}
                  </div>
                  <h1 class="text-3xl font-bold text-stone-800 tracking-tight">
                    ${zone.name}
                  </h1>
                  ${zone.description
                    ? html`
                      <p class="text-stone-500 mt-1">${zone.description}</p>
                    `
                    : html`

                    `}
                </div>
                ${!zone.isArchived
                  ? html`
                    <button
                      type="button"
                      @click="${() => {
                        state.showArchiveDialog = true;
                      }}"
                      class="px-4 py-2 bg-red-50 text-red-700 font-semibold rounded-lg hover:bg-red-100 transition text-sm border border-red-200"
                    >
                      Archive Zone
                    </button>
                  `
                  : html`

                  `}
              </div>
            </div>

            <!-- Devices in zone -->
            <h2 class="text-lg font-bold text-stone-800 mb-4">Devices in Zone</h2>
            ${zone.devices && zone.devices.length > 0
              ? html`
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                  ${zone.devices.map((d: DeviceSummaryDto) => deviceCard(d))}
                </div>
              `
              : html`
                <div class="bg-stone-50 border border-stone-200 rounded-2xl p-12 text-center">
                  <span class="text-4xl block mb-3">📡</span>
                  <p class="text-lg font-medium text-stone-600 mb-2">No devices in this zone</p>
                  <p class="text-sm text-stone-500">
                    Assign devices from the device detail page.
                  </p>
                </div>
              `}

            <!-- Archive confirm dialog -->
            ${ArrowConfirmDialog({
              isOpen: () => state.showArchiveDialog,
              title: "Archive Zone",
              message:
                "Are you sure you want to archive this zone? Devices will be unassigned.",
              confirmLabel: "Archive",
              danger: true,
              onConfirm: handleArchive,
              onCancel: () => {
                state.showArchiveDialog = false;
              },
            })}
          `;
        }}
      </div>
    `;

    template(containerRef.current);
    fetchZone();
  }, [zoneId]);

  return <div ref={containerRef}></div>;
}
