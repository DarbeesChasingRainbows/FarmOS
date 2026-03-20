import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";
import { showToast } from "../utils/toastState.ts";
import ConfirmDialog from "./ConfirmDialog.tsx";
import Tooltip, { InfoIcon } from "../components/Tooltip.tsx";

interface FeedingTimerProps {
  cultureId: string;
  cultureName: string;
  lastFedISO: string;
  intervalHours: number;
}

export default function FeedingTimer(
  { cultureId, cultureName, lastFedISO, intervalHours }: FeedingTimerProps,
) {
  const timeRemaining = useSignal("");
  const isOverdue = useSignal(false);
  const isFeeding = useSignal(false);
  const confirmOpen = useSignal(false);

  useEffect(() => {
    const update = () => {
      const lastFed = new Date(lastFedISO).getTime();
      const nextFeed = lastFed + intervalHours * 60 * 60 * 1000;
      const now = Date.now();
      const diff = nextFeed - now;

      if (diff <= 0) {
        const overdue = Math.abs(diff);
        const hours = Math.floor(overdue / (1000 * 60 * 60));
        const mins = Math.floor((overdue % (1000 * 60 * 60)) / (1000 * 60));
        timeRemaining.value = `${hours}h ${mins}m overdue`;
        isOverdue.value = true;
      } else {
        const hours = Math.floor(diff / (1000 * 60 * 60));
        const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        timeRemaining.value = `${hours}h ${mins}m`;
        isOverdue.value = false;
      }
    };

    update();
    const interval = setInterval(update, 60000);
    return () => clearInterval(interval);
  }, [lastFedISO, intervalHours]);

  const doFeed = async () => {
    confirmOpen.value = false;
    isFeeding.value = true;
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
      isFeeding.value = false;
    }
  };

  return (
    <div>
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-1">
          <div>
            <p class="text-xs text-stone-400 uppercase tracking-wider font-medium">
              Next Feed
            </p>
            <p
              class={`text-sm font-bold mt-0.5 ${
                isOverdue.value ? "text-red-600" : "text-stone-800"
              }`}
            >
              {timeRemaining.value}
            </p>
          </div>
          <Tooltip
            text={`This culture needs feeding every ${intervalHours}h. Regular feeding maintains yeast/bacteria balance and keeps the culture active.`}
          >
            <InfoIcon />
          </Tooltip>
        </div>
        <button
          onClick={() => confirmOpen.value = true}
          disabled={isFeeding.value}
          class={`px-3 py-1.5 text-xs font-semibold rounded-lg transition shadow-sm ${
            isOverdue.value
              ? "bg-red-600 text-white hover:bg-red-700"
              : "bg-stone-100 text-stone-700 hover:bg-stone-200"
          } disabled:opacity-50`}
        >
          {isFeeding.value ? "Feeding..." : "Feed Now"}
        </button>
      </div>

      <ConfirmDialog
        open={confirmOpen.value}
        title={`Feed ${cultureName}?`}
        message={`This will record a feeding of 100g flour + 100g water for ${cultureName}. The feeding timer will reset.`}
        confirmLabel="Feed Now"
        onConfirm={doFeed}
        onCancel={() => confirmOpen.value = false}
      />
    </div>
  );
}
