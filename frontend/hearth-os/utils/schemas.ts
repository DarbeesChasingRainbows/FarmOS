import { z } from "zod";

// ─── Shared ──────────────────────────────────────────────────────────

const positiveNumber = z.coerce.number().positive("Must be a positive number");

const quantity = z.object({
  value: positiveNumber,
  unit: z.string().min(1),
  type: z.string().min(1),
});

// ─── Sourdough ───────────────────────────────────────────────────────

export const SourdoughBatchSchema = z.object({
  batchCode: z.string().min(3, "Batch code must be at least 3 characters")
    .regex(/^[A-Za-z0-9-]+$/, "Only letters, numbers, and hyphens"),
  flourGrams: z.coerce.number().min(100, "Minimum 100g flour").max(
    10000,
    "Maximum 10kg",
  ),
  waterGrams: z.coerce.number().min(50, "Minimum 50g water").max(
    10000,
    "Maximum 10kg",
  ),
  saltGrams: z.coerce.number().min(1, "Minimum 1g salt").max(
    500,
    "Maximum 500g",
  ),
});

// ─── Kombucha ────────────────────────────────────────────────────────

export const KombuchaBatchSchema = z.object({
  batchCode: z.string().min(3, "Batch code must be at least 3 characters")
    .regex(/^[A-Za-z0-9-]+$/, "Only letters, numbers, and hyphens"),
  teaType: z.enum(["Black", "Green", "Oolong", "White"], {
    errorMap: () => ({ message: "Select a tea type" }),
  }),
  sugarGrams: z.coerce.number().min(50, "Minimum 50g sugar").max(
    2000,
    "Maximum 2kg",
  ),
});

// ─── pH Record ───────────────────────────────────────────────────────

export const PHRecordSchema = z.object({
  pH: z.coerce.number().min(0, "pH cannot be negative").max(14, "pH max is 14"),
  temperature: z.coerce.number().min(32, "Min 32°F").max(212, "Max 212°F")
    .optional(),
});

// ─── Culture ─────────────────────────────────────────────────────────

export const CultureSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters").max(50),
  type: z.coerce.number().min(0).max(3),
  origin: z.string().min(3, "Describe the culture origin").max(200),
});

export const FeedCultureSchema = z.object({
  flourGrams: z.coerce.number().min(10, "Minimum 10g").max(5000),
  waterGrams: z.coerce.number().min(10, "Minimum 10g").max(5000),
  notes: z.string().max(500).optional(),
});

// ─── Mushroom ────────────────────────────────────────────────────────

export const MushroomBatchSchema = z.object({
  batchCode: z.string().min(3, "Batch code must be at least 3 characters")
    .regex(/^[A-Za-z0-9-]+$/, "Only letters, numbers, and hyphens"),
  species: z.string().min(2, "Species is required").max(100),
  substrateType: z.string().min(2, "Substrate type is required").max(100),
});

export const MushroomEnvironmentSchema = z.object({
  temperatureF: z.coerce.number().min(32, "Temp must be ≥ 32°F").max(
    120,
    "Temp must be ≤ 120°F",
  ).optional(),
  humidityPercent: z.coerce.number().min(0, "Humidity must be ≥ 0%").max(
    100,
    "Humidity must be ≤ 100%",
  ).optional(),
  notes: z.string().max(500).optional(),
}).refine(
  (data) =>
    data.temperatureF !== undefined || data.humidityPercent !== undefined,
  {
    message: "Provide either temperature or humidity",
    path: ["notes"], // Attach error here if general
  },
);

export const MushroomFlushSchema = z.object({
  flushNumber: z.coerce.number().int().min(1, "Flush must be ≥ 1").max(
    10,
    "Max flushes is 10",
  ),
  yieldLbs: z.coerce.number().min(0.1, "Minimum yield is 0.1 lbs"),
  date: z.string().min(1, "Date is required"),
});

export const MushroomCompleteSchema = z.object({
  totalYieldLbs: z.coerce.number().min(0.1, "Minimum total yield is 0.1 lbs"),
});

// ─── Hive ────────────────────────────────────────────────────────────

export const HiveSchema = z.object({
  name: z.string().min(2, "Name required").max(50),
  location: z.string().min(2, "Location required").max(100),
  hiveType: z.coerce.number().min(0).max(2),
});

export const HiveInspectionSchema = z.object({
  queenSeen: z.boolean(),
  broodPattern: z.string().min(1, "Describe the brood pattern"),
  temperament: z.string().min(1, "Rate the temperament"),
  mitesPerHundred: z.coerce.number().min(0, "Cannot be negative").max(100),
  notes: z.string().max(1000).optional().default(""),
  date: z.string().min(1, "Date required"),
});

export const HoneyHarvestSchema = z.object({
  supers: z.coerce.number().min(1, "At least 1 super").max(20),
  estimatedYieldLbs: z.coerce.number().min(0.1, "Minimum 0.1 lbs"),
  date: z.string().min(1, "Date required"),
});

// ─── Commercial Kitchen Compliance ───────────────────────────────

