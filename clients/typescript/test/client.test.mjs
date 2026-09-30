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

test("the root client is a composition facade with focused class responsibilities", () => {
  const api = client(async () => json(info));
  assert.equal(typeof api.system.info, "function");
  assert.equal(typeof api.authentication.passwordLogin, "function");
  assert.equal(typeof api.oidc.authorize, "function");
  assert.equal(typeof api.authorization.evaluate, "function");
  assert.equal(typeof api.administration.context.get, "function");
  assert.equal(typeof api.administration.users.list, "function");
  assert.equal(typeof api.administration.credentials.get, "function");
  assert.equal(typeof api.administration.credentials.create, "function");
  assert.equal(typeof api.administration.credentials.changePassword, "function");
  assert.equal(typeof api.administration.groups.list, "function");
  assert.equal(typeof api.administration.groups.listTemplates, "function");
  assert.equal(typeof api.administration.groups.createFromTemplate, "function");
  assert.equal(typeof api.administration.groups.updateReusable, "function");
  assert.equal(typeof api.administration.securityAudit.list, "function");
  assert.equal(typeof api.administration.managedPolicies.list, "function");
  assert.equal(typeof api.administration.managedPolicyBindings.list, "function");
  assert.equal(typeof api.administration.memberships.list, "function");
  assert.equal(typeof api.administration.organizations.list, "function");
  assert.equal(typeof api.administration.organizationMemberships.listForTenantMembership, "function");
  assert.equal(typeof api.administration.organizationResourceScopeLinks.get, "function");
  assert.equal(typeof api.administration.tenantUsers.list, "function");
  assert.equal(typeof api.administration.scopeAuthority.listGroups, "function");
  assert.equal(typeof api.administration.scopeAuthority.listPolicies, "function");
  assert.equal(typeof api.administration.scopeAuthority.getGroup, "function");
  assert.equal("info" in api, false);
  assert.equal("passwordLogin" in api, false);
  assert.equal("listUsers" in api, false);
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
  assert.deepEqual(await api.system.liveness(), { status: "alive" });
});

test("validates and returns the service descriptor", async () => {
  assert.deepEqual(await client(async () => json(info)).system.info(), info);
});

test("readiness accepts a documented 503 without converting it into a transport failure", async () => {
  assert.deepEqual(await client(async () => json(notReady, 503)).system.readiness(), notReady);
});

test("readiness permits 200 only with ready true", async () => {
  const ready = { ready: true, stage: "future", blockingCapabilities: [] };
  assert.deepEqual(await client(async () => json(ready)).system.readiness(), ready);
  await assert.rejects(client(async () => json(notReady)).system.readiness(), code("protocol"));
});

test("readiness rejects a 503 that claims ready true", async () => {
  await assert.rejects(client(async () => json({ ...notReady, ready: true }, 503)).system.readiness(), code("protocol"));
});

test("invalid blocking capability values are rejected", async () => {
  await assert.rejects(client(async () => json({ ...notReady, blockingCapabilities: [42] }, 503)).system.readiness(), code("protocol"));
});

test("security HTTP statuses remain distinct", async () => {
  await assert.rejects(client(async () => json({}, 401)).system.info(), code("unauthenticated"));
  await assert.rejects(client(async () => json({}, 403)).system.info(), code("forbidden"));
  await assert.rejects(client(async () => json({}, 503)).system.info(), code("unavailable"));
  await assert.rejects(client(async () => json({}, 500)).system.info(), code("http"));
});

test("HTTP errors never retain response bodies", async () => {
  await assert.rejects(client(async () => json({ secret: "must-not-escape" }, 403)).system.info(), (error) => {
    assert.equal(error.code, "forbidden");
    assert.equal(error.httpStatus, 403);
    assert.ok(!JSON.stringify(error).includes("must-not-escape"));
    assert.ok(!String(error).includes("must-not-escape"));
    return true;
  });
});

test("malformed JSON is a protocol error", async () => {
  await assert.rejects(client(async () => new Response("<html>secret</html>")).system.info(), code("protocol"));
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
    await assert.rejects(client(async () => json(missing)).system.info(), code("protocol"));
  }
});

test("incompatible API versions are rejected", async () => {
  await assert.rejects(client(async () => json({ ...info, apiVersion: "v99" })).system.info(), code("protocol"));
});

test("transport failures do not disclose underlying error details", async () => {
  await assert.rejects(client(async () => { throw new Error("secret-token-in-url"); }).system.info(), (error) => {
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
  await assert.rejects(client(abortingTransport, { timeoutMs: 10 }).system.info(), code("timeout"));
});

test("already-cancelled request never calls the transport", async () => {
  const signal = AbortSignal.abort();
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json(info); }).system.info(signal), code("cancelled"));
  assert.equal(calls, 0);
});

test("caller cancellation remains separate from timeout", async () => {
  const controller = new AbortController();
  const pending = client(abortingTransport).system.info(controller.signal);
  controller.abort();
  await assert.rejects(pending, code("cancelled"));
});

test("requests are not automatically retried", async () => {
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json({}, 500); }).system.info(), code("http"));
  assert.equal(calls, 1);
});

