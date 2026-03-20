// Re-export shared client infrastructure
export { ApiError, fetchFarmOS } from "../../shared/farmos-client.ts";
import { fetchFarmOS } from "../../shared/farmos-client.ts";

// ─── Hearth API ────────────────────────────────────────────────────

export const HearthAPI = {
  // Sourdough
  startSourdough: (cmd: {
    batchCode: string;
    starterId: string;
    ingredients: {
      name: string;
      amount: { value: number; unit: string; type: string };
    }[];
  }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/sourdough", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordSourdoughCCP: (batchId: string, cmd: {
    step: string;
    pH: number;
    temperature: number;
    measuredAt: string;
  }) =>
    fetchFarmOS(`/api/hearth/sourdough/${batchId}/ccp`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  advanceSourdough: (batchId: string, cmd: { newPhase: number }) =>
    fetchFarmOS(`/api/hearth/sourdough/${batchId}/advance`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  completeSourdough: (
    batchId: string,
    cmd: { yieldQty: { value: number; unit: string; type: string } },
  ) =>
    fetchFarmOS(`/api/hearth/sourdough/${batchId}/complete`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Kombucha
  startKombucha: (cmd: {
    batchCode: string;
    scobyCultureId: string;
    teaType: string;
    sugarGrams: number;
  }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/kombucha", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordKombuchaPH: (
    batchId: string,
    cmd: { pH: number; temperature: number; notes?: string },
  ) =>
    fetchFarmOS(`/api/hearth/kombucha/${batchId}/ph`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  addKombuchaFlavoring: (
    batchId: string,
    cmd: {
      flavorName: string;
      amount: { value: number; unit: string; type: string };
    },
  ) =>
    fetchFarmOS(`/api/hearth/kombucha/${batchId}/flavor`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  advanceKombucha: (batchId: string, cmd: { newPhase: number }) =>
    fetchFarmOS(`/api/hearth/kombucha/${batchId}/advance`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  completeKombucha: (
    batchId: string,
    cmd: { yieldQty: { value: number; unit: string; type: string } },
  ) =>
    fetchFarmOS(`/api/hearth/kombucha/${batchId}/complete`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Cultures
  createCulture: (cmd: { name: string; type: number; origin: string }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/cultures", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  feedCulture: (cultureId: string, cmd: {
    flourGrams: number;
    waterGrams: number;
    notes?: string;
  }) =>
    fetchFarmOS(`/api/hearth/cultures/${cultureId}/feed`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  splitCulture: (cultureId: string, cmd: { newName: string }) =>
    fetchFarmOS<{ id: string }>(`/api/hearth/cultures/${cultureId}/split`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Mushroom API ──────────────────────────────────────────────────

export const MushroomAPI = {
  startBatch: (
    cmd: {
      batchCode: string;
      species: string;
      substrateType: string;
      inoculatedAt: string;
    },
  ) =>
    fetchFarmOS<{ id: string }>("/api/hearth/mushrooms", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordTemperature: (
    batchId: string,
    cmd: { temperatureF: number; notes?: string },
  ) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/temperature`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordHumidity: (
    batchId: string,
    cmd: { humidityPercent: number; notes?: string },
  ) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/humidity`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  advancePhase: (batchId: string, cmd: { newPhase: string }) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/advance`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordFlush: (
    batchId: string,
    cmd: {
      yieldQty: { value: number; unit: string; type: string };
      flushNumber: number;
      date: string;
    },
  ) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/flush`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  markContaminated: (batchId: string, cmd: { reason: string }) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/contaminate`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  completeBatch: (
    batchId: string,
    cmd: { totalYield: { value: number; unit: string; type: string } },
  ) =>
    fetchFarmOS(`/api/hearth/mushrooms/${batchId}/complete`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Kitchen (Compliance + IoT) API ────────────────────────────

export const KitchenAPI = {
  logTemp: (cmd: { equipmentId: string; tempF: number; notes?: string }) =>
    fetchFarmOS("/api/hearth/kitchen/temps", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  logSanitation: (cmd: {
    surfaceType: number;
    area: string;
    cleaningMethod: string;
    sanitizer: number;
    sanitizerPpm?: number;
    cleanedBy: string;
    notes?: string;
  }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/kitchen/sanitation", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  addCert: (cmd: {
    staffName: string;
    certType: string;
    issuedDate: string;
    expiryDate: string;
    issuer: string;
    notes?: string;
  }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/kitchen/certs", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  logDelivery: (cmd: {
    supplier: string;
    items: string;
    arrivalTempF: number;
    receivedBy: string;
    accepted: boolean;
    notes?: string;
  }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/kitchen/deliveries", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  ingestReading: (cmd: {
    deviceId: string;
    sensorType: string;
    value: number;
    unit: string;
  }) =>
    fetchFarmOS<{ level: string; message: string; correctiveAction?: string }>(
      "/api/hearth/iot/readings",
      { method: "POST", body: JSON.stringify(cmd) },
    ),
};

// ─── HACCP / Compliance API ────────────────────────────────────────

export const HACCPAPI = {
  createPlan: (cmd: { planName: string; facilityName: string }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/compliance/haccp/plans", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  addCCPDefinition: (planId: string, cmd: {
    definition: {
      product: string;
      ccpName: string;
      hazardType: number;
      criticalLimitExpression: string;
      monitoringProcedure: string;
      defaultCorrectiveAction: string;
    };
  }) =>
    fetchFarmOS(`/api/hearth/compliance/haccp/plans/${planId}/ccps`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  removeCCPDefinition: (planId: string, cmd: { product: string; ccpName: string }) =>
    fetchFarmOS(`/api/hearth/compliance/haccp/plans/${planId}/ccps`, {
      method: "DELETE",
      body: JSON.stringify(cmd),
    }),

  openCAPA: (cmd: { description: string; deviationSource: string; relatedCTE?: number }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/compliance/capa", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  closeCAPA: (capaId: string, cmd: { resolution: string; verifiedBy: string }) =>
    fetchFarmOS(`/api/hearth/compliance/capa/${capaId}/close`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  appendCorrection: (logId: string, cmd: { reason: string; correctedValueF?: number; correctedBy: string }) =>
    fetchFarmOS(`/api/hearth/kitchen/temps/${logId}/correction`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Freeze-Dryer API ─────────────────────────────────────────────

export interface FreezeDryerBatchDto {
  id: string;
  batchCode: string;
  productDescription: string;
  phase: number;
  preDryWeight: number;
  postDryWeight?: number;
}

export const FreezeDryerAPI = {
  getBatches: () => fetchFarmOS<FreezeDryerBatchDto[]>("/api/hearth/freeze-dryer"),

  startBatch: (cmd: { batchCode: string; dryerId: string; productDescription: string; preDryWeight: number }) =>
    fetchFarmOS<{ id: string }>("/api/hearth/freeze-dryer", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  recordReading: (batchId: string, cmd: {
    reading: { shelfTempF: number; vacuumMTorr: number; productTempF?: number; notes?: string };
  }) =>
    fetchFarmOS(`/api/hearth/freeze-dryer/${batchId}/readings`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  advancePhase: (batchId: string, cmd: { nextPhase: number }) =>
    fetchFarmOS(`/api/hearth/freeze-dryer/${batchId}/advance`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  complete: (batchId: string, cmd: { postDryWeight: number }) =>
    fetchFarmOS(`/api/hearth/freeze-dryer/${batchId}/complete`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  abort: (batchId: string, cmd: { reason: string }) =>
    fetchFarmOS(`/api/hearth/freeze-dryer/${batchId}/abort`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Harvest Right IoT API ────────────────────────────────────────

export interface HarvestRightDryer {
  dryerId: number;
  serial: string;
  name: string;
  model: string;
  firmwareVersion: string;
}

export interface HarvestRightStatus {
  connected: boolean;
  dryers: HarvestRightDryer[];
  lastTelemetryAt: string | null;
}

export const HarvestRightAPI = {
  getStatus: () =>
    fetchFarmOS<HarvestRightStatus>("/api/hearth/harvest-right/status"),
};

// ─── Apiary API ────────────────────────────────────────────────────
// Moved to standalone Apiary OS micro-frontend.

// ─── IoT Types & API ───────────────────────────────────────────────

export interface GridPosition {
  x: number;
  y: number;
  z: number;
  note?: string;
}

export interface GeoPosition {
  latitude: number;
  longitude: number;
  altitude?: number;
}

export interface DeviceSummaryDto {
  id: string;
  deviceCode: string;
  name: string;
  sensorType: number;
  status: number;
  zoneId?: string;
}

export interface DeviceAssignment {
  asset: {
    context: string;
    assetType: string;
    assetId: string;
  };
  assignedAt: string;
}

export interface DeviceDetailDto extends DeviceSummaryDto {
  gridPos?: GridPosition;
  geoPos?: GeoPosition;
  assignments: DeviceAssignment[];
  metadata: Record<string, string>;
}

export interface ZoneSummaryDto {
  id: string;
  name: string;
  zoneType: number;
  parentZoneId?: string;
}

export interface ZoneDetailDto extends ZoneSummaryDto {
  description?: string;
  gridPos?: GridPosition;
  geoPos?: GeoPosition;
  isArchived: boolean;
  devices: DeviceSummaryDto[];
}

export const IoTAPI = {
  // Devices
  getDevices: () => fetchFarmOS<DeviceSummaryDto[]>("/api/iot/devices"),
  getDevice: (id: string) => fetchFarmOS<DeviceDetailDto>(`/api/iot/devices/${id}`),
  registerDevice: (cmd: { deviceCode: string; name: string; sensorType: number; metadata?: Record<string, string> }) =>
    fetchFarmOS<{ id: string }>("/api/iot/devices", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  updateDevice: (id: string, cmd: { deviceId: string; name: string; status: number; metadata?: Record<string, string> }) =>
    fetchFarmOS(`/api/iot/devices/${id}`, {
      method: "PUT",
      body: JSON.stringify(cmd),
    }),
  decommissionDevice: (id: string, cmd: { deviceId: string; reason: string }) =>
    fetchFarmOS(`/api/iot/devices/${id}/decommission`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  assignDeviceToZone: (id: string, cmd: { deviceId: string; zoneId: string; gridPos?: GridPosition; geoPos?: GeoPosition }) =>
    fetchFarmOS(`/api/iot/devices/${id}/zone`, {
      method: "PUT",
      body: JSON.stringify(cmd),
    }),
  unassignDeviceFromZone: (id: string) =>
    fetchFarmOS(`/api/iot/devices/${id}/zone`, {
      method: "DELETE",
    }),

  // Zones
  getZones: () => fetchFarmOS<ZoneSummaryDto[]>("/api/iot/zones"),
  getZone: (id: string) => fetchFarmOS<ZoneDetailDto>(`/api/iot/zones/${id}`),
  createZone: (cmd: { name: string; zoneType: number; description?: string; parentZoneId?: string; gridPos?: GridPosition; geoPos?: GeoPosition }) =>
    fetchFarmOS<{ id: string }>("/api/iot/zones", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  updateZone: (id: string, cmd: { zoneId: string; name: string; description?: string }) =>
    fetchFarmOS(`/api/iot/zones/${id}`, {
      method: "PUT",
      body: JSON.stringify(cmd),
    }),
  archiveZone: (id: string, cmd: { zoneId: string; reason: string }) =>
    fetchFarmOS(`/api/iot/zones/${id}/archive`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};
