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

const adminHelpers = {
  createNextTenantAdministrationContext: (_session, identityScopeId, applicationKey, tenantId) => ({
    identityScopeId, applicationKey, tenantId, credential: admin.credential,
  }),
};

const workspaceDenied = source("resource-scope-workspace.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./administration": adminHelpers,
  "./authorization": { isAllowedOnServer: async (_session, boundary, requirement) =>
    requirement.feature === "scope-type" && requirement.action === "read" ? true : false },
});

const mutations = source("resource-scope-mutations.ts", {
  "@generic-identity/auth": { GenericIdentityClientError: TestClientError },
  "./administration-form": source("administration-form.ts"),
  "./administration": adminHelpers,
  "./authorization": { isAllowedOnServer: async () => true },
});

function form(values) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, value);
  return data;
}

test("membership-limited ResourceScope workspace cannot select unrelated tenant", async () => {
  const session = { client: { directory: { tenants: { get: async () => { throw new Error("scope-wide lookup bypass"); } } } } };
  await assert.rejects(
    workspaceDenied.loadNextResourceScopeWorkspace(session, admin, delegated, { tenantId: "tenant-b" }),
    (error) => error.code === "forbidden",
  );
});

test("ResourceScope collection is not fetched when resource-scope/read is absent", async () => {
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a", displayName: "Tenant A" }) } },
    accessControl: { resourceScopes: { list: async () => { throw new Error("resource scope read bypass"); } } },
  } };
  const result = await workspaceDenied.loadNextResourceScopeWorkspace(session, admin, scopeWide, { tenantId: "tenant-a" });
  assert.equal(result.resourceScopes.length, 0);
  assert.equal(result.permissions.canReadResourceScopes, false);
  assert.equal(result.permissions.canReadScopeTypes, true);
});

test("ResourceScope create revalidates registered scope type and parent", async () => {
  const calls = [];
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a" }) } },
    applicationSecurity: { scopeTypes: { list: async () => [{ key: "business", displayName: "Business", canAttachToTenant: true }] } },
    accessControl: { resourceScopes: {
      get: async (_ctx, id) => id === "parent-a" ? { resourceScopeId: id, status: 1 } : null,
      create: async (_ctx, request) => { calls.push(request); return { resourceScopeId: "scope-a", version: 1, ...request }; },
    } },
  } };
  const created = await mutations.createNextResourceScopeFromForm(session, admin, scopeWide, form({
    tenantId: "tenant-a",
    modelVersion: "5",
    scopeType: "business",
    externalResourceId: "BUS-1",
    displayName: "Business One",
    parentResourceScopeId: "parent-a",
    status: "1",
  }));
  assert.equal(created.resourceScopeId, "scope-a");
  assert.equal(calls[0].modelVersion, 5);
  assert.equal(calls[0].parentResourceScopeId, "parent-a");
});

test("ResourceScope create rejects an unregistered scope type", async () => {
  let created = 0;
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a" }) } },
    applicationSecurity: { scopeTypes: { list: async () => [{ key: "business", displayName: "Business", canAttachToTenant: true }] } },
    accessControl: { resourceScopes: {
      create: async () => { created++; return {}; },
    } },
  } };
  await assert.rejects(
    mutations.createNextResourceScopeFromForm(session, admin, scopeWide, form({
      tenantId: "tenant-a", modelVersion: "5", scopeType: "unknown",
      externalResourceId: "BUS-1", displayName: "Business One", status: "1",
    })),
    /selected scope type is not registered/,
  );
  assert.equal(created, 0);
});

test("ResourceScope update preserves optimistic version", async () => {
  const calls = [];
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a" }) } },
    applicationSecurity: { scopeTypes: { list: async () => [{ key: "business", displayName: "Business", canAttachToTenant: true }] } },
    accessControl: { resourceScopes: {
      get: async (_ctx, id) => ({ resourceScopeId: id, status: 1 }),
      update: async (_ctx, id, request) => { calls.push([id, request]); return { resourceScopeId: id, ...request }; },
    } },
  } };
  await mutations.updateNextResourceScopeFromForm(session, admin, scopeWide, form({
    tenantId: "tenant-a", resourceScopeId: "scope-a", modelVersion: "5", scopeType: "business",
    externalResourceId: "BUS-1", displayName: "Business Two", status: "2", expectedVersion: "7",
  }));
  assert.equal(calls[0][1].expectedVersion, 7);
  assert.equal(calls[0][1].status, 2);
});

test("ResourceScope update rejects self-parenting before transport mutation", async () => {
  let updated = 0;
  const session = { client: {
    directory: { tenants: { get: async () => ({ tenantId: "tenant-a" }) } },
    applicationSecurity: { scopeTypes: { list: async () => [{ key: "business", displayName: "Business", canAttachToTenant: true }] } },
    accessControl: { resourceScopes: {
      get: async (_ctx, id) => ({ resourceScopeId: id, status: 1 }),
      update: async () => { updated++; return {}; },
    } },
  } };
  await assert.rejects(
    mutations.updateNextResourceScopeFromForm(session, admin, scopeWide, form({
      tenantId: "tenant-a", resourceScopeId: "scope-a", modelVersion: "5", scopeType: "business",
      externalResourceId: "BUS-1", displayName: "Business Two", parentResourceScopeId: "scope-a",
      status: "1", expectedVersion: "2",
    })),
    /cannot be its own parent/,
  );
  assert.equal(updated, 0);
});
