import { expect } from "@std/expect";
import { render } from "npm:preact-render-to-string";
import StatusBadge from "../components/StatusBadge.tsx";

Deno.test("StatusBadge", async (t) => {
  await t.step("renders default label for active variant", () => {
    const html = render(<StatusBadge variant="active" />);
    expect(html).toContain("Active");
    expect(html).toContain("bg-emerald-100");
  });

  await t.step("renders custom label", () => {
    const html = render(<StatusBadge variant="fermenting" label="My Label" />);
    expect(html).toContain("My Label");
    expect(html).toContain("bg-amber-100");
  });

  await t.step("renders with correct icon", () => {
    const html = render(<StatusBadge variant="complete" />);
    expect(html).toContain("✅");
  });
});
