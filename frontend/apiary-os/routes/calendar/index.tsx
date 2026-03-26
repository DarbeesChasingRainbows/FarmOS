import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowTaskCalendar from "../../islands/ArrowTaskCalendar.tsx";

export default define.page(function CalendarPage() {
  return (
    <div>
      <Head>
        <title>Calendar — Apiary OS</title>
      </Head>
      <ArrowTaskCalendar />
    </div>
  );
});
