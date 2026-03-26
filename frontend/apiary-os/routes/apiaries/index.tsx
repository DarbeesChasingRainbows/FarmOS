import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowApiaryManager from "../../islands/ArrowApiaryManager.tsx";

export default define.page(function ApiariesPage() {
  return (
    <div>
      <Head>
        <title>Apiaries — Apiary OS</title>
      </Head>
      <ArrowApiaryManager />
    </div>
  );
});
