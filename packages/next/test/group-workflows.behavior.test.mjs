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
  { membershipId: "member-a", tenantId: "tenant-a" },
] };
const tenant = { ...admin, tenantId: "tenant-a" };
const adminHelpers = {
  createNextTenantAdministrationContext: (_session, identityScopeId, applicationKey, tenantId) => ({ identityScopeId, applicationKey, tenantId, credential: admin.credential }),
};
const workspace = source("group-workspace.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./authorization": { isAllowedOnServer: async () => false },
  "./administration": adminHelpers,
});
const mutations = source("group-mutations.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./authorization": { isAllowedOnServer: async () => true },
  "./administration-form": source("administration-form.ts"),
});

function form(values) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) {
    if (Array.isArray(value)) for (const item of value) data.append(key, item);
    else data.set(key, value);
  }
  return data;
}

test("delegated Groups workspace cannot select an unrelated tenant", async () => {
  const session = { client: { directory: { tenants: { get: async () => { throw new Error("scope-wide lookup bypass"); } } } } };
  await assert.rejects(
    workspace.loadNextGroupWorkspace(session, admin, delegated, { tenantId: "tenant-b" }),
    (error) => error.code === "forbidden",
  );
});

test("group directory is not fetched without group/read", async () => {
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a", displayName: "A" }) } },
    accessControl: { groups: { list: async () => { throw new Error("group read bypass"); } } },
  } };
  const result = await workspace.loadNextGroupWorkspace(session, admin, scopeWide, { tenantId: "tenant-a" });
  assert.equal(result.groups.length, 0);
  assert.equal(result.permissions.canReadGroups, false);
});

test("group create/update preserve GOLDEN bounds and optimistic version", async () => {
  const calls = [];
  const session = { client: { accessControl: { groups: {
    create: async (_ctx, request) => { calls.push(["create", request]); return { tenantId: "tenant-a", groupId: "group-a", isTemplate: false, version: 1, ...request }; },
    get: async () => ({ tenantId: "tenant-a", groupId: "group-a", status: 1, isTemplate: false }),
    update: async (_ctx, id, request) => { calls.push(["update", id, request]); return { groupId: id, ...request }; },
  } } } };
  await mutations.createNextGroupFromForm(session, tenant, form({ displayName: "Operators", status: "1" }));
  await mutations.updateNextGroupFromForm(session, tenant, delegated, form({ groupId: "group-a", displayName: "Operators 2", status: "2", expectedVersion: "7" }));
  assert.deepEqual(calls[0], ["create", { displayName: "Operators", status: 1 }]);
  assert.equal(calls[1][2].expectedVersion, 7);
});

test("tenant administrator cannot mutate reusable source definition", async () => {
  const session = { client: { accessControl: { groups: {
    get: async () => ({ tenantId: "tenant-a", groupId: "group-template", status: 1, isTemplate: true }),
    update: async () => { throw new Error("template mutation bypass"); },
  } } } };
  await assert.rejects(
    mutations.updateNextGroupFromForm(session, tenant, delegated, form({ groupId: "group-template", displayName: "Template", status: "1", expectedVersion: "2" })),
    (error) => error.code === "forbidden",
  );
});

test("group member add revalidates active same-tenant membership", async () => {
  let added = 0;
  const session = { client: {
    accessControl: { groups: {
      get: async () => ({ tenantId: "tenant-a", status: 1 }),
      addMember: async () => { added++; return {}; },
    } },
    directory: { memberships: { get: async () => ({ tenantId: "tenant-a", status: 2 }) } },
  } };
  await assert.rejects(
    mutations.addNextGroupMemberFromForm(session, tenant, form({ groupId: "group-a", tenantMembershipId: "member-a" })),
    /tenant member is unavailable or inactive/,
  );
  assert.equal(added, 0);
});

test("managed policy binding revalidates published active policy and active scope", async () => {
  const calls = [];
  const session = { client: { accessControl: {
    groups: { get: async () => ({ tenantId: "tenant-a", status: 1 }) },
    managedPolicyBindings: {
      listAvailablePolicies: async () => [{ policyId: "policy-a", status: 1, defaultVersion: 4 }],
      add: async (_ctx, groupId, request) => { calls.push([groupId, request]); return { groupId, policyVersion: 4, ...request }; },
    },
    resourceScopes: { get: async () => ({ resourceScopeId: "scope-a", status: 1 }) },
  } } };
  await mutations.addNextManagedGroupPolicyBindingFromForm(session, tenant, form({ groupId: "group-a", managedPolicyId: "policy-a", resourceScopeId: "scope-a", includeDescendants: "true" }));
  assert.deepEqual(calls, [["group-a", { policyId: "policy-a", resourceScopeId: "scope-a", includeDescendants: true }]]);
});

test("template clone validates compatible scope mappings before mutation", async () => {
  const calls = [];
  const session = { client: { accessControl: {
    groups: {
      listTemplates: async () => [{ tenantId: "source-tenant", groupId: "template-a", status: 1, isTemplate: true }],
      listTemplateScopeRequirements: async () => [{ sourceResourceScopeId: "source-scope", modelVersion: 5, scopeType: "business", displayName: "Business" }],
      createFromTemplate: async (_ctx, request) => { calls.push(request); return { groupId: "new-group" }; },
    },
    resourceScopes: { list: async () => [{ resourceScopeId: "target-scope", modelVersion: 5, scopeType: "business", status: 1 }] },
  } } };
  await mutations.createNextGroupFromTemplateFromForm(session, tenant, form({ sourceGroup: "source-tenant:template-a", "resourceScopeMapping:source-scope": "target-scope" }));
  assert.deepEqual(calls[0].resourceScopeMappings, [{ sourceResourceScopeId: "source-scope", targetResourceScopeId: "target-scope" }]);
});

test("remove member and binding require literal REMOVE confirmation", async () => {
  let removed = 0;
  const session = { client: { accessControl: {
    groups: { removeMember: async () => { removed++; return true; } },
    managedPolicyBindings: { remove: async () => { removed++; return true; } },
  } } };
  await assert.rejects(
    mutations.removeNextGroupMemberFromForm(session, tenant, form({ groupId: "group-a", tenantMembershipId: "member-a", confirmation: "remove" })),
    /confirmation must be REMOVE/,
  );
  await assert.rejects(
    mutations.removeNextManagedGroupPolicyBindingFromForm(session, tenant, form({ groupId: "group-a", managedPolicyId: "policy-a", policyVersion: "1", confirmation: "NO" })),
    /confirmation must be REMOVE/,
  );
  assert.equal(removed, 0);
});
