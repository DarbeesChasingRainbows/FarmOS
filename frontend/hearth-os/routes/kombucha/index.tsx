import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import ArrowKombuchaDashboard from "../../islands/ArrowKombuchaDashboard.tsx";

export default define.page(function KombuchaIndex() {
  return (
    <div>
      <Head>
        <title>Kombucha — Hearth OS</title>
      </Head>
      <ArrowKombuchaDashboard />
    </div>
  );
});