test("concurrent clients preserve independent destinations", async () => {
  const urls = [];
  const transport = async (url) => { urls.push(url); return json(info); };
  await Promise.all([
    client(transport, { baseUrl: "https://one.example.test" }).system.info(),
    client(transport, { baseUrl: "https://two.example.test" }).system.info(),
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

test("admin UI builder is class-based, route-aware, and uses scope-correct authorization contexts", async () => {
  const seen = [];
  const transport = async (url, init) => {
    const capability = JSON.parse(init.body);
    seen.push({ feature: capability.feature, tenant: url.includes(`/tenants/${tenantId}/`) });
    return json({ allowed: capability.feature !== "policy" });
  };

  const api = client(transport);
  const scopeAuth = new IdentityAuthorizationContext(api, {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    credential: bearer,
  });
  const tenantAuth = new IdentityAuthorizationContext(api, {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    tenantId,
    credential: bearer,
  });

  const builder = new IdentityAccessAdminUiBuilder(scopeAuth, { basePath: "/identity-admin/" })
    .withTenantAuthorization(tenantAuth)
    .withUsers()
    .withGroups()
    .withPolicies();

  const built = builder.build();
  assert.equal(built.basePath, "/identity-admin");
  assert.deepEqual(built.entries.map((entry) => entry.href), [
    "/identity-admin/users",
    "/identity-admin/groups",
    "/identity-admin/policies",
  ]);
  assert.deepEqual((await builder.buildVisible()).entries.map((entry) => entry.section), ["users", "groups"]);
  assert.deepEqual(seen, [
    { feature: "user", tenant: true },
    { feature: "group", tenant: true },
    { feature: "policy", tenant: false },
  ]);
});



test("managed policy navigation uses identity-scope authorization even when tenant contexts are present", async () => {
  const seen = [];
  const transport = async (url, init) => {
    const capability = JSON.parse(init.body);
    seen.push({ feature: capability.feature, tenant: url.includes(`/tenants/${tenantId}/`) });
    return json({ allowed: capability.feature === "policy" && !url.includes(`/tenants/${tenantId}/`) });
  };

  const api = client(transport);
  const scopeAuth = new IdentityAuthorizationContext(api, {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    credential: bearer,
  });
  const tenantAuth = new IdentityAuthorizationContext(api, {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    tenantId,
    credential: bearer,
  });

  const visible = await new IdentityAccessAdminUiBuilder(scopeAuth)
    .withTenantAuthorization(tenantAuth)
    .withPolicies()
    .buildVisible();

  assert.deepEqual(visible.entries.map((entry) => entry.section), ["policies"]);
  assert.deepEqual(seen, [{ feature: "policy", tenant: false }]);
});

test("admin UI builder exposes a tenant-aware section when any trusted tenant context allows it", async () => {
  const tenantOne = "42333333-3333-3333-3333-333333333333";
  const tenantTwo = "42444444-4444-4444-4444-444444444444";
  const seen = [];
  const transport = async (url, init) => {
    const capability = JSON.parse(init.body);
    const tenant = url.includes(`/tenants/${tenantOne}/`) ? tenantOne : tenantTwo;
    seen.push({ feature: capability.feature, tenant });
    return json({ allowed: tenant === tenantTwo });
  };
  const api = client(transport);
  const scopeAuth = new IdentityAuthorizationContext(api, { identityScopeId: scopeId, applicationKey: "app-a", credential: bearer });
  const first = new IdentityAuthorizationContext(api, { identityScopeId: scopeId, applicationKey: "app-a", tenantId: tenantOne, credential: bearer });
  const second = new IdentityAuthorizationContext(api, { identityScopeId: scopeId, applicationKey: "app-a", tenantId: tenantTwo, credential: bearer });

  const visible = await new IdentityAccessAdminUiBuilder(scopeAuth)
    .withTenantAuthorizations([first, second])
    .withGroups()
    .buildVisible();

  assert.deepEqual(visible.entries.map((entry) => entry.section), ["groups"]);
  assert.deepEqual(seen, [
    { feature: "group", tenant: tenantOne },
    { feature: "group", tenant: tenantTwo },
  ]);
});

test("admin UI builder falls back to scope authorization for tenant-aware sections before a tenant is selected", async () => {
  const seen = [];
  const transport = async (url, init) => {
    const capability = JSON.parse(init.body);
    seen.push({ feature: capability.feature, tenant: url.includes(`/tenants/${tenantId}/`) });
    return json({ allowed: true });
  };

  const builder = new IdentityAccessAdminUiBuilder(new IdentityAuthorizationContext(client(transport), {
    identityScopeId: scopeId,
    applicationKey: "app-a",
    credential: bearer,
  }), { basePath: "/identity" }).withUsers().withGroups();

  assert.deepEqual((await builder.buildVisible()).entries.map((entry) => entry.section), ["users", "groups"]);
  assert.deepEqual(seen, [
    { feature: "user", tenant: false },
    { feature: "group", tenant: false },
  ]);
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
    assert.deepEqual(await api.system.info(), info);
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
const loginVerifiedAt = "2026-09-23T10:00:00+00:00";
const loginRedirectUri = "https://app.example.test/callback";
const passwordAssurance = {
  level: "password",
  methods: ["pwd"],
  verifiedAt: loginVerifiedAt,
  acr: "urn:generic-identity-access:acr:password",
};
const mfaAssurance = {
  level: "mfa",
  methods: ["mfa", "otp", "pwd"],
  verifiedAt: "2026-09-23T10:05:00+00:00",
  acr: "urn:generic-identity-access:acr:mfa",
};

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
      assurance: passwordAssurance,
    });
  });

  assert.deepEqual(await api.authentication.passwordLogin({
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
    assurance: passwordAssurance,
  });
});

test("self-service password change reuses the exact local-session provenance and keeps passwords out of the URL", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/api/v1/authentication/clients/admin-web/credentials/password/change");
    assert.equal(url.includes("current-password"), false);
    assert.equal(url.includes("replacement-password"), false);
    assert.equal(init.method, "POST");
    assert.equal(init.headers.Authorization, "IdentitySession local-session-token");
    assert.equal(init.headers["X-Identity-Access-Session"], loginSessionId);
    assert.equal(init.headers["Content-Type"], "application/json");
    assert.deepEqual(JSON.parse(init.body), {
      currentPassword: "current-password-value",
      newPassword: "replacement-password-value",
    });
    return new Response(null, { status: 204 });
  });

  await api.authentication.changePassword({
    credential: loginSession,
    currentPassword: "current-password-value",
    newPassword: "replacement-password-value",
  });
});

test("recovery-code password reset sends only the requested recovery proof in the body", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/api/v1/authentication/clients/admin-web/recovery/password");
    assert.equal(url.includes("marco@example.test"), false);
    assert.equal(url.includes("ABCD"), false);
    assert.equal(init.method, "POST");
    assert.equal(init.headers["Content-Type"], "application/json");
    assert.equal("Authorization" in init.headers, false);
    assert.deepEqual(JSON.parse(init.body), {
      loginIdentifier: "marco@example.test",
      recoveryCode: "ABCD-EFGH-JKLM-NPQR",
      newPassword: "replacement-password-value",
    });
    return new Response(null, { status: 204 });
  });

  await api.authentication.recoverPasswordWithCode({
    clientId: "admin-web",
    loginIdentifier: "marco@example.test",
    recoveryCode: "ABCD-EFGH-JKLM-NPQR",
    newPassword: "replacement-password-value",
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
      return json({ userId: loginUserId, sessionId: loginSessionId, expiresAt: loginExpiresAt, assurance: passwordAssurance });
    }
    assert.ok(url.endsWith("/logout"));
    assert.deepEqual(JSON.parse(init.body), {
      sessionId: loginSessionId,
      sessionToken: "local-session-token",
      postLogoutRedirectUri: "https://app.example.test/signed-out",
    });
    return json({ postLogoutRedirectUri: "https://app.example.test/signed-out" });
  });

  assert.deepEqual(await api.authentication.validateSession(loginSession), {
    userId: loginUserId,
    sessionId: loginSessionId,
    expiresAt: loginExpiresAt,
    assurance: passwordAssurance,
  });
  assert.deepEqual(await api.authentication.logout(loginSession, "https://app.example.test/signed-out"), {
    postLogoutRedirectUri: "https://app.example.test/signed-out",
  });
  assert.equal(calls, 2);
});

test("TOTP and recovery step-up bind the proof to the exact local session", async () => {
  let calls = 0;
  const authenticatorId = "42555555-5555-5555-5555-555555555555";
  const api = client(async (url, init) => {
    calls++;
    assert.equal(init.headers.Authorization, "IdentitySession local-session-token");
    assert.equal(init.headers["X-Identity-Access-Session"], loginSessionId);
    assert.equal(init.headers["Content-Type"], "application/json");

    if (url.includes("/totp/")) {
      assert.deepEqual(JSON.parse(init.body), { code: "123456" });
    } else {
      assert.ok(url.includes("/recovery/"));
      assert.deepEqual(JSON.parse(init.body), { code: "ABCD-EFGH-JKLM-NPQR" });
    }

    return json(mfaAssurance);
  });

  assert.deepEqual(
    await api.authentication.verifyTotp(loginSession, authenticatorId, "123456"),
    mfaAssurance,
  );
  assert.deepEqual(
    await api.authentication.verifyRecoveryCode(loginSession, authenticatorId, "ABCD-EFGH-JKLM-NPQR"),
    mfaAssurance,
  );
  assert.equal(calls, 2);
});

