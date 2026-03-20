import { signal } from "@preact/signals";

export type ConnectionStatus = "connected" | "reconnecting" | "offline";

export interface SensorEvent {
  reading: {
    deviceId: string;
    sensorType: string;
    value: number;
    unit: string;
    timestamp: string;
  };
  alert: {
    level: "Safe" | "Warning" | "Critical";
    message: string;
    correctiveAction?: string;
  };
}

export interface FreezeDryerTelemetry {
  dryerSerial: string;
  batchId?: string;
  phase: string;
  temperatureF: number;
  vacuumMTorr: number;
  progressPercent: number;
  screenNumber: number;
  alertLevel?: string;
  alertMessage?: string;
  timestamp: string;
}

export const connectionStatus = signal<ConnectionStatus>("offline");
export const lastDataTimestamp = signal<string | null>(null);
export const sensorEvents = signal<SensorEvent[]>([]);
export const freezeDryerTelemetry = signal<FreezeDryerTelemetry | null>(null);

export function getLastDataAge(): string | null {
  if (!lastDataTimestamp.value) return null;

  const now = Date.now();
  const then = new Date(lastDataTimestamp.value).getTime();
  const diffMs = now - then;

  if (diffMs < 60_000) return "just now";
  if (diffMs < 3_600_000) return `${Math.floor(diffMs / 60_000)} min ago`;
  if (diffMs < 86_400_000) return `${Math.floor(diffMs / 3_600_000)} hr ago`;
  return "over a day ago";
}
