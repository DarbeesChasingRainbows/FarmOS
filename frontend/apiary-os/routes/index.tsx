import { Head } from "fresh/runtime";
import { define } from "../utils.ts";
import ArrowDashboard from "../islands/ArrowDashboard.tsx";

export default define.page(function Home() {
  return (
    <div>
      <Head>
        <title>Dashboard — Apiary OS</title>
      </Head>
      <ArrowDashboard />
    </div>
  );
});