test("WebAuthn step-up exposes request options and returns upgraded assurance", async () => {
  const challengeId = "42666666-6666-6666-6666-666666666666";
  let calls = 0;
  const api = client(async (url, init) => {
    calls++;
    assert.equal(init.headers.Authorization, "IdentitySession local-session-token");
    assert.equal(init.headers["X-Identity-Access-Session"], loginSessionId);

    if (url.endsWith("/webauthn/options")) {
      assert.equal(init.method, "POST");
      assert.equal(init.body, undefined);
      return json({
        challengeId,
        challenge: "A".repeat(43),
        relyingPartyId: "identity.example.test",
        timeoutMilliseconds: 60000,
        allowCredentialIds: ["B".repeat(43)],
        userVerification: "required",
      });
    }

    assert.ok(url.endsWith("/webauthn/complete"));
    assert.deepEqual(JSON.parse(init.body), {
      challengeId,
      credentialId: "B".repeat(43),
      clientDataJson: "C".repeat(43),
      authenticatorData: "D".repeat(43),
      signature: "E".repeat(43),
    });
    return json({ ...mfaAssurance, methods: ["mfa", "pop", "pwd"] });
  });

  const options = await api.authentication.beginWebAuthnStepUp(loginSession);
  assert.equal(options.challengeId, challengeId);
  assert.equal(options.userVerification, "required");

  const assurance = await api.authentication.completeWebAuthnStepUp(loginSession, {
    challengeId,
    credentialId: "B".repeat(43),
    clientDataJson: "C".repeat(43),
    authenticatorData: "D".repeat(43),
    signature: "E".repeat(43),
  });
  assert.deepEqual(assurance.methods, ["mfa", "pop", "pwd"]);
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

  const authorization = await api.oidc.authorize(loginSession, {
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

  await assert.rejects(api.oidc.authorize(loginSession, {
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

test("OIDC interaction_required remains a typed protocol error for MFA step-up", async () => {
  const api = client(async () => new Response(null, {
    status: 302,
    headers: { location: `${loginRedirectUri}?error=interaction_required&state=state-423-fixed-value` },
  }));

  await assert.rejects(api.oidc.authorize(loginSession, {
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    state: "state-423-fixed-value",
    nonce: "nonce-423-fixed-value",
  }), (error) => {
    assert.ok(error instanceof IdentityAccessClientError);
    assert.equal(error.code, "oidc");
    assert.equal(error.protocolCode, "interaction_required");
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

  assert.deepEqual(await api.oidc.exchangeAuthorizationCode({
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

  assert.deepEqual(await api.oidc.refreshTokens("admin-web", refreshTokenOne), {
    accessToken: "new.header.payload",
    tokenType: "Bearer",
    expiresIn: 600,
    refreshToken: refreshTokenTwo,
    scope: "openid",
  });
});

test("OIDC token errors distinguish invalid grants from temporary unavailability", async () => {
  const invalid = client(async () => json({ error: "invalid_grant" }, 400));
  await assert.rejects(invalid.oidc.refreshTokens("admin-web", refreshTokenOne), (error) => {
    assert.equal(error.code, "oidc");
    assert.equal(error.protocolCode, "invalid_grant");
    return true;
  });

  const unavailable = client(async () => json({ error: "temporarily_unavailable" }, 503));
  await assert.rejects(unavailable.oidc.refreshTokens("admin-web", refreshTokenOne), (error) => {
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

  await assert.rejects(api.oidc.authorize(loginSession, {
    clientId: "admin-web",
    redirectUri: loginRedirectUri,
    state: "state-423-fixed-value",
    nonce: "nonce-423-fixed-value",
  }), code("protocol"));
});

const adminScopeId = "42411111-1111-1111-1111-111111111111";
const adminTenantId = "42422222-2222-2222-2222-222222222222";
const adminUserId = "42433333-3333-3333-3333-333333333333";
const adminMembershipId = "42444444-4444-4444-4444-444444444444";
const adminGroupId = "42455555-5555-5555-5555-555555555555";
const adminGroupTemplateId = "42455555-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const adminPolicyId = "42466666-6666-6666-6666-666666666666";
const adminStatementId = "42477777-7777-7777-7777-777777777777";
const adminManagedPolicyId = "42499999-1111-1111-1111-111111111111";
const adminManagedStatementId = "42499999-2222-2222-2222-222222222222";
const adminResourceScopeId = "42488888-8888-8888-8888-888888888888";
const adminBearerContext = {
  identityScopeId: adminScopeId,
  applicationKey: "admin-app",
  credential: bearer,
};
const adminTenantContext = { ...adminBearerContext, tenantId: adminTenantId };


test("effective administration context is read from the trusted API boundary", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/administration-context`);
    assert.equal(init.method, "GET");
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    return json({
      identityScopeId: adminScopeId,
      userId: adminUserId,
      applicationKey: "admin-app",
      tenantVisibility: "membership-limited",
      activeTenantMemberships: [{ membershipId: adminMembershipId, tenantId: adminTenantId }],
    });
  });

  assert.deepEqual(await api.administration.context.get(adminBearerContext), {
    identityScopeId: adminScopeId,
    userId: adminUserId,
    applicationKey: "admin-app",
    tenantVisibility: "membership-limited",
    activeTenantMemberships: [{ membershipId: adminMembershipId, tenantId: adminTenantId }],
  });
});

test("typed user administration sends trusted Bearer provenance and optimistic concurrency", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    if (call === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/users`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        userId: "00000000-0000-0000-0000-000000000000",
        displayName: "Alice",
        status: 1,
      });
      return json({ userId: adminUserId, displayName: "Alice", status: 1, version: 1 }, 201);
    }

    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/users/${adminUserId}`);
    assert.equal(init.method, "PUT");
    assert.deepEqual(JSON.parse(init.body), { displayName: "Alice Updated", status: 2, expectedVersion: 1 });
    return json({ userId: adminUserId, displayName: "Alice Updated", status: 2, version: 2 });
  });

  assert.deepEqual(await api.administration.users.create(adminBearerContext, { displayName: "Alice" }), {
    userId: adminUserId, displayName: "Alice", status: 1, version: 1,
  });
  assert.deepEqual(await api.administration.users.update(adminBearerContext, adminUserId, {
    displayName: "Alice Updated", status: 2, expectedVersion: 1,
  }), {
    userId: adminUserId, displayName: "Alice Updated", status: 2, version: 2,
  });
});


test("typed password credential administration exposes metadata only and preserves secret request bodies", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    const path = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/users/${adminUserId}/password-credential`;
    assert.equal(url, path);
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");

    if (call === 1) {
      assert.equal(init.method, "GET");
      return json({
        userId: adminUserId,
        loginIdentifier: "alice@example.test",
        failedAccessCount: 0,
        lockoutUntil: null,
        version: 1,
      });
    }

    if (call === 2) {
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        loginIdentifier: "alice@example.test",
        password: "correct-horse-battery-staple",
      });
      return json({
        userId: adminUserId,
        loginIdentifier: "alice@example.test",
        failedAccessCount: 0,
        lockoutUntil: null,
        version: 1,
      }, 201);
    }

    assert.equal(init.method, "PUT");
    assert.deepEqual(JSON.parse(init.body), {
      loginIdentifier: "alice@example.test",
      password: "different-correct-horse-battery-staple",
      expectedVersion: 1,
    });
    return json({
      userId: adminUserId,
      loginIdentifier: "alice@example.test",
      failedAccessCount: 0,
      lockoutUntil: null,
      version: 2,
    });
  });

  assert.deepEqual(await api.administration.credentials.get(adminBearerContext, adminUserId), {
    userId: adminUserId,
    loginIdentifier: "alice@example.test",
    failedAccessCount: 0,
    version: 1,
  });
  assert.equal((await api.administration.credentials.create(adminBearerContext, adminUserId, {
    loginIdentifier: "alice@example.test",
    password: "correct-horse-battery-staple",
  })).version, 1);
  assert.equal((await api.administration.credentials.changePassword(adminBearerContext, adminUserId, {
    loginIdentifier: "alice@example.test",
    password: "different-correct-horse-battery-staple",
    expectedVersion: 1,
  })).version, 2);
});

test("typed nullable administration GET returns null on 404 without parsing an empty body", async () => {
  const api = client(async (_url, init) => {
    assert.equal(init.method, "GET");
    return new Response(null, { status: 404 });
  });
  assert.equal(await api.administration.users.get(adminBearerContext, adminUserId), null);
  assert.equal(await api.administration.tenants.get(adminBearerContext, adminTenantId), null);
});


test("tenant membership administration preserves the API route shape and IdentitySession headers", async () => {
  const context = {
    identityScopeId: adminScopeId,
    applicationKey: "admin-app",
    tenantId: adminTenantId,
    credential: session,
  };
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/tenants/${adminTenantId}/memberships/by-user/${adminUserId}`);
    assert.equal(init.headers.Authorization, "IdentitySession opaque-session-token");
    assert.equal(init.headers["X-Identity-Access-Client"], "admin-web");
    assert.equal(init.headers["X-Identity-Access-Session"], sessionId);
    return json({ membershipId: adminMembershipId, tenantId: adminTenantId, userId: adminUserId, status: 1, version: 3 });
  });

  assert.deepEqual(await api.administration.memberships.findByUser(context, adminUserId), {
    membershipId: adminMembershipId, tenantId: adminTenantId, userId: adminUserId, status: 1, version: 3,
  });
});


test("group membership administration supports list, add, and idempotent not-found remove result", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    const base = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/groups/${adminGroupId}/members`;
    if (call === 1) {
      assert.equal(url, base);
      assert.equal(init.method, "GET");
      return json([{ tenantMembershipId: adminMembershipId, userId: adminUserId }]);
    }
    if (call === 2) {
      assert.equal(url, base);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), { tenantMembershipId: adminMembershipId });
      return json({ tenantMembershipId: adminMembershipId, userId: adminUserId }, 201);
    }
    assert.equal(url, `${base}/${adminMembershipId}`);
    assert.equal(init.method, "DELETE");
    return new Response(null, { status: 404 });
  });

  assert.deepEqual(await api.administration.groups.listMembers(adminTenantContext, adminGroupId), [
    { tenantMembershipId: adminMembershipId, userId: adminUserId },
  ]);
  assert.deepEqual(await api.administration.groups.addMember(adminTenantContext, adminGroupId, adminMembershipId), {
    tenantMembershipId: adminMembershipId, userId: adminUserId,
  });
  assert.equal(await api.administration.groups.removeMember(adminTenantContext, adminGroupId, adminMembershipId), false);
});




test("reusable groups expose scope requirements and remap scoped bindings during clone", async () => {
  let call = 0;
  const targetScopeId = "42499999-9999-9999-9999-999999999999";
  const api = client(async (url, init) => {
    call++;
    const tenantBase = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/groups`;
    const globalBase = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/reusable-groups`;
    if (call === 1) {
      assert.equal(url, `${tenantBase}/templates?limit=20`);
      assert.equal(init.method, "GET");
      return json([{ tenantId: adminTenantId, groupId: adminGroupTemplateId, displayName: "Read Only", status: 1, isTemplate: true, version: 2 }]);
    }
    if (call === 2) {
      assert.equal(url, `${tenantBase}/templates/${adminTenantId}/${adminGroupTemplateId}/scope-requirements`);
      assert.equal(init.method, "GET");
      return json([{
        sourceResourceScopeId: adminResourceScopeId,
        modelVersion: 2,
        scopeType: "business",
        displayName: "Source Business",
      }]);
    }
    if (call === 3) {
      assert.equal(url, `${tenantBase}/from-template`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        sourceTenantId: adminTenantId,
        sourceGroupId: adminGroupTemplateId,
        groupId: "00000000-0000-0000-0000-000000000000",
        resourceScopeMappings: [{
          sourceResourceScopeId: adminResourceScopeId,
          targetResourceScopeId: targetScopeId,
        }],
      });
      return json({ tenantId: adminTenantId, groupId: adminGroupId, displayName: "Read Only", status: 1, isTemplate: false, version: 1 }, 201);
    }
    assert.equal(url, `${globalBase}/${adminTenantId}/${adminGroupTemplateId}`);
    assert.equal(init.method, "PUT");
    assert.deepEqual(JSON.parse(init.body), { displayName: "Read Only Users", status: 1, isTemplate: false, expectedVersion: 2 });
    return json({ tenantId: adminTenantId, groupId: adminGroupTemplateId, displayName: "Read Only Users", status: 1, isTemplate: false, version: 3 });
  });

  const templates = await api.administration.groups.listTemplates(adminTenantContext, { limit: 20 });
  assert.equal(templates[0].groupId, adminGroupTemplateId);
  assert.equal(templates[0].isTemplate, true);

  const requirements = await api.administration.groups.listTemplateScopeRequirements(
    adminTenantContext,
    adminTenantId,
    adminGroupTemplateId,
  );
  assert.deepEqual(requirements, [{
    sourceResourceScopeId: adminResourceScopeId,
    modelVersion: 2,
    scopeType: "business",
    displayName: "Source Business",
  }]);

  const instance = await api.administration.groups.createFromTemplate(adminTenantContext, {
    sourceTenantId: adminTenantId,
    sourceGroupId: adminGroupTemplateId,
    resourceScopeMappings: [{
      sourceResourceScopeId: adminResourceScopeId,
      targetResourceScopeId: targetScopeId,
    }],
  });
  assert.equal(instance.isTemplate, false);

  assert.equal((await api.administration.groups.updateReusable(adminBearerContext, adminTenantId, adminGroupTemplateId, {
    displayName: "Read Only Users", status: 1, isTemplate: false, expectedVersion: 2,
  })).version, 3);
});

test("managed policy binding administration keeps shared policy identity separate from tenant binding scope", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    const base = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/managed-policy-bindings`;
    if (call === 1) {
      assert.equal(url, `${base}/available-policies?limit=20&search=user`);
      assert.equal(init.method, "GET");
      return json([{
        policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration",
        status: 1, defaultVersion: 3, version: 4,
      }]);
    }
    if (call === 2) {
      assert.equal(url, `${base}/groups/${adminGroupId}`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        policyId: adminManagedPolicyId,
        policyVersion: null,
        resourceScopeId: adminResourceScopeId,
        includeDescendants: true,
      });
      return json({
        groupId: adminGroupId, policyId: adminManagedPolicyId, policyVersion: 3,
        resourceScopeId: adminResourceScopeId, includeDescendants: true,
      }, 201);
    }
    if (call === 3) {
      assert.equal(url, `${base}/groups/${adminGroupId}`);
      assert.equal(init.method, "GET");
      return json([{
        groupId: adminGroupId, policyId: adminManagedPolicyId, policyVersion: 3,
        resourceScopeId: adminResourceScopeId, includeDescendants: true,
      }]);
    }
    assert.equal(
      url,
      `${base}/groups/${adminGroupId}/${adminManagedPolicyId}/versions/3?resourceScopeId=${adminResourceScopeId}`,
    );
    assert.equal(init.method, "DELETE");
    return new Response(null, { status: 204 });
  });

  assert.equal((await api.administration.managedPolicyBindings.listAvailablePolicies(
    adminTenantContext, { search: "user", limit: 20 },
  ))[0].defaultVersion, 3);
  assert.equal((await api.administration.managedPolicyBindings.add(adminTenantContext, adminGroupId, {
    policyId: adminManagedPolicyId,
    resourceScopeId: adminResourceScopeId,
    includeDescendants: true,
  })).policyVersion, 3);
  assert.equal((await api.administration.managedPolicyBindings.list(adminTenantContext, adminGroupId))[0].policyId, adminManagedPolicyId);
  assert.equal(await api.administration.managedPolicyBindings.remove(
    adminTenantContext, adminGroupId, adminManagedPolicyId, 3, adminResourceScopeId,
  ), true);
});

test("resource-scope administration decodes hierarchy metadata and sends nullable parent explicitly", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    const base = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/resource-scopes`;
    assert.equal(url, base);
    if (call === 1) {
      assert.equal(init.method, "GET");
      return json([{
        resourceScopeId: adminResourceScopeId,
        modelVersion: 4,
        scopeType: "business",
        externalResourceId: "business-42",
        displayName: "Business 42",
        parentResourceScopeId: null,
        status: 1,
        version: 5,
      }]);
    }
    assert.equal(init.method, "POST");
    assert.deepEqual(JSON.parse(init.body), {
      resourceScopeId: "00000000-0000-0000-0000-000000000000",
      modelVersion: 4,
      scopeType: "business",
      externalResourceId: "business-43",
      displayName: "Business 43",
      parentResourceScopeId: null,
      status: 1,
    });
    return json({
      resourceScopeId: adminResourceScopeId,
      modelVersion: 4,
      scopeType: "business",
      externalResourceId: "business-43",
      displayName: "Business 43",
      parentResourceScopeId: null,
      status: 1,
      version: 1,
    }, 201);
  });

  assert.deepEqual(await api.administration.resourceScopes.list(adminTenantContext), [{
    resourceScopeId: adminResourceScopeId,
    modelVersion: 4,
    scopeType: "business",
    externalResourceId: "business-42",
    displayName: "Business 42",
    status: 1,
    version: 5,
  }]);
  assert.equal((await api.administration.resourceScopes.create(adminTenantContext, {
    modelVersion: 4,
    scopeType: "business",
    externalResourceId: "business-43",
    displayName: "Business 43",
  })).version, 1);
});


