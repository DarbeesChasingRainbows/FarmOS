import { z } from "zod";

// ─── Shared ──────────────────────────────────────────────────────────

const positiveNumber = z.coerce.number().positive("Must be a positive number");

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

// ─── Apiary ─────────────────────────────────────────────────────────

export const ApiarySchema = z.object({
  name: z.string().min(2, "Name required").max(50),
  latitude: z.coerce.number().min(-90).max(90),
  longitude: z.coerce.number().min(-180).max(180),
  maxCapacity: z.coerce.number().min(1, "At least 1").max(500),
  notes: z.string().max(500).optional().default(""),
});

// ─── Queen ──────────────────────────────────────────────────────────

export const QueenSchema = z.object({
  color: z.coerce.number().min(0).max(4).optional(),
  origin: z.coerce.number().min(0).max(2),
  introducedDate: z.string().min(1, "Date required"),
  breed: z.string().max(100).optional().default(""),
  notes: z.string().max(500).optional().default(""),
});

export const QueenLostSchema = z.object({
  reason: z.string().min(1, "Reason required").max(200),
  date: z.string().min(1, "Date required"),
});

// ─── Feeding ────────────────────────────────────────────────────────

export const FeedingSchema = z.object({
  feedType: z.coerce.number().min(0).max(3),
  amountValue: z.coerce.number().positive("Must be positive"),
  amountUnit: z.string().min(1, "Unit required"),
  concentration: z.string().max(20).optional().default(""),
  date: z.string().min(1, "Date required"),
  notes: z.string().max(500).optional().default(""),
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
