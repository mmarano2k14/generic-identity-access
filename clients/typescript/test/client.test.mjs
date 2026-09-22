import test from "node:test";
import assert from "node:assert/strict";
import { createServer } from "node:http";
import * as apiExports from "../dist/index.js";
import {
  IdentityAccessAdminUiBuilder,
  IdentityAccessClient,
  IdentityAccessClientError,
  IdentityAuthorizationContext,
  RequireCapability,
} from "../dist/index.js";

const scopeId = "42111111-1111-1111-1111-111111111111";
const tenantId = "42bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const resourceScopeId = "42cccccc-cccc-cccc-cccc-cccccccccccc";
const sessionId = "42dddddd-dddd-dddd-dddd-dddddddddddd";

const info = {
  service: "identity-access", apiVersion: "v1", moduleVersion: "0.42.0", stage: "configuration",
  storageProvider: "postgresql", databaseRoutingConfigured: false, storageConfigured: false,
  authenticationConfigured: false, authorizationConfigured: false,
};
const notReady = {
  ready: false,
  stage: "configuration",
  blockingCapabilities: [
    "database-routing",
    "postgresql-persistence",
    "authentication",
    "administration-authorization",
  ],
};
const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } });
const client = (transport, options = {}) => new IdentityAccessClient({ baseUrl: "https://identity.example.test/", fetch: transport, ...options });
const code = (expected) => (error) => error instanceof IdentityAccessClientError && error.code === expected;
const bearer = { kind: "bearer", accessToken: "header.payload.signature" };
const session = { kind: "session", clientId: "admin-web", sessionId, sessionToken: "opaque-session-token" };

for (const baseUrl of ["", "invalid", " http://localhost", "http://remote.example.test", "https://user:secret@example.test", "https://example.test/?token=secret", "https://example.test/#token", "file:///tmp/identity"]) {
  test(`rejects unsafe or invalid base URL: ${baseUrl}`, () => {
    assert.throws(() => new IdentityAccessClient({ baseUrl }), code("configuration"));
  });
}

test("the legacy functional client factory is not exported", () => {
  assert.equal("createIdentityAccessClient" in apiExports, false);
});

test("liveness uses the configured path prefix and non-cacheable transport", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/iam/health/live");
    assert.equal(init.cache, "no-store");
    assert.equal(init.redirect, "error");
    assert.equal(init.credentials, "omit");
    assert.equal(init.method, "GET");
    assert.ok(init.signal instanceof AbortSignal);
    assert.deepEqual(Object.keys(init.headers), ["Accept"]);
    return json({ status: "alive" });
  }, { baseUrl: "https://identity.example.test/iam" });
  assert.deepEqual(await api.liveness(), { status: "alive" });
});

test("validates and returns the service descriptor", async () => {
  assert.deepEqual(await client(async () => json(info)).info(), info);
});

test("readiness accepts a documented 503 without converting it into a transport failure", async () => {
  assert.deepEqual(await client(async () => json(notReady, 503)).readiness(), notReady);
});

test("readiness permits 200 only with ready true", async () => {
  const ready = { ready: true, stage: "future", blockingCapabilities: [] };
  assert.deepEqual(await client(async () => json(ready)).readiness(), ready);
  await assert.rejects(client(async () => json(notReady)).readiness(), code("protocol"));
});

test("readiness rejects a 503 that claims ready true", async () => {
  await assert.rejects(client(async () => json({ ...notReady, ready: true }, 503)).readiness(), code("protocol"));
});

test("invalid blocking capability values are rejected", async () => {
  await assert.rejects(client(async () => json({ ...notReady, blockingCapabilities: [42] }, 503)).readiness(), code("protocol"));
});

test("security HTTP statuses remain distinct", async () => {
  await assert.rejects(client(async () => json({}, 401)).info(), code("unauthenticated"));
  await assert.rejects(client(async () => json({}, 403)).info(), code("forbidden"));
  await assert.rejects(client(async () => json({}, 503)).info(), code("unavailable"));
  await assert.rejects(client(async () => json({}, 500)).info(), code("http"));
});

