import { expect } from "@std/expect";
import { formatRelative } from "../utils/format.ts";

Deno.test("formatRelative converts dates to human-readable strings", () => {
  const now = new Date();

  // Past
  const pastMinute = new Date(now.getTime() - 60 * 1000);
  expect(formatRelative(pastMinute.toISOString())).toBe("1m ago");

  const pastHour = new Date(now.getTime() - 60 * 60 * 1000);
  expect(formatRelative(pastHour.toISOString())).toBe("1h ago");

  const pastDay = new Date(now.getTime() - 24 * 60 * 60 * 1000);
  expect(formatRelative(pastDay.toISOString())).toBe("Yesterday");

  // Future
  const futureMinute = new Date(now.getTime() + 60 * 1000);
  expect(formatRelative(futureMinute.toISOString())).toBe("in the future");

  const futureHour = new Date(now.getTime() + 60 * 60 * 1000);
  expect(formatRelative(futureHour.toISOString())).toBe("in the future");
});
