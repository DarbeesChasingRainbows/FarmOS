import type { WeatherSnapshot } from "../utils/farmos-client.ts";

interface WeatherBadgeProps {
  weather: WeatherSnapshot | null;
}

/**
 * Compact weather badge shown during inspections or on hive detail panels.
 * Displays temperature, humidity, and conditions from the latest weather snapshot.
 */
export default function WeatherBadge({ weather }: WeatherBadgeProps) {
  if (!weather) {
    return (
      <div class="inline-flex items-center gap-1.5 bg-stone-50 border border-stone-200 rounded-lg px-3 py-1.5 text-xs text-stone-400">
        <span>🌤️</span>
        <span>No weather data</span>
      </div>
    );
  }

  const tempColor = weather.tempF >= 90
    ? "text-red-600"
    : weather.tempF >= 70
    ? "text-amber-600"
    : weather.tempF >= 50
    ? "text-emerald-600"
    : "text-sky-600";

  return (
    <div class="inline-flex items-center gap-3 bg-sky-50 border border-sky-200 rounded-lg px-3 py-2 text-xs">
      <div class="flex items-center gap-1">
        <span>🌡️</span>
        <span class={`font-bold ${tempColor}`}>{weather.tempF}°F</span>
      </div>
      <div class="flex items-center gap-1">
        <span>💧</span>
        <span class="font-bold text-sky-700">{weather.humidity}%</span>
      </div>
      {weather.windMph != null && (
        <div class="flex items-center gap-1">
          <span>💨</span>
          <span class="font-medium text-stone-600">{weather.windMph} mph</span>
        </div>
      )}
      {weather.conditions && (
        <span class="text-stone-500">{weather.conditions}</span>
      )}
    </div>
  );
}
