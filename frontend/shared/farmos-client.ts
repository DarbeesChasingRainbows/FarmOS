/**
 * Shared FarmOS API client factory.
 *
 * Provides the core `fetchFarmOS<T>()` function and `ApiError` class
 * used by all FarmOS micro-frontends. Each app imports from here
 * and adds its own domain-specific API object (e.g. FloraAPI, ApiaryAPI).
 *
 * Wire format: MessagePack (`application/x-msgpack`) for all internal APIs.
 * The backend MessagePackMiddleware handles content negotiation:
 *   - Request bodies are encoded as MessagePack binary
 *   - Response bodies are decoded from MessagePack binary
 *   - Falls back to JSON if the server doesn't support msgpack (e.g. error pages)
 *
 * Gateway URL resolution:
 *   Server-side (Deno): reads FARMOS_GATEWAY_URL or GATEWAY_URL env vars
 *   Client-side (browser): uses the current page origin (Caddy proxies /api/*)
 */

import { decode, encode } from "@msgpack/msgpack";

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
    // Browser — Caddy reverse-proxies /api/* on the same origin
    return globalThis.location.origin;
  }
  // Server-side Deno process
  return Deno.env.get("FARMOS_GATEWAY_URL") ||
    Deno.env.get("GATEWAY_URL") ||
    "http://localhost:5050";
};

const GATEWAY_URL = resolveGatewayUrl();

const MSGPACK_CONTENT_TYPE = "application/x-msgpack";

// ─── Core Fetch ───────────────────────────────────────────────────

/**
 * Unified FarmOS API client with MessagePack wire format.
 * All requests go through the Gateway (Caddy reverse proxy).
 * Throws ApiError for structured error handling in route handlers.
 *
 * Request bodies are encoded as MessagePack binary.
 * Responses are decoded from MessagePack binary, with JSON fallback.
 *
 * @param path   API path (e.g. "/api/flora/beds")
 * @param options Standard RequestInit options (body should be a plain object, not stringified)
 * @returns Parsed response, or null for 204 No Content
 */
export async function fetchFarmOS<T = unknown>(
  path: string,
  options: RequestInit = {},
): Promise<T | null> {
  const headers = new Headers(options.headers);

  // Always request MessagePack responses
  headers.set("Accept", MSGPACK_CONTENT_TYPE);

  // Encode request body as MessagePack for mutating operations
  let body = options.body;
  if (body && typeof body === "string") {
    // Body was passed as JSON string — parse and re-encode as MessagePack
    const parsed = JSON.parse(body);
    body = encode(parsed);
    headers.set("Content-Type", MSGPACK_CONTENT_TYPE);
  }

  let response: Response;
  try {
    response = await fetch(`${GATEWAY_URL}${path}`, {
      ...options,
      body,
      headers,
    });
  } catch (_err) {
    throw new ApiError(503, "Gateway unreachable — check network connection");
  }

  if (!response.ok) {
    // Error responses may come as JSON or MessagePack — try both
    if (response.status === 400) {
      const err = await decodeResponse(response);
      throw new ApiError(
        400,
        (err as Record<string, string>)?.message || "Domain rule violation",
      );
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
  return decodeResponse(response) as Promise<T>;
}

/**
 * Decode a response body based on Content-Type.
 * Supports both MessagePack and JSON (fallback for error pages, etc.)
 */
async function decodeResponse(response: Response): Promise<unknown> {
  const contentType = response.headers.get("Content-Type") || "";

  if (contentType.includes(MSGPACK_CONTENT_TYPE)) {
    const buffer = await response.arrayBuffer();
    return decode(new Uint8Array(buffer));
  }

  // Fallback to JSON (error responses, non-msgpack endpoints)
  return response.json();
}
