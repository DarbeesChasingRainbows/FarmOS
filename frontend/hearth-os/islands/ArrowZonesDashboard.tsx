import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { ArrowKPICard } from "../components/ArrowKPICard.ts";
import { ArrowEmptyState } from "../components/ArrowEmptyState.ts";
import { ArrowFormField } from "../components/ArrowFormField.ts";
import type { ZoneSummaryDto } from "../utils/farmos-client.ts";

const ZONE_TYPES = [
  "Greenhouse",
  "Field",
  "Barn",
  "Cellar",
  "Storage",
  "Other",
];

export default function ArrowZonesDashboard() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      zones: [] as ZoneSummaryDto[],
      loading: true,
      error: "",
      showForm: false,
      formName: "",
      formZoneType: 0,
      formDescription: "",
      formError: "",
      creating: false,
    });

    const fetchZones = async () => {
      state.loading = true;
      state.error = "";
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        state.zones = (await IoTAPI.getZones()) || [];
      } catch (err: unknown) {
        state.error = err instanceof Error
          ? err.message
          : "Failed to load zones";
      } finally {
        state.loading = false;
      }
    };

    const handleCreate = async () => {
      if (!state.formName.trim()) {
        state.formError = "Zone name is required";
        return;
      }
      state.formError = "";
      state.creating = true;
      try {
        const { IoTAPI } = await import("../utils/farmos-client.ts");
        await IoTAPI.createZone({
          name: state.formName.trim(),
          zoneType: state.formZoneType,
          description: state.formDescription.trim() || undefined,
        });
        state.formName = "";
        state.formZoneType = 0;
        state.formDescription = "";
        state.showForm = false;
        await fetchZones();
      } catch (err: unknown) {
        state.formError = err instanceof Error
          ? err.message
          : "Failed to create zone";
      } finally {
        state.creating = false;
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

    const inlineForm = () =>
      html`
        <div class="bg-white rounded-2xl border border-stone-200 shadow-sm p-6 mb-6">
          <h3 class="text-sm font-bold text-stone-700 uppercase tracking-wider mb-4">
            New Zone
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            ${ArrowFormField({
              label: "Zone Name",
              required: true,
              error: () => state.formError,
              children: html`
                <input
                  type="text"
                  value="${() => state.formName}"
                  @input="${(e: Event) => {
                    state.formName = (e.target as HTMLInputElement).value;
                  }}"
                  placeholder="e.g. Greenhouse A"
                  class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-emerald-200 focus:border-emerald-400 outline-none transition"
                />
              `,
            })} ${ArrowFormField({
              label: "Zone Type",
              children: html`
                <select
                  @change="${(e: Event) => {
                    state.formZoneType = Number(
                      (e.target as HTMLSelectElement).value,
                    );
                  }}"
                  class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-emerald-200 focus:border-emerald-400 outline-none transition bg-white"
                >
                  ${ZONE_TYPES.map((t, i) =>
                    html`
                      <option value="${i}">${t}</option>
                    `
                  )}
                </select>
              `,
            })} ${ArrowFormField({
              label: "Description",
              children: html`
                <input
                  type="text"
                  value="${() => state.formDescription}"
                  @input="${(e: Event) => {
                    state.formDescription =
                      (e.target as HTMLInputElement).value;
                  }}"
                  placeholder="Optional description"
                  class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:ring-2 focus:ring-emerald-200 focus:border-emerald-400 outline-none transition"
                />
              `,
            })}
          </div>
          <div class="flex items-center gap-3">
            <button
              type="button"
              @click="${handleCreate}"
              disabled="${() => state.creating}"
              class="px-4 py-2 bg-emerald-600 text-white font-semibold rounded-lg hover:bg-emerald-700 transition shadow-sm disabled:opacity-50 text-sm"
            >
              ${() => state.creating ? "Creating..." : "Create Zone"}
            </button>
            <button
              type="button"
              @click="${() => {
                state.showForm = false;
                state.formError = "";
              }}"
              class="px-4 py-2 text-stone-600 hover:bg-stone-100 rounded-lg transition text-sm font-medium"
            >
              Cancel
            </button>
          </div>
        </div>
      `;

    const zoneCard = (zone: ZoneSummaryDto) =>
      html`
        <a
          href="${`/iot/zones/${zone.id}`}"
          class="bg-white rounded-2xl border border-stone-200/60 shadow-sm p-5 hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 block"
        >
          <div class="flex items-center justify-between mb-3">
            <h3 class="font-bold text-stone-800 text-sm truncate">${zone
              .name}</h3>
            <span class="${`text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full border ${
              zoneTypeColor(zone.zoneType)
            }`}">
              ${zoneTypeName(zone.zoneType)}
            </span>
          </div>
          <div class="text-xs text-stone-400 font-mono truncate">${zone
            .id}</div>
          ${zone.parentZoneId
            ? html`
              <p class="text-xs text-stone-500 mt-2">
                Parent: <span class="font-mono">${zone.parentZoneId}</span>
              </p>
            `
            : html`

            `}
        </a>
      `;

    const template = html`
      <div class="p-8 max-w-7xl mx-auto">
        <!-- Back link -->
        <div class="mb-8">
          <a
            href="/iot"
            class="text-emerald-600 hover:text-emerald-800 font-semibold mb-4 inline-block transition"
          >
            &larr; Back to Devices
          </a>
          <div
            class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4"
          >
            <div>
              <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
                IoT Zones
              </h1>
              <p class="text-stone-500 mt-1">
                Group and locate your devices hierarchically across the farm.
              </p>
            </div>
            <button
              type="button"
              @click="${() => {
                state.showForm = !state.showForm;
              }}"
              class="px-4 py-2 bg-emerald-600 text-white font-semibold rounded-lg hover:bg-emerald-700 transition shadow-sm text-sm"
            >
              ${() => state.showForm ? "Hide Form" : "Create Zone"}
            </button>
          </div>
        </div>

        <!-- Inline form -->
        ${() =>
          state.showForm ? inlineForm() : html`

          `}

        <!-- KPI -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          ${ArrowKPICard({
            label: "Total Zones",
            value: () => String(state.zones.length),
            icon: "🗺️",
            color: "emerald",
          })}
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
              <div class="text-center py-12 text-stone-400">Loading zones...</div>
            `
            : html`

            `}

        <!-- Zone grid or empty state -->
        ${() => {
          if (state.loading) {
            return html`

            `;
          }
          if (state.zones.length === 0) {
            return ArrowEmptyState({
              icon: "🗺️",
              title: "No zones created",
              message:
                "Create your first zone to organize your sensor network across the farm.",
            });
          }
          return html`
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              ${state.zones.map((z: ZoneSummaryDto) => zoneCard(z))}
            </div>
          `;
        }}
      </div>
    `;

    template(containerRef.current);
    fetchZones();
  }, []);

  return <div ref={containerRef}></div>;
}
