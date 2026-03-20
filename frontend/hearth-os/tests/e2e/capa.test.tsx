import { expect } from "@std/expect";
import { buildFreshApp, startTestServer } from "../test-utils.ts";

const app = await buildFreshApp();

Deno.test("CAPA route returns 200", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/compliance/capa`);
    await response.text(); // consume body to avoid leak

    expect(response.status).toBe(200);
  } finally {
    await server.shutdown();
  }
});

Deno.test("CAPA page renders dashboard elements", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/compliance/capa`);
    const html = await response.text();

    expect(html).toContain("CAPA Tracking");
    expect(html).toContain("Corrective and Preventive Actions");
  } finally {
    await server.shutdown();
  }
});
