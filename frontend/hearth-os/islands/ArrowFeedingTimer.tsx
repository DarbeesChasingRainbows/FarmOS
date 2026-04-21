import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";
import { showToast } from "../utils/toastState.ts";
import { ArrowInfoIcon, ArrowTooltip } from "../components/ArrowTooltip.ts";

export interface ArrowFeedingTimerProps {
  cultureId: string;
  cultureName: string;
  lastFedISO: string;
  intervalHours: number;
}

export default function ArrowFeedingTimer(
  { cultureId, cultureName, lastFedISO, intervalHours }: ArrowFeedingTimerProps,
) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    const state = reactive({
      timeRemaining: "",
      isOverdue: false,
      isFeeding: false,
      confirmOpen: false,
    });

    const updateTimer = () => {
      const lastFed = new Date(lastFedISO).getTime();
      const nextFeed = lastFed + intervalHours * 60 * 60 * 1000;
      const now = Date.now();
      const diff = nextFeed - now;

      if (diff <= 0) {
        const overdue = Math.abs(diff);
        const hours = Math.floor(overdue / (1000 * 60 * 60));
        const mins = Math.floor((overdue % (1000 * 60 * 60)) / (1000 * 60));
        state.timeRemaining = `${hours}h ${mins}m overdue`;
        state.isOverdue = true;
      } else {
        const hours = Math.floor(diff / (1000 * 60 * 60));
        const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        state.timeRemaining = `${hours}h ${mins}m`;
        state.isOverdue = false;
      }
    };

    updateTimer();
    const intervalId = setInterval(updateTimer, 60000);

    const doFeed = async () => {
      state.confirmOpen = false;
      state.isFeeding = true;
      try {
        const { HearthAPI } = await import("../utils/farmos-client.ts");
        await HearthAPI.feedCulture(cultureId, {
          flourGrams: 100,
          waterGrams: 100,
          notes: `Routine feeding from HearthOS`,
        });
        showToast(
          "success",
          `${cultureName} fed!`,
          "100g flour + 100g water. Next feeding in " + intervalHours + "h.",
        );
      } catch (err: unknown) {
        showToast(
          "error",
          "Feeding failed",
          err instanceof Error ? err.message : "Unknown error",
        );
      } finally {
        state.isFeeding = false;
      }
    };

    const template = html`
      <div>
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-1">
            <div>
              <p class="text-xs text-stone-400 uppercase tracking-wider font-medium">
                Next Feed
              </p>
              <p class="${() =>
                `text-sm font-bold mt-0.5 ${
                  state.isOverdue ? "text-red-600" : "text-stone-800"
                }`}">${() => state.timeRemaining}</p>
            </div>
            ${ArrowTooltip({
              text:
                `This culture needs feeding every ${intervalHours}h. Regular feeding maintains yeast/bacteria balance and keeps the culture active.`,
              children: ArrowInfoIcon(),
            })}
          </div>
          <button
            @click="${() => state.confirmOpen = true}"
            disabled="${() => state.isFeeding}"
            class="${() =>
              `px-3 py-1.5 text-xs font-semibold rounded-lg transition shadow-sm ${
                state.isOverdue
                  ? "bg-red-600 text-white hover:bg-red-700"
                  : "bg-stone-100 text-stone-700 hover:bg-stone-200"
              } disabled:opacity-50`}"
          >
            ${() => state.isFeeding ? "Feeding..." : "Feed Now"}
          </button>
        </div>

        ${() => {
          if (!state.confirmOpen) return "";

          return html`
            <!-- We implement an inline confirm dialog for speed and less coupling -->
            <div
              class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-60 animate-in fade-in duration-200"
              @click="${(e: Event) => {
                if (e.target === e.currentTarget) state.confirmOpen = false;
              }}"
            >
              <div
                class="bg-white rounded-2xl shadow-xl w-full max-w-sm overflow-hidden border border-stone-200/50 scale-in duration-200"
              >
                <div class="p-6">
                  <h3 class="text-lg font-bold text-stone-900 mb-2">Feed ${cultureName}?</h3>
                  <p class="text-stone-500 text-sm leading-relaxed mb-6">
                    This will record a feeding of 100g flour + 100g water for ${cultureName}.
                    The feeding timer will reset.
                  </p>
                  <div class="flex gap-3 justify-end mt-4">
                    <button
                      @click="${() => state.confirmOpen = false}"
                      class="px-4 py-2 text-stone-600 font-medium hover:bg-stone-50 rounded-lg transition text-sm"
                    >
                      Cancel
                    </button>
                    <button
                      @click="${doFeed}"
                      class="bg-stone-900 text-white px-4 py-2 rounded-lg font-semibold hover:bg-stone-800 transition shadow-sm text-sm"
                    >
                      Feed Now
                    </button>
                  </div>
                </div>
              </div>
            </div>
          `;
        }}
      </div>
    `;

    template(containerRef.current);

    return () => clearInterval(intervalId);
  }, [cultureId, cultureName, lastFedISO, intervalHours]);

  return <div ref={containerRef}></div>;
}
