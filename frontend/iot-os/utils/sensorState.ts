import { signal } from "@preact/signals";

export type ConnectionStatus = "connected" | "reconnecting" | "offline";

export interface SensorEvent {
  reading: {
    deviceId: string;
    sensorType: string;
    value: number;
    unit: string;
    timestamp: string;
    zoneId?: string;
  };
  alert: {
    level: "Safe" | "Warning" | "Critical";
    message: string;
    correctiveAction?: string;
  };
}

export const connectionStatus = signal<ConnectionStatus>("offline");
export const lastDataTimestamp = signal<string | null>(null);
export const sensorEvents = signal<SensorEvent[]>([]);
export const activeExcursionCount = signal(0);
