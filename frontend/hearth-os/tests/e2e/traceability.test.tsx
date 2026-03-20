import { expect } from "@std/expect";
import { buildFreshApp, startTestServer } from "../test-utils.ts";

const app = await buildFreshApp();

Deno.test("Traceability route returns 200", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/compliance/traceability`);
    await response.text(); // consume body to avoid leak

    expect(response.status).toBe(200);
  } finally {
    await server.shutdown();
  }
});

Deno.test("Traceability page contains CTE form elements", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/compliance/traceability`);
    const html = await response.text();

    // HTML-escaped ampersand in rendered output
    expect(html).toContain("Traceability &amp; FSMA 204");
    expect(html).toContain("Receiving");
    expect(html).toContain("Transformation");
    expect(html).toContain("Shipping");
  } finally {
    await server.shutdown();
  }
});

Deno.test("Traceability page has 24-Hour Audit export button", async () => {
  const { server, address } = startTestServer(app);

  try {
    const response = await fetch(`${address}/compliance/traceability`);
    const html = await response.text();

    expect(html).toContain("24-Hour");
    expect(html).toContain("Audit");
  } finally {
    await server.shutdown();
  }
});
