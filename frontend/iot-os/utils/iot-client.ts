/**
 * IoT OS API client — wraps IoT telemetry + device + zone endpoints.
 * All requests go through the Gateway (Caddy reverse proxy).
 */

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

const GATEWAY_URL = typeof Deno !== "undefined"
  ? (Deno.env.get("FARMOS_GATEWAY_URL") || Deno.env.get("GATEWAY_URL") ||
    "http://localhost:5050")
  : "http://localhost:5050";

export async function fetchIoT<T = unknown>(
  path: string,
  options: RequestInit = {},
): Promise<T | null> {
  const headers = new Headers(options.headers);
  if (!headers.has("Content-Type") && options.method && options.method !== "GET") {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;
  try {
    response = await fetch(`${GATEWAY_URL}${path}`, { ...options, headers });
  } catch (_err) {
    throw new ApiError(503, "Gateway unreachable");
  }

  if (!response.ok) {
    if (response.status === 400) {
      const err = await response.json();
      throw new Error(err.message || "Domain rule violation");
    }
    if (response.status === 404) throw new ApiError(404, "Not found");
    throw new ApiError(response.status, `HTTP ${response.status}: ${response.statusText}`);
  }

  if (response.status === 204) return null;
  return response.json() as Promise<T>;
}

// ─── Types ────────────────────────────────────────────────────────────

export interface DeviceSummaryDto {
  id: string;
  deviceCode: string;
  name: string;
  sensorType: number;
  status: number;
  zoneId?: string;
}

export interface ZoneSummaryDto {
  id: string;
  name: string;
  zoneType: number;
  parentZoneId?: string;
}

export interface ClimateLogEntry {
  deviceCode: string;
  sensorType: string;
  value: number;
  unit: string;
  timestamp: string;
  zoneId?: string;
  zoneName?: string;
}

export interface ComplianceReport {
  zoneId: string;
  zoneName: string;
  totalReadings: number;
  inRangeReadings: number;
  compliancePercent: number;
  violations: ComplianceViolation[];
}

export interface ComplianceViolation {
  sensorType: string;
  value: number;
  threshold: number;
  direction: string;
  timestamp: string;
}

export interface ActiveExcursion {
  excursionId: string;
  deviceId: string;
  zoneId: string;
  sensorType: string;
  severity: string;
  alertMessage: string;
  correctiveAction?: string;
  startedAt: string;
}

// ─── API Methods ──────────────────────────────────────────────────────

export const IoTAPI = {
  // Devices
  getDevices: () => fetchIoT<DeviceSummaryDto[]>("/api/iot/devices"),
  getDevice: (id: string) => fetchIoT<DeviceSummaryDto>(`/api/iot/devices/${id}`),

  // Zones
  getZones: () => fetchIoT<ZoneSummaryDto[]>("/api/iot/zones"),
  getZone: (id: string) => fetchIoT<ZoneSummaryDto>(`/api/iot/zones/${id}`),

  // Telemetry
  getClimateLog: (zoneId?: string) => {
    const params = zoneId ? `?zoneId=${zoneId}` : "";
    return fetchIoT<ClimateLogEntry[]>(`/api/iot/telemetry/climate-log${params}`);
  },

  getComplianceReport: (zoneId?: string) => {
    const params = zoneId ? `?zoneId=${zoneId}` : "";
    return fetchIoT<ComplianceReport[]>(`/api/iot/telemetry/compliance-report${params}`);
  },

  getActiveExcursions: () =>
    fetchIoT<ActiveExcursion[]>("/api/iot/telemetry/active-excursions"),

  // Traceability Graph (via Hearth)
  getRecallGraph: (lotId: string) =>
    fetchIoT(`/api/hearth/compliance/traceability/graph/recall/${lotId}`),
};

// ─── Sensor Helpers ───────────────────────────────────────────────────

export const SENSOR_TYPE_NAMES: Record<number, string> = {
  0: "Temperature",
  1: "Humidity",
  2: "Soil Moisture",
  3: "Light",
  4: "CO2",
  5: "pH",
};

export const ZONE_TYPE_NAMES: Record<number, string> = {
  0: "Freezer",
  1: "Refrigerator",
  2: "Storage",
  3: "FermentationRoom",
  4: "Greenhouse",
  5: "Field",
  6: "Apothecary",
};

export const SENSOR_ICONS: Record<string, string> = {
  Temperature: "🌡️",
  Humidity: "💧",
  pH: "⚗️",
  CO2: "🫧",
  Light: "☀️",
  "Soil Moisture": "🌱",
};
