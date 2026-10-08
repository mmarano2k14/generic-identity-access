import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const ts = require("typescript");
const root = path.dirname(fileURLToPath(import.meta.url));

function source(relative, dependencies = {}) {
  const file = path.join(root, "../src/server", relative);
  const output = ts.transpileModule(fs.readFileSync(file, "utf8"), {
    fileName: file,
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const module = { exports: {} };
  new Function("require", "module", "exports", output)((id) => {
    if (id === "server-only") return {};
    if (Object.hasOwn(dependencies, id)) return dependencies[id];
    throw new Error(`Unexpected test dependency: ${id}`);
  }, module, module.exports);
  return module.exports;
}

const admin = {
  identityScopeId: "00000000-0000-0000-0000-000000000001",
  applicationKey: "consumer-app",
  credential: { kind: "session" },
};

const effective = {
  identityScopeId: admin.identityScopeId,
  applicationKey: admin.applicationKey,
  userId: "00000000-0000-0000-0000-000000000003",
  tenantVisibility: "scope-wide",
  activeTenantMemberships: [],
};

const failurePresentation = {
  presentNextIdentityMutationFailure: (error) => ({
    kind: "unavailable",
    title: "Read unavailable",
    message: error instanceof Error ? error.message : "Unavailable",
    recovery: "retry",
  }),
};

function workspaceWith(capability, authErrors = {}) {
  return source("session-security-workspace.ts", {
    "@generic-identity/auth": {
      GenericIdentityClientError: class GenericIdentityClientError extends Error {
        constructor(code) { super(code); this.code = code; }
      },
      ...authErrors,
    },
    "./authorization": {
      isAllowedOnServer: async (_session, _boundary, requirement) => capability(requirement),
    },
    "./mutation-failure": failurePresentation,
  });
}

function form(values) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, String(value));
  return data;
}

test("no audit transport occurs without security-audit/read", async () => {
  const workspace = workspaceWith((requirement) =>
    requirement.feature === "session" && requirement.action === "write");
  const session = { client: {
    security: {
      audit: { list: async () => { throw new Error("audit bypass"); } },
    },
  } };
  const result = await workspace.loadNextSessionSecurityWorkspace(session, admin, effective);
  assert.equal(result.evidenceState, "forbidden");
  assert.equal(result.permissions.canWriteSessions, true);
  assert.equal(result.records.length, 0);
});

test("session evidence uses only the GOLDEN bounded event types", async () => {
  const workspace = workspaceWith(() => true);
  const calls = [];
  const session = { client: {
    security: {
      audit: {
        list: async (_context, query) => {
          calls.push(query);
          return [];
        },
      },
    },
  } };
  await workspace.loadNextSessionSecurityWorkspace(session, admin, effective, { limit: "25" });
  assert.equal(calls.length, 9);
  assert.deepEqual(
    calls.map((call) => call.eventType),
    [
      "PasswordLoginSucceeded",
      "SessionRevoked",
      "SessionRevocationFailed",
      "UserSessionsRevoked",
      "ClientSessionsRevoked",
      "SessionAssuranceUpgraded",
      "SessionAssuranceUpgradeFailed",
      "OidcRefreshTokenReuseDetected",
      "OidcRefreshTokenFamilyRevoked",
    ],
  );
  assert.ok(calls.every((call) => call.limit === 25));
});

test("client filtering stays server-side in the host and bounded by source windows", async () => {
  const workspace = workspaceWith(() => true);
  const events = [
    {
      eventId: "00000000-0000-0000-0000-000000000010",
      occurredAt: "2026-10-07T02:00:00Z",
      eventType: "PasswordLoginSucceeded",
      outcome: "Succeeded",
      identityScopeId: admin.identityScopeId,
      clientId: "consumer-web",
    },
    {
      eventId: "00000000-0000-0000-0000-000000000011",
      occurredAt: "2026-10-07T01:00:00Z",
      eventType: "PasswordLoginSucceeded",
      outcome: "Succeeded",
      identityScopeId: admin.identityScopeId,
      clientId: "other-client",
    },
  ];
  const session = { client: {
    security: {
      audit: {
        list: async (_context, query) =>
          query.eventType === "PasswordLoginSucceeded" ? events : [],
      },
    },
  } };
  const result = await workspace.loadNextSessionSecurityWorkspace(
    session,
    admin,
    effective,
    { clientId: "consumer-web", limit: "25" },
  );
  assert.equal(result.records.length, 1);
  assert.equal(result.records[0].clientId, "consumer-web");
});