test("managed policy administration is application scoped and supports draft publication lifecycle", async () => {
  let call = 0;
  const base = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/managed-policies`;
  const api = client(async (url, init) => {
    call++;
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    if (call === 1) {
      assert.equal(url, `${base}?limit=20&search=user`);
      assert.equal(init.method, "GET");
      return json([{
        policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration",
        status: 1, defaultVersion: null, version: 1,
      }]);
    }
    if (call === 2) {
      assert.equal(url, base);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration", status: 1,
      });
      return json({
        policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration",
        status: 1, defaultVersion: null, version: 1,
      }, 201);
    }
    if (call === 3) {
      assert.equal(url, `${base}/${adminManagedPolicyId}/versions`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), { policyVersion: 1, modelVersion: 7 });
      return json({ policyId: adminManagedPolicyId, policyVersion: 1, modelVersion: 7, publishedAt: null }, 201);
    }
    if (call === 4) {
      assert.equal(url, `${base}/${adminManagedPolicyId}/versions/1/statements`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), {
        statementId: adminManagedStatementId, resource: "identity-access", feature: "user", action: "read",
      });
      return json({
        statementId: adminManagedStatementId, policyVersion: 1, modelVersion: 7,
        resource: "identity-access", feature: "user", action: "read",
      }, 201);
    }
    if (call === 5) {
      assert.equal(url, `${base}/${adminManagedPolicyId}/versions/1/publish`);
      assert.equal(init.method, "PUT");
      assert.deepEqual(JSON.parse(init.body), { makeDefault: true });
      return json({
        policyId: adminManagedPolicyId, policyVersion: 1, modelVersion: 7, publishedAt: "2026-09-26T09:00:00Z",
      });
    }
    throw new Error(`unexpected call ${call}`);
  });

  assert.deepEqual(await api.administration.managedPolicies.list(adminBearerContext, { search: "user", limit: 20 }), [{
    policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration",
    status: 1, version: 1,
  }]);
  assert.equal((await api.administration.managedPolicies.create(adminBearerContext, {
    policyId: adminManagedPolicyId, policyKey: "user-administration", displayName: "User Administration",
  })).policyKey, "user-administration");
  assert.equal((await api.administration.managedPolicies.createVersion(adminBearerContext, adminManagedPolicyId, {
    policyVersion: 1, modelVersion: 7,
  })).publishedAt, undefined);
  assert.equal((await api.administration.managedPolicies.addStatement(adminBearerContext, adminManagedPolicyId, 1, {
    statementId: adminManagedStatementId, resource: "identity-access", feature: "user", action: "read",
  })).policyVersion, 1);
  assert.equal((await api.administration.managedPolicies.publishVersion(
    adminBearerContext, adminManagedPolicyId, 1, { makeDefault: true },
  )).publishedAt, "2026-09-26T09:00:00Z");
});

test("application security-model administration lists registered manifest versions", async () => {
  const sha = "a".repeat(64);
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/security-models`);
    assert.equal(init.method, "GET");
    return json([{
      schemaVersion: 1,
      applicationKey: "admin-app",
      modelVersion: 7,
      rbacProject: "sample-project",
      rbacNamespaces: ["crm"],
      manifestSha256: sha,
      capabilityCount: 2,
    }]);
  });

  assert.deepEqual(await api.administration.securityModels.list(adminBearerContext), [{
    schemaVersion: 1,
    applicationKey: "admin-app",
    modelVersion: 7,
    rbacProject: "sample-project",
    rbacNamespaces: ["crm"],
    manifestSha256: sha,
    capabilityCount: 2,
  }]);
});


