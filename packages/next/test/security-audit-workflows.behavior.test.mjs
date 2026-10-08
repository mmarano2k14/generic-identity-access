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

function workspaceWith(capability) {
  return source("security-audit-workspace.ts", {
    "./authorization": {
      isAllowedOnServer: async (_session, _boundary, requirement) => capability(requirement),
    },
  });
}

test("Security Audit transport is not called without security-audit/read", async () => {
  const workspace = workspaceWith(() => false);
  const session = { client: { security: { audit: {
    list: async () => { throw new Error("audit read bypass"); },
  } } } };
  const result = await workspace.loadNextSecurityAuditWorkspace(session, admin, effective);
  assert.equal(result.records.length, 0);
  assert.equal(result.permissions.canReadAudit, false);
});

test("invalid UUID filter prevents audit transport", async () => {
  const workspace = workspaceWith((requirement) =>
    requirement.feature === "security-audit" && requirement.action === "read");
  let calls = 0;
  const session = { client: { security: { audit: {
    list: async () => { calls++; return []; },
  } } } };
  const result = await workspace.loadNextSecurityAuditWorkspace(
    session, admin, effective, { userId: "not-a-uuid" },
  );
  assert.match(result.query.validationMessage, /User ID must be a UUID/);
  assert.equal(calls, 0);
});

test("bounded exact filters are normalized and forwarded", async () => {
  const workspace = workspaceWith(() => true);
  const calls = [];
  const session = { client: { security: { audit: {
    list: async (_context, query) => {
      calls.push(query);
      return [{
        eventId: "00000000-0000-0000-0000-000000000010",
        occurredAt: "2026-10-07T00:00:00Z",
        eventType: "UserLoginSucceeded",
        outcome: "Succeeded",
        identityScopeId: admin.identityScopeId,
      }];
    },
  } } } };

  const result = await workspace.loadNextSecurityAuditWorkspace(session, admin, effective, {
    tenantId: "00000000-0000-0000-0000-000000000002",
    userId: "00000000-0000-0000-0000-000000000003",
    outcome: "Succeeded",
    correlationId: "corr-1",
    limit: "100",
  });

  assert.deepEqual(calls[0], {
    tenantId: "00000000-0000-0000-0000-000000000002",
    userId: "00000000-0000-0000-0000-000000000003",
    outcome: "Succeeded",
    correlationId: "corr-1",
    limit: 100,
  });
  assert.equal(result.summary.total, 1);
  assert.equal(result.summary.succeeded, 1);
});

test("invalid outcome and overlong correlation are rejected", () => {
  const workspace = workspaceWith(() => true);
  const invalidOutcome = workspace.normalizeNextSecurityAuditQuery({ outcome: "Maybe" });
  assert.match(invalidOutcome.validationMessage, /Outcome filter is invalid/);

  const invalidCorrelation = workspace.normalizeNextSecurityAuditQuery({
    correlationId: "x".repeat(65),
  });
  assert.match(invalidCorrelation.validationMessage, /must not exceed 64/);
});

test("window is bounded to GOLDEN values with default 50", () => {
  const workspace = workspaceWith(() => true);
  assert.equal(workspace.normalizeNextSecurityAuditQuery({ limit: "25" }).limit, 25);
  assert.equal(workspace.normalizeNextSecurityAuditQuery({ limit: "200" }).limit, 200);
  assert.equal(workspace.normalizeNextSecurityAuditQuery({ limit: "999" }).limit, 50);
});

test("summary separates succeeded denied and failed", () => {
  const workspace = workspaceWith(() => true);
  const records = [
    { outcome: "Succeeded" },
    { outcome: "Succeeded" },
    { outcome: "Denied" },
    { outcome: "Failed" },
  ];
  assert.deepEqual(workspace.summarizeSecurityAudit(records), {
    total: 4,
    succeeded: 2,
    denied: 1,
    failed: 1,
  });
});

test("user and tenant lookup controls require scope-wide visibility plus read capability", async () => {
  const workspace = workspaceWith((requirement) =>
    (requirement.feature === "security-audit" && requirement.action === "read")
    || (requirement.feature === "user" && requirement.action === "read")
    || (requirement.feature === "tenant" && requirement.action === "read"));
  const session = { client: { security: { audit: { list: async () => [] } } } };

  const scopeWide = await workspace.loadNextSecurityAuditWorkspace(session, admin, effective);
  assert.equal(scopeWide.permissions.scopeWide, true);
  assert.equal(scopeWide.permissions.canReadUsers, true);
  assert.equal(scopeWide.permissions.canReadTenants, true);

  const membershipLimited = await workspace.loadNextSecurityAuditWorkspace(session, admin, {
    ...effective,
    tenantVisibility: "membership-limited",
  });
  assert.equal(membershipLimited.permissions.scopeWide, false);
});
