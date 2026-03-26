/**
 * Shared FarmOS API client factory.
 *
 * Provides the core `fetchFarmOS<T>()` function and `ApiError` class
 * used by all FarmOS micro-frontends. Each app imports from here
 * and adds its own domain-specific API object (e.g. FloraAPI, ApiaryAPI).
 *
 * Wire format: JSON (`application/json`) for all internal APIs.
 *
 * Gateway URL resolution:
 *   Server-side (Deno): reads FARMOS_GATEWAY_URL or GATEWAY_URL env vars
 *   Client-side (browser): uses localhost:5050 (Caddy gateway)
 */

// ─── Error Types ──────────────────────────────────────────────────

/**
 * Structured API error with HTTP status code.
 * Thrown by fetchFarmOS for non-2xx responses.
 */
export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

// ─── Gateway URL Resolution ───────────────────────────────────────

const resolveGatewayUrl = (): string => {
  if (typeof document !== "undefined") {
    // Browser — in dev, Deno Fresh runs on :8000 but Caddy gateway is on :5050
    const injected = (globalThis as unknown as Record<string, string>).__FARMOS_GATEWAY_URL__;
    if (injected) return injected;
    return "http://localhost:5050";
  }
  // Server-side Deno process
  return Deno.env.get("FARMOS_GATEWAY_URL") ||
    Deno.env.get("GATEWAY_URL") ||
    "http://localhost:5050";
};

const GATEWAY_URL = resolveGatewayUrl();

// ─── Core Fetch ───────────────────────────────────────────────────

/**
 * Unified FarmOS API client with JSON wire format.
 * All requests go through the Gateway (Caddy reverse proxy).
 * Throws ApiError for structured error handling in route handlers.
 *
 * @param path   API path (e.g. "/api/flora/beds")
 * @param options Standard RequestInit options (body should be a JSON string)
 * @returns Parsed response, or null for 204 No Content
 */
export async function fetchFarmOS<T = unknown>(
  path: string,
  options: RequestInit = {},
): Promise<T | null> {
  const headers = new Headers(options.headers);

  headers.set("Accept", "application/json");

  // Set Content-Type for request bodies
  if (options.body && typeof options.body === "string") {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;
  try {
    response = await fetch(`${GATEWAY_URL}${path}`, {
      ...options,
      headers,
    });
  } catch (_err) {
    throw new ApiError(503, "Gateway unreachable — check network connection");
  }

  if (!response.ok) {
    if (response.status === 400) {
      try {
        const err = await response.json();
        throw new ApiError(
          400,
          (err as Record<string, string>)?.message || "Domain rule violation",
        );
      } catch (e) {
        if (e instanceof ApiError) throw e;
        throw new ApiError(400, "Bad request");
      }
    }
    if (response.status === 404) {
      throw new ApiError(404, "Resource not found");
    }
    throw new ApiError(
      response.status,
      `HTTP ${response.status}: ${response.statusText}`,
    );
  }

  if (response.status === 204) return null;
  return response.json() as Promise<T>;
}
