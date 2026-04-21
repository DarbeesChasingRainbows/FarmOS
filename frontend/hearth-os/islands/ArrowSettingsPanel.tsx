import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";

// ── Dropdown item management types ──────────────────────────────────────
interface DropdownItem {
  id: string;
  label: string;
  description?: string;
}

interface DropdownCategory {
  key: string;
  title: string;
  icon: string;
  items: DropdownItem[];
}

export default function ArrowSettingsPanel() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      activeTab: "general" as string,
      // ── General ────────────────────────────────────────
      farmName: "Darbee's Chasing Rainbows",
      timezone: "America/Chicago",
      units: "imperial" as "imperial" | "metric",
      // ── Dropdown Management ────────────────────────────
      dropdownCategories: [
        {
          key: "cultureTypes",
          title: "Culture Types",
          icon: "🧫",
          items: [
            {
              id: "ct-1",
              label: "Sourdough Starter",
              description: "Wild yeast + lactobacillus for bread leavening",
            },
            {
              id: "ct-2",
              label: "Kombucha SCOBY",
              description: "Symbiotic colony for tea fermentation",
            },
            {
              id: "ct-3",
              label: "Milk Kefir Grains",
              description: "Polysaccharide matrix for milk fermentation",
            },
            {
              id: "ct-4",
              label: "Other",
              description: "Water kefir, tempeh, vinegar mother, etc.",
            },
          ],
        },
        {
          key: "substrateTypes",
          title: "Substrate Types",
          icon: "🍄",
          items: [
            { id: "st-1", label: "Hardwood Sawdust + Soy Hulls" },
            { id: "st-2", label: "Straw" },
            { id: "st-3", label: "Brown Rice Flour + Vermiculite" },
            { id: "st-4", label: "Coffee Grounds" },
          ],
        },
        {
          key: "batchTypes",
          title: "Batch Types",
          icon: "🍞",
          items: [
            { id: "bt-1", label: "Sourdough" },
            { id: "bt-2", label: "Kombucha" },
          ],
        },
        {
          key: "phaseNames",
          title: "Fermentation Phases",
          icon: "🔄",
          items: [
            {
              id: "ph-1",
              label: "Bulk Ferment",
              description: "Primary rise at room temperature",
            },
            { id: "ph-2", label: "Proofing", description: "Final shaped rise" },
            {
              id: "ph-3",
              label: "Primary",
              description: "Initial fermentation",
            },
            {
              id: "ph-4",
              label: "Secondary",
              description: "Flavoring & carbonation",
            },
            { id: "ph-5", label: "Complete", description: "Ready for yield" },
          ],
        },
        {
          key: "sanitationSurfaces",
          title: "Sanitation Surfaces",
          icon: "🧹",
          items: [
            { id: "ss-1", label: "Prep Table" },
            { id: "ss-2", label: "Floor Drain" },
            { id: "ss-3", label: "Exhaust Hood" },
            { id: "ss-4", label: "Walk-in Cooler" },
            { id: "ss-5", label: "Equipment (General)" },
          ],
        },
      ] as DropdownCategory[],
      editingCategoryKey: null as string | null,
      newItemLabel: "",
      newItemDescription: "",
      // ── Notifications ──────────────────────────────────
      phAlertLow: "3.0",
      phAlertHigh: "5.0",
      tempAlertLow: "60",
      tempAlertHigh: "85",
      enableEmailAlerts: true,
      enablePushAlerts: false,
      // ── Integrations ───────────────────────────────────
      iotHubUrl: "http://localhost:5050/hubs/kitchen",
      harvestRightEnabled: false,
      harvestRightSerial: "",
    });

    const tabs = [
      { id: "general", label: "General", icon: "🏠" },
      { id: "dropdowns", label: "Dropdown Management", icon: "📋" },
      { id: "notifications", label: "Notifications", icon: "🔔" },
      { id: "integrations", label: "Integrations", icon: "🔌" },
    ];

    const tabClass = (id: string) => () =>
      `flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all w-full text-left ${
        state.activeTab === id
          ? "bg-amber-50 text-amber-800 border border-amber-200 shadow-sm"
          : "text-stone-500 hover:bg-stone-50 hover:text-stone-700 border border-transparent"
      }`;

    const inputClass =
      "w-full px-4 py-2.5 border border-stone-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 focus:border-transparent transition text-sm";
    const labelClass = "block text-sm font-semibold text-stone-700 mb-1.5";
    const cardClass =
      "bg-white rounded-xl border border-stone-200 p-6 shadow-sm";

    const handleSave = () => {
      showToast("success", "Settings saved", "Your changes have been applied.");
    };

    const addDropdownItem = (categoryKey: string) => {
      if (!state.newItemLabel.trim()) return;
      const cat = state.dropdownCategories.find((c: DropdownCategory) =>
        c.key === categoryKey
      );
      if (!cat) return;
      cat.items.push({
        id: `${categoryKey}-${Date.now()}`,
        label: state.newItemLabel.trim(),
        description: state.newItemDescription.trim() || undefined,
      });
      state.newItemLabel = "";
      state.newItemDescription = "";
      showToast(
        "success",
        "Item added",
        `"${cat.items[cat.items.length - 1].label}" added to ${cat.title}.`,
      );
    };

    const removeDropdownItem = (categoryKey: string, itemId: string) => {
      const cat = state.dropdownCategories.find((c: DropdownCategory) =>
        c.key === categoryKey
      );
      if (!cat) return;
      const item = cat.items.find((i: DropdownItem) => i.id === itemId);
      cat.items = cat.items.filter((i: DropdownItem) => i.id !== itemId);
      if (item) {
        showToast(
          "info",
          "Item removed",
          `"${item.label}" removed from ${cat.title}.`,
        );
      }
    };

    const template = html`
      <div class="flex gap-8 min-h-[600px]">
        <!-- Sidebar Tabs (Jakob's Law: conventional settings layout) -->
        <nav class="w-56 shrink-0">
          <div class="sticky top-8 space-y-1">
            ${tabs.map((tab) =>
              html`
                <button
                  type="button"
                  class="${tabClass(tab.id)}"
                  @click="${() => state.activeTab = tab.id}"
                >
                  <span class="text-lg">${tab.icon}</span>
                  <span>${tab.label}</span>
                </button>
              `
            )}
          </div>
        </nav>

        <!-- Content Area -->
        <div class="flex-1 max-w-2xl">
          <!-- General Tab -->
          ${() =>
            state.activeTab === "general"
              ? html`
                <div class="space-y-6">
                  <div>
                    <h2 class="text-xl font-bold text-stone-800 mb-1">General Settings</h2>
                    <p class="text-sm text-stone-500">
                      Configure your farm identity and measurement preferences.
                    </p>
                  </div>

                  <div class="${cardClass}">
                    <div class="space-y-5">
                      <div>
                        <label class="${labelClass}">Farm Name</label>
                        <input
                          type="text"
                          class="${inputClass}"
                          value="${() => state.farmName}"
                          @input="${(e: Event) =>
                            state.farmName =
                              (e.target as HTMLInputElement).value}"
                        />
                        <p class="text-xs text-stone-400 mt-1">
                          Appears in reports and compliance documents.
                        </p>
                      </div>

                      <div>
                        <label class="${labelClass}">Timezone</label>
                        <select
                          class="${inputClass}"
                          value="${() => state.timezone}"
                          @change="${(e: Event) =>
                            state.timezone =
                              (e.target as HTMLSelectElement).value}"
                        >
                          <option value="America/New_York">Eastern (ET)</option>
                          <option value="America/Chicago">Central (CT)</option>
                          <option value="America/Denver">Mountain (MT)</option>
                          <option value="America/Los_Angeles">Pacific (PT)</option>
                        </select>
                      </div>

                      <div>
                        <label class="${labelClass}">Measurement Units</label>
                        <div class="flex gap-3">
                          <button
                            type="button"
                            @click="${() => state.units = "imperial"}"
                            class="${() =>
                              `flex-1 py-2.5 text-sm font-semibold rounded-lg border transition ${
                                state.units === "imperial"
                                  ? "bg-amber-50 border-amber-300 text-amber-800"
                                  : "bg-white border-stone-200 text-stone-500 hover:bg-stone-50"
                              }`}"
                          >
                            🇺🇸 Imperial (°F, lbs)
                          </button>
                          <button
                            type="button"
                            @click="${() => state.units = "metric"}"
                            class="${() =>
                              `flex-1 py-2.5 text-sm font-semibold rounded-lg border transition ${
                                state.units === "metric"
                                  ? "bg-amber-50 border-amber-300 text-amber-800"
                                  : "bg-white border-stone-200 text-stone-500 hover:bg-stone-50"
                              }`}"
                          >
                            🌍 Metric (°C, kg)
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="flex justify-end">
                    <button
                      @click="${handleSave}"
                      class="bg-amber-600 text-white font-semibold py-2.5 px-6 rounded-lg hover:bg-amber-700 transition shadow-sm"
                    >
                      Save Changes
                    </button>
                  </div>
                </div>
              `
              : ""}

          <!-- Dropdown Management Tab (Miller's Law: chunked groups) -->
          ${() =>
            state.activeTab === "dropdowns"
              ? html`
                <div class="space-y-6">
                  <div>
                    <h2 class="text-xl font-bold text-stone-800 mb-1">Dropdown Management</h2>
                    <p class="text-sm text-stone-500">
                      Add, remove, or reorder items in system dropdowns. Changes apply across
                      all forms.
                    </p>
                  </div>

                  ${() =>
                    state.dropdownCategories.map((cat: DropdownCategory) =>
                      html`
                        <div class="${cardClass}">
                          <div class="flex items-center justify-between mb-4">
                            <h3 class="text-base font-bold text-stone-800 flex items-center gap-2">
                              <span class="text-lg">${cat.icon}</span> ${cat
                                .title}
                              <span
                                class="text-xs font-medium bg-stone-100 text-stone-500 px-2 py-0.5 rounded-full"
                              >${cat.items.length} items</span>
                            </h3>
                            <button
                              type="button"
                              @click="${() =>
                                state.editingCategoryKey =
                                  state.editingCategoryKey === cat.key
                                    ? null
                                    : cat.key}"
                              class="${() =>
                                `text-xs font-semibold px-3 py-1.5 rounded-lg transition ${
                                  state.editingCategoryKey === cat.key
                                    ? "bg-amber-100 text-amber-700"
                                    : "bg-stone-100 text-stone-600 hover:bg-stone-200"
                                }`}"
                            >
                              ${() =>
                                state.editingCategoryKey === cat.key
                                  ? "Done"
                                  : "Edit"}
                            </button>
                          </div>

                          <!-- Item List -->
                          <div class="space-y-2">
                            ${() =>
                              cat.items.map((item: DropdownItem) =>
                                html`
                                  <div
                                    class="flex items-center justify-between px-3 py-2.5 bg-stone-50 rounded-lg border border-stone-100 group"
                                  >
                                    <div>
                                      <span class="text-sm font-medium text-stone-700">${item
                                        .label}</span>
                                      ${item.description
                                        ? html`
                                          <p class="text-xs text-stone-400 mt-0.5">${item
                                            .description}</p>
                                        `
                                        : ""}
                                    </div>
                                    ${() =>
                                      state.editingCategoryKey === cat.key
                                        ? html`
                                          <button
                                            type="button"
                                            @click="${() =>
                                              removeDropdownItem(
                                                cat.key,
                                                item.id,
                                              )}"
                                            class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1.5 rounded-lg transition text-xs font-bold"
                                            title="Remove item"
                                          >
                                            ✕
                                          </button>
                                        `
                                        : ""}
                                  </div>
                                `.key(item.id)
                              )}
                          </div>

                          <!-- Add New Item Form (appears when editing) -->
                          ${() =>
                            state.editingCategoryKey === cat.key
                              ? html`
                                <div class="mt-4 pt-4 border-t border-stone-100">
                                  <div class="flex gap-2">
                                    <div class="flex-1">
                                      <input
                                        type="text"
                                        class="w-full px-3 py-2 border border-stone-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
                                        placeholder="New item name"
                                        value="${() => state.newItemLabel}"
                                        @input="${(e: Event) =>
                                          state.newItemLabel =
                                            (e.target as HTMLInputElement)
                                              .value}"
                                      />
                                    </div>
                                    <button
                                      type="button"
                                      @click="${() => addDropdownItem(cat.key)}"
                                      disabled="${() =>
                                        !state.newItemLabel.trim()}"
                                      class="px-4 py-2 bg-emerald-600 text-white text-sm font-semibold rounded-lg hover:bg-emerald-700 disabled:opacity-50 transition"
                                    >
                                      + Add
                                    </button>
                                  </div>
                                  <input
                                    type="text"
                                    class="w-full mt-2 px-3 py-2 border border-stone-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
                                    placeholder="Description (optional)"
                                    value="${() => state.newItemDescription}"
                                    @input="${(e: Event) =>
                                      state.newItemDescription =
                                        (e.target as HTMLInputElement).value}"
                                  />
                                </div>
                              `
                              : ""}
                        </div>
                      `.key(cat.key)
                    )}
                </div>
              `
              : ""}

          <!-- Notifications Tab -->
          ${() =>
            state.activeTab === "notifications"
              ? html`
                <div class="space-y-6">
                  <div>
                    <h2 class="text-xl font-bold text-stone-800 mb-1">Notification Settings</h2>
                    <p class="text-sm text-stone-500">
                      Configure alert thresholds and notification channels.
                    </p>
                  </div>

                  <div class="${cardClass}">
                    <h3 class="text-base font-bold text-stone-800 mb-4">pH Alert Thresholds</h3>
                    <div class="grid grid-cols-2 gap-4">
                      <div>
                        <label class="${labelClass}">Low Alert (below)</label>
                        <input
                          type="number"
                          step="0.1"
                          class="${inputClass}"
                          value="${() => state.phAlertLow}"
                          @input="${(e: Event) =>
                            state.phAlertLow =
                              (e.target as HTMLInputElement).value}"
                        />
                      </div>
                      <div>
                        <label class="${labelClass}">High Alert (above)</label>
                        <input
                          type="number"
                          step="0.1"
                          class="${inputClass}"
                          value="${() => state.phAlertHigh}"
                          @input="${(e: Event) =>
                            state.phAlertHigh =
                              (e.target as HTMLInputElement).value}"
                        />
                      </div>
                    </div>
                  </div>

                  <div class="${cardClass}">
                    <h3 class="text-base font-bold text-stone-800 mb-4">
                      Temperature Alert Thresholds (°F)
                    </h3>
                    <div class="grid grid-cols-2 gap-4">
                      <div>
                        <label class="${labelClass}">Low Alert (below)</label>
                        <input
                          type="number"
                          class="${inputClass}"
                          value="${() => state.tempAlertLow}"
                          @input="${(e: Event) =>
                            state.tempAlertLow =
                              (e.target as HTMLInputElement).value}"
                        />
                      </div>
                      <div>
                        <label class="${labelClass}">High Alert (above)</label>
                        <input
                          type="number"
                          class="${inputClass}"
                          value="${() => state.tempAlertHigh}"
                          @input="${(e: Event) =>
                            state.tempAlertHigh =
                              (e.target as HTMLInputElement).value}"
                        />
                      </div>
                    </div>
                  </div>

                  <div class="${cardClass}">
                    <h3 class="text-base font-bold text-stone-800 mb-4">Channels</h3>
                    <div class="space-y-4">
                      <label class="flex items-center justify-between cursor-pointer">
                        <div>
                          <span class="text-sm font-semibold text-stone-700">Email Alerts</span>
                          <p class="text-xs text-stone-400">
                            Receive critical alerts via email
                          </p>
                        </div>
                        <div class="${() =>
                          `relative w-12 h-6 rounded-full transition ${
                            state.enableEmailAlerts
                              ? "bg-amber-500"
                              : "bg-stone-300"
                          }`}" @click="${() =>
                          state.enableEmailAlerts = !state.enableEmailAlerts}">
                          <div class="${() =>
                            `absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
                              state.enableEmailAlerts
                                ? "translate-x-6"
                                : "translate-x-0.5"
                            }`}"></div>
                        </div>
                      </label>
                      <label class="flex items-center justify-between cursor-pointer">
                        <div>
                          <span class="text-sm font-semibold text-stone-700"
                          >Push Notifications</span>
                          <p class="text-xs text-stone-400">
                            Browser push for real-time alerts
                          </p>
                        </div>
                        <div class="${() =>
                          `relative w-12 h-6 rounded-full transition ${
                            state.enablePushAlerts
                              ? "bg-amber-500"
                              : "bg-stone-300"
                          }`}" @click="${() =>
                          state.enablePushAlerts = !state.enablePushAlerts}">
                          <div class="${() =>
                            `absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
                              state.enablePushAlerts
                                ? "translate-x-6"
                                : "translate-x-0.5"
                            }`}"></div>
                        </div>
                      </label>
                    </div>
                  </div>

                  <div class="flex justify-end">
                    <button
                      @click="${handleSave}"
                      class="bg-amber-600 text-white font-semibold py-2.5 px-6 rounded-lg hover:bg-amber-700 transition shadow-sm"
                    >
                      Save Notifications
                    </button>
                  </div>
                </div>
              `
              : ""}

          <!-- Integrations Tab -->
          ${() =>
            state.activeTab === "integrations"
              ? html`
                <div class="space-y-6">
                  <div>
                    <h2 class="text-xl font-bold text-stone-800 mb-1">Integrations</h2>
                    <p class="text-sm text-stone-500">
                      Configure connections to IoT hubs, external devices, and third-party
                      services.
                    </p>
                  </div>

                  <div class="${cardClass}">
                    <h3 class="text-base font-bold text-stone-800 mb-4 flex items-center gap-2">
                      📡 SignalR / IoT Hub
                    </h3>
                    <div>
                      <label class="${labelClass}">Hub URL</label>
                      <input
                        type="text"
                        class="${inputClass}"
                        value="${() => state.iotHubUrl}"
                        @input="${(e: Event) =>
                          state.iotHubUrl =
                            (e.target as HTMLInputElement).value}"
                      />
                      <p class="text-xs text-stone-400 mt-1">
                        The SignalR endpoint for real-time sensor data.
                      </p>
                    </div>
                  </div>

                  <div class="${cardClass}">
                    <div class="flex items-center justify-between mb-4">
                      <h3 class="text-base font-bold text-stone-800 flex items-center gap-2">
                        🥶 HarvestRight Freeze Dryer
                      </h3>
                      <div class="${() =>
                        `relative w-12 h-6 rounded-full transition ${
                          state.harvestRightEnabled
                            ? "bg-amber-500"
                            : "bg-stone-300"
                        }`}" @click="${() =>
                        state.harvestRightEnabled = !state
                          .harvestRightEnabled}">
                        <div class="${() =>
                          `absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
                            state.harvestRightEnabled
                              ? "translate-x-6"
                              : "translate-x-0.5"
                          }`}"></div>
                      </div>
                    </div>
                    ${() =>
                      state.harvestRightEnabled
                        ? html`
                          <div>
                            <label class="${labelClass}">Dryer Serial Number</label>
                            <input
                              type="text"
                              class="${inputClass}"
                              placeholder="e.g. HR-12345"
                              value="${() => state.harvestRightSerial}"
                              @input="${(e: Event) =>
                                state.harvestRightSerial =
                                  (e.target as HTMLInputElement).value}"
                            />
                          </div>
                        `
                        : html`
                          <p class="text-sm text-stone-400">
                            Enable to configure your HarvestRight freeze dryer integration.
                          </p>
                        `}
                  </div>

                  <div class="${cardClass}">
                    <h3 class="text-base font-bold text-stone-800 mb-4 flex items-center gap-2">
                      🔑 API Access
                    </h3>
                    <div class="bg-stone-50 rounded-lg p-4 border border-stone-100">
                      <p
                        class="text-xs text-stone-500 mb-2 font-semibold uppercase tracking-wider"
                      >
                        Gateway URL
                      </p>
                      <code
                        class="text-sm text-stone-700 bg-stone-100 px-3 py-1.5 rounded font-mono block"
                      >http://localhost:5050</code>
                    </div>
                  </div>

                  <div class="flex justify-end">
                    <button
                      @click="${handleSave}"
                      class="bg-amber-600 text-white font-semibold py-2.5 px-6 rounded-lg hover:bg-amber-700 transition shadow-sm"
                    >
                      Save Integrations
                    </button>
                  </div>
                </div>
              `
              : ""}
        </div>
      </div>
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
