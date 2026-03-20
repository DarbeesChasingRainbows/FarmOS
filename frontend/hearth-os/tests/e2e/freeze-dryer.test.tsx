import { expect } from "@std/expect";
import { buildFreshApp, startTestServer } from "../test-utils.ts";

const app = await buildFreshApp();

Deno.test("Freeze-dryer route returns 200", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/freeze-dryer`);
    await response.text(); // consume body to avoid leak

    expect(response.status).toBe(200);
  } finally {
    await server.shutdown();
  }
});

Deno.test("Freeze-dryer page renders batch form elements", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/freeze-dryer`);
    const html = await response.text();

    expect(html).toContain("Freeze-Dryer Management");
    expect(html).toContain("Harvest Right");
    expect(html).toContain("New Batch");
  } finally {
    await server.shutdown();
  }
});