test("application security-model administration gets one concrete capability catalog", async () => {
  const sha = "b".repeat(64);
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/security-models/7`);
    assert.equal(init.method, "GET");
    return json({
      schemaVersion: 1,
      applicationKey: "admin-app",
      modelVersion: 7,
      rbacProject: "sample-project",
      rbacNamespaces: ["crm", "operations"],
      manifestSha256: sha,
      capabilities: [
        { resource: "billing", feature: "invoice", action: "read", displayName: "Read invoices" },
        { resource: "billing", feature: "invoice", action: "refund", displayName: "Refund invoices" },
      ],
    });
  });

  assert.deepEqual(await api.administration.securityModels.get(adminBearerContext, 7), {
    schemaVersion: 1,
    applicationKey: "admin-app",
    modelVersion: 7,
    rbacProject: "sample-project",
    rbacNamespaces: ["crm", "operations"],
    manifestSha256: sha,
    capabilityCount: 2,
    capabilities: [
      { resource: "billing", feature: "invoice", action: "read", displayName: "Read invoices" },
      { resource: "billing", feature: "invoice", action: "refund", displayName: "Refund invoices" },
    ],
  });
});


test("application security-model registration preserves RBAC context separately from capability coordinates", async () => {
  const sha = "c".repeat(64);
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/security-models/7`);
    assert.equal(init.method, "PUT");
    assert.deepEqual(JSON.parse(init.body), {
      schemaVersion: 1,
      applicationKey: "admin-app",
      modelVersion: 7,
      rbac: {
        project: "sample-project",
        namespaces: ["crm"],
      },
      resources: [{
        name: "billing",
        features: [{
          name: "invoice",
          actions: [
            { name: "read", displayName: "Read invoices" },
            { name: "refund", displayName: "Refund invoices" },
          ],
        }],
      }],
    });
    return json({
      schemaVersion: 1,
      applicationKey: "admin-app",
      modelVersion: 7,
      rbacProject: "sample-project",
      rbacNamespaces: ["crm"],
      manifestSha256: sha,
      capabilities: [
        { resource: "billing", feature: "invoice", action: "read", displayName: "Read invoices" },
        { resource: "billing", feature: "invoice", action: "refund", displayName: "Refund invoices" },
      ],
    });
  });

  const registered = await api.administration.securityModels.registerManifest(adminBearerContext, {
    schemaVersion: 1,
    applicationKey: "admin-app",
    modelVersion: 7,
    rbac: { project: "sample-project", namespaces: ["crm"] },
    resources: [{
      name: "billing",
      features: [{
        name: "invoice",
        actions: [
          { name: "read", displayName: "Read invoices" },
          { name: "refund", displayName: "Refund invoices" },
        ],
      }],
    }],
  });

  assert.equal(registered.rbacProject, "sample-project");
  assert.deepEqual(registered.rbacNamespaces, ["crm"]);
  assert.deepEqual(registered.capabilities.map((capability) => [capability.resource, capability.feature, capability.action]), [
    ["billing", "invoice", "read"],
    ["billing", "invoice", "refund"],
  ]);
});


