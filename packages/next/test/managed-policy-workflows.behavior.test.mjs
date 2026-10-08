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

const admin = { identityScopeId: "scope-one", applicationKey: "consumer-app", credential: { kind: "session" } };
const allowAll = async () => true;
const workspace = source("managed-policy-workspace.ts", {
  "./authorization": { isAllowedOnServer: allowAll },
});
const mutations = source("managed-policy-mutations.ts", {
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

test("managed policy catalog is not fetched without policy/read", async () => {
  const deniedWorkspace = source("managed-policy-workspace.ts", {
    "./authorization": {
      isAllowedOnServer: async (_session, _boundary, requirement) => !(requirement.feature === "policy" && requirement.action === "read"),
    },
  });
  const session = { client: { accessControl: { managedPolicies: { list: async () => { throw new Error("policy read bypass"); } } } } };
  const result = await deniedWorkspace.loadNextManagedPolicyWorkspace(session, admin);
  assert.equal(result.permissions.canReadPolicies, false);
  assert.equal(result.policies.length, 0);
});

test("workspace selects default version, statements, and registered capability catalog", async () => {
  const session = { client: {
    accessControl: { managedPolicies: {
      list: async () => [{ policyId: "policy-a", policyKey: "ops-read", displayName: "Ops read", status: 1, defaultVersion: 2, version: 3 }],
      listVersions: async () => [
        { policyId: "policy-a", policyVersion: 1, modelVersion: 4, publishedAt: "2026-01-01T00:00:00Z" },
        { policyId: "policy-a", policyVersion: 2, modelVersion: 5, publishedAt: "2026-02-01T00:00:00Z" },
      ],
      listStatements: async () => [{ statementId: "statement-a", policyVersion: 2, modelVersion: 5, resource: "runtime", feature: "read", action: "read" }],
    } },
    applicationSecurity: { models: {
      list: async () => [{ modelVersion: 5 }],
      get: async () => ({ modelVersion: 5, rbacProject: "magellan", rbacNamespaces: ["consumer-app"], manifestSha256: "abc", capabilities: [{ displayName: "Runtime read", resource: "runtime", feature: "read", action: "read" }] }),
    } },
  } };
  const result = await workspace.loadNextManagedPolicyWorkspace(session, admin, { policyId: "policy-a" });
  assert.equal(result.selectedVersion.policyVersion, 2);
  assert.equal(result.statements.length, 1);
  assert.equal(result.policyBuilderModels[0].capabilities[0].value, "5|runtime|read|read");
});

test("create and update preserve GOLDEN policy bounds and optimistic version", async () => {
  const calls = [];
  const session = { client: { accessControl: { managedPolicies: {
    create: async (_ctx, request) => { calls.push(["create", request]); return { policyId: "policy-a", version: 1, ...request }; },
    update: async (_ctx, id, request) => { calls.push(["update", id, request]); return { policyId: id, version: 8, ...request }; },
  } } } };
  await mutations.createNextManagedPolicyFromForm(session, admin, form({ policyKey: "ops-read", displayName: "Ops read", status: "1" }));
  await mutations.updateNextManagedPolicyFromForm(session, admin, form({ policyId: "policy-a", policyKey: "ops-read", displayName: "Ops read 2", status: "2", expectedVersion: "7" }));
  assert.deepEqual(calls[0], ["create", { policyKey: "ops-read", displayName: "Ops read", status: 1 }]);
  assert.equal(calls[1][2].expectedVersion, 7);
  await assert.rejects(
    mutations.createNextManagedPolicyFromForm(session, admin, form({ policyKey: "BAD KEY", displayName: "Bad", status: "1" })),
    /policyKey must use lower-case/,
  );
});

test("new draft version revalidates registered security model", async () => {
  let created = 0;
  const session = { client: {
    applicationSecurity: { models: { get: async () => null } },
    accessControl: { managedPolicies: { createVersion: async () => { created++; return {}; } } },
  } };
  await assert.rejects(
    mutations.createNextManagedPolicyVersionFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "3", modelVersion: "99" })),
    /security model was not found/,
  );
  assert.equal(created, 0);
});

test("statement add rejects published versions and mismatched security-model capability", async () => {
  let added = 0;
  const session = { client: { accessControl: { managedPolicies: {
    getVersion: async (_ctx, _id, version) => version === 1
      ? { policyVersion: 1, modelVersion: 5, publishedAt: "2026-01-01T00:00:00Z" }
      : { policyVersion: 2, modelVersion: 5 },
    addStatement: async () => { added++; return {}; },
  } } } };
  await assert.rejects(
    mutations.addNextManagedPolicyStatementFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "1", capability: "5|runtime|read|read" })),
    /published policy versions are immutable/,
  );
  await assert.rejects(
    mutations.addNextManagedPolicyStatementFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "2", capability: "6|runtime|read|read" })),
    /does not belong to the policy version security model/,
  );
  assert.equal(added, 0);
});

test("statement add uses selected registered capability coordinates", async () => {
  const calls = [];
  const session = { client: { accessControl: { managedPolicies: {
    getVersion: async () => ({ policyVersion: 2, modelVersion: 5 }),
    addStatement: async (_ctx, id, version, request) => { calls.push([id, version, request]); return { statementId: "s1", policyVersion: version, modelVersion: 5, ...request }; },
  } } } };
  await mutations.addNextManagedPolicyStatementFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "2", capability: "5|runtime|executions|read" }));
  assert.deepEqual(calls, [["policy-a", 2, { resource: "runtime", feature: "executions", action: "read" }]]);
});

test("publish forwards makeDefault exactly", async () => {
  const calls = [];
  const session = { client: { accessControl: { managedPolicies: {
    publishVersion: async (_ctx, id, version, request) => { calls.push([id, version, request]); return { policyId: id, policyVersion: version, modelVersion: 5, publishedAt: "now" }; },
  } } } };
  await mutations.publishNextManagedPolicyVersionFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "2", makeDefault: "true" }));
  assert.deepEqual(calls, [["policy-a", 2, { makeDefault: true }]]);
});

test("statement removal requires literal REMOVE", async () => {
  let removed = 0;
  const session = { client: { accessControl: { managedPolicies: { removeStatement: async () => { removed++; return true; } } } } };
  await assert.rejects(
    mutations.removeNextManagedPolicyStatementFromForm(session, admin, form({ policyId: "policy-a", policyVersion: "2", statementId: "statement-a", confirmation: "remove" })),
    /confirmation must be REMOVE/,
  );
  assert.equal(removed, 0);
});
