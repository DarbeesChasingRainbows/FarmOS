import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowHACCPPlanBuilder from "../../islands/ArrowHACCPPlanBuilder.tsx";

/**
 * HACCP Plan Template route.
 * Renders the seven HACCP principles as a structured, printable document
 * with CCP definitions per product line.
 * The HACCPPlanBuilder island enables dynamic CCP management via the API.
 */
export default define.page(function HACCPPlan() {
  // Static fallback CCPs — the HACCPPlanBuilder island below manages dynamic ones via API
  const ccpDefinitions = [
    {
      product: "Sourdough",
      ccp: "Internal Bake Temperature",
      criticalLimit: "≥ 190°F",
      monitoring: "Probe thermometer at center of loaf, every batch",
      correctiveAction: "Return to oven until internal temp reaches 190°F",
    },
    {
      product: "Sourdough",
      ccp: "Cooling Temperature",
      criticalLimit: "≤ 70°F within 2 hours",
      monitoring: "IR thermometer check at 2-hour mark",
      correctiveAction: "Move to cooling rack with increased airflow",
    },
    {
      product: "Kombucha",
      ccp: "pH Level",
      criticalLimit: "≤ 4.2 within 7 days",
      monitoring: "Daily pH reading with calibrated meter",
      correctiveAction: "If pH > 4.2 at day 7, discard entire batch",
    },
    {
      product: "Kombucha",
      ccp: "Alcohol Content (ABV)",
      criticalLimit: "< 0.5% ABV",
      monitoring: "Hydrometer reading at bottling",
      correctiveAction:
        "Extend primary fermentation or dilute. Do not bottle if ≥ 0.5%",
    },
  ];

  return (
    <div class="px-6 py-8 max-w-7xl mx-auto">
      <Head>
        <title>HACCP Plan — Hearth OS</title>
      </Head>
      <div class="mb-2">
        <a
          href="/compliance"
          class="text-orange-600 hover:text-orange-700 text-sm font-semibold transition"
        >
          &larr; Back to Compliance
        </a>
      </div>
      {/* Document Header */}
      <div class="mb-8 pb-6 border-b-2 border-stone-800">
        <h1 class="text-2xl font-bold text-stone-800">
          HACCP Plan — Hearth Kitchen
        </h1>
        <p class="text-sm text-stone-500 mt-1">
          Hazard Analysis and Critical Control Points
        </p>
        <p class="text-xs text-stone-400 mt-2">
          Georgia Department of Agriculture — Food Sales Establishment License
          Compliance
        </p>
        <p class="text-xs text-stone-400">
          Last Reviewed: {new Date().toLocaleDateString("en-US", {
            year: "numeric",
            month: "long",
            day: "numeric",
          })}
        </p>
      </div>

      {/* Principle 1: Hazard Analysis */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-stone-800 text-white text-xs flex items-center justify-center font-bold">
            1
          </span>
          Hazard Analysis
        </h2>
        <div class="bg-white rounded-lg border border-stone-200 overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-stone-50">
              <tr>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Product
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Hazard
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Type
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Severity
                </th>
              </tr>
            </thead>
            <tbody>
              <tr class="border-t border-stone-100">
                <td class="px-4 py-3">Sourdough</td>
                <td class="px-4 py-3">
                  Insufficient bake temperature (pathogen survival)
                </td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 bg-red-50 text-red-700 rounded text-xs font-medium">
                    Biological
                  </span>
                </td>
                <td class="px-4 py-3">High</td>
              </tr>
              <tr class="border-t border-stone-100">
                <td class="px-4 py-3">Kombucha</td>
                <td class="px-4 py-3">
                  pH not reaching safe level (pathogen growth)
                </td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 bg-red-50 text-red-700 rounded text-xs font-medium">
                    Biological
                  </span>
                </td>
                <td class="px-4 py-3">High</td>
              </tr>
              <tr class="border-t border-stone-100">
                <td class="px-4 py-3">Kombucha</td>
                <td class="px-4 py-3">
                  Alcohol content exceeding 0.5% ABV (TTB violation)
                </td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 bg-amber-50 text-amber-700 rounded text-xs font-medium">
                    Regulatory
                  </span>
                </td>
                <td class="px-4 py-3">High</td>
              </tr>
              <tr class="border-t border-stone-100">
                <td class="px-4 py-3">All Products</td>
                <td class="px-4 py-3">
                  Inadequate sanitization of food contact surfaces
                </td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 bg-red-50 text-red-700 rounded text-xs font-medium">
                    Biological
                  </span>
                </td>
                <td class="px-4 py-3">Medium</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Principle 2 & 3: CCP Identification & Critical Limits */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-stone-800 text-white text-xs flex items-center justify-center font-bold">
            2
          </span>
          Critical Control Points & Limits
        </h2>
        <div class="bg-white rounded-lg border border-stone-200 overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-stone-50">
              <tr>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Product
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  CCP
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Critical Limit
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Monitoring
                </th>
              </tr>
            </thead>
            <tbody>
              {ccpDefinitions.map((ccp, i) => (
                <tr key={i} class="border-t border-stone-100">
                  <td class="px-4 py-3 font-medium">{ccp.product}</td>
                  <td class="px-4 py-3">{ccp.ccp}</td>
                  <td class="px-4 py-3">
                    <span class="px-2 py-0.5 bg-stone-100 text-stone-800 rounded text-xs font-mono font-bold">
                      {ccp.criticalLimit}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-stone-600">{ccp.monitoring}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* Principle 5: Corrective Actions */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-stone-800 text-white text-xs flex items-center justify-center font-bold">
            5
          </span>
          Corrective Actions
        </h2>
        <div class="bg-white rounded-lg border border-stone-200 overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-stone-50">
              <tr>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  CCP
                </th>
                <th class="text-left px-4 py-3 font-semibold text-stone-600">
                  Corrective Action
                </th>
              </tr>
            </thead>
            <tbody>
              {ccpDefinitions.map((ccp, i) => (
                <tr key={i} class="border-t border-stone-100">
                  <td class="px-4 py-3 font-medium">
                    {ccp.product} — {ccp.ccp}
                  </td>
                  <td class="px-4 py-3 text-stone-600">
                    {ccp.correctiveAction}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div class="mt-3 bg-amber-50 border border-amber-200 rounded-lg p-3 text-xs text-amber-700">
          <strong>⚠️ Enforcement:</strong> When a CCP reading is out of limits (
          <code>WithinLimits: false</code>), HearthOS requires a corrective
          action to be recorded before the form can be submitted. This field is
          validated at both the Zod schema level (frontend) and the domain
          aggregate level (backend). The domain enforces this as an unbypassable
          business rule.
        </div>
      </section>

      {/* Dynamic CCP Management */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-emerald-600 text-white text-xs flex items-center justify-center font-bold">
            +
          </span>
          Manage CCP Definitions
        </h2>
        <ArrowHACCPPlanBuilder />
      </section>

      {/* Principle 6: Verification */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-stone-800 text-white text-xs flex items-center justify-center font-bold">
            6
          </span>
          Verification Procedures
        </h2>
        <div class="bg-white rounded-lg border border-stone-200 p-4 space-y-3 text-sm">
          <div class="flex items-start gap-3">
            <span class="w-5 h-5 rounded bg-stone-100 text-stone-600 flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
              W
            </span>
            <div>
              <p class="font-medium text-stone-800">
                Weekly Log Review
              </p>
              <p class="text-stone-500 text-xs">
                Review all CCP readings and sanitation logs. Verify all
                out-of-limit readings have documented corrective actions.
              </p>
            </div>
          </div>
          <div class="flex items-start gap-3">
            <span class="w-5 h-5 rounded bg-stone-100 text-stone-600 flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
              M
            </span>
            <div>
              <p class="font-medium text-stone-800">
                Monthly Equipment Calibration
              </p>
              <p class="text-stone-500 text-xs">
                Calibrate pH meters, thermometers, and hydrometers. Record
                calibration results.
              </p>
            </div>
          </div>
          <div class="flex items-start gap-3">
            <span class="w-5 h-5 rounded bg-stone-100 text-stone-600 flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
              A
            </span>
            <div>
              <p class="font-medium text-stone-800">
                Annual Plan Review
              </p>
              <p class="text-stone-500 text-xs">
                Full review of HACCP plan, CCP definitions, and critical limits.
                Update as needed based on product changes or regulatory updates.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Principle 7: Record Keeping */}
      <section class="mb-8">
        <h2 class="text-lg font-bold text-stone-800 mb-3 flex items-center gap-2">
          <span class="w-7 h-7 rounded-full bg-stone-800 text-white text-xs flex items-center justify-center font-bold">
            7
          </span>
          Record Keeping
        </h2>
        <div class="bg-white rounded-lg border border-stone-200 p-4 text-sm text-stone-600">
          <p class="mb-3">
            All records are maintained digitally within HearthOS and available
            for inspector review at:
          </p>
          <ul class="space-y-2">
            <li class="flex items-center gap-2">
              <span class="text-emerald-500">✓</span>
              <a
                href="/compliance"
                class="text-stone-700 underline hover:text-stone-900"
              >
                Sanitation Logs
              </a>{" "}
              — Daily cleaning records with PPM documentation
            </li>
            <li class="flex items-center gap-2">
              <span class="text-emerald-500">✓</span>
              <a
                href="/batches"
                class="text-stone-700 underline hover:text-stone-900"
              >
                Batch CCP Records
              </a>{" "}
              — Temperature, pH, and corrective actions per batch
            </li>
            <li class="flex items-center gap-2">
              <span class="text-emerald-500">✓</span>
              <a
                href="/kombucha"
                class="text-stone-700 underline hover:text-stone-900"
              >
                Kombucha pH Logs
              </a>{" "}
              — Daily pH readings with threshold enforcement
            </li>
          </ul>
          <p class="mt-3 text-xs text-stone-400">
            All records can be printed in inspector-ready format using the
            "Print for Inspector" button.
          </p>
        </div>
      </section>
    </div>
  );
});