test("query validation rejects malformed user, client, and outcome", () => {
  const workspace = workspaceWith(() => true);
  assert.match(
    workspace.normalizeNextSessionSecurityQuery({ userId: "bad" }).validationMessage,
    /User ID must be a UUID/,
  );
  assert.match(
    workspace.normalizeNextSessionSecurityQuery({ clientId: "x".repeat(129) }).validationMessage,
    /Client ID must not exceed 128/,
  );
  assert.match(
    workspace.normalizeNextSessionSecurityQuery({ outcome: "Maybe" }).validationMessage,
    /Outcome filter is invalid/,
  );
});

test("summary preserves issuance revocation assurance and continuity classes", () => {
  const workspace = workspaceWith(() => true);
  const records = [
    { eventType: "PasswordLoginSucceeded" },
    { eventType: "SessionRevoked" },
    { eventType: "SessionAssuranceUpgraded" },
    { eventType: "OidcRefreshTokenReuseDetected" },
    { eventType: "OidcRefreshTokenFamilyRevoked" },
  ];
  assert.deepEqual(workspace.summarizeSessionSecurity(records), {
    total: 5,
    issued: 1,
    revocationEvents: 2,
    assuranceEvents: 1,
    continuityAlerts: 1,
  });
});

test("audit transport failure produces unavailable evidence without inventing session state", async () => {
  const workspace = workspaceWith(() => true);
  const session = { client: {
    security: {
      audit: { list: async () => { throw new Error("dependency down"); } },
    },
  } };
  const result = await workspace.loadNextSessionSecurityWorkspace(session, admin, effective);
  assert.equal(result.evidenceState, "unavailable");
  assert.equal(result.records.length, 0);
  assert.equal(result.failure.kind, "unavailable");
});

test("revocation mutations require literal REVOKE and use existing typed transports", async () => {
  const adminForm = source("administration-form.ts");
  const mutations = source("session-security-mutations.ts", {
    "./administration-form": adminForm,
  });
  const calls = [];
  const session = { client: { security: { sessions: {
    revokeUser: async (_context, userId) => {
      calls.push(["user", userId]);
      return { revokedCount: 2 };
    },
    revokeClient: async (_context, clientId) => {
      calls.push(["client", clientId]);
      return { revokedCount: 3 };
    },
  } } } };

  await assert.rejects(
    mutations.revokeNextUserSessionsFromForm(
      session,
      admin,
      form({ userId: "00000000-0000-0000-0000-000000000003", confirmation: "NO" }),
    ),
    /confirmation must be REVOKE/,
  );

  const userResult = await mutations.revokeNextUserSessionsFromForm(
    session,
    admin,
    form({ userId: "00000000-0000-0000-0000-000000000003", confirmation: "REVOKE" }),
  );
  const clientResult = await mutations.revokeNextClientSessionsFromForm(
    session,
    admin,
    form({ clientId: "consumer-web", confirmation: "REVOKE" }),
  );

  assert.equal(userResult.revokedCount, 2);
  assert.equal(clientResult.revokedCount, 3);
  assert.deepEqual(calls, [
    ["user", "00000000-0000-0000-0000-000000000003"],
    ["client", "consumer-web"],
  ]);
});

test("global user lookup is exposed only for scope-wide subjects with user/read", async () => {
  const workspace = workspaceWith((requirement) =>
    requirement.feature === "user" && requirement.action === "read");
  const session = { client: { security: { audit: { list: async () => [] } } } };

  const scopeWide = await workspace.loadNextSessionSecurityWorkspace(session, admin, effective);
  assert.equal(scopeWide.permissions.scopeWide, true);
  assert.equal(scopeWide.permissions.canReadUsers, true);

  const limited = await workspace.loadNextSessionSecurityWorkspace(session, admin, {
    ...effective,
    tenantVisibility: "membership-limited",
  });
  assert.equal(limited.permissions.scopeWide, false);
});
