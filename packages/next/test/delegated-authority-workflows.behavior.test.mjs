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

function workspaceWith(capability) {
  return source("delegated-authority-workspace.ts", {
    "./authorization": { isAllowedOnServer: async (_session, _boundary, requirement) => capability(requirement) },
  });
}

const mutations = source("delegated-authority-mutations.ts", {
  "./administration-form": adminForm,
});

function form(values) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, String(value));
  return data;
}

test("authority catalogs are not fetched when read capabilities are absent", async () => {
  const workspace = workspaceWith(() => false);
  const session = { client: { accessControl: { delegatedAuthority: {
    listGroups: async () => { throw new Error("group read bypass"); },
    listPolicies: async () => { throw new Error("policy read bypass"); },
  } } } };
  const result = await workspace.loadNextDelegatedAuthorityWorkspace(session, admin);
  assert.equal(result.groups.length, 0);
  assert.equal(result.policies.length, 0);
  assert.equal(result.permissions.canReadGroups, false);
  assert.equal(result.permissions.canReadPolicies, false);
});

test("Super Admin authority objects, members, statements and bindings compose in one tenant-free workspace", async () => {
  const workspace = workspaceWith(() => true);
  const group = { groupId: "super-group", displayName: "Super Admin", status: 1, version: 3 };
  const policy = { policyId: "super-policy", displayName: "Super Admin Policy", status: 1, version: 4 };
  const session = { client: {
    accessControl: { delegatedAuthority: {
      listGroups: async () => [group],
      listPolicies: async () => [policy],
      getGroup: async () => group,
      getPolicy: async () => policy,
      listMembers: async () => [{ groupId: group.groupId, userId: "user-one" }],
      listPolicyStatements: async () => [{ statementId: "statement-one", modelVersion: 5, resource: "identity-access", feature: "*", action: "*" }],
      listPolicyBindings: async () => [{ groupId: group.groupId, policyId: policy.policyId }],
    } },
    directory: { users: { get: async () => ({ userId: "user-one", displayName: "Admin User", status: 1, version: 1 }) } },
  } };
  const result = await workspace.loadNextDelegatedAuthorityWorkspace(session, admin, {
    groupId: group.groupId,
    policyId: policy.policyId,
  });
  assert.equal(result.groups[0].displayName, "Super Admin");
  assert.equal(result.policies[0].displayName, "Super Admin Policy");
  assert.equal(result.members.length, 1);
  assert.equal(result.statements[0].feature, "*");
  assert.equal(result.bindings[0].policyId, "super-policy");
  assert.equal(result.memberUsers[0].displayName, "Admin User");
});

test("authority group create/update preserve lifecycle and optimistic version", async () => {
  const calls = [];
  const session = { client: { accessControl: { delegatedAuthority: {
    createGroup: async (_ctx, request) => { calls.push(["create", request]); return { groupId: "g1", version: 1, ...request }; },
    updateGroup: async (_ctx, groupId, request) => { calls.push(["update", groupId, request]); return { groupId, ...request }; },
  } } } };
  await mutations.createNextScopeAuthorityGroupFromForm(session, admin, form({ displayName: "Super Admin", status: "1" }));
  await mutations.updateNextScopeAuthorityGroupFromForm(session, admin, form({ groupId: "g1", displayName: "Super Admin 2", status: "2", expectedVersion: "7" }));
  assert.deepEqual(calls[0], ["create", { displayName: "Super Admin", status: 1 }]);
  assert.equal(calls[1][2].expectedVersion, 7);
});

test("authority policy create/update preserve lifecycle and optimistic version", async () => {
  const calls = [];
  const session = { client: { accessControl: { delegatedAuthority: {
    createPolicy: async (_ctx, request) => { calls.push(["create", request]); return { policyId: "p1", version: 1, ...request }; },
    updatePolicy: async (_ctx, policyId, request) => { calls.push(["update", policyId, request]); return { policyId, ...request }; },
  } } } };
  await mutations.createNextScopeAuthorityPolicyFromForm(session, admin, form({ displayName: "Super Policy", status: "1" }));
  await mutations.updateNextScopeAuthorityPolicyFromForm(session, admin, form({ policyId: "p1", displayName: "Super Policy 2", status: "2", expectedVersion: "8" }));
  assert.equal(calls[1][2].expectedVersion, 8);
});

test("authority member add revalidates active group and active user", async () => {
  let added = 0;
  const session = { client: {
    accessControl: { delegatedAuthority: {
      getGroup: async () => ({ groupId: "g1", status: 1 }),
      addMember: async () => { added++; return {}; },
    } },
    directory: { users: { get: async () => ({ userId: "u1", status: 2 }) } },
  } };
  await assert.rejects(
    mutations.addNextScopeAuthorityMemberFromForm(session, admin, form({ groupId: "g1", userId: "u1" })),
    /selected user is unavailable or inactive/,
  );
  assert.equal(added, 0);
});

test("authority binding add revalidates active group and policy", async () => {
  const calls = [];
  const session = { client: { accessControl: { delegatedAuthority: {
    getGroup: async () => ({ groupId: "g1", status: 1 }),
    getPolicy: async () => ({ policyId: "p1", status: 1 }),
    addPolicyBinding: async (_ctx, groupId, policyId) => { calls.push([groupId, policyId]); return { groupId, policyId }; },
  } } } };
  await mutations.addNextScopeAuthorityPolicyBindingFromForm(session, admin, form({ groupId: "g1", policyId: "p1" }));
  assert.deepEqual(calls, [["g1", "p1"]]);
});

test("authority statement add preserves typed GOLDEN capability pattern", async () => {
  const calls = [];
  const session = { client: { accessControl: { delegatedAuthority: {
    getPolicy: async () => ({ policyId: "p1", status: 1 }),
    addPolicyStatement: async (_ctx, policyId, request) => { calls.push([policyId, request]); return { statementId: "s1", ...request }; },
  } } } };
  await mutations.addNextScopeAuthorityPolicyStatementFromForm(session, admin, form({
    policyId: "p1", modelVersion: "5", resource: "identity-access", feature: "*", action: "*",
  }));
  assert.deepEqual(calls, [["p1", { modelVersion: 5, resource: "identity-access", feature: "*", action: "*" }]]);
});

test("destructive authority relationship removals require literal REMOVE", async () => {
  let removed = 0;
  const session = { client: { accessControl: { delegatedAuthority: {
    removeMember: async () => { removed++; return true; },
    removePolicyStatement: async () => { removed++; return true; },
    removePolicyBinding: async () => { removed++; return true; },
  } } } };
  await assert.rejects(
    mutations.removeNextScopeAuthorityMemberFromForm(session, admin, form({ groupId: "g1", userId: "u1", confirmation: "remove" })),
    /confirmation must be REMOVE/,
  );
  await assert.rejects(
    mutations.removeNextScopeAuthorityPolicyStatementFromForm(session, admin, form({ policyId: "p1", statementId: "s1", confirmation: "NO" })),
    /confirmation must be REMOVE/,
  );
  await assert.rejects(
    mutations.removeNextScopeAuthorityPolicyBindingFromForm(session, admin, form({ groupId: "g1", policyId: "p1", confirmation: "DELETE" })),
    /confirmation must be REMOVE/,
  );
  assert.equal(removed, 0);
});