test("HTTP errors never retain response bodies", async () => {
  await assert.rejects(client(async () => json({ secret: "must-not-escape" }, 403)).info(), (error) => {
    assert.equal(error.code, "forbidden");
    assert.equal(error.httpStatus, 403);
    assert.ok(!JSON.stringify(error).includes("must-not-escape"));
    assert.ok(!String(error).includes("must-not-escape"));
    return true;
  });
});

test("malformed JSON is a protocol error", async () => {
  await assert.rejects(client(async () => new Response("<html>secret</html>")).info(), code("protocol"));
});

test("missing descriptor flags are rejected", async () => {
  for (const field of [
    "databaseRoutingConfigured",
    "storageConfigured",
    "authenticationConfigured",
    "authorizationConfigured",
  ]) {
    const missing = { ...info };
    delete missing[field];
    await assert.rejects(client(async () => json(missing)).info(), code("protocol"));
  }
});

test("incompatible API versions are rejected", async () => {
  await assert.rejects(client(async () => json({ ...info, apiVersion: "v99" })).info(), code("protocol"));
});

test("transport failures do not disclose underlying error details", async () => {
  await assert.rejects(client(async () => { throw new Error("secret-token-in-url"); }).info(), (error) => {
    assert.equal(error.code, "transport");
    assert.ok(!String(error).includes("secret-token-in-url"));
    assert.equal(error.cause, undefined);
    return true;
  });
});

const abortingTransport = (_url, init) => new Promise((_resolve, reject) => {
  init.signal.addEventListener("abort", () => reject(new Error("aborted")), { once: true });
});

test("request timeout aborts the transport", async () => {
  await assert.rejects(client(abortingTransport, { timeoutMs: 10 }).info(), code("timeout"));
});

test("already-cancelled request never calls the transport", async () => {
  const signal = AbortSignal.abort();
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json(info); }).info(signal), code("cancelled"));
  assert.equal(calls, 0);
});

test("caller cancellation remains separate from timeout", async () => {
  const controller = new AbortController();
  const pending = client(abortingTransport).info(controller.signal);
  controller.abort();
  await assert.rejects(pending, code("cancelled"));
});

test("requests are not automatically retried", async () => {
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json({}, 500); }).info(), code("http"));
  assert.equal(calls, 1);
});

test("concurrent clients preserve independent destinations", async () => {
  const urls = [];
  const transport = async (url) => { urls.push(url); return json(info); };
  await Promise.all([
    client(transport, { baseUrl: "https://one.example.test" }).info(),
    client(transport, { baseUrl: "https://two.example.test" }).info(),
  ]);
  assert.deepEqual(urls.sort(), ["https://one.example.test/api/v1/system/info", "https://two.example.test/api/v1/system/info"]);
});

test("invalid timeout values are rejected", () => {
  for (const timeoutMs of [0, -1, 0.5, Infinity, NaN, 120001]) {
    assert.throws(() => client(async () => json(info), { timeoutMs }), code("configuration"));
  }
});

