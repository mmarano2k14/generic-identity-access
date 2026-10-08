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
const workspace = source("organization-workspace.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./authorization": { isAllowedOnServer: async () => false },
  "./administration": adminHelpers,
});
const mutations = source("organization-mutations.ts", {
  "./administration-form": source("administration-form.ts"),
});

function form(values) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, value);
  return data;
}

test("delegated Organization workspace cannot select an unrelated tenant", async () => {
  const session = { client: { directory: { tenants: { get: async () => { throw new Error("scope-wide lookup bypass"); } } } } };
  await assert.rejects(
    workspace.loadNextOrganizationWorkspace(session, admin, delegated, { tenantId: "tenant-b" }),
    (error) => error.code === "forbidden",
  );
});

test("organization directory is not fetched without organization/read", async () => {
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a", displayName: "A", status: 1, version: 1 }) } },
    organizations: { organizations: { list: async () => { throw new Error("organization read bypass"); } } },
  } };
  const result = await workspace.loadNextOrganizationWorkspace(session, admin, scopeWide, { tenantId: "tenant-a" });
  assert.equal(result.organizations.length, 0);
  assert.equal(result.permissions.canReadOrganizations, false);
});

test("create/update preserve GOLDEN slug bounds and optimistic row version", async () => {
  const calls = [];
  const session = { client: { organizations: { organizations: {
    create: async (_ctx, request) => { calls.push(["create", request]); return { organizationId: "org-new", ...request }; },
    update: async (_ctx, id, request) => { calls.push(["update", id, request]); return { organizationId: id, ...request }; },
  } } } };
  await mutations.createNextOrganizationFromForm(session, tenant, form({ organizationKey: "division-a", displayName: "Division A", organizationType: "division" }));
  await mutations.updateNextOrganizationFromForm(session, tenant, form({ organizationId: "org-new", displayName: "Division Alpha", organizationType: "division", expectedRowVersion: "9" }));
  assert.equal(calls[0][1].organizationKey, "division-a");
  assert.equal(calls[1][2].expectedRowVersion, 9);
  await assert.rejects(
    mutations.createNextOrganizationFromForm(session, tenant, form({ organizationKey: "Bad Key", displayName: "Bad", organizationType: "division" })),
    /lowercase stable key/,
  );
});

test("membership add revalidates an active membership in the same tenant", async () => {
  let added = 0;
  const session = { client: {
    directory: { memberships: { get: async () => ({ tenantId: "tenant-a", status: 2 }) } },
    organizations: { memberships: { add: async () => { added++; } } },
  } };
  await assert.rejects(
    mutations.addNextOrganizationMembershipFromForm(session, tenant, form({ organizationId: "org-a", tenantMembershipId: "member-a" })),
    /active tenant membership/,
  );
  assert.equal(added, 0);
});

test("scope link revalidates active ResourceScope and preserves row version", async () => {
  const calls = [];
  const session = { client: {
    accessControl: { resourceScopes: { get: async (_ctx, id) => ({ resourceScopeId: id, status: 1 }) } },
    organizations: { resourceScopeLinks: {
      update: async (_ctx, org, scope, version) => { calls.push([org, scope, version]); return {}; },
    } },
  } };
  await mutations.relinkNextOrganizationResourceScopeFromForm(session, tenant, form({ organizationId: "org-a", resourceScopeId: "scope-a", expectedRowVersion: "12" }));
  assert.deepEqual(calls, [["org-a", "scope-a", 12]]);
});

test("unlink requires explicit REMOVE confirmation", async () => {
  let removed = 0;
  const session = { client: { organizations: { resourceScopeLinks: { remove: async () => { removed++; return true; } } } } };
  await assert.rejects(
    mutations.unlinkNextOrganizationResourceScopeFromForm(session, tenant, form({ organizationId: "org-a", expectedRowVersion: "3", confirmation: "remove" })),
    /type REMOVE/,
  );
  assert.equal(removed, 0);
});
