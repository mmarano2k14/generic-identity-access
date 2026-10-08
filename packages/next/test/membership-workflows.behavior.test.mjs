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

class TestClientError extends Error {
  constructor(code, httpStatus) { super(code); this.code = code; this.httpStatus = httpStatus; }
}

const admin = { identityScopeId: "scope-one", applicationKey: "consumer-app", credential: { kind: "session" } };
const scopeWide = { ...admin, userId: "user-1", tenantVisibility: "scope-wide", activeTenantMemberships: [] };
const delegated = { ...admin, userId: "user-1", tenantVisibility: "membership-limited", activeTenantMemberships: [
  { membershipId: "member-one", tenantId: "tenant-a" },
] };
const tenant = { ...admin, tenantId: "tenant-a" };
const auth = { isAllowedOnServer: async () => false };
const adminHelpers = {
  createNextTenantAdministrationContext: (_session, identityScopeId, applicationKey, tenantId) => (
    { identityScopeId, applicationKey, tenantId, credential: admin.credential }
  ),
};
const workspace = source("membership-workspace.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./authorization": auth,
  "./administration": adminHelpers,
});
const mutations = source("membership-mutations.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./authorization": { isAllowedOnServer: async () => true },
  "./administration-form": source("administration-form.ts"),
});

function makeForm(values) {
  const form = new FormData();
  for (const [key, value] of Object.entries(values)) {
    if (Array.isArray(value)) for (const item of value) form.append(key, item);
    else form.set(key, value);
  }
  return form;
}

test("delegated membership view never loads the scope-wide tenant directory", async () => {
  const calls = [];
  const session = { client: { directory: {
    tenants: { list: async () => { throw new Error("scope-wide tenants read forbidden"); } },
    memberships: { findByUser: async (context, userId) => {
      calls.push([context.tenantId, userId]);
      return { membershipId: "member-one", tenantId: "tenant-a", userId, status: 1, version: 1 };
    } },
  } } };
  const result = await workspace.loadNextMembershipWorkspace(session, admin, delegated, { tenantId: "tenant-a", membershipId: "member-one" });
  assert.equal(result.scopeWide, false);
  assert.equal(result.selectedMember?.membershipId, "member-one");
  assert.deepEqual(calls, [["tenant-a", "user-1"]]);
  await assert.rejects(
    workspace.loadNextMembershipWorkspace(session, admin, delegated, { tenantId: "tenant-b" }),
    (error) => error.code === "forbidden",
  );
});

test("member/group/organization directory is not fetched when capability is absent", async () => {
  const session = { client: { directory: {
    tenants: {
      get: async (_context, tenantId) => tenantId === "tenant-a"
        ? { tenantId: "tenant-a", displayName: "A", status: 1, version: 1 }
        : null,
      list: async () => { throw new Error("scope-wide tenant catalog bypass"); },
    },
    tenantUsers: { list: async () => { throw new Error("member read bypass"); } },
    tenantGroupAssignments: { list: async () => { throw new Error("assignment read bypass"); } },
  }, accessControl: { groups: { list: async () => { throw new Error("group read bypass"); } } },
    organizations: { organizations: { list: async () => { throw new Error("org read bypass"); } } } } };
  const result = await workspace.loadNextMembershipWorkspace(session, admin, scopeWide, { tenantId: "tenant-a" });
  assert.equal(result.members.length, 0);
  assert.equal(result.groups.length, 0);
  assert.equal(result.organizations.length, 0);
  assert.equal(result.permissions.canWriteMembers, false);
});

test("scope-wide creation uses UserId; delegated creation uses eligible exact login", async () => {
  const calls = [];
  const session = { client: { directory: {
    memberships: { create: async (ctx, request) => {
      calls.push(["by-id", ctx.tenantId, request]);
      return { membershipId: "new-member", tenantId: ctx.tenantId, ...request, version: 1 };
    } },
    membershipCandidates: {
      findByLogin: async (_ctx, login) => {
        calls.push(["find", login]);
        return { userId: "user-new", userStatus: 1, displayName: "New" };
      },
      createMembershipByLogin: async (_ctx, login, status) => {
        calls.push(["by-login", login, status]);
        return { membershipId: "new-member", userId: "user-new" };
      },
    },
  } } };
  await mutations.createNextTenantMembershipFromForm(session, tenant, scopeWide, makeForm({ userId: "user-new", status: "1" }));
  await mutations.createNextTenantMembershipFromForm(session, tenant, delegated, makeForm({ loginIdentifier: "user@example.test", status: "2" }));
  assert.deepEqual(calls[0], ["by-id", "tenant-a", { userId: "user-new", status: 1 }]);
  assert.deepEqual(calls[1], ["find", "user@example.test"]);
  assert.deepEqual(calls[2], ["by-login", "user@example.test", 2]);
});

