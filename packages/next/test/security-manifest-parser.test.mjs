import assert from "node:assert/strict";
import { test } from "node:test";
import { parseApplicationSecurityManifestFileCore } from "../src/internal/security-manifest-parser-core.ts";

// Test the same validation core used by the Next.js server-only adapter.
// Node's test runner must not import `server-only`, which is a Next.js boundary.
const parseNextApplicationSecurityManifestFile = (formData) =>
  parseApplicationSecurityManifestFileCore(formData, 262_144);

function manifest(changes = {}) {
  return {
    schemaVersion: 1,
    applicationKey: "sample-app",
    modelVersion: 7,
    rbac: { project: "magellan", namespaces: ["admin"] },
    resources: [
      {
        name: "sample-app",
        features: [{ name: "console", actions: [{ name: "read", displayName: "Read console" }] }],
      },
    ],
    ...changes,
  };
}

function upload(value, filename = "model.json") {
  const data = new FormData();
  data.set("manifestFile", new File([JSON.stringify(value)], filename, { type: "application/json" }));
  return data;
}

test("accepts a valid project-authored manifest, retains a typed projection", async () => {
  const parsed = await parseNextApplicationSecurityManifestFile(upload(manifest()));
  assert.equal(parsed.applicationKey, "sample-app");
  assert.equal(parsed.modelVersion, 7);
  assert.equal(parsed.rbac.project, "magellan");
  assert.deepEqual(parsed.rbac.namespaces, ["admin"]);
  assert.equal(parsed.resources[0].features[0].actions[0].displayName, "Read console");
});

test("rejects omitted file and a non-.json extension", async () => {
  await assert.rejects(parseNextApplicationSecurityManifestFile(new FormData()), /file is required/);
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest(), "model.txt")), /\.json extension/);
});

test("rejects empty or over-limit files before parsing JSON", async () => {
  const empty = new FormData();
  empty.set("manifestFile", new File([], "model.json"));
  await assert.rejects(parseNextApplicationSecurityManifestFile(empty), /file is empty/);
  const large = new FormData();
  large.set("manifestFile", new File([new Uint8Array(262_145)], "model.json"));
  await assert.rejects(parseNextApplicationSecurityManifestFile(large), /256 KiB/);
});

test("rejects invalid JSON", async () => {
  const invalid = new FormData();
  invalid.set("manifestFile", new File(["not valid json"], "model.json"));
  await assert.rejects(parseNextApplicationSecurityManifestFile(invalid), /not valid JSON/);
});

test("rejects wrong schema version, invalid model version and empty resources", async () => {
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest({ schemaVersion: 2 }))), /schemaVersion must be 1/);
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest({ modelVersion: 0 }))), /modelVersion must be a positive integer/);
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest({ resources: [] }))), /at least one resource/);
});

test("rejects duplicate namespaces and forbidden RBAC context segments", async () => {
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest({ rbac: { project: "magellan", namespaces: ["admin", "admin"] } }))), /duplicates/);
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(manifest({ rbac: { project: "wrong:project", namespaces: ["admin"] } }))), /RBAC context segment/);
});

test("rejects duplicate capability definitions and untrimmed action labels", async () => {
  const duplicate = manifest();
  duplicate.resources[0].features[0].actions.push({ name: "read", displayName: "Read again" });
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(duplicate)), /duplicate capability/);

  const invalidLabel = manifest();
  invalidLabel.resources[0].features[0].actions[0].displayName = " leading space";
  await assert.rejects(parseNextApplicationSecurityManifestFile(upload(invalidLabel)), /printable trimmed characters/);
});
