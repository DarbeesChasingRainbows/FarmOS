// Re-export shared client infrastructure
export { ApiError, fetchFarmOS } from "../../shared/farmos-client.ts";
import { fetchFarmOS } from "../../shared/farmos-client.ts";

// ─── Shared Types ─────────────────────────────────────────────────

export interface Quantity {
  value: number;
  unit: string;
  type: string;
}

export interface CropVariety {
  species: string;
  cultivar: string;
  daysToMaturity: number;
  color?: string;
}

// ─── Bed Types ────────────────────────────────────────────────────

export interface FlowerBedSummary {
  id: string;
  name: string;
  block: string;
  lengthFeet: number;
  widthFeet: number;
  successionCount: number;
}

export interface FlowerBedDetail
  extends Omit<FlowerBedSummary, "successionCount"> {
  successions: Succession[];
}

export interface Succession {
  id: string;
  species: string;
  cultivar: string;
  daysToMaturity: number;
  color?: string;
  sowDate: string;
  transplantDate: string;
  harvestStart: string;
  harvestEnd?: string;
  harvests: HarvestEntry[];
}

export interface HarvestEntry {
  stemCount: number;
  unit: string;
  date: string;
}

// ─── Seed Types ───────────────────────────────────────────────────

export interface SeedLotSummary {
  id: string;
  species: string;
  cultivar: string;
  supplier: string;
  qtyOnHand: number;
  unit: string;
  germinationPct: number;
  harvestYear: number;
  isOrganic: boolean;
}

export interface SeedLotDetail extends SeedLotSummary {
  daysToMaturity: number;
  color?: string;
  lotNumber?: string;
  purchaseDate?: string;
}

// ─── Guild Types ──────────────────────────────────────────────────

export interface GuildSummary {
  id: string;
  name: string;
  type: number;
  latitude: number;
  longitude: number;
  planted: string;
  memberCount: number;
}

export interface GuildDetail extends Omit<GuildSummary, "memberCount"> {
  boundary?: unknown;
  members: GuildMember[];
}

export interface GuildMember {
  plantId: string;
  species: string;
  cultivar: string;
  role: number;
}

// ─── PostHarvestBatch Types ───────────────────────────────────────

export interface BatchSummary {
  id: string;
  species: string;
  cultivar: string;
  totalStems: number;
  stemsRemaining: number;
  harvestDate: string;
  isConditioned: boolean;
  inCooler: boolean;
}

export interface BatchDetail extends BatchSummary {
  sourceBedId: string;
  successionId: string;
  stemsUsed: number;
  grades: StemGrade[];
  conditioningSolution?: string;
  waterTempF?: number;
  coolerTempF?: number;
  coolerSlot?: string;
}

export interface StemGrade {
  grade: number; // 0=Premium, 1=Standard, 2=Seconds, 3=Cull
  stemCount: number;
  stemLengthInches: number;
}

// ─── BouquetRecipe Types ──────────────────────────────────────────

export interface RecipeSummary {
  id: string;
  name: string;
  category: string;
  itemCount: number;
  totalStemsPerBouquet: number;
}

export interface RecipeDetail extends Omit<RecipeSummary, "itemCount"> {
  items: RecipeItem[];
  totalBouquetsMade: number;
}

export interface RecipeItem {
  species: string;
  cultivar: string;
  stemCount: number;
  color?: string;
  role: string; // focal, filler, greenery, accent
}

// ─── CropPlan Types ───────────────────────────────────────────────

export interface CropPlanSummary {
  id: string;
  seasonYear: number;
  seasonName: string;
  planName: string;
  bedCount: number;
  totalStemsHarvested: number;
  totalRevenue: number;
  totalCosts: number;
}

export interface CropPlanDetail extends Omit<CropPlanSummary, "bedCount"> {
  bedAssignments: BedAssignment[];
  costs: CostEntry[];
  revenues: RevenueEntry[];
}

export interface BedAssignment {
  bedId: string;
  species: string;
  cultivar: string;
  plannedSuccessions: number;
}

export interface CostEntry {
  category: string;
  amount: number;
  notes?: string;
}

export interface RevenueEntry {
  channel: number; // 0=FarmersMarket, 1=CSA, 2=Wholesale, 3=Wedding, 4=DirectSale
  amount: number;
  date: string;
  notes?: string;
}

// ─── Flora API ────────────────────────────────────────────────────