export const TempLogSchema = z.object({
  equipmentId: z.string().min(1, "Equipment ID required"),
  tempF: z.coerce.number().min(-60, "Below -60°F seems wrong").max(
    500,
    "Above 500°F seems wrong",
  ),
  notes: z.string().max(300).optional(),
});

export const SanitationLogSchema = z.object({
  surfaceType: z.coerce.number().min(0).max(5),
  area: z.string().min(1, "Area required"),
  cleaningMethod: z.string().min(1, "Cleaning method required"),
  sanitizer: z.coerce.number().min(0).max(3),
  sanitizerPpm: z.coerce.number().positive().optional(),
  cleanedBy: z.string().min(2, "Staff name required").max(100),
  notes: z.string().max(500).optional(),
});

export const CertSchema = z.object({
  staffName: z.string().min(2, "Name required").max(100),
  certType: z.string().min(2, "Cert type required").max(100),
  issuedDate: z.string().min(1, "Issued date required"),
  expiryDate: z.string().min(1, "Expiry date required"),
  issuer: z.string().min(2, "Issuer required").max(100),
  notes: z.string().max(500).optional(),
});

export const DeliverySchema = z.object({
  supplier: z.string().min(2, "Supplier required").max(100),
  items: z.string().min(2, "Items required").max(500),
  arrivalTempF: z.coerce.number().min(-60, "Temperature too low").max(
    500,
    "Temperature too high",
  ),
  receivedBy: z.string().min(2, "Receiver name required").max(100),
  accepted: z.boolean(),
  notes: z.string().max(500).optional(),
});

// ─── HACCP Reading (with corrective action enforcement) ─────────

export const HACCPReadingSchema = z.object({
  criticalControlPoint: z.string().min(2, "CCP name required").max(100),
  temperatureF: z.coerce.number().min(-60, "Temp too low").max(
    500,
    "Temp too high",
  ),
  pH: z.coerce.number().min(0, "pH cannot be negative").max(14, "pH max is 14")
    .optional(),
  withinLimits: z.boolean(),
  correctiveAction: z.string().max(500).optional(),
}).refine(
  (data) => {
    // If reading is out of limits, corrective action is REQUIRED
    if (!data.withinLimits) {
      return !!data.correctiveAction &&
        data.correctiveAction.trim().length >= 5;
    }
    return true;
  },
  {
    message:
      "Corrective action is required when reading is out of limits (minimum 5 characters)",
    path: ["correctiveAction"],
  },
);

// ─── Freeze-Dryer ───────────────────────────────────────────────────

export const FreezeDryerBatchSchema = z.object({
  batchCode: z.string().min(3, "Batch code must be at least 3 characters")
    .regex(/^[A-Za-z0-9-]+$/, "Only letters, numbers, and hyphens"),
  dryerId: z.string().uuid("Select a freeze dryer"),
  productDescription: z.string().min(2, "Product description required").max(200),
  preDryWeight: z.coerce.number().positive("Pre-dry weight must be positive"),
});

export const FreezeDryerReadingSchema = z.object({
  shelfTempF: z.coerce.number().min(-60, "Too low").max(200, "Too high"),
  vacuumMTorr: z.coerce.number().min(0, "Vacuum cannot be negative").max(10000),
  productTempF: z.coerce.number().min(-60).max(200).optional(),
  notes: z.string().max(500).optional(),
});

// ─── CAPA ───────────────────────────────────────────────────────────

export const CAPASchema = z.object({
  description: z.string().min(5, "Description required (min 5 chars)").max(500),
  deviationSource: z.string().min(2, "Deviation source required").max(200),
  relatedCTE: z.coerce.number().min(0).max(2).optional(),
});

export const CAPACloseSchema = z.object({
  resolution: z.string().min(5, "Resolution required (min 5 chars)").max(500),
  verifiedBy: z.string().min(2, "Verifier name required").max(100),
});

// ─── CCP Definition ─────────────────────────────────────────────────

export const CCPDefinitionSchema = z.object({
  product: z.string().min(2, "Product name required").max(100),
  ccpName: z.string().min(2, "CCP name required").max(100),
  hazardType: z.coerce.number().min(0).max(3),
  criticalLimitExpression: z.string().min(2, "Critical limit required").max(200),
  monitoringProcedure: z.string().min(5, "Monitoring procedure required").max(500),
  defaultCorrectiveAction: z.string().min(5, "Corrective action required").max(500),
});

// ─── Monitoring Correction ──────────────────────────────────────────

export const MonitoringCorrectionSchema = z.object({
  reason: z.string().min(5, "Reason required (min 5 chars)").max(500),
  correctedValueF: z.coerce.number().min(-60).max(500).optional(),
  correctedBy: z.string().min(2, "Corrector name required").max(100),
});

// ─── Helper ──────────────────────────────────────────────────────────

export type FieldErrors = Record<string, string>;

export function extractErrors(
  result: z.SafeParseReturnType<unknown, unknown>,
): FieldErrors {
  if (result.success) return {};
  const errors: FieldErrors = {};
  for (const issue of result.error.issues) {
    const key = issue.path.join(".");
    if (!errors[key]) errors[key] = issue.message;
  }
  return errors;
}
