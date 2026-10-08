import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const ts = require("typescript");
const root = path.dirname(fileURLToPath(import.meta.url));

/** Run source-level server helpers with deliberate fake transports, not live API. */
function source(relative, dependencies = {}) {
  const file = path.join(root, "../src/server", relative);
  const output = ts.transpileModule(fs.readFileSync(file, "utf8"), {
    fileName: file,
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const module = { exports: {} };
  const localRequire = (id) => {
    if (id === "server-only") return {};
    if (Object.hasOwn(dependencies, id)) return dependencies[id];
    throw new Error(`Unexpected test dependency: ${id}`);
  };
  new Function("require", "module", "exports", output)(localRequire, module, module.exports);
  return module.exports;
}

class TestClientError extends Error {
  constructor(code, httpStatus) { super(code); this.code = code; this.httpStatus = httpStatus; }
}

const admin = { identityScopeId: "scope-one", applicationKey: "consumer-app", credential: { kind: "session" } };
const scopeWide = { ...admin, userId: "user-1", tenantVisibility: "scope-wide", activeTenantMemberships: [] };
const memberLimited = { ...admin, userId: "user-1", tenantVisibility: "membership-limited", activeTenantMemberships: [
  { membershipId: "membership-a", tenantId: "tenant-a" },
  { membershipId: "membership-b", tenantId: "tenant-b" },
] };

const directory = source("user-directory.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./mutation-failure": { presentNextIdentityMutationFailure: () => ({ kind: "forbidden" }) },
  "./administration": { createNextTenantAdministrationContext: (_session, identityScopeId, applicationKey, tenantId) => (
    { identityScopeId, applicationKey, tenantId, credential: admin.credential }
  ) },
});

const adminForm = source("administration-form.ts");
const mutations = source("user-mutations.ts", { "./administration-form": adminForm });

test("membership-limited users never hit scope-wide user directory", async () => {
  let scopeReads = 0;
  const session = { client: { directory: {
    tenants: { list: async () => { throw new Error("scope tenant list called"); } },
    users: { list: async () => { scopeReads++; throw new Error("scope-wide forbidden"); } },
    tenantUsers: { list: async (context) => [{
      userId: "user-1", displayName: "Shared user", userStatus: 1, userVersion: 2,
      membershipId: `membership-${context.tenantId}`, tenantId: context.tenantId,
    }] },
  } } };
  const result = await directory.loadNextUsersDirectory(session, admin, memberLimited, { tenantId: "tenant-a" });
  assert.equal(result.mode, "tenant");
  assert.equal(result.users.length, 1);
  assert.equal(scopeReads, 0);
  await assert.rejects(() => directory.loadNextUsersDirectory(session, admin, memberLimited, { tenantId: "tenant-outside" }),
    (e) => e.code === "forbidden");
});

test("cross-tenant aggregate remains membership-backed and preserves duplicates", async () => {
  const calls = [];
  const session = { client: { directory: {
    tenantUsers: { list: async (context) => {
      calls.push(context.tenantId);
      return [{ userId: "same-user", displayName: "Same user", userStatus: 1, userVersion: 3,
        membershipId: `member-${context.tenantId}` }];
    } },
  } } };
  const result = await directory.loadNextUsersDirectory(session, admin, memberLimited, { tenantView: "all" });
  assert.deepEqual(calls.sort(), ["tenant-a", "tenant-b"]);
  assert.equal(result.mode, "all");
  assert.equal(result.linkedUsers.length, 2);
  assert.equal(result.linkedUsers[0].userId, result.linkedUsers[1].userId);
  assert.notEqual(result.linkedUsers[0].membershipId, result.linkedUsers[1].membershipId);
});

test("scope-wide list may load one selected user not in first result window", async () => {
  const session = { client: { directory: {
    tenants: { list: async () => [] },
    users: { list: async () => [], get: async (_context, id) => ({ userId: id, displayName: "External", status: 1, version: 5 }) },
  } } };
  const result = await directory.loadNextUsersDirectory(session, admin, scopeWide, { userId: "outside-list" });
  assert.equal(result.mode, "scope");
  assert.equal(result.selectedUser.userId, "outside-list");
});

test("form mutations preserve optimistic concurrency and never echo password values", async () => {
  const calls = [];
  const session = { client: {
    directory: { users: {
      create: async (_context, request) => { calls.push(request); return { userId: "new-user", ...request, version: 1 }; },
      update: async (_context, id, request) => { calls.push({ id, ...request }); return { userId: id, ...request }; },
    } },
    account: { passwordCredentials: {
      create: async (_context, id, request) => { calls.push({ id, ...request }); return { userId: id, version: 1 }; },
      changePassword: async (_context, id, request) => { calls.push({ id, ...request }); return { userId: id, version: 2 }; },
    } },
  } };
  const userCreate = new FormData();
  userCreate.set("displayName", " Test user ");
  await mutations.createNextUserFromForm(session, admin, userCreate);
  assert.equal(calls[0].displayName, "Test user");
  const userUpdate = new FormData();
  Object.entries({ userId: "user-1", displayName: "Edited", status: "2", expectedVersion: "7" })
    .forEach(([k, v]) => userUpdate.set(k, v));
  await mutations.updateNextUserFromForm(session, admin, userUpdate);
  assert.equal(calls[1].expectedVersion, 7);
  assert.equal(calls[1].status, 2);
  const credential = new FormData();
  Object.entries({ userId: "user-1", loginIdentifier: "user@example.test", password: "SecureEnoughForTest!", expectedVersion: "3" })
    .forEach(([k, v]) => credential.set(k, v));
  await mutations.createNextPasswordCredentialFromForm(session, admin, credential);
  await mutations.changeNextPasswordCredentialFromForm(session, admin, credential);
  assert.equal(calls[3].expectedVersion, 3);
  const bad = new FormData();
  Object.entries({ userId: "user-1", loginIdentifier: "user@example.test", password: "short" })
    .forEach(([k, v]) => bad.set(k, v));
  await assert.rejects(() => mutations.createNextPasswordCredentialFromForm(session, admin, bad),
    (error) => !error.message.includes("short"));
});

test("access insight describes assignments but never calls evaluate", async () => {
  const insight = source("user-access-insight.ts");
  let evaluations = 0;
  const session = { client: {
    authorization: { evaluate: async () => { evaluations++; throw new Error("must not evaluate"); } },
    directory: { memberships: { findByUser: async () => ({ membershipId: "member-one", status: 1 }) } },
    accessControl: {
      groups: {
        list: async () => [{ groupId: "group", displayName: "Readers", status: 1 }],
        listMembers: async () => [{ tenantMembershipId: "member-one" }],
      },
      managedPolicyBindings: { list: async () => [{ policyId: "policy", policyVersion: 1, includeDescendants: false }] },
      managedPolicies: {
        get: async () => ({ policyId: "policy", displayName: "Read Only", status: 1 }),
        getVersion: async () => ({ publishedAt: "2026-10-07T00:00:00Z" }),
        listStatements: async () => [{ statementId: "stmt", modelVersion: 1, resource: "app", feature: "users", action: "read" }],
      },
      resourceScopes: { get: async () => null },
    },
  } };
  const result = await insight.loadNextUserAccessInsight(session, admin, { ...admin, tenantId: "tenant-a" },
    { userId: "user-1", displayName: "Example", status: 1, version: 1 });
  assert.equal(result.assignmentCount, 1);
  assert.equal(result.statementCount, 1);
  assert.equal(result.lifecycleReadyAssignmentCount, 1);
  assert.equal(evaluations, 0);
});