test("scope-type administration is typed against the existing security-model version route", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/security-models/7/scope-types`);
    assert.equal(init.method, "POST");
    assert.deepEqual(JSON.parse(init.body), {
      key: "business",
      displayName: "Business",
      parentKey: null,
      canAttachToTenant: true,
    });
    return json({ key: "business", displayName: "Business", parentKey: null, canAttachToTenant: true }, 201);
  });
  assert.deepEqual(await api.administration.securityModels.addScopeType(adminBearerContext, 7, {
    key: "business", displayName: "Business", canAttachToTenant: true,
  }), { key: "business", displayName: "Business", canAttachToTenant: true });
});


test("session administration exposes bulk revocation as a typed result", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    assert.equal(init.method, "DELETE");
    if (call === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/sessions/users/${adminUserId}`);
      return json({ revokedCount: 3 });
    }
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/sessions/clients/admin-web`);
    return json({ revokedCount: 2 });
  });
  assert.deepEqual(await api.administration.sessions.revokeUser(adminBearerContext, adminUserId), { revokedCount: 3 });
  assert.deepEqual(await api.administration.sessions.revokeClient(adminBearerContext, "admin-web"), { revokedCount: 2 });
});


test("security audit administration is read-only, bounded, and decodes secret-safe metadata", async () => {
  const eventId = "42499999-9999-9999-9999-999999999999";
  const api = client(async (url, init) => {
    assert.equal(init.method, "GET");
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/security-audit?tenantId=${adminTenantId}&userId=${adminUserId}&outcome=Denied&correlationId=abc123&limit=25`);
    return json([{
      eventId,
      occurredAt: "2026-09-24T08:15:00.000Z",
      eventType: "PasswordLoginFailed",
      outcome: "Denied",
      identityScopeId: adminScopeId,
      tenantId: adminTenantId,
      userId: adminUserId,
      applicationKey: "admin-app",
      clientId: "admin-web",
      targetId: adminUserId,
      reasonCode: "InvalidCredentials",
      correlationId: "abc123",
    }]);
  });

  assert.deepEqual(await api.administration.securityAudit.list(adminBearerContext, {
    tenantId: adminTenantId,
    userId: adminUserId,
    outcome: "Denied",
    correlationId: "abc123",
    limit: 25,
  }), [{
    eventId,
    occurredAt: "2026-09-24T08:15:00.000Z",
    eventType: "PasswordLoginFailed",
    outcome: "Denied",
    identityScopeId: adminScopeId,
    tenantId: adminTenantId,
    userId: adminUserId,
    applicationKey: "admin-app",
    clientId: "admin-web",
    targetId: adminUserId,
    reasonCode: "InvalidCredentials",
    correlationId: "abc123",
  }]);
});

test("identity-scope authority administration stays tenant-free and typed", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    const authority = `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/scope-authority`;
    if (call === 1) {
      assert.equal(url, `${authority}/groups/${adminGroupId}/members`);
      assert.equal(init.method, "POST");
      assert.deepEqual(JSON.parse(init.body), { userId: adminUserId });
      return json({ groupId: adminGroupId, userId: adminUserId }, 201);
    }
    if (call === 2) {
      assert.equal(url, `${authority}/policies/${adminPolicyId}/statements`);
      assert.equal(init.method, "POST");
      return json({ statementId: adminStatementId, modelVersion: 2, resource: "*", feature: "*", action: "read" }, 201);
    }
    assert.equal(url, `${authority}/groups/${adminGroupId}/policy-bindings`);
    assert.equal(init.method, "POST");
    assert.deepEqual(JSON.parse(init.body), { policyId: adminPolicyId });
    return json({ groupId: adminGroupId, policyId: adminPolicyId }, 201);
  });

  assert.deepEqual(await api.administration.scopeAuthority.addMember(adminBearerContext, adminGroupId, adminUserId), {
    groupId: adminGroupId, userId: adminUserId,
  });
  assert.equal((await api.administration.scopeAuthority.addPolicyStatement(adminBearerContext, adminPolicyId, {
    statementId: adminStatementId,
    modelVersion: 2,
    resource: "*",
    feature: "*",
    action: "read",
  })).resource, "*");
  assert.deepEqual(await api.administration.scopeAuthority.addPolicyBinding(adminBearerContext, adminGroupId, adminPolicyId), {
    groupId: adminGroupId, policyId: adminPolicyId,
  });
});


test("typed administration rejects invalid lifecycle and optimistic-concurrency input before transport", async () => {
  let calls = 0;
  const api = client(async () => { calls++; return json({}); });
  await assert.rejects(api.administration.users.update(adminBearerContext, adminUserId, {
    displayName: "Alice",
    status: 3,
    expectedVersion: 1,
  }), code("configuration"));
  await assert.rejects(api.administration.tenants.update(adminBearerContext, adminTenantId, {
    displayName: "Tenant",
    status: 1,
    expectedVersion: 0,
  }), code("configuration"));
  assert.equal(calls, 0);
});


test("bounded administration list methods preserve explicit paging and typed records", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    assert.equal(init.method, "GET");
    if (call === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/users?offset=10&limit=25`);
      return json([{ userId: adminUserId, displayName: "Alice", status: 1, version: 3 }]);
    }
    if (call === 2) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/tenants?limit=20`);
      return json([{ tenantId: adminTenantId, displayName: "Tenant", status: 1, version: 2 }]);
    }
    if (call === 3) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/groups?offset=5`);
      return json([{ tenantId: adminTenantId, groupId: adminGroupId, displayName: "Operators", status: 1, isTemplate: false, version: 4 }]);
    }
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/managed-policies`);
    return json([{ policyId: adminManagedPolicyId, policyKey: "operators", displayName: "Operators", status: 1, defaultVersion: 3, version: 7 }]);
  });

  assert.equal((await api.administration.users.list(adminBearerContext, { offset: 10, limit: 25 }))[0].displayName, "Alice");
  assert.equal((await api.administration.tenants.list(adminBearerContext, { limit: 20 }))[0].tenantId, adminTenantId);
  assert.equal((await api.administration.groups.list(adminTenantContext, { offset: 5 }))[0].groupId, adminGroupId);
  assert.equal((await api.administration.managedPolicies.list(adminBearerContext))[0].policyId, adminManagedPolicyId);
});

test("tenant user reads stay tenant constrained and can require active memberships", async () => {
  const api = client(async (url, init) => {
    assert.equal(
      url,
      `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/users?limit=20&search=tes&activeMembershipsOnly=true`,
    );
    assert.equal(init.method, "GET");
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    return json([{
      membershipId: adminMembershipId,
      tenantId: adminTenantId,
      userId: adminUserId,
      displayName: "Test User",
      userStatus: 1,
      membershipStatus: 1,
      userVersion: 4,
      membershipVersion: 7,
    }]);
  });

  assert.deepEqual(
    (await api.administration.tenantUsers.list(adminTenantContext, {
      search: " tes ",
      limit: 20,
      activeMembershipsOnly: true,
    }))[0],
    {
      membershipId: adminMembershipId,
      tenantId: adminTenantId,
      userId: adminUserId,
      displayName: "Test User",
      userStatus: 1,
      membershipStatus: 1,
      userVersion: 4,
      membershipVersion: 7,
    },
  );
});