export const FloraAPI = {
  // Beds
  getBeds: () => fetchFarmOS<FlowerBedSummary[]>("/api/flora/beds"),
  getBed: (id: string) => fetchFarmOS<FlowerBedDetail>(`/api/flora/beds/${id}`),
  createBed: (
    cmd: {
      name: string;
      block: string;
      dimensions: { lengthFeet: number; widthFeet: number };
    },
  ) =>
    fetchFarmOS<{ id: string }>("/api/flora/beds", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  planSuccession: (
    bedId: string,
    cmd: {
      variety: CropVariety;
      sowDate: string;
      transplantDate: string;
      harvestStart: string;
    },
  ) =>
    fetchFarmOS<{ id: string }>(`/api/flora/beds/${bedId}/successions`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordSeeding: (
    bedId: string,
    succId: string,
    cmd: { seedLotId: string; quantity: Quantity; date: string },
  ) =>
    fetchFarmOS(`/api/flora/beds/${bedId}/successions/${succId}/seed`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordTransplant: (
    bedId: string,
    succId: string,
    cmd: { quantity: Quantity; date: string },
  ) =>
    fetchFarmOS(`/api/flora/beds/${bedId}/successions/${succId}/transplant`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordHarvest: (
    bedId: string,
    succId: string,
    cmd: { stems: Quantity; date: string },
  ) =>
    fetchFarmOS(`/api/flora/beds/${bedId}/successions/${succId}/harvest`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Seeds
  getSeedLots: () => fetchFarmOS<SeedLotSummary[]>("/api/flora/seeds"),
  getSeedLot: (id: string) =>
    fetchFarmOS<SeedLotDetail>(`/api/flora/seeds/${id}`),
  createSeedLot: (
    cmd: {
      variety: CropVariety;
      supplier: string;
      quantity: Quantity;
      germinationPct: number;
      harvestYear: number;
      isOrganic: boolean;
    },
  ) =>
    fetchFarmOS<{ id: string }>("/api/flora/seeds", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  restockSeed: (id: string, cmd: { quantity: Quantity; lotNumber?: string }) =>
    fetchFarmOS(`/api/flora/seeds/${id}/restock`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Guilds
  getGuilds: () => fetchFarmOS<GuildSummary[]>("/api/flora/guilds"),
  getGuild: (id: string) => fetchFarmOS<GuildDetail>(`/api/flora/guilds/${id}`),
  createGuild: (
    cmd: {
      name: string;
      type: number;
      position: { latitude: number; longitude: number };
      planted: string;
    },
  ) =>
    fetchFarmOS<{ id: string }>("/api/flora/guilds", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // PostHarvest Batches
  getBatches: () => fetchFarmOS<BatchSummary[]>("/api/flora/batches"),
  getBatch: (id: string) =>
    fetchFarmOS<BatchDetail>(`/api/flora/batches/${id}`),
  createBatch: (
    cmd: {
      sourceBedId: string;
      successionId: string;
      species: string;
      cultivar: string;
      totalStems: number;
      harvestDate: string;
    },
  ) =>
    fetchFarmOS<{ id: string }>("/api/flora/batches", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  gradeStems: (
    id: string,
    cmd: {
      grade: { grade: number; stemCount: number; stemLengthInches: number };
    },
  ) =>
    fetchFarmOS(`/api/flora/batches/${id}/grade`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  conditionStems: (id: string, cmd: { solution: string; waterTempF: number }) =>
    fetchFarmOS(`/api/flora/batches/${id}/condition`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  moveToCooler: (
    id: string,
    cmd: { temperatureF: number; slotLabel?: string },
  ) =>
    fetchFarmOS(`/api/flora/batches/${id}/cooler`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  useStems: (id: string, cmd: { stemsUsed: number; purpose: string }) =>
    fetchFarmOS(`/api/flora/batches/${id}/use-stems`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Bouquet Recipes
  getRecipes: () => fetchFarmOS<RecipeSummary[]>("/api/flora/recipes"),
  getRecipe: (id: string) =>
    fetchFarmOS<RecipeDetail>(`/api/flora/recipes/${id}`),
  createRecipe: (cmd: { name: string; category: string }) =>
    fetchFarmOS<{ id: string }>("/api/flora/recipes", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  addRecipeItem: (id: string, cmd: { item: RecipeItem }) =>
    fetchFarmOS(`/api/flora/recipes/${id}/items`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  removeRecipeItem: (id: string, species: string, cultivar: string) =>
    fetchFarmOS(
      `/api/flora/recipes/${id}/items/${encodeURIComponent(species)}/${
        encodeURIComponent(cultivar)
      }`,
      { method: "DELETE" },
    ),
  makeBouquet: (
    id: string,
    cmd: { quantity: number; date: string; notes?: string },
  ) =>
    fetchFarmOS(`/api/flora/recipes/${id}/make`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),

  // Crop Plans
  getCropPlans: () => fetchFarmOS<CropPlanSummary[]>("/api/flora/plans"),
  getCropPlan: (id: string) =>
    fetchFarmOS<CropPlanDetail>(`/api/flora/plans/${id}`),
  createCropPlan: (
    cmd: { seasonYear: number; seasonName: string; planName: string },
  ) =>
    fetchFarmOS<{ id: string }>("/api/flora/plans", {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  assignBedToPlan: (id: string, cmd: { assignment: BedAssignment }) =>
    fetchFarmOS(`/api/flora/plans/${id}/beds`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordYield: (
    id: string,
    cmd: {
      bedId: string;
      successionId: string;
      stemsHarvested: number;
      stemsPerLinearFoot: number;
      date: string;
    },
  ) =>
    fetchFarmOS(`/api/flora/plans/${id}/yields`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordCost: (id: string, cmd: { cost: CostEntry }) =>
    fetchFarmOS(`/api/flora/plans/${id}/costs`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
  recordRevenue: (
    id: string,
    cmd: { channel: number; amount: number; date: string; notes?: string },
  ) =>
    fetchFarmOS(`/api/flora/plans/${id}/revenue`, {
      method: "POST",
      body: JSON.stringify(cmd),
    }),
};