test("bearer authorization context delegates to the tenant authorization endpoint", async () => {
  const transport = async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${scopeId}/tenants/${tenantId}/applications/app-a/authorization/evaluate`);
    assert.equal(init.method, "POST");
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    assert.equal("X-Identity-Access-Client" in init.headers, false);
    assert.deepEqual(JSON.parse(init.body), { resource: "billing", feature: "invoice", action: "refund" });
    return json({ allowed: true });
  };

  const auth = new IdentityAuthorizationContext(client(transport), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    tenantId,
    credential: bearer,
  });

  assert.equal(await auth.isAllowed("billing", "invoice", "refund"), true);
});

test("local session authorization uses only IdentitySession provenance", async () => {
  const transport = async (_url, init) => {
    assert.equal(init.headers.Authorization, "IdentitySession opaque-session-token");
    assert.equal(init.headers["X-Identity-Access-Client"], "admin-web");
    assert.equal(init.headers["X-Identity-Access-Session"], sessionId);
    return json({ allowed: false });
  };

  const auth = new IdentityAuthorizationContext(client(transport), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    tenantId,
    resourceScopeId,
    credential: session,
  });

  assert.equal(await auth.isAllowed("billing", "invoice", "refund"), false);
});

test("resource scope cannot exist without a tenant", () => {
  assert.throws(() => new IdentityAuthorizationContext(client(async () => json({ allowed: true })), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    resourceScopeId,
    credential: bearer,
  }), code("configuration"));
});

test("RequireCapability metadata evaluates through IdentityAuthorizationContext", async () => {
  class ReplayHandler {
    async run() { return "executed"; }
  }

  const handler = new ReplayHandler();
  RequireCapability("replay", "execution", "run")(handler.run, {});

  const transport = async (_url, init) => {
    assert.deepEqual(JSON.parse(init.body), { resource: "replay", feature: "execution", action: "run" });
    return json({ allowed: true });
  };

  const auth = new IdentityAuthorizationContext(client(transport), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    credential: bearer,
  });

  assert.equal(await auth.isAllowedFor(handler, "run"), true);
});

test("admin UI builder is class-based and visibility uses the same authorization context", async () => {
  const seen = [];
  const transport = async (_url, init) => {
    const capability = JSON.parse(init.body);
    seen.push(capability.feature);
    return json({ allowed: capability.feature !== "policy" });
  };

  const auth = new IdentityAuthorizationContext(client(transport), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    credential: bearer,
  });

  const builder = new IdentityAccessAdminUiBuilder(auth)
    .withUsers()
    .withGroups()
    .withPolicies();

  assert.deepEqual(builder.build().entries.map((entry) => entry.section), ["users", "groups", "policies"]);
  assert.deepEqual((await builder.buildVisible()).entries.map((entry) => entry.section), ["users", "groups"]);
  assert.deepEqual(seen, ["user", "group", "policy"]);
});

test("native fetch interoperates with a local HTTP fixture, not a .NET server", async () => {
  const server = createServer((_request, response) => {
    response.writeHead(200, { "content-type": "application/json" });
    response.end(JSON.stringify(info));
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  try {
    const address = server.address();
    assert.ok(address && typeof address !== "string");
    const api = new IdentityAccessClient({ baseUrl: `http://127.0.0.1:${address.port}` });
    assert.deepEqual(await api.info(), info);
  } finally {
    await new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve()));
  }
});

const oidcCode = "A".repeat(43);
const refreshTokenOne = "B".repeat(43);
const refreshTokenTwo = "C".repeat(43);
const accessTokenOne = "header.payload.signature";
const idTokenOne = "idheader.idpayload.idsignature";
const loginSessionId = "42333333-3333-3333-3333-333333333333";
const loginUserId = "42444444-4444-4444-4444-444444444444";
const loginExpiresAt = "2026-09-23T12:00:00+00:00";
const loginRedirectUri = "https://app.example.test/callback";

const loginSession = {
  kind: "session",
  clientId: "admin-web",
  sessionId: loginSessionId,
  sessionToken: "local-session-token",
};

test("password login returns a class-client session credential without placing the password in the URL", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/api/v1/authentication/clients/admin-web/password-login");
    assert.equal(url.includes("correct horse"), false);
    assert.equal(init.method, "POST");
    assert.equal(init.headers["Content-Type"], "application/json");
    assert.deepEqual(JSON.parse(init.body), {
      loginIdentifier: "marco@example.test",
      password: "correct horse battery staple",
      redirectUri: loginRedirectUri,
    });
    return json({
      userId: loginUserId,
      sessionId: loginSessionId,
      sessionToken: "local-session-token",
      expiresAt: loginExpiresAt,
      redirectUri: loginRedirectUri,
    });
  });

  assert.deepEqual(await api.passwordLogin({
    clientId: "admin-web",
    loginIdentifier: "marco@example.test",
    password: "correct horse battery staple",
    redirectUri: loginRedirectUri,
  }), {
    kind: "session",
    clientId: "admin-web",
    userId: loginUserId,
    sessionId: loginSessionId,
    sessionToken: "local-session-token",
    expiresAt: loginExpiresAt,
    redirectUri: loginRedirectUri,
  });
});