test("relationship lookup list methods preserve bounded trusted routes", async () => {
  let call = 0;
  const tenantContext = {
    identityScopeId: adminScopeId,
    applicationKey: "admin-app",
    tenantId: adminTenantId,
    credential: bearer,
  };
  const api = client(async (url, init) => {
    call++;
    assert.equal(init.method, "GET");
    assert.equal(init.headers.Authorization, "Bearer header.payload.signature");
    if (call === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/tenants/${adminTenantId}/memberships?limit=200`);
      return json([{ membershipId: adminMembershipId, tenantId: adminTenantId, userId: adminUserId, status: 1, version: 3 }]);
    }
    if (call === 2) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/scope-authority/groups?offset=1&limit=20`);
      return json([{ groupId: adminGroupId, displayName: "Authority operators", status: 1, version: 4 }]);
    }
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/scope-authority/policies?limit=20`);
    return json([{ policyId: adminPolicyId, displayName: "Authority reader", status: 1, version: 7 }]);
  });

  assert.equal((await api.administration.memberships.list(tenantContext, { limit: 200 }))[0].membershipId, adminMembershipId);
  assert.equal((await api.administration.scopeAuthority.listGroups(adminBearerContext, { offset: 1, limit: 20 }))[0].groupId, adminGroupId);
  assert.equal((await api.administration.scopeAuthority.listPolicies(adminBearerContext, { limit: 20 }))[0].policyId, adminPolicyId);
});

test("bounded administration list methods preserve server-side search and reject unsafe search lengths", async () => {
  let calls = 0;
  const api = client(async (url, init) => {
    calls++;
    assert.equal(init.method, "GET");
    if (calls === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/users?limit=20&search=ali`);
      return json([{ userId: adminUserId, displayName: "Alice", status: 1, version: 3 }]);
    }
    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/managed-policies?search=operator`);
    return json([{ policyId: adminManagedPolicyId, policyKey: "operators", displayName: "Operators", status: 1, defaultVersion: 3, version: 7 }]);
  });

  assert.equal((await api.administration.users.list(adminBearerContext, { search: " ali ", limit: 20 }))[0].displayName, "Alice");
  assert.equal((await api.administration.managedPolicies.list(adminBearerContext, { search: "operator" }))[0].policyId, adminManagedPolicyId);

  await assert.rejects(api.administration.users.list(adminBearerContext, { search: "ab" }), code("configuration"));
  await assert.rejects(api.administration.users.list(adminBearerContext, { search: "x".repeat(129) }), code("configuration"));
  assert.equal(calls, 2);
});

test("bounded administration list methods reject invalid paging before transport", async () => {
  let calls = 0;
  const api = client(async () => { calls++; return json([]); });
  await assert.rejects(api.administration.users.list(adminBearerContext, { offset: -1 }), code("configuration"));
  await assert.rejects(api.administration.tenants.list(adminBearerContext, { limit: 0 }), code("configuration"));
  await assert.rejects(api.administration.groups.list(adminTenantContext, { limit: 201 }), code("configuration"));
  assert.equal(calls, 0);
});

test("MFA administration stays provider-neutral and class-based", async () => {
  const requests = [];
  const api = client(async (url, init) => {
    requests.push({ url, init });
    if (url.endsWith("/mfa/providers")) {
      return json([
        { key: "totp", displayName: "Authenticator app", capabilities: ["enrollment", "verification"] },
        { key: "webauthn", displayName: "Passkey", capabilities: ["enrollment", "verification"] },
      ]);
    }
    if (url.endsWith("/mfa/policy")) {
      return json({ mode: 2, allowedProviders: ["totp", "webauthn"], version: 3 });
    }
    if (url.endsWith(`/mfa/users/${adminUserId}/state`)) {
      return json({
        policyConfigured: true,
        policyMode: 2,
        mfaRequired: false,
        hasActiveVerificationFactor: true,
        hasActivePrimaryFactor: true,
        hasActiveRecoveryFactor: false,
        satisfiesCurrentPolicy: true,
        activeVerificationProviders: ["totp"],
        activePrimaryProviders: ["totp"],
        activeRecoveryProviders: [],
      });
    }
    if (url.endsWith(`/mfa/users/${adminUserId}/authenticators/${adminGroupId}/recovery-revoke?expectedVersion=4`)) {
      assert.equal(init.method, "POST");
      assert.equal(init.body, undefined);
      return json({
        authenticatorId: adminGroupId,
        userId: adminUserId,
        providerKey: "totp",
        displayName: "Authenticator app",
        status: 3,
        createdAt: "2026-09-24T00:00:00Z",
        revokedAt: "2026-09-24T00:05:00Z",
        version: 5,
      });
    }
    throw new Error(`unexpected url ${url}`);
  });
  const context = { identityScopeId: scopeId, applicationKey: "app-a", credential: bearer };

  const providers = await api.administration.mfa.listProviders(context);
  const policy = await api.administration.mfa.getPolicy(context);
  const state = await api.administration.mfa.getUserSecurityState(context, adminUserId);
  const recovered = await api.administration.mfa.revokeAuthenticatorForRecovery(
    context, adminUserId, adminGroupId, 4,
  );

  assert.deepEqual(providers.map((item) => item.key), ["totp", "webauthn"]);
  assert.deepEqual(policy, { mode: 2, allowedProviders: ["totp", "webauthn"], version: 3 });
  assert.equal(state.satisfiesCurrentPolicy, true);
  assert.deepEqual(state.activePrimaryProviders, ["totp"]);
  assert.equal(recovered.status, 3);
  assert.equal(recovered.version, 5);
  assert.equal(typeof api.administration.mfa.createPolicy, "function");
  assert.equal(typeof api.administration.mfa.listAuthenticators, "function");
  assert.equal(requests.every((request) => request.init.headers.Authorization === "Bearer header.payload.signature"), true);
});

test("tenant group assignment aggregate and exact-login membership candidate stay tenant scoped", async () => {
  let call = 0;
  const api = client(async (url, init) => {
    call++;
    if (call === 1) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/tenants/${adminTenantId}/applications/admin-app/group-memberships`);
      assert.equal(init.method, "GET");
      return json([{ groupId: adminGroupId, tenantMembershipId: adminMembershipId, userId: adminUserId }]);
    }
    if (call === 2) {
      assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/tenants/${adminTenantId}/membership-candidates/by-login?loginIdentifier=alice%40example.test`);
      assert.equal(init.method, "GET");
      return json({
        userId: adminUserId,
        displayName: "Alice",
        userStatus: 1,
        existingMembershipId: null,
        existingMembershipStatus: null,
      });
    }

    assert.equal(url, `https://identity.example.test/api/v1/identity-scopes/${adminScopeId}/applications/admin-app/tenants/${adminTenantId}/membership-candidates/by-login/membership`);
    assert.equal(init.method, "POST");
    assert.deepEqual(JSON.parse(init.body), { loginIdentifier: "alice@example.test", status: 1 });
    return json({
      membershipId: adminMembershipId,
      tenantId: adminTenantId,
      userId: adminUserId,
      status: 1,
      version: 1,
    }, 201);
  });

  assert.deepEqual(await api.administration.tenantGroupAssignments.list(adminTenantContext), [
    { groupId: adminGroupId, tenantMembershipId: adminMembershipId, userId: adminUserId },
  ]);
  assert.deepEqual(await api.administration.membershipCandidates.findByLogin(adminTenantContext, "alice@example.test"), {
    userId: adminUserId,
    displayName: "Alice",
    userStatus: 1,
  });
  assert.deepEqual(await api.administration.membershipCandidates.createMembershipByLogin(
    adminTenantContext,
    "alice@example.test",
  ), {
    membershipId: adminMembershipId,
    tenantId: adminTenantId,
    userId: adminUserId,
    status: 1,
    version: 1,
  });
});


test("Organization Directory client is composed into administration without a second API host", async () => {
  const organizationId = "42eeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
  const urls = [];
  const methods = [];
  const api = client(async (url, init) => {
    urls.push(url);
    methods.push(init.method);
    if (init.method === "GET") {
      return json([{
        identityScopeId: scopeId,
        tenantId,
        organizationId,
        organizationKey: "urban-flower",
        displayName: "Urban Flower",
        organizationType: "business",
        parentOrganizationId: null,
        status: 1,
        rowVersion: 1,
        createdAt: "2026-09-29T00:00:00Z",
        updatedAt: "2026-09-29T00:00:00Z",
      }]);
    }
    return json({
      identityScopeId: scopeId,
      tenantId,
      organizationId,
      organizationKey: "urban-flower",
      displayName: "Urban Flower",
      organizationType: "business",
      parentOrganizationId: null,
      status: 1,
      rowVersion: 1,
      createdAt: "2026-09-29T00:00:00Z",
      updatedAt: "2026-09-29T00:00:00Z",
    }, 201);
  });

  const context = { identityScopeId: scopeId, applicationKey: "app-a", tenantId, credential: bearer };
  const listed = await api.administration.organizations.list(context, { limit: 20 });
  assert.equal(listed[0].organizationKey, "urban-flower");

  await api.administration.organizations.create(context, {
    organizationKey: "urban-flower",
    displayName: "Urban Flower",
    organizationType: "business",
  });

  assert.equal(urls[0], `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organizations?limit=20`);
  assert.equal(urls[1], `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organizations`);
  assert.deepEqual(methods, ["GET", "POST"]);
});

