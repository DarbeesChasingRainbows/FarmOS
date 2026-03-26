import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import ArrowHearthDashboard from "../islands/ArrowHearthDashboard.tsx";

export default define.page(function Dashboard() {
  return (
    <div>
      <Head>
        <title>Dashboard — Hearth OS</title>
      </Head>
      <ArrowHearthDashboard />
    </div>
  );
});
