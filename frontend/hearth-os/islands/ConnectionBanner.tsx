import { useEffect } from "preact/hooks";
import { useSignal } from "@preact/signals";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import {
  connectionStatus,
  lastDataTimestamp,
  getLastDataAge,
  sensorEvents,
  freezeDryerTelemetry,
  SensorEvent,
  type FreezeDryerTelemetry,
} from "../utils/connectionState.ts";

export default function ConnectionBanner() {
  const lastAge = useSignal<string | null>(null);
  const mounted = useSignal(false);

  useEffect(() => {
    mounted.value = true;
    const interval = setInterval(() => {
      lastAge.value = getLastDataAge();
    }, 1000);

    let connection: { stop: () => void } | null = null;
    const connect = async () => {
      try {
        // For SignalR from the browser, use localhost gateway
        // The GATEWAY_URL env var contains Docker-internal hostnames (caddy:5050) which don't resolve in browser
        const hubUrl = `http://localhost:5050/hubs/kitchen`;

        const hub = new HubConnectionBuilder()
          .withUrl(hubUrl)
          .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
          .configureLogging(LogLevel.Warning)
          .build();

        hub.on("SensorReading", (evt: SensorEvent) => {
          sensorEvents.value = [evt, ...sensorEvents.value].slice(0, 50);
          lastDataTimestamp.value = evt.reading.timestamp;
        });

        hub.on("FreezeDryerTelemetry", (evt: FreezeDryerTelemetry) => {
          freezeDryerTelemetry.value = evt;
          lastDataTimestamp.value = evt.timestamp;
        });

        hub.onclose(() => {
          connectionStatus.value = "offline";
        });
        hub.onreconnecting(() => {
          connectionStatus.value = "reconnecting";
        });
        hub.onreconnected(() => {
          connectionStatus.value = "connected";
        });

        await hub.start();
        connectionStatus.value = "connected";
        connection = hub;
      } catch (err) {
        connectionStatus.value = "offline";
        console.warn("[ConnectionBanner] SignalR connection failed:", err);
      }
    };

    connect();

    return () => {
      clearInterval(interval);
      connection?.stop();
    };
  }, []);

  if (!mounted.value) {
    return null;
  }

  if (connectionStatus.value === "connected") {
    return (
      <div class="flex items-center gap-2 px-3 py-1.5 text-xs font-medium text-emerald-700 bg-emerald-50 border-b border-emerald-100">
        <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
        Live
      </div>
    );
  }

  if (connectionStatus.value === "reconnecting") {
    return (
      <div class="flex items-center gap-2 px-3 py-1.5 text-xs font-medium text-amber-700 bg-amber-50 border-b border-amber-100">
        <span class="w-2 h-2 rounded-full bg-amber-500 animate-pulse" />
        Reconnecting…
      </div>
    );
  }

  return (
    <div class="flex items-center gap-2 px-3 py-1.5 text-xs font-medium text-red-700 bg-red-50 border-b border-red-100">
      <span class="w-2 h-2 rounded-full bg-red-500" />
      Offline{lastAge.value ? ` — last data ${lastAge.value}` : ""}
    </div>
  );
}
