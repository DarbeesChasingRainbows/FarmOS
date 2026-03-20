// ─── Base fetch ──────────────────────────────────────────────────────────────

const GATEWAY_URL = typeof window !== "undefined"
  ? ((window as unknown as Record<string, string>).__GATEWAY_URL__ ??
    "http://localhost:5050")
  : "http://localhost:5050";

async function fetchAssets<T = unknown>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const res = await fetch(`${GATEWAY_URL}${path}`, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Assets API ${res.status}: ${text}`);
  }
  // 204 No Content
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ─── Equipment API ────────────────────────────────────────────────────────────

export interface EquipmentSummary {
  id: string;
  name: string;
  make: string;
  model: string;
  year?: number;
  status: "Active" | "Maintenance" | "Retired";
  maintenanceCount: number;
  lat: number;
  lng: number;
}

export interface MaintenanceEntry {
  date: string;
  description: string;
  costDollars?: number;
  technician?: string;
}

export const EquipmentAPI = {
  list: () => fetchAssets<EquipmentSummary[]>("/api/assets/equipment"),

  register: (cmd: {
    name: string;
    make: string;
    model: string;
    year?: number;
    location: { lat: number; lng: number };
  }) =>
    fetchAssets<{ id: string }>("/api/assets/equipment", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  logMaintenance: (id: string, record: {
    date: string;
    description: string;
    costDollars?: number;
    technician?: string;
  }) =>
    fetchAssets(`/api/assets/equipment/${id}/maintenance`, {
      method: "POST",
      body: JSON.stringify({ record }),
    }),

  move: (id: string, lat: number, lng: number) =>
    fetchAssets(`/api/assets/equipment/${id}/move`, {
      method: "POST",
      body: JSON.stringify({ newLocation: { lat, lng } }),
    }),

  retire: (id: string, reason: string) =>
    fetchAssets(`/api/assets/equipment/${id}/retire`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),
};

// ─── Compost API ──────────────────────────────────────────────────────────────

export type CompostMethod =
  | "HotAerobic"
  | "ColdPassive"
  | "Permaculture"
  | "KoreanNaturalFarming"
  | "Bokashi"
  | "Vermicompost";
export type CompostPhase =
  | "Active"
  | "Turning"
  | "Fermentation"
  | "Inoculation"
  | "Curing"
  | "Finished"
  | "Abandoned";
export type TempZone =
  | "Optimal"
  | "TooHot"
  | "TooLow"
  | "Fermentation"
  | "Ambient";
export type PhStatus =
  | "Optimal"
  | "TooHigh"
  | "TooLow"
  | "Neutral"
  | "Acidic"
  | "Alkaline";
export type NoteCategory =
  | "Observation"
  | "Amendment"
  | "Issue"
  | "Milestone"
  | "Harvest";
export type KnfInputType =
  | "IMO1"
  | "IMO2"
  | "IMO3"
  | "IMO4"
  | "LAB"
  | "FPJ"
  | "FAA"
  | "WSCA"
  | "OHN";

export interface CompostInputDto {
  material: string;
  amount: string;
  unit: string;
  type: string;
  cnRatio?: number;
}

export interface TempReadingDto {
  timestamp: string;
  temperatureF: number;
  zone: TempZone;
}

export interface TurnEntryDto {
  date: string;
  notes?: string;
  daysSincePrev: number;
}

export interface KnfInputDto {
  inputType: KnfInputType;
  description: string;
  preparedDate: string;
  amount: string;
  unit: string;
}

export interface PhEntryDto {
  date: string;
  pH: number;
  notes?: string;
  status: PhStatus;
}

export interface CompostNoteDto {
  date: string;
  category: NoteCategory;
  body: string;
}

export interface CompostBatchSummary {
  id: string;
  batchCode: string;
  method: CompostMethod;
  phase: CompostPhase;
  cnRatioDisplay?: string;
  carbonRatio?: number;
  nitrogenRatio?: number;
  lastTempF?: number;
  lastTempAt?: string;
  tempZone?: TempZone;
  turnCount: number;
  inoculationCount: number;
  noteCount: number;
  latestPH?: number;
  latestPhDate?: string;
  startedAt: string;
  daysElapsed: number;
  latitude: number;
  longitude: number;
  yieldCuYd?: string;
}

export interface CompostBatchDetail extends CompostBatchSummary {
  startNotes?: string;
  inputs: CompostInputDto[];
  temperatureLog: TempReadingDto[];
  turnLog: TurnEntryDto[];
  inoculations: KnfInputDto[];
  phLog: PhEntryDto[];
  notes: CompostNoteDto[];
}

export const CompostAPI = {
  list: () => fetchAssets<CompostBatchSummary[]>("/api/assets/compost"),

  detail: (id: string) =>
    fetchAssets<CompostBatchDetail>(`/api/assets/compost/${id}`),

  start: (cmd: {
    batchCode: string;
    method: CompostMethod;
    location: { lat: number; lng: number };
    inputs: Array<
      {
        material: string;
        amount: { value: number; unit: string; displayUnit: string };
        type: string;
        cnRatio?: number;
      }
    >;
    carbonRatio?: number;
    nitrogenRatio?: number;
    notes?: string;
  }) =>
    fetchAssets<{ id: string }>("/api/assets/compost", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  logTemp: (id: string, reading: { timestamp: string; temperatureF: number }) =>
    fetchAssets(`/api/assets/compost/${id}/temp`, {
      method: "POST",
      body: JSON.stringify({ reading }),
    }),

  turn: (id: string, date: string, notes?: string) =>
    fetchAssets(`/api/assets/compost/${id}/turn`, {
      method: "POST",
      body: JSON.stringify({ date, notes }),
    }),

  changePhase: (id: string, newPhase: CompostPhase, notes?: string) =>
    fetchAssets(`/api/assets/compost/${id}/phase`, {
      method: "POST",
      body: JSON.stringify({ newPhase, notes }),
    }),

  inoculate: (id: string, input: {
    inputType: KnfInputType;
    description: string;
    preparedDate: string;
    amount: { value: number; unit: string; displayUnit: string };
  }) =>
    fetchAssets(`/api/assets/compost/${id}/inoculate`, {
      method: "POST",
      body: JSON.stringify({ input }),
    }),

  measurePH: (
    id: string,
    measurement: { date: string; pH: number; notes?: string },
  ) =>
    fetchAssets(`/api/assets/compost/${id}/ph`, {
      method: "POST",
      body: JSON.stringify({ measurement }),
    }),

  addNote: (
    id: string,
    note: { date: string; category: NoteCategory; body: string },
  ) =>
    fetchAssets(`/api/assets/compost/${id}/note`, {
      method: "POST",
      body: JSON.stringify({ note }),
    }),

  complete: (
    id: string,
    yieldCuYd: { value: number; unit: string; displayUnit: string },
    notes?: string,
  ) =>
    fetchAssets(`/api/assets/compost/${id}/complete`, {
      method: "POST",
      body: JSON.stringify({ yieldCuYd, notes }),
    }),
};

// ─── Home Assistant Sensor API ────────────────────────────────────────────────

export interface HaSensorSummary {
  entityId: string;
  state: string;
  friendlyName?: string;
  unitOfMeasurement?: string;
  deviceClass?: string;
  icon?: string;
  lastChanged: string;
  lastUpdated: string;
}

export interface HaSensorDetail {
  entityId: string;
  state: string;
  attributes: Record<string, unknown>;
  lastChanged: string;
  lastUpdated: string;
}

export interface HaHistoryEntry {
  state: string;
  lastChanged: string;
}

export const HaSensorAPI = {
  list: () => fetchAssets<HaSensorSummary[]>("/api/assets/ha/sensors"),

  detail: (entityId: string) =>
    fetchAssets<HaSensorDetail>(`/api/assets/ha/sensors/${entityId}`),

  history: (entityId: string, hours = 24) =>
    fetchAssets<HaHistoryEntry[]>(
      `/api/assets/ha/sensors/${entityId}/history?hours=${hours}`,
    ),

  status: () => fetchAssets<{ available: boolean }>("/api/assets/ha/status"),
};
