import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import Tooltip, { InfoIcon } from "../../components/Tooltip.tsx";
import TaskCalendar from "../../islands/TaskCalendar.tsx";

export default define.page(function CalendarPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Calendar — Apiary OS</title>
      </Head>

      <div class="flex items-center justify-between mb-8">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              Seasonal Task Calendar
            </h1>
            <Tooltip text="Monthly beekeeping tasks based on standard temperate-climate management practices. Adjust timing based on your local climate and conditions.">
              <InfoIcon />
            </Tooltip>
          </div>
          <p class="text-stone-500 mt-1">
            Stay on top of seasonal management — inspections, treatments, and harvest windows.
          </p>
        </div>
      </div>

      <TaskCalendar />
    </div>
  );
});