test("OrganizationMembership client uses organization-centric and member-centric routes", async () => {
  const organizationId = "42eeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
  const tenantMembershipId = "42ffffff-ffff-ffff-ffff-ffffffffffff";
  const seen = [];
  const record = {
    identityScopeId: scopeId,
    tenantId,
    organizationId,
    tenantMembershipId,
    status: 1,
    rowVersion: 1,
    createdAt: "2026-09-29T00:00:00Z",
    updatedAt: "2026-09-29T00:00:00Z",
  };
  const api = client(async (url, init) => {
    seen.push([url, init.method]);
    if (init.method === "POST") return json(record, 201);
    if (init.method === "DELETE") return new Response(null, { status: 204 });
    return json([record]);
  });
  const context = { identityScopeId: scopeId, applicationKey: "app-a", tenantId, credential: bearer };

  await api.administration.organizationMemberships.listForTenantMembership(context, tenantMembershipId, { limit: 10 });
  await api.administration.organizationMemberships.add(context, organizationId, tenantMembershipId);
  assert.equal(await api.administration.organizationMemberships.remove(context, organizationId, tenantMembershipId, 1), true);

  assert.equal(seen[0][0], `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/tenant-memberships/${tenantMembershipId}/organizations?limit=10`);
  assert.equal(seen[1][0], `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organizations/${organizationId}/memberships`);
  assert.equal(seen[2][0], `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organizations/${organizationId}/memberships/${tenantMembershipId}?expectedRowVersion=1`);
});

test("Organization ResourceScope link client stays application-aware", async () => {
  const organizationId = "42eeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
  const link = {
    organizationId,
    applicationKey: "app-a",
    resourceScopeId,
    scopeType: "organization",
    modelVersion: 3,
    status: 1,
    rowVersion: 1,
    createdAt: "2026-09-29T00:00:00Z",
    updatedAt: "2026-09-29T00:00:00Z",
  };
  const seen = [];
  const api = client(async (url, init) => {
    seen.push([url, init.method, init.body]);
    if (init.method === "DELETE") return new Response(null, { status: 204 });
    return json(link, init.method === "POST" ? 201 : 200);
  });
  const context = { identityScopeId: scopeId, applicationKey: "app-a", tenantId, credential: bearer };

  assert.equal((await api.administration.organizationResourceScopeLinks.get(context, organizationId)).resourceScopeId, resourceScopeId);
  await api.administration.organizationResourceScopeLinks.create(context, organizationId, resourceScopeId);
  await api.administration.organizationResourceScopeLinks.update(context, organizationId, resourceScopeId, 1);
  assert.equal(await api.administration.organizationResourceScopeLinks.remove(context, organizationId, 1), true);

  const expected = `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organizations/${organizationId}/resource-scope-link`;
  assert.equal(seen[0][0], expected);
  assert.equal(seen[1][0], expected);
  assert.equal(seen[2][0], expected);
  assert.equal(seen[3][0], `${expected}?expectedRowVersion=1`);
});

test("OrganisationProfile clients expose focused API routes and deterministic contracts", async () => {
  const organizationId = "43111111-1111-1111-1111-111111111111";
  const profileId = "43222222-2222-2222-2222-222222222222";
  const seen = [];
  const profile = {
    organisationProfileId: profileId,
    identityScopeId: scopeId,
    tenantId,
    organizationId,
    templatePin: { templateKey: "ecommerce-standard", templateVersion: 1 },
    status: 1,
    rowVersion: 1,
    createdAt: "2026-09-30T00:00:00Z",
    updatedAt: "2026-09-30T00:00:00Z",
  };
  const effective = {
    organisationProfileId: profileId,
    identityScopeId: scopeId,
    tenantId,
    organizationId,
    version: 1,
    templatePin: { templateKey: "ecommerce-standard", templateVersion: 1 },
    domains: [{ domainKey: "commerce", domainVersion: 4 }],
    contentHash: "a".repeat(64),
    resolvedAt: "2026-09-30T00:00:00Z",
  };
  const template = {
    templateKey: "ecommerce-standard",
    displayName: "Ecommerce Standard",
    status: 1,
    rowVersion: 1,
    createdAt: "2026-09-30T00:00:00Z",
    updatedAt: "2026-09-30T00:00:00Z",
  };
  const version = {
    templateKey: "ecommerce-standard",
    templateVersion: 1,
    status: 1,
    domains: [{ domainKey: "commerce", domainVersion: 4 }],
    contentHash: null,
    rowVersion: 1,
    createdAt: "2026-09-30T00:00:00Z",
    updatedAt: "2026-09-30T00:00:00Z",
    publishedAt: null,
    retiredAt: null,
  };

  const api = client(async (url, init) => {
    seen.push([url, init.method, init.body]);
    if (url.includes("/effective-versions/resolve")) return json(effective);
    if (url.includes("/effective-versions")) return json([effective]);
    if (url.includes("/domain-overrides")) return init.method === "GET"
      ? json([{ domainKey: "inventory", domainVersion: null, operation: 2 }])
      : json({ ...profile, rowVersion: 2 });
    if (url.includes("/organisation-profile-templates/ecommerce-standard/versions")) {
      if (init.method === "POST") return json(version, 201);
      return json([version]);
    }
    if (url.includes("/organisation-profile-templates")) {
      if (init.method === "POST") return json(template, 201);
      return json([template]);
    }
    if (init.method === "POST") return json(profile, 201);
    return json([profile]);
  });

  const tenantContext = { identityScopeId: scopeId, applicationKey: "app-a", tenantId, credential: bearer };
  const adminContext = { identityScopeId: scopeId, applicationKey: "app-a", credential: bearer };

  assert.equal(typeof api.administration.organisationProfiles.create, "function");
  assert.equal(typeof api.administration.organisationProfileDomainOverrides.replace, "function");
  assert.equal(typeof api.administration.organisationProfileEffectiveVersions.resolve, "function");
  assert.equal(typeof api.administration.organisationProfileTemplates.create, "function");
  assert.equal(typeof api.administration.organisationProfileTemplateVersions.createDraft, "function");

  await api.administration.organisationProfiles.create(tenantContext, {
    organizationId,
    templateKey: "ecommerce-standard",
    templateVersion: 1,
  });
  await api.administration.organisationProfileDomainOverrides.list(tenantContext, profileId);
  await api.administration.organisationProfileEffectiveVersions.resolve(tenantContext, profileId, {
    expectedRowVersion: 1,
  });
  await api.administration.organisationProfileTemplates.create(adminContext, {
    templateKey: "ecommerce-standard",
    displayName: "Ecommerce Standard",
  });
  await api.administration.organisationProfileTemplateVersions.createDraft(
    adminContext,
    "ecommerce-standard",
    { templateVersion: 1, domains: [{ domainKey: "commerce", domainVersion: 4 }] },
  );

  assert.equal(
    seen[0][0],
    `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organisation-profiles`,
  );
  assert.equal(
    seen[1][0],
    `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organisation-profiles/${profileId}/domain-overrides`,
  );
  assert.equal(
    seen[2][0],
    `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/tenants/${tenantId}/organisation-profiles/${profileId}/effective-versions/resolve`,
  );
  assert.equal(
    seen[3][0],
    `https://identity.example.test/api/v1/identity-scopes/${scopeId}/applications/app-a/organisation-profile-templates`,
  );
});

