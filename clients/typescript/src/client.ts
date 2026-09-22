import type {
  AuthorizationEvaluationResponse,
  IdentityAccessCredential,
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
  IdentityLocalSession,
  IdentityLogoutResult,
  IdentityOidcAuthorizationCode,
  IdentityOidcAuthorizationOptions,
  IdentityOidcTokenSet,
  IdentityPasswordLoginRequest,
  IdentitySessionCredential,
  IdentitySessionValidationResult,
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
  /** An injected transport must honor AbortSignal and the requested redirect mode. */
  readonly fetch?: FetchTransport;
}

type JsonObject = Record<string, unknown>;

type RequestOptions = {
  readonly method: "GET" | "POST";
  readonly acceptedStatuses: readonly number[];
  readonly headers?: Readonly<Record<string, string>>;
  readonly body?: string;
  readonly signal?: AbortSignal;
  readonly redirect?: RequestRedirect;
};

/**
 * Class-based HTTP client for the Generic Identity Access API.
 * The client owns validated transport configuration but never owns global current-user or token state.
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
    return this.#requestJson(
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
    return this.#requestJson(
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
    return this.#requestJson(
      "api/v1/system/info",
      { method: "GET", acceptedStatuses: [200], ...(signal === undefined ? {} : { signal }) },
      (value) => IdentityAccessClient.serviceInfo(value),
    );
  }

  /** Authenticates with the registered local client and returns a reusable opaque session credential. */
  public async passwordLogin(
    request: IdentityPasswordLoginRequest,
    signal?: AbortSignal,
  ): Promise<IdentityLocalSession> {
    const clientId = IdentityAccessClient.clientId(request.clientId);
    const redirectUri = IdentityAccessClient.redirectUri(request.redirectUri);
    const loginIdentifier = IdentityAccessClient.nonEmpty(request.loginIdentifier);
    const password = IdentityAccessClient.nonEmptySecret(request.password);

    return this.#requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/password-login`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ loginIdentifier, password, redirectUri }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessClient.object(value);
        const returnedRedirectUri = IdentityAccessClient.redirectUri(IdentityAccessClient.text(data.redirectUri));
        if (returnedRedirectUri !== redirectUri) {
          throw new IdentityAccessClientError("protocol");
        }

        return {
          kind: "session" as const,
          clientId,
          userId: IdentityAccessClient.uuid(IdentityAccessClient.text(data.userId)),
          sessionId: IdentityAccessClient.uuid(IdentityAccessClient.text(data.sessionId)),
          sessionToken: IdentityAccessClient.token(IdentityAccessClient.text(data.sessionToken)),
          expiresAt: IdentityAccessClient.timestamp(data.expiresAt),
          redirectUri: returnedRedirectUri,
        };
      },
    );
  }

  /** Revalidates an opaque local session against current persisted session/user state. */
  public async validateSession(
    credential: IdentitySessionCredential,
    signal?: AbortSignal,
  ): Promise<IdentitySessionValidationResult> {
    const clientId = IdentityAccessClient.clientId(credential.clientId);
    const sessionId = IdentityAccessClient.uuid(credential.sessionId);
    const sessionToken = IdentityAccessClient.token(credential.sessionToken);

    return this.#requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/sessions/validate`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId, sessionToken }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessClient.object(value);
        const returnedSessionId = IdentityAccessClient.uuid(IdentityAccessClient.text(data.sessionId));
        if (returnedSessionId !== sessionId) {
          throw new IdentityAccessClientError("protocol");
        }
        return {
          userId: IdentityAccessClient.uuid(IdentityAccessClient.text(data.userId)),
          sessionId: returnedSessionId,
          expiresAt: IdentityAccessClient.timestamp(data.expiresAt),
        };
      },
    );
  }

  /** Revokes a local session. A post-logout redirect is accepted only when the server registration accepts it. */
  public async logout(
    credential: IdentitySessionCredential,
    postLogoutRedirectUri?: string,
    signal?: AbortSignal,
  ): Promise<IdentityLogoutResult> {
    const clientId = IdentityAccessClient.clientId(credential.clientId);
    const sessionId = IdentityAccessClient.uuid(credential.sessionId);
    const sessionToken = IdentityAccessClient.token(credential.sessionToken);
    const redirect = postLogoutRedirectUri === undefined
      ? undefined
      : IdentityAccessClient.redirectUri(postLogoutRedirectUri);

    return this.#requestJson(
      `api/v1/authentication/clients/${encodeURIComponent(clientId)}/logout`,
      {
        method: "POST",
        acceptedStatuses: [200],
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId,
          sessionToken,
          ...(redirect === undefined ? {} : { postLogoutRedirectUri: redirect }),
        }),
        ...(signal === undefined ? {} : { signal }),
      },
      (value) => {
        const data = IdentityAccessClient.object(value);
        if (data.postLogoutRedirectUri === null || data.postLogoutRedirectUri === undefined) {
          return {};
        }
        const returned = IdentityAccessClient.redirectUri(IdentityAccessClient.text(data.postLogoutRedirectUri));
        if (redirect === undefined || returned !== redirect) {
          throw new IdentityAccessClientError("protocol");
        }
        return { postLogoutRedirectUri: returned };
      },
    );
  }

  /**
   * Executes the OIDC authorization endpoint through an already validated local session.
   * PKCE verifier, state, and nonce are generated cryptographically when not supplied.
   * The redirect is never followed; the one-time code is extracted from the validated Location header.
   */
  public async authorizeOidc(
    credential: IdentitySessionCredential,
    options: IdentityOidcAuthorizationOptions,
    signal?: AbortSignal,
  ): Promise<IdentityOidcAuthorizationCode> {
    const clientId = IdentityAccessClient.clientId(options.clientId);
    if (IdentityAccessClient.clientId(credential.clientId) !== clientId) {
      throw new IdentityAccessClientError("configuration");
    }

    const redirectUri = IdentityAccessClient.redirectUri(options.redirectUri);
    const state = options.state === undefined
      ? IdentityAccessClient.randomOpaque(32)
      : IdentityAccessClient.opaqueState(options.state);
    const nonce = options.nonce === undefined
      ? IdentityAccessClient.randomOpaque(32)
      : IdentityAccessClient.nonce(options.nonce);
    const codeVerifier = IdentityAccessClient.randomOpaque(32);
    const codeChallenge = await IdentityAccessClient.computeS256Challenge(codeVerifier);

    const query = new URLSearchParams({
      client_id: clientId,
      redirect_uri: redirectUri,
      response_type: "code",
      scope: "openid",
      state,
      nonce,
      code_challenge: codeChallenge,
      code_challenge_method: "S256",
    });

    const response = await this.#perform(
      `connect/authorize?${query.toString()}`,
      {
        method: "GET",
        acceptedStatuses: [302],
        headers: IdentityAccessClient.oidcSessionHeaders(credential),
        redirect: "manual",
        ...(signal === undefined ? {} : { signal }),
      },
      async (result) => result,
    );

    const location = response.headers.get("location");
    if (location === null || location.trim().length === 0) {
      throw new IdentityAccessClientError("protocol");
    }

    const redirect = IdentityAccessClient.parseAuthorizationRedirect(location, redirectUri, state);
    return {
      clientId,
      redirectUri,
      code: redirect.code,
      state,
      nonce,
      codeVerifier,
    };
  }

  /** Exchanges one authorization code using the exact PKCE verifier bound to that authorization attempt. */
  public async exchangeAuthorizationCode(
    authorization: IdentityOidcAuthorizationCode,
    signal?: AbortSignal,
  ): Promise<IdentityOidcTokenSet> {
    const clientId = IdentityAccessClient.clientId(authorization.clientId);
    const redirectUri = IdentityAccessClient.redirectUri(authorization.redirectUri);
    const code = IdentityAccessClient.opaqueToken43(authorization.code);
    const codeVerifier = IdentityAccessClient.pkceVerifier(authorization.codeVerifier);

    const result = await this.#tokenRequest(
      new URLSearchParams({
        client_id: clientId,
        grant_type: "authorization_code",
        code,
        redirect_uri: redirectUri,
        code_verifier: codeVerifier,
      }),
      signal,
    );

    if (result.idToken === undefined) {
      throw new IdentityAccessClientError("protocol");
    }
    return result;
  }

  /** Rotates one refresh token. The caller must discard the presented refresh token after success. */
  public async refreshOidcTokens(
    clientIdValue: string,
    refreshTokenValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOidcTokenSet> {
    const clientId = IdentityAccessClient.clientId(clientIdValue);
    const refreshToken = IdentityAccessClient.opaqueToken43(refreshTokenValue);
    const result = await this.#tokenRequest(
      new URLSearchParams({
        client_id: clientId,
        grant_type: "refresh_token",
        refresh_token: refreshToken,
      }),
      signal,
    );

    if (result.idToken !== undefined) {
      throw new IdentityAccessClientError("protocol");
    }
    return result;
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

    const response = await this.#requestJson<AuthorizationEvaluationResponse>(
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
      "X-Identity-Access-Client": IdentityAccessClient.clientId(credential.clientId),
      "X-Identity-Access-Session": IdentityAccessClient.uuid(credential.sessionId),
    };
  }

  async #tokenRequest(form: URLSearchParams, signal?: AbortSignal): Promise<IdentityOidcTokenSet> {
    return this.#requestJson(
      "connect/token",
      {
        method: "POST",
        acceptedStatuses: [200, 400, 503],
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: form.toString(),
        ...(signal === undefined ? {} : { signal }),
      },
      (value, status) => {
        if (status !== 200) {
          const error = IdentityAccessClient.oidcError(value);
          if (status === 503 || error === "temporarily_unavailable") {
            throw new IdentityAccessClientError("unavailable", status, error);
          }
          throw new IdentityAccessClientError("oidc", status, error);
        }

        const data = IdentityAccessClient.object(value);
        if (data.token_type !== "Bearer" || data.scope !== "openid") {
          throw new IdentityAccessClientError("protocol");
        }
        const expiresIn = IdentityAccessClient.positiveInteger(data.expires_in);
        const idToken = data.id_token === undefined
          ? undefined
          : IdentityAccessClient.token(IdentityAccessClient.text(data.id_token));

        return {
          accessToken: IdentityAccessClient.token(IdentityAccessClient.text(data.access_token)),
          tokenType: "Bearer" as const,
          expiresIn,
          ...(idToken === undefined ? {} : { idToken }),
          refreshToken: IdentityAccessClient.opaqueToken43(IdentityAccessClient.text(data.refresh_token)),
          scope: "openid" as const,
        };
      },
    );
  }

  async #requestJson<T>(
    path: string,
    options: RequestOptions,
    decode: (body: unknown, status: number) => T,
  ): Promise<T> {
    return this.#perform(path, options, async (response) => {
      let body: unknown;
      try {
        body = await response.json();
      } catch {
        throw new IdentityAccessClientError("protocol");
      }
      return decode(body, response.status);
    });
  }

  async #perform<T>(
    path: string,
    options: RequestOptions,
    decode: (response: Response) => Promise<T>,
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
        redirect: options.redirect ?? "error",
        signal: controller.signal,
      });

      if (!options.acceptedStatuses.includes(response.status)) {
        throw IdentityAccessClient.statusError(response.status);
      }

      return await decode(response);
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

  static parseAuthorizationRedirect(
    location: string,
    expectedRedirectUri: string,
    expectedState: string,
  ): { readonly code: string } {
    let actual: URL;
    let expected: URL;
    try {
      actual = new URL(location);
      expected = new URL(expectedRedirectUri);
    } catch {
      throw new IdentityAccessClientError("protocol");
    }

    const expectedProtocolParameters = ["code", "state", "error"];
    if (expectedProtocolParameters.some((name) => expected.searchParams.has(name))) {
      throw new IdentityAccessClientError("configuration");
    }

    const base = new URL(actual.toString());
    for (const parameter of expectedProtocolParameters) {
      base.searchParams.delete(parameter);
    }
    if (base.toString() !== expected.toString()) {
      throw new IdentityAccessClientError("protocol");
    }

    const stateValues = actual.searchParams.getAll("state");
    if (stateValues.length !== 1 || stateValues[0] !== expectedState) {
      throw new IdentityAccessClientError("protocol");
    }

    const errorValues = actual.searchParams.getAll("error");
    const codeValues = actual.searchParams.getAll("code");
    if (errorValues.length === 1 && codeValues.length === 0) {
      const protocolCode = IdentityAccessClient.oidcProtocolCode(errorValues[0] ?? "");
      throw new IdentityAccessClientError("oidc", 302, protocolCode);
    }
    if (errorValues.length !== 0 || codeValues.length !== 1) {
      throw new IdentityAccessClientError("protocol");
    }

    return { code: IdentityAccessClient.opaqueToken43(codeValues[0] ?? "") };
  }

  static oidcSessionHeaders(credential: IdentitySessionCredential): Readonly<Record<string, string>> {
    return {
      Authorization: `IdentitySession ${IdentityAccessClient.token(credential.sessionToken)}`,
      "X-Identity-Access-Session": IdentityAccessClient.uuid(credential.sessionId),
    };
  }

  static async computeS256Challenge(codeVerifier: string): Promise<string> {
    const verifier = IdentityAccessClient.pkceVerifier(codeVerifier);
    const crypto = globalThis.crypto;
    if (crypto === undefined || crypto.subtle === undefined) {
      throw new IdentityAccessClientError("configuration");
    }
    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
    return IdentityAccessClient.base64Url(new Uint8Array(digest));
  }

  static randomOpaque(byteLength: number): string {
    if (!Number.isSafeInteger(byteLength) || byteLength < 16 || byteLength > 64) {
      throw new IdentityAccessClientError("configuration");
    }
    const crypto = globalThis.crypto;
    if (crypto === undefined || typeof crypto.getRandomValues !== "function") {
      throw new IdentityAccessClientError("configuration");
    }
    const bytes = new Uint8Array(byteLength);
    crypto.getRandomValues(bytes);
    return IdentityAccessClient.base64Url(bytes);
  }

  static base64Url(bytes: Uint8Array): string {
    const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    let output = "";
    for (let index = 0; index < bytes.length; index += 3) {
      const first = bytes[index] ?? 0;
      const secondPresent = index + 1 < bytes.length;
      const thirdPresent = index + 2 < bytes.length;
      const second = bytes[index + 1] ?? 0;
      const third = bytes[index + 2] ?? 0;
      const value = (first << 16) | (second << 8) | third;
      output += alphabet[(value >> 18) & 63] ?? "";
      output += alphabet[(value >> 12) & 63] ?? "";
      output += secondPresent ? (alphabet[(value >> 6) & 63] ?? "") : "=";
      output += thirdPresent ? (alphabet[value & 63] ?? "") : "=";
    }
    return output.replace(/=+$/u, "").replace(/\+/gu, "-").replace(/\//gu, "_");
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

  static positiveInteger(value: unknown): number {
    if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 1) {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  static timestamp(value: unknown): string {
    const text = IdentityAccessClient.text(value);
    if (!Number.isFinite(Date.parse(text))) {
      throw new IdentityAccessClientError("protocol");
    }
    return text;
  }

  static nonEmpty(value: string): string {
    if (typeof value !== "string" || value.length === 0 || value !== value.trim()) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  static nonEmptySecret(value: string): string {
    if (typeof value !== "string" || value.length === 0) {
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

  static opaqueToken43(value: string): string {
    const token = IdentityAccessClient.token(value);
    if (!/^[A-Za-z0-9_-]{43}$/u.test(token)) {
      throw new IdentityAccessClientError("protocol");
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

  static clientId(value: string): string {
    const normalized = IdentityAccessClient.nonEmpty(value);
    if (!/^[a-z][a-z0-9_-]{0,127}$/u.test(normalized)) {
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

  static redirectUri(value: string): string {
    const uri = IdentityAccessClient.nonEmpty(value);
    if (uri.includes("*")) {
      throw new IdentityAccessClientError("configuration");
    }
    let parsed: URL;
    try {
      parsed = new URL(uri);
    } catch {
      throw new IdentityAccessClientError("configuration");
    }
    const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(parsed.hostname);
    if (
      parsed.username ||
      parsed.password ||
      parsed.hash ||
      !(parsed.protocol === "https:" || (parsed.protocol === "http:" && loopback))
    ) {
      throw new IdentityAccessClientError("configuration");
    }
    return uri;
  }

  static opaqueState(value: string): string {
    const state = IdentityAccessClient.nonEmpty(value);
    if (state.length < 8 || state.length > 512 || /[\u0000-\u001f\u007f]/u.test(state)) {
      throw new IdentityAccessClientError("configuration");
    }
    return state;
  }

  static nonce(value: string): string {
    const nonce = IdentityAccessClient.nonEmpty(value);
    if (nonce.length < 8 || nonce.length > 256 || /[\u0000-\u001f\u007f]/u.test(nonce)) {
      throw new IdentityAccessClientError("configuration");
    }
    return nonce;
  }

  static pkceVerifier(value: string): string {
    const verifier = IdentityAccessClient.nonEmpty(value);
    if (!/^[A-Za-z0-9\-._~]{43,128}$/u.test(verifier)) {
      throw new IdentityAccessClientError("configuration");
    }
    return verifier;
  }

  static oidcError(value: unknown): string {
    const data = IdentityAccessClient.object(value);
    return IdentityAccessClient.oidcProtocolCode(IdentityAccessClient.text(data.error));
  }

  static oidcProtocolCode(value: string): string {
    const code = IdentityAccessClient.nonEmpty(value);
    const allowed = new Set([
      "invalid_request",
      "invalid_client",
      "invalid_grant",
      "unsupported_grant_type",
      "unsupported_response_type",
      "invalid_scope",
      "login_required",
      "temporarily_unavailable",
    ]);
    if (!allowed.has(code)) {
      throw new IdentityAccessClientError("protocol");
    }
    return code;
  }
}
