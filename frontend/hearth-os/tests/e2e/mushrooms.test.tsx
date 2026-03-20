import { expect } from "@std/expect";
import { buildFreshApp, startTestServer } from "../test-utils.ts";

const app = await buildFreshApp();

Deno.test("Mushroom dashboard renders correctly", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/mushrooms`);
    const html = await response.text();

    expect(response.status).toBe(200);
    expect(html).toContain("Mushroom Cultivation");
    expect(html).toContain("New Batch");
  } finally {
    await server.shutdown();
  }
});
