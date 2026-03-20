import CompostPanel from "../../islands/CompostPanel.tsx";
import { define } from "../../utils.ts";

export default define.page(function CompostPage() {
  return (
    <main
      style={{ padding: "2rem", minHeight: "100%", boxSizing: "border-box" }}
    >
      <CompostPanel />
    </main>
  );
});