test("cannot add a candidate that is inactive or already a member", async () => {
  let mutationsCalled = false;
  const session = { client: { directory: { membershipCandidates: {
    findByLogin: async () => ({ userId: "user-1", userStatus: 2, existingMembershipId: "x" }),
    createMembershipByLogin: async () => { mutationsCalled = true; },
  } } } };
  await assert.rejects(
    mutations.createNextTenantMembershipFromForm(session, tenant, delegated, makeForm({ loginIdentifier: "inactive@example.test" })),
    /not eligible/,
  );
  assert.equal(mutationsCalled, false);
});

test("membership edit forwards the expected row version unchanged", async () => {
  let captured;
  const session = { client: { directory: { memberships: {
    update: async (_ctx, id, request) => { captured = { id, request }; return { membershipId: id }; },
  } } } };
  await mutations.updateNextTenantMembershipFromForm(session, tenant,
    makeForm({ membershipId: "member-one", status: "2", expectedVersion: "17" }));
  assert.deepEqual(captured, { id: "member-one", request: { status: 2, expectedVersion: 17 } });
});

test("group replacement validates choices before changing any assignment", async () => {
  const calls = [];
  const session = { client: {
    directory: {
      memberships: { get: async () => ({ membershipId: "member-one", tenantId: "tenant-a" }) },
      tenantGroupAssignments: { list: async () => [{ tenantMembershipId: "member-one", groupId: "group-old" }] },
    },
    accessControl: { groups: {
      list: async () => [
        { groupId: "group-old", status: 1 },
        { groupId: "group-new", status: 1 },
      ],
      removeMember: async (_ctx, groupId) => { calls.push(["remove", groupId]); },
      addMember: async (_ctx, groupId) => { calls.push(["add", groupId]); },
    } },
  } };
  await assert.rejects(
    mutations.replaceNextTenantMemberGroupsFromForm(session, tenant, makeForm({
      tenantMembershipId: "member-one", groupSelection: ["group:unknown"],
    })), /unavailable/,
  );
  assert.deepEqual(calls, []);
  await mutations.replaceNextTenantMemberGroupsFromForm(session, tenant, makeForm({
    tenantMembershipId: "member-one", groupSelection: ["group:group-new"],
  }));
  assert.deepEqual(calls, [["remove", "group-old"], ["add", "group-new"]]);
});

test("organization replacement preserves concurrency on removal/reactivation", async () => {
  const calls = [];
  const session = { client: {
    directory: { memberships: { get: async () => ({ tenantId: "tenant-a" }) } },
    organizations: {
      organizations: { list: async () => [{ organizationId: "org-a", status: 1 }, { organizationId: "org-b", status: 1 }] },
      memberships: {
        listForTenantMembership: async () => [
          { organizationId: "org-a", status: 1, rowVersion: 12 },
          { organizationId: "org-b", status: 2, rowVersion: 8 },
        ],
        remove: async (_ctx, id, _member, version) => { calls.push(["remove", id, version]); },
        activate: async (_ctx, id, _member, version) => { calls.push(["activate", id, version]); },
        add: async () => { throw new Error("unexpected add"); },
      },
    },
  } };
  await mutations.replaceNextTenantMemberOrganizationsFromForm(session, tenant, makeForm({
    tenantMembershipId: "member-one", organizationSelection: ["organization:org-b"],
  }));
  assert.deepEqual(calls, [["remove", "org-a", 12], ["activate", "org-b", 8]]);
});

test("candidate preview requires the actual tenant-membership write capability", async () => {
  let lookups = 0;
  const forbiddenMutations = source("membership-mutations.ts", {
    "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
    "./authorization": { isAllowedOnServer: async () => false },
    "./administration-form": source("administration-form.ts"),
  });
  const session = { client: { directory: { membershipCandidates: {
    findByLogin: async () => { lookups++; return { userId: "private-user", userStatus: 1 }; },
  } } } };
  await assert.rejects(
    forbiddenMutations.findNextTenantMembershipCandidateFromForm(session, tenant,
      makeForm({ loginIdentifier: "private@example.test" })),
    (error) => error.code === "forbidden",
  );
  assert.equal(lookups, 0);
  const candidate = await mutations.findNextTenantMembershipCandidateFromForm(
    session, tenant, makeForm({ loginIdentifier: "private@example.test" }),
  );
  assert.equal(candidate.userId, "private-user");
  assert.equal(lookups, 1);
});
