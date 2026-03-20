import { z } from "zod";

// ─── Equipment ───────────────────────────────────────────────────────────────

export const RegisterEquipmentSchema = z.object({
  name: z.string().min(2, "Name required").max(100),
  make: z.string().min(1, "Make required").max(100),
  model: z.string().min(1, "Model required").max(100),
  year: z.coerce.number().int().min(1900).max(new Date().getFullYear() + 1)
    .optional(),
  lat: z.coerce.number().min(-90).max(90),
  lng: z.coerce.number().min(-180).max(180),
});

export const MaintenanceSchema = z.object({
  date: z.string().min(1, "Date required"),
  description: z.string().min(2, "Description required").max(500),
  costDollars: z.coerce.number().min(0).optional(),
  technician: z.string().max(100).optional(),
});

export const RetireSchema = z.object({
  reason: z.string().min(2, "Reason required").max(500),
});

// ─── Helper ──────────────────────────────────────────────────────────────────

export type FieldErrors = Record<string, string>;

export function extractErrors<T>(result: z.SafeParseError<T>): FieldErrors {
  const errs: FieldErrors = {};
  for (const issue of result.error.issues) {
    const key = issue.path.join(".");
    if (key) errs[key] = issue.message;
  }
  return errs;
}
