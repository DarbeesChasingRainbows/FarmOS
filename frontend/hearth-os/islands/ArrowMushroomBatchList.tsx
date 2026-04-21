import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { formatRelative } from "../utils/format.ts";

interface MushroomBatch {
  id: string;
  batchCode: string;
  species: string;
  substrateType: string;
  phase: number;
  currentFlushes: number;
  inoculatedAt: string;
}

const formatPhase = (phase: number) => {
  const phases: Record<number, { label: string; color: string }> = {
    0: {
      label: "Incubating",
      color: "bg-stone-100 text-stone-800 border-stone-200",
    },
    1: { label: "Pinning", color: "bg-blue-50 text-blue-800 border-blue-200" },
    2: {
      label: "Fruiting",
      color: "bg-emerald-50 text-emerald-800 border-emerald-200",
    },
    3: {
      label: "Completed",
      color: "bg-purple-50 text-purple-800 border-purple-200",
    },
    4: {
      label: "Contaminated",
      color: "bg-red-50 text-red-800 border-red-200",
    },
  };
  return phases[phase] ??
    { label: "Unknown", color: "bg-stone-100 text-stone-800 border-stone-200" };
};

export default function ArrowMushroomBatchList() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      batches: [
        {
          id: "mb-lion-01",
          batchCode: "LM-03-A",
          species: "Lion's Mane",
          substrateType: "Hardwood Sawdust + Soy Hulls",
          phase: 2,
          currentFlushes: 1,
          inoculatedAt: new Date(Date.now() - 21 * 24 * 3600 * 1000)
            .toISOString(),
        },
        {
          id: "mb-oyster-02",
          batchCode: "BO-03-B",
          species: "Blue Oyster",
          substrateType: "Straw",
          phase: 0,
          currentFlushes: 0,
          inoculatedAt: new Date(Date.now() - 5 * 24 * 3600 * 1000)
            .toISOString(),
        },
      ] as MushroomBatch[],
    });

    const template = html`
      ${() => {
        if (state.batches.length === 0) {
          return html`
            <div class="text-center py-12">
              <div class="text-4xl mb-4">🍄</div>
              <h3 class="text-lg font-bold text-stone-800 mb-2">No Active Blocks</h3>
              <p class="text-stone-500">Inoculate some substrate to start tracking!</p>
            </div>
          `;
        }

        return html`
          <div class="space-y-4">
            ${() =>
              state.batches.map((batch: MushroomBatch) => {
                const p = formatPhase(batch.phase);
                return html`
                  <a
                    href="/mushrooms/${batch.id}"
                    class="block bg-white border border-stone-200 rounded-lg p-5 hover:border-emerald-300 hover:shadow-md transition group"
                  >
                    <div class="flex items-center justify-between">
                      <div>
                        <div class="flex items-center gap-3 mb-1">
                          <h3
                            class="text-lg font-bold text-stone-800 group-hover:text-emerald-700 transition"
                          >
                            ${batch.species}
                          </h3>
                          <span class="${`text-xs font-bold px-2 py-0.5 rounded-full border ${p.color}`}">
                            ${p.label}
                          </span>
                        </div>
                        <p class="text-sm font-medium text-stone-500 uppercase tracking-wide">
                          ${batch.batchCode}
                        </p>
                      </div>

                      <div class="text-right">
                        <p class="text-sm text-stone-600">
                          <span class="font-semibold text-stone-800">${batch
                            .currentFlushes}</span> flushes
                        </p>
                        <p class="text-xs text-stone-400 mt-1">Inoculated ${formatRelative(
                          batch.inoculatedAt,
                        )}</p>
                      </div>
                    </div>

                    <div
                      class="mt-4 pt-4 border-t border-stone-100 flex items-center justify-between text-sm"
                    >
                      <span class="text-stone-500">${batch.substrateType}</span>
                      <span
                        class="text-emerald-600 font-medium group-hover:translate-x-1 transition-transform inline-block"
                      >
                        Manage Block →
                      </span>
                    </div>
                  </a>
                `.key(batch.id);
              })}
          </div>
        `;
      }}
    `;

    template(containerRef.current);
  }, []);

  return <div ref={containerRef}></div>;
}
