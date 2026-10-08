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
  identityScopeId: "scope-one",
  applicationKey: "consumer-app",
  credential: { kind: "session" },
};

const adminForm = source("administration-form.ts");
const mutations = source("mfa-mutations.ts", {
  "./administration-form": adminForm,
});

function workspaceWith(capability) {
  return source("mfa-workspace.ts", {
    "./authorization": {
      isAllowedOnServer: async (_session, _boundary, requirement) => capability(requirement),
    },
  });
}

function form(values, lists = {}) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, String(value));
  for (const [key, values] of Object.entries(lists)) {
    for (const value of values) data.append(key, String(value));
  }
  return data;
}

test("MFA data is not fetched when read capabilities are absent", async () => {
  const workspace = workspaceWith(() => false);
  const session = { client: {
    security: { mfa: {
      listProviders: async () => { throw new Error("provider read bypass"); },
      getPolicy: async () => { throw new Error("policy read bypass"); },
      getUserSecurityState: async () => { throw new Error("state read bypass"); },
      listAuthenticators: async () => { throw new Error("authenticator read bypass"); },
    } },
    directory: { users: { get: async () => { throw new Error("user read bypass"); } } },
  } };
  const result = await workspace.loadNextMfaWorkspace(session, admin, { userId: "user-one" });
  assert.equal(result.providers.length, 0);
  assert.equal(result.policy, null);
  assert.equal(result.selectedUser, null);
  assert.equal(result.authenticators.length, 0);
});

test("policy/providers load only under mfa-policy read", async () => {
  const workspace = workspaceWith((requirement) =>
    requirement.feature === "mfa-policy" && requirement.action === "read");
  const session = { client: {
    security: { mfa: {
      listProviders: async () => [{ key: "totp", displayName: "TOTP", capabilities: ["verification"] }],
      getPolicy: async () => ({ mode: 2, allowedProviders: ["totp"], version: 4 }),
    } },
    directory: { users: { get: async () => null } },
  } };
  const result = await workspace.loadNextMfaWorkspace(session, admin);
  assert.equal(result.providers[0].key, "totp");
  assert.equal(result.policy.version, 4);
  assert.equal(result.permissions.canReadPolicy, true);
  assert.equal(result.permissions.canReadAuthenticators, false);
});

test("selected user effective state and authenticators compose under user + authenticator read", async () => {
  const workspace = workspaceWith((requirement) =>
    (requirement.feature === "user" && requirement.action === "read")
    || (requirement.feature === "mfa-authenticator" && requirement.action === "read"));
  const session = { client: {
    security: { mfa: {
      listProviders: async () => [],
      getPolicy: async () => null,
      getUserSecurityState: async () => ({
        policyConfigured: true,
        policyMode: 3,
        mfaRequired: true,
        hasActiveVerificationFactor: true,
        hasActivePrimaryFactor: true,
        hasActiveRecoveryFactor: false,
        satisfiesCurrentPolicy: true,
        activeVerificationProviders: ["totp"],
        activePrimaryProviders: ["totp"],
        activeRecoveryProviders: [],
      }),
      listAuthenticators: async () => [{
        authenticatorId: "a1", userId: "u1", providerKey: "totp",
        displayName: "Phone", status: 2, createdAt: "2026-01-01T00:00:00Z", version: 3,
      }],
    } },
    directory: { users: { get: async () => ({ userId: "u1", displayName: "User One", status: 1, version: 1 }) } },
  } };
  const result = await workspace.loadNextMfaWorkspace(session, admin, { userId: "u1" });
  assert.equal(result.selectedUser.displayName, "User One");
  assert.equal(result.securityState.satisfiesCurrentPolicy, true);
  assert.equal(result.authenticators[0].providerKey, "totp");
});

test("MFA policy create preserves mode and provider allow-list", async () => {
  const calls = [];
  const session = { client: { security: { mfa: {
    createPolicy: async (_ctx, request) => { calls.push(request); return { ...request, version: 1 }; },
  } } } };
  await mutations.createNextMfaPolicyFromForm(
    session,
    admin,
    form({ mode: "3" }, { allowedProviders: ["totp", "webauthn", "totp"] }),
  );
  assert.deepEqual(calls[0], { mode: 3, allowedProviders: ["totp", "webauthn"] });
});

test("MFA policy update forwards optimistic expectedVersion", async () => {
  const calls = [];
  const session = { client: { security: { mfa: {
    updatePolicy: async (_ctx, request) => { calls.push(request); return { ...request, version: 8 }; },
  } } } };
  await mutations.updateNextMfaPolicyFromForm(
    session,
    admin,
    form({ mode: "2", expectedVersion: "7" }, { allowedProviders: ["totp"] }),
  );
  assert.equal(calls[0].expectedVersion, 7);
  assert.equal(calls[0].mode, 2);
});

test("normal authenticator revoke requires literal REVOKE and forwards version", async () => {
  const calls = [];
  const session = { client: { security: { mfa: {
    revokeAuthenticator: async (_ctx, userId, authenticatorId, expectedVersion) => {
      calls.push([userId, authenticatorId, expectedVersion]); return { authenticatorId, userId, version: expectedVersion + 1 };
    },
  } } } };
  await assert.rejects(
    mutations.revokeNextMfaAuthenticatorFromForm(
      session, admin, form({ userId: "u1", authenticatorId: "a1", expectedVersion: "3", confirmation: "NO" }),
    ),
    /confirmation must be REVOKE/,
  );
  await mutations.revokeNextMfaAuthenticatorFromForm(
    session, admin, form({ userId: "u1", authenticatorId: "a1", expectedVersion: "3", confirmation: "REVOKE" }),
  );
  assert.deepEqual(calls, [["u1", "a1", 3]]);
});

test("lost-factor recovery revoke uses explicit recovery transport", async () => {
  const calls = [];
  const session = { client: { security: { mfa: {
    revokeAuthenticatorForRecovery: async (_ctx, userId, authenticatorId, expectedVersion) => {
      calls.push([userId, authenticatorId, expectedVersion]); return { authenticatorId, userId, version: expectedVersion + 1 };
    },
  } } } };
  await mutations.recoveryRevokeNextMfaAuthenticatorFromForm(
    session, admin, form({ userId: "u1", authenticatorId: "a1", expectedVersion: "9", confirmation: "REVOKE" }),
  );
  assert.deepEqual(calls, [["u1", "a1", 9]]);
});

test("invalid MFA mode/provider key is rejected before transport", async () => {
  let called = 0;
  const session = { client: { security: { mfa: {
    createPolicy: async () => { called++; return {}; },
  } } } };
  await assert.rejects(
    mutations.createNextMfaPolicyFromForm(session, admin, form({ mode: "99" })),
    /must be Disabled, Optional, or Required/,
  );
  await assert.rejects(
    mutations.createNextMfaPolicyFromForm(session, admin, form({ mode: "2" }, { allowedProviders: ["BAD KEY"] })),
    /invalid provider key/,
  );
  assert.equal(called, 0);
});
