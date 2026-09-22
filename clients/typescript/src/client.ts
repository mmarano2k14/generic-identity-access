import type { LivenessResponse, ReadinessResponse, ServiceInfoResponse } from "./contracts.js";
import { IdentityAccessClientError } from "./errors.js";

export type FetchTransport = (url: string, init: RequestInit) => Promise<Response>;

export interface IdentityAccessClientOptions {
  /** Trusted deployment configuration, never a value from a browser request. */
  readonly baseUrl: string;
  readonly timeoutMs?: number;
  /** An injected transport must honor AbortSignal and redirect: "error". */
  readonly fetch?: FetchTransport;
}

export interface IdentityAccessClient {
  liveness(signal?: AbortSignal): Promise<LivenessResponse>;
  readiness(signal?: AbortSignal): Promise<ReadinessResponse>;
  info(signal?: AbortSignal): Promise<ServiceInfoResponse>;
}

type JsonObject = Record<string, unknown>;

function object(value: unknown): JsonObject {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new IdentityAccessClientError("protocol");
  }
  return value as JsonObject;
}

function text(value: unknown): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new IdentityAccessClientError("protocol");
  }
  return value;
}

function flag(value: unknown): boolean {
  if (typeof value !== "boolean") throw new IdentityAccessClientError("protocol");
  return value;
}

function serviceInfo(value: unknown): ServiceInfoResponse {
  const data = object(value);
  if (data.service !== "identity-access" || data.apiVersion !== "v1" || data.storageProvider !== "postgresql") {
    throw new IdentityAccessClientError("protocol");
  }
  return {
    service: "identity-access",
    apiVersion: "v1",
    storageProvider: "postgresql",
    moduleVersion: text(data.moduleVersion),
    stage: text(data.stage),
    databaseRoutingConfigured: flag(data.databaseRoutingConfigured),
    storageConfigured: flag(data.storageConfigured),
    authenticationConfigured: flag(data.authenticationConfigured),
    authorizationConfigured: flag(data.authorizationConfigured),
  };
}

function baseAddress(value: string): URL {
  if (typeof value !== "string" || value !== value.trim()) {
    throw new IdentityAccessClientError("configuration");
  }
  let url: URL;
  try { url = new URL(value); }
  catch { throw new IdentityAccessClientError("configuration"); }
  const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(url.hostname);
  if (url.username || url.password || url.search || url.hash ||
      !(url.protocol === "https:" || (url.protocol === "http:" && loopback))) {
    throw new IdentityAccessClientError("configuration");
  }
  if (!url.pathname.endsWith("/")) url.pathname += "/";
  return url;
}

/**
 * Initial diagnostic transport only. No login, permission evaluation, token
 * refresh or X-Access-Context rotation is implemented by this client.
 */
export function createIdentityAccessClient(options: IdentityAccessClientOptions): IdentityAccessClient {
  if (typeof URL !== "function" || typeof AbortController !== "function") {
    throw new IdentityAccessClientError("configuration");
  }
  const baseUrl = baseAddress(options.baseUrl);
  const timeoutMs = options.timeoutMs ?? 10_000;
  if (!Number.isSafeInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 120_000) {
    throw new IdentityAccessClientError("configuration");
  }
  const transport = options.fetch ?? globalThis.fetch?.bind(globalThis);
  if (typeof transport !== "function") throw new IdentityAccessClientError("configuration");

  async function request<T>(path: string, statuses: readonly number[],
    decode: (body: unknown, status: number) => T, signal?: AbortSignal): Promise<T> {
    if (signal?.aborted) throw new IdentityAccessClientError("cancelled");
    const controller = new AbortController();
    let timedOut = false;
    const onAbort = () => controller.abort();
    signal?.addEventListener("abort", onAbort, { once: true });
    const timer = setTimeout(() => { timedOut = true; controller.abort(); }, timeoutMs);
    try {
      const response = await transport!(new URL(path, baseUrl).toString(), {
        method: "GET",
        headers: { Accept: "application/json" },
        cache: "no-store",
        credentials: "omit",
        redirect: "error",
        signal: controller.signal,
      });
      if (!statuses.includes(response.status)) {
        throw new IdentityAccessClientError("http", response.status);
      }
      let body: unknown;
      try { body = await response.json(); }
      catch {
        if (controller.signal.aborted) throw new IdentityAccessClientError(signal?.aborted ? "cancelled" : "timeout");
        throw new IdentityAccessClientError("protocol");
      }
      return decode(body, response.status);
    } catch (error) {
      if (error instanceof IdentityAccessClientError) throw error;
      if (signal?.aborted) throw new IdentityAccessClientError("cancelled");
      if (timedOut) throw new IdentityAccessClientError("timeout");
      throw new IdentityAccessClientError("transport");
    } finally {
      clearTimeout(timer);
      signal?.removeEventListener("abort", onAbort);
    }
  }

  return Object.freeze({
    liveness: (signal?: AbortSignal) => request("health/live", [200], (value) => {
      if (object(value).status !== "alive") throw new IdentityAccessClientError("protocol");
      return { status: "alive" as const };
    }, signal),
    readiness: (signal?: AbortSignal) => request("health/ready", [200, 503], (value, status) => {
      const data = object(value);
      const ready = flag(data.ready);
      if (!Array.isArray(data.blockingCapabilities) || ready !== (status === 200)) {
        throw new IdentityAccessClientError("protocol");
      }
      return { ready, stage: text(data.stage), blockingCapabilities: data.blockingCapabilities.map(text) };
    }, signal),
    info: (signal?: AbortSignal) => request("api/v1/system/info", [200], serviceInfo, signal),
  });
}
