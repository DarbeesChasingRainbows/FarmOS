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

// ─── Apiary API ────────────────────────────────────────────────────

export const ApiaryAPI = {
  createHive: (cmd: { name: string; location: string; hiveType: number }) =>
    fetchFarmOS<{ id: string }>("/api/apiary/hives", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  inspectHive: (hiveId: string, cmd: {
    queenSeen: boolean;
    broodPattern: string;
    temperament: string;
    mitesPerHundred: number;
    notes: string;
    date: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/inspect`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  harvestHoney: (hiveId: string, cmd: {
    supers: number;
    estimatedYield: { value: number; unit: string; type: string };
    date: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/harvest`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  treatHive: (hiveId: string, cmd: {
    treatment: string;
    method: string;
    date: string;
    notes?: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/treat`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // ─── Feature 6: Multi-Product Harvest ──────────────────────────
  harvestProduct: (hiveId: string, cmd: {
    data: {
      product: number;
      yield: { value: number; unit: string; measure: string };
      date: string;
      method?: string;
      notes?: string;
      moisturePercent?: number;
    };
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/harvest/product`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // ─── Feature 2: Queen Tracking ────────────────────────────────
  introduceQueen: (hiveId: string, cmd: {
    queen: {
      color?: number;
      origin: number;
      introducedDate: string;
      breed?: string;
      notes?: string;
    };
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/queen`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  markQueenLost: (hiveId: string, cmd: {
    reason: string;
    date: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/queen/lost`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  replaceQueen: (hiveId: string, cmd: {
    newQueen: {
      color?: number;
      origin: number;
      introducedDate: string;
      breed?: string;
      notes?: string;
    };
    reason: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/queen/replace`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // ─── Feature 3: Feeding ───────────────────────────────────────
  feedHive: (hiveId: string, cmd: {
    data: {
      feedType: number;
      amount: { value: number; unit: string; measure: string };
      concentration?: string;
      date: string;
      notes?: string;
    };
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/feed`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  // ─── Feature 4: Colony Splitting & Merging ─────────────────────
  splitColony: (hiveId: string, cmd: {
    newHiveName: string;
    newHiveType: number;
    newPosition: { latitude: number; longitude: number };
    date: string;
  }) =>
    fetchFarmOS<{ id: string }>(`/api/apiary/hives/${hiveId}/split`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  mergeColonies: (survivingHiveId: string, cmd: {
    absorbedHiveId: string;
    date: string;
  }) =>
    fetchFarmOS(`/api/apiary/hives/${survivingHiveId}/merge`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // ─── Feature 5: Equipment/Super Tracking ──────────────────────
  addSuper: (hiveId: string) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/super/add`, {
      method: "POST",
    }),

  removeSuper: (hiveId: string) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/super/remove`, {
      method: "POST",
    }),

  updateConfiguration: (hiveId: string, cmd: {
    config: {
      broodBoxes: number;
      honeySupers: number;
      frameType: number;
      excluderInstalled: boolean;
    };
  }) =>
    fetchFarmOS(`/api/apiary/hives/${hiveId}/configuration`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Feature 1: Apiary Location API ────────────────────────────────

export const ApiaryLocationAPI = {
  createApiary: (cmd: {
    name: string;
    position: { latitude: number; longitude: number };
    maxCapacity: number;
    notes?: string;
  }) =>
    fetchFarmOS<{ id: string }>("/api/apiary/apiaries", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  moveHiveToApiary: (apiaryId: string, cmd: { hiveId: string }) =>
    fetchFarmOS(`/api/apiary/apiaries/${apiaryId}/hives`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  retireApiary: (apiaryId: string, cmd: { reason: string }) =>
    fetchFarmOS(`/api/apiary/apiaries/${apiaryId}/retire`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};

// ─── Read-Side APIs (Reports, Calendar, Sensors, Weather, Financials) ──

export interface HiveSummary {
  id: string;
  name: string;
  type: string;
  status: string;
  apiaryId?: string;
  queenStatus?: string;
  lastInspection?: string;
  miteCount?: number;
  honeySupers: number;
}

export interface ApiaryOverview {
  id: string;
  name: string;
  status: string;
  capacity: number;
  hiveCount: number;
}

export interface MiteTrendPoint {
  date: string;
  hiveId: string;
  hiveName: string;
  miteCount: number;
}

export interface YieldReport {
  year: number;
  totalHoneyLbs: number;
  totalWaxLbs: number;
  harvestCount: number;
  byProduct: Record<string, number>;
}

export interface ColonySurvivalReport {
  totalCreated: number;
  currentlyActive: number;
  dead: number;
  swarmed: number;
  survivalRate: number;
}

export interface SeasonalTask {
  month: number;
  title: string;
  description: string;
  priority: string;
  category: string;
}

export interface FinancialSummary {
  totalExpenses: number;
  totalRevenue: number;
  netProfit: number;
}

export interface ExpenseEntry {
  date: string;
  category: string;
  description: string;
  amount: number;
}

export interface RevenueEntry {
  date: string;
  product: string;
  description: string;
  amount: number;
}

export interface SensorReading {
  timestamp: string;
  sensorType: string;
  value: number;
  unit: string;
}

export interface SensorSummary {
  hiveId: string;
  temperature?: number;
  humidity?: number;
  weight?: number;
  lastReading?: string;
}

export interface WeightTrend {
  date: string;
  weightKg: number;
}

export interface WeatherCorrelation {
  date: string;
  tempF: number;
  humidity: number;
  miteCount?: number;
  honeyFrames: number;
  hiveId: string;
  hiveName: string;
}

export interface WeatherSnapshot {
  tempF: number;
  humidity: number;
  windMph?: number;
  conditions?: string;
  source?: string;
}

export const ApiaryReportsAPI = {
  getAllHives: () =>
    fetchFarmOS<HiveSummary[]>("/api/apiary/hives"),

  getAllApiaries: () =>
    fetchFarmOS<ApiaryOverview[]>("/api/apiary/apiaries"),

  getMiteTrends: (hiveId?: string) =>
    fetchFarmOS<MiteTrendPoint[]>(
      `/api/apiary/reports/mite-trends${hiveId ? `?hiveId=${hiveId}` : ""}`
    ),

  getYieldReport: (year?: number) =>
    fetchFarmOS<YieldReport>(
      `/api/apiary/reports/yield${year ? `?year=${year}` : ""}`
    ),

  getSurvivalReport: () =>
    fetchFarmOS<ColonySurvivalReport>("/api/apiary/reports/survival"),

  getCalendar: (month?: number) =>
    fetchFarmOS<SeasonalTask[]>(
      `/api/apiary/calendar${month ? `?month=${month}` : ""}`
    ),

  getWeatherCorrelations: () =>
    fetchFarmOS<WeatherCorrelation[]>("/api/apiary/reports/weather-correlation"),

  getCurrentWeather: (lat: number, lng: number) =>
    fetchFarmOS<WeatherSnapshot | null>(
      `/api/apiary/weather/current?lat=${lat}&lng=${lng}`
    ),
};

export const ApiaryFinancialsAPI = {
  getSummary: () =>
    fetchFarmOS<FinancialSummary>("/api/apiary/financials/summary"),

  getExpenses: () =>
    fetchFarmOS<ExpenseEntry[]>("/api/apiary/financials/expenses"),

  getRevenue: () =>
    fetchFarmOS<RevenueEntry[]>("/api/apiary/financials/revenue"),
};

export const ApiarySensorsAPI = {
  getReadings: (hiveId: string) =>
    fetchFarmOS<SensorReading[]>(`/api/apiary/hives/${hiveId}/sensors`),

  getSummary: (hiveId: string) =>
    fetchFarmOS<SensorSummary | null>(`/api/apiary/hives/${hiveId}/sensors/summary`),

  getWeightTrend: (hiveId: string) =>
    fetchFarmOS<WeightTrend[]>(`/api/apiary/hives/${hiveId}/sensors/weight-trend`),
};
