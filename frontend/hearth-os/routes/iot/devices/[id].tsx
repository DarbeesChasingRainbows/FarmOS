import { Head } from "fresh/runtime";
import { define } from "../../../utils.ts";
import ArrowDeviceDetail from "../../../islands/ArrowDeviceDetail.tsx";

export default define.page(function DeviceDetailPage(ctx) {
  const { id } = ctx.params;
  return (
    <div>
      <Head>
        <title>Device Detail — Hearth OS</title>
      </Head>
      <ArrowDeviceDetail deviceId={id} />
    </div>
  );
});
