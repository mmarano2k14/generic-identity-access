import assert from "node:assert/strict";
import { createIdentityAccessClient } from "../dist/index.js";

const api = createIdentityAccessClient({
  baseUrl: process.argv[2] ?? "http://127.0.0.1:5080",
});

const [live, ready, info] = await Promise.all([
  api.liveness(),
  api.readiness(),
  api.info(),
]);

assert.equal(live.status, "alive");
assert.equal(ready.ready, false);
assert.equal(ready.stage, "configuration");
assert.equal(info.service, "identity-access");
assert.equal(info.apiVersion, "v1");
assert.equal(info.databaseRoutingConfigured, false);
assert.equal(info.storageConfigured, false);
assert.equal(info.authenticationConfigured, false);
assert.equal(info.authorizationConfigured, false);

console.log(
  "PASS: diagnostic contract verified against the running API. Readiness remains false.",
);
