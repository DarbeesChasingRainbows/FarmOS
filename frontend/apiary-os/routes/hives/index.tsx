import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowHiveManager from "../../islands/ArrowHiveManager.tsx";

export default define.page(function HivesPage() {
  return (
    <div>
      <Head>
        <title>Hives — Apiary OS</title>
      </Head>
      <ArrowHiveManager />
    </div>
  );
});
