import type { ComplianceReport } from "../utils/iot-client.ts";

interface Props {
  report: ComplianceReport;
}

export default function ComplianceGauge({ report }: Props) {
  const pct = report.compliancePercent;
  const color = pct >= 95 ? "emerald" : pct >= 80 ? "amber" : "red";

  const ringColor: Record<string, string> = {
    emerald: "stroke-emerald-500",
    amber: "stroke-amber-500",
    red: "stroke-red-500",
  };

  const textColor: Record<string, string> = {
    emerald: "text-emerald-400",
    amber: "text-amber-400",
    red: "text-red-400",
  };

  const bgColor: Record<string, string> = {
    emerald: "border-emerald-700/30 bg-emerald-950/30",
    amber: "border-amber-700/30 bg-amber-950/30",
    red: "border-red-700/30 bg-red-950/30",
  };

  // SVG circular gauge
  const radius = 50;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (pct / 100) * circumference;

  return (
    <div class={`rounded-2xl border-2 p-6 ${bgColor[color]}`}>
      {/* Zone Name */}
      <h3 class="text-lg font-black text-amber-50 mb-4">{report.zoneName}</h3>

      {/* Gauge */}
      <div class="flex items-center justify-center mb-4">
        <div class="relative w-32 h-32">
          <svg class="w-full h-full -rotate-90" viewBox="0 0 120 120">
            <circle
              cx="60" cy="60" r={radius}
              fill="none"
              stroke="currentColor"
              class="text-stone-700/50"
              stroke-width="10"
            />
            <circle
              cx="60" cy="60" r={radius}
              fill="none"
              class={ringColor[color]}
              stroke-width="10"
              stroke-linecap="round"
              stroke-dasharray={circumference}
              stroke-dashoffset={strokeDashoffset}
            />
          </svg>
          <div class="absolute inset-0 flex items-center justify-center">
            <span class={`text-3xl font-black ${textColor[color]}`}>
              {pct.toFixed(0)}%
            </span>
          </div>
        </div>
      </div>

      {/* Stats */}
      <div class="grid grid-cols-2 gap-3 text-center">
        <div class="bg-stone-900/50 rounded-xl p-3">
          <div class="text-xl font-black text-amber-50">{report.totalReadings}</div>
          <div class="text-xs text-stone-500">Total</div>
        </div>
        <div class="bg-stone-900/50 rounded-xl p-3">
          <div class="text-xl font-black text-amber-50">{report.violations.length}</div>
          <div class="text-xs text-stone-500">Violations</div>
        </div>
      </div>
    </div>
  );
}