test("session validation and logout use the registered client path and opaque session payload", async () => {
  let calls = 0;
  const api = client(async (url, init) => {
    calls++;
    if (url.endsWith("/sessions/validate")) {
      assert.deepEqual(JSON.parse(init.body), {
        sessionId: loginSessionId,
        sessionToken: "local-session-token",
      });
      return json({ userId: loginUserId, sessionId: loginSessionId, expiresAt: loginExpiresAt });
    }
    assert.ok(url.endsWith("/logout"));
    assert.deepEqual(JSON.parse(init.body), {
      sessionId: loginSessionId,
      sessionToken: "local-session-token",
      postLogoutRedirectUri: "https://app.example.test/signed-out",
    });
    return json({ postLogoutRedirectUri: "https://app.example.test/signed-out" });
  });

  assert.deepEqual(await api.validateSession(loginSession), {
    userId: loginUserId,
    sessionId: loginSessionId,
    expiresAt: loginExpiresAt,
  });
  assert.deepEqual(await api.logout(loginSession, "https://app.example.test/signed-out"), {
    postLogoutRedirectUri: "https://app.example.test/signed-out",
  });
  assert.equal(calls, 2);
});

test("OIDC authorization generates S256 PKCE and never follows the authorization redirect", async () => {
  const fixedState = "state-423-fixed-value";
  const fixedNonce = "nonce-423-fixed-value";
  let observedChallenge;

  const api = client(async (url, init) => {
    const request = new URL(url);
    assert.equal(request.pathname, "/connect/authorize");
    assert.equal(request.searchParams.get("client_id"), "admin-web");
    assert.equal(request.searchParams.get("redirect_uri"), loginRedirectUri);
    assert.equal(request.searchParams.get("response_type"), "code");
    assert.equal(request.searchParams.get("scope"), "openid");
    assert.equal(request.searchParams.get("state"), fixedState);
    assert.equal(request.searchParams.get("nonce"), fixedNonce);
    assert.equal(request.searchParams.get("code_challenge_method"), "S256");
    observedChallenge = request.searchParams.get("code_challenge");
    assert.match(observedChallenge, /^[A-Za-z0-9_-]{43}$/);
    assert.equal(init.redirect, "manual");
    assert.equal(init.headers.Authorization, "IdentitySession local-session-token");
    assert.equal(init.headers["X-Identity-Access-Session"], loginSessionId);
    assert.equal("X-Identity-Access-Client" in init.headers, false);
    return new Response(null, {
      status: 302,
      headers: { location: `${loginRedirectUri}?code=${oidcCode}&state=${fixedState}` },
    });
  });

  const authorization = await api.authorizeOidc(loginSession, {
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    state: fixedState,
    nonce: fixedNonce,
  });

  assert.equal(authorization.code, oidcCode);
  assert.equal(authorization.state, fixedState);
  assert.equal(authorization.nonce, fixedNonce);
  assert.match(authorization.codeVerifier, /^[A-Za-z0-9_-]{43}$/);

  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(authorization.codeVerifier));
  const expectedChallenge = Buffer.from(digest).toString("base64url");
  assert.equal(observedChallenge, expectedChallenge);
});

test("OIDC authorization protocol errors preserve the stable error code without following the redirect", async () => {
  const api = client(async () => new Response(null, {
    status: 302,
    headers: { location: `${loginRedirectUri}?error=login_required&state=state-423-fixed-value` },
  }));

  await assert.rejects(api.authorizeOidc(loginSession, {
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    state: "state-423-fixed-value",
    nonce: "nonce-423-fixed-value",
  }), (error) => {
    assert.equal(error.code, "oidc");
    assert.equal(error.protocolCode, "login_required");
    assert.equal(error.httpStatus, 302);
    return true;
  });
});

