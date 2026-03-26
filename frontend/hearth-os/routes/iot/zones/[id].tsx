import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowZoneDetail from "../../../islands/ArrowZoneDetail.tsx";

export default define.page(function ZoneDetailPage(ctx) {
  const { id } = ctx.params;
  return (
    <div>
      <Head>
        <title>Zone Detail — Hearth OS</title>
      </Head>
      <ArrowZoneDetail zoneId={id} />
    </div>
  );
});
