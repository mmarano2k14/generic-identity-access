import type {
  AuthorizationEvaluationResponse,
  IdentityAccessCredential,
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
  LivenessResponse,
  ReadinessResponse,
  ServiceInfoResponse,
} from "./contracts.js";
import { IdentityAccessClientError } from "./errors.js";

export type FetchTransport = (url: string, init: RequestInit) => Promise<Response>;

export interface IdentityAccessClientOptions {
  /** Trusted deployment configuration, never a value from a browser request. */
  readonly baseUrl: string;
  readonly timeoutMs?: number;
  /** An injected transport must honor AbortSignal and redirect: "error". */
  readonly fetch?: FetchTransport;
}

type JsonObject = Record<string, unknown>;

type RequestOptions = {
  readonly method: "GET" | "POST";
  readonly acceptedStatuses: readonly number[];
  readonly headers?: Readonly<Record<string, string>>;
  readonly body?: string;
  readonly signal?: AbortSignal;
};

/**
 * Class-based HTTP client for the Generic Identity Access API.
 * The client owns validated transport configuration but never owns global current-user state.
 */
export class IdentityAccessClient {
  readonly #baseUrl: URL;
  readonly #timeoutMs: number;
  readonly #transport: FetchTransport;

  public constructor(options: IdentityAccessClientOptions) {
    if (typeof URL !== "function" || typeof AbortController !== "function") {
      throw new IdentityAccessClientError("configuration");
    }

    this.#baseUrl = IdentityAccessClient.parseBaseAddress(options.baseUrl);
    this.#timeoutMs = options.timeoutMs ?? 10_000;

    if (!Number.isSafeInteger(this.#timeoutMs) || this.#timeoutMs < 1 || this.#timeoutMs > 120_000) {
      throw new IdentityAccessClientError("configuration");
    }

    const transport = options.fetch ?? globalThis.fetch?.bind(globalThis);
    if (typeof transport !== "function") {
      throw new IdentityAccessClientError("configuration");
    }

    this.#transport = transport;
  }

  public async liveness(signal?: AbortSignal): Promise<LivenessResponse> {
    return this.#request(
      "health/live",
      { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) },
      (value) => {
        if (IdentityAccessClient.object(value).status !== "alive") {
          throw new IdentityAccessClientError("protocol");
        }
        return { status: "alive" as const };
      },
    );
  }

  public async readiness(signal?: AbortSignal): Promise<ReadinessResponse> {
    return this.#request(
      "health/ready",
      { method: "GET", acceptedStatuses: [200, 503], ...(signal === undefined ? {} : { signal }) },
      (value, status) => {
        const data = IdentityAccessClient.object(value);
        const ready = IdentityAccessClient.flag(data.ready);
        if (!Array.isArray(data.blockingCapabilities) || ready !== (status === 200)) {
          throw new IdentityAccessClientError("protocol");
        }
        return {
          ready,
          stage: IdentityAccessClient.text(data.stage),
          blockingCapabilities: data.blockingCapabilities.map(IdentityAccessClient.text),
        };
      },
    );
  }

  public async info(signal?: AbortSignal): Promise<ServiceInfoResponse> {
    return this.#request(
      "api/v1/system/info",
      { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) },
      (value) => IdentityAccessClient.serviceInfo(value),
    );
  }

  /**
   * Evaluates one concrete capability through the server-side .NET authorization boundary.
   * A normal RBAC deny resolves to false; authentication, boundary, and technical failures throw.
   */
  public async evaluateCapability(
    boundary: IdentityAuthorizationBoundary,
    requirement: IdentityCapabilityRequirement,
    credential: IdentityAccessCredential,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const path = IdentityAccessClient.authorizationPath(boundary);
    const headers = IdentityAccessClient.credentialHeaders(credential);

    const response = await this.#request<AuthorizationEvaluationResponse>(
      path,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: {
          ...headers,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          resource: IdentityAccessClient.slug(requirement.resource),
          feature: IdentityAccessClient.slug(requirement.feature),
          action: IdentityAccessClient.slug(requirement.action),
        }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessClient.object(value);
        return { allowed: IdentityAccessClient.flag(data.allowed) };
      },
    );

    return response.allowed;
  }

  static authorizationPath(boundary: IdentityAuthorizationBoundary): string {
    const identityScopeId = IdentityAccessClient.uuid(boundary.identityScopeId);
    const applicationKey = IdentityAccessClient.slug(boundary.applicationKey);
    const tenantId = boundary.tenantId === undefined ? undefined : IdentityAccessClient.uuid(boundary.tenantId);
    const resourceScopeId = boundary.resourceScopeId === undefined
      ? undefined
      : IdentityAccessClient.uuid(boundary.resourceScopeId);

    if (resourceScopeId !== undefined && tenantId === undefined) {
      throw new IdentityAccessClientError("configuration");
    }

    if (resourceScopeId !== undefined) {
      return `api/v1/identity-scopes/${identityScopeId}/tenants/${tenantId}/applications/${applicationKey}/resource-scopes/${resourceScopeId}/authorization/evaluate`;
    }

    if (tenantId !== undefined) {
      return `api/v1/identity-scopes/${identityScopeId}/tenants/${tenantId}/applications/${applicationKey}/authorization/evaluate`;
    }

    return `api/v1/identity-scopes/${identityScopeId}/applications/${applicationKey}/authorization/evaluate`;
  }

  static credentialHeaders(credential: IdentityAccessCredential): Readonly<Record<string, string>> {
    if (credential.kind === "bearer") {
      return {
        Authorization: `Bearer ${IdentityAccessClient.token(credential.accessToken)}`,
      };
    }

    return {
      Authorization: `IdentitySession ${IdentityAccessClient.token(credential.sessionToken)}`,
      "X-Identity-Access-Client": IdentityAccessClient.nonEmpty(credential.clientId),
      "X-Identity-Access-Session": IdentityAccessClient.uuid(credential.sessionId),
    };
  }

  async #request<T>(
    path: string,
    options: RequestOptions,
    decode: (body: unknown, status: number) => T,
  ): Promise<T> {
    if (options.signal?.aborted) {
      throw new IdentityAccessClientError("cancelled");
    }

    const controller = new AbortController();
    let timedOut = false;
    const onAbort = (): void => controller.abort();
    options.signal?.addEventListener("abort", onAbort, { once: true });
    const timer = setTimeout(() => {
      timedOut = true;
      controller.abort();
    }, this.#timeoutMs);

    try {
      const headers: Record<string, string> = {
        Accept: "application/json",
        ...options.headers,
      };

      const response = await this.#transport(new URL(path, this.#baseUrl).toString(), {
        method: options.method,
        headers,
        ...(options.body === undefined ? {} : { body: options.body }),
        cache: "no-store",
        credentials: "omit",
        redirect: "error",
        signal: controller.signal,
      });

      if (!options.acceptedStatuses.includes(response.status)) {
        throw IdentityAccessClient.statusError(response.status);
      }

      let body: unknown;
      try {
        body = await response.json();
      } catch {
        if (controller.signal.aborted) {
          throw new IdentityAccessClientError(options.signal?.aborted ? "cancelled" : "timeout");
        }
        throw new IdentityAccessClientError("protocol");
      }

      return decode(body, response.status);
    } catch (error) {
      if (error instanceof IdentityAccessClientError) {
        throw error;
      }
      if (options.signal?.aborted) {
        throw new IdentityAccessClientError("cancelled");
      }
      if (timedOut) {
        throw new IdentityAccessClientError("timeout");
      }
      throw new IdentityAccessClientError("transport");
    } finally {
      clearTimeout(timer);
      options.signal?.removeEventListener("abort", onAbort);
    }
  }

  static statusError(status: number): IdentityAccessClientError {
    if (status === 401) return new IdentityAccessClientError("unauthenticated", status);
    if (status === 403) return new IdentityAccessClientError("forbidden", status);
    if (status === 503) return new IdentityAccessClientError("unavailable", status);
    return new IdentityAccessClientError("http", status);
  }

  static parseBaseAddress(value: string): URL {
    if (typeof value !== "string" || value !== value.trim()) {
      throw new IdentityAccessClientError("configuration");
    }

    let url: URL;
    try {
      url = new URL(value);
    } catch {
      throw new IdentityAccessClientError("configuration");
    }

    const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(url.hostname);
    if (
      url.username ||
      url.password ||
      url.search ||
      url.hash ||
      !(url.protocol === "https:" || (url.protocol === "http:" && loopback))
    ) {
      throw new IdentityAccessClientError("configuration");
    }

    if (!url.pathname.endsWith("/")) {
      url.pathname += "/";
    }

    return url;
  }

  static serviceInfo(value: unknown): ServiceInfoResponse {
    const data = IdentityAccessClient.object(value);
    if (data.service !== "identity-access" || data.apiVersion !== "v1" || data.storageProvider !== "postgresql") {
      throw new IdentityAccessClientError("protocol");
    }

    return {
      service: "identity-access",
      apiVersion: "v1",
      storageProvider: "postgresql",
      moduleVersion: IdentityAccessClient.text(data.moduleVersion),
      stage: IdentityAccessClient.text(data.stage),
      databaseRoutingConfigured: IdentityAccessClient.flag(data.databaseRoutingConfigured),
      storageConfigured: IdentityAccessClient.flag(data.storageConfigured),
      authenticationConfigured: IdentityAccessClient.flag(data.authenticationConfigured),
      authorizationConfigured: IdentityAccessClient.flag(data.authorizationConfigured),
    };
  }

  static object(value: unknown): JsonObject {
    if (typeof value !== "object" || value === null || Array.isArray(value)) {
      throw new IdentityAccessClientError("protocol");
    }
    return value as JsonObject;
  }

  static text(value: unknown): string {
    if (typeof value !== "string" || value.trim().length === 0) {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  static flag(value: unknown): boolean {
    if (typeof value !== "boolean") {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  static nonEmpty(value: string): string {
    if (typeof value !== "string" || value.length === 0 || value !== value.trim()) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  static token(value: string): string {
    const token = IdentityAccessClient.nonEmpty(value);
    if (/\s/u.test(token)) {
      throw new IdentityAccessClientError("configuration");
    }
    return token;
  }

  static uuid(value: string): string {
    const normalized = IdentityAccessClient.nonEmpty(value).toLowerCase();
    if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }

  static slug(value: string): string {
    const normalized = IdentityAccessClient.nonEmpty(value).toLowerCase();
    if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }
}