test("authorization-code exchange sends only the public-client PKCE form and requires an ID token", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/connect/token");
    assert.equal(init.method, "POST");
    assert.equal(init.headers["Content-Type"], "application/x-www-form-urlencoded");
    const form = new URLSearchParams(init.body);
    assert.deepEqual([...form.keys()].sort(), ["client_id", "code", "code_verifier", "grant_type", "redirect_uri"]);
    assert.equal(form.get("client_id"), "admin-web");
    assert.equal(form.get("grant_type"), "authorization_code");
    assert.equal(form.get("code"), oidcCode);
    assert.equal(form.get("redirect_uri"), loginRedirectUri);
    assert.equal(form.get("code_verifier"), "D".repeat(43));
    assert.equal(form.has("client_secret"), false);
    return json({
      access_token: accessTokenOne,
      token_type: "Bearer",
      expires_in: 600,
      id_token: idTokenOne,
      refresh_token: refreshTokenOne,
      scope: "openid",
    });
  });

  assert.deepEqual(await api.exchangeAuthorizationCode({
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    code: oidcCode,
    state: "state-423-fixed-value",
    nonce: "nonce-423-fixed-value",
    codeVerifier: "D".repeat(43),
  }), {
    accessToken: accessTokenOne,
    tokenType: "Bearer",
    expiresIn: 600,
    idToken: idTokenOne,
    refreshToken: refreshTokenOne,
    scope: "openid",
  });
});

test("refresh-token rotation sends no authorization-code fields and returns the replacement refresh token", async () => {
  const api = client(async (_url, init) => {
    const form = new URLSearchParams(init.body);
    assert.deepEqual([...form.keys()].sort(), ["client_id", "grant_type", "refresh_token"]);
    assert.equal(form.get("client_id"), "admin-web");
    assert.equal(form.get("grant_type"), "refresh_token");
    assert.equal(form.get("refresh_token"), refreshTokenOne);
    assert.equal(form.has("code"), false);
    assert.equal(form.has("code_verifier"), false);
    assert.equal(form.has("client_secret"), false);
    return json({
      access_token: "new.header.payload",
      token_type: "Bearer",
      expires_in: 600,
      refresh_token: refreshTokenTwo,
      scope: "openid",
    });
  });

  assert.deepEqual(await api.refreshOidcTokens("admin-web", refreshTokenOne), {
    accessToken: "new.header.payload",
    tokenType: "Bearer",
    expiresIn: 600,
    refreshToken: refreshTokenTwo,
    scope: "openid",
  });
});

test("OIDC token errors distinguish invalid grants from temporary unavailability", async () => {
  const invalid = client(async () => json({ error: "invalid_grant" }, 400));
  await assert.rejects(invalid.refreshOidcTokens("admin-web", refreshTokenOne), (error) => {
    assert.equal(error.code, "oidc");
    assert.equal(error.protocolCode, "invalid_grant");
    return true;
  });

  const unavailable = client(async () => json({ error: "temporarily_unavailable" }, 503));
  await assert.rejects(unavailable.refreshOidcTokens("admin-web", refreshTokenOne), (error) => {
    assert.equal(error.code, "unavailable");
    assert.equal(error.protocolCode, "temporarily_unavailable");
    return true;
  });
});

test("OIDC redirect validation rejects redirect target substitution", async () => {
  const api = client(async () => new Response(null, {
    status: 302,
    headers: { location: `https://attacker.example.test/callback?code=${oidcCode}&state=state-423-fixed-value` },
  }));

  await assert.rejects(api.authorizeOidc(loginSession, {
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    state: "state-423-fixed-value",
    nonce: "nonce-423-fixed-value",
  }), code("protocol"));
});
