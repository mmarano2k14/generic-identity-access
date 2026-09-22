import test from "node:test";
import assert from "node:assert/strict";
import { createServer } from "node:http";
import { createIdentityAccessClient, IdentityAccessClientError } from "../dist/index.js";

const info = {
  service: "identity-access", apiVersion: "v1", moduleVersion: "0.29.0", stage: "configuration",
  storageProvider: "postgresql", databaseRoutingConfigured: false, storageConfigured: false,
  authenticationConfigured: false, authorizationConfigured: false,
};
const notReady = {
  ready: false,
  stage: "configuration",
  blockingCapabilities: [
    "database-routing",
    "postgresql-persistence",
    "authentication",
    "administration-authorization",
  ],
};
const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } });
const client = (transport, options = {}) => createIdentityAccessClient({ baseUrl: "https://identity.example.test/", fetch: transport, ...options });
const code = (expected) => (error) => error instanceof IdentityAccessClientError && error.code === expected;

for (const baseUrl of ["", "invalid", " http://localhost", "http://remote.example.test", "https://user:secret@example.test", "https://example.test/?token=secret", "https://example.test/#token", "file:///tmp/identity"]) {
  test(`rejects unsafe or invalid base URL: ${baseUrl}`, () => {
    assert.throws(() => createIdentityAccessClient({ baseUrl }), code("configuration"));
  });
}

test("liveness uses the configured path prefix and non-cacheable transport", async () => {
  const api = client(async (url, init) => {
    assert.equal(url, "https://identity.example.test/iam/health/live");
    assert.equal(init.cache, "no-store");
    assert.equal(init.redirect, "error");
    assert.equal(init.credentials, "omit");
    assert.equal(init.method, "GET");
    assert.ok(init.signal instanceof AbortSignal);
    assert.deepEqual(Object.keys(init.headers), ["Accept"]);
    return json({ status: "alive" });
  }, { baseUrl: "https://identity.example.test/iam" });
  assert.deepEqual(await api.liveness(), { status: "alive" });
});

test("validates and returns the service descriptor", async () => {
  assert.deepEqual(await client(async () => json(info)).info(), info);
});

test("readiness accepts a documented 503 without converting it into success", async () => {
  assert.deepEqual(await client(async () => json(notReady, 503)).readiness(), notReady);
});

test("readiness permits 200 only with ready true", async () => {
  const ready = { ready: true, stage: "future", blockingCapabilities: [] };
  assert.deepEqual(await client(async () => json(ready)).readiness(), ready);
  await assert.rejects(client(async () => json(notReady)).readiness(), code("protocol"));
});

test("readiness rejects a 503 that claims ready true", async () => {
  await assert.rejects(client(async () => json({ ...notReady, ready: true }, 503)).readiness(), code("protocol"));
});

test("invalid blocking capability values are rejected", async () => {
  await assert.rejects(client(async () => json({ ...notReady, blockingCapabilities: [42] }, 503)).readiness(), code("protocol"));
});

test("unexpected HTTP errors are distinct from authorization denial", async () => {
  await assert.rejects(client(async () => json({ secret: "must-not-escape" }, 403)).info(), (error) => {
    assert.equal(error.code, "http");
    assert.equal(error.httpStatus, 403);
    assert.ok(!JSON.stringify(error).includes("must-not-escape"));
    assert.ok(!String(error).includes("must-not-escape"));
    return true;
  });
});

test("malformed JSON is a protocol error", async () => {
  await assert.rejects(client(async () => new Response("<html>secret</html>")).info(), code("protocol"));
});

test("missing descriptor flags are rejected", async () => {
  for (const field of [
    "databaseRoutingConfigured",
    "storageConfigured",
    "authenticationConfigured",
    "authorizationConfigured",
  ]) {
    const missing = { ...info };
    delete missing[field];
    await assert.rejects(client(async () => json(missing)).info(), code("protocol"));
  }
});

test("incompatible API versions are rejected", async () => {
  await assert.rejects(client(async () => json({ ...info, apiVersion: "v99" })).info(), code("protocol"));
});

test("transport failures do not disclose underlying error details", async () => {
  await assert.rejects(client(async () => { throw new Error("secret-token-in-url"); }).info(), (error) => {
    assert.equal(error.code, "transport");
    assert.ok(!String(error).includes("secret-token-in-url"));
    assert.equal(error.cause, undefined);
    return true;
  });
});

const abortingTransport = (_url, init) => new Promise((_resolve, reject) => {
  init.signal.addEventListener("abort", () => reject(new Error("aborted")), { once: true });
});

test("request timeout aborts the transport", async () => {
  await assert.rejects(client(abortingTransport, { timeoutMs: 10 }).info(), code("timeout"));
});

test("already-cancelled request never calls the transport", async () => {
  const signal = AbortSignal.abort();
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json(info); }).info(signal), code("cancelled"));
  assert.equal(calls, 0);
});

test("caller cancellation remains separate from timeout", async () => {
  const controller = new AbortController();
  const pending = client(abortingTransport).info(controller.signal);
  controller.abort();
  await assert.rejects(pending, code("cancelled"));
});

test("requests are not automatically retried", async () => {
  let calls = 0;
  await assert.rejects(client(async () => { calls++; return json({}, 500); }).info(), code("http"));
  assert.equal(calls, 1);
});

test("concurrent clients preserve independent destinations", async () => {
  const urls = [];
  const transport = async (url) => { urls.push(url); return json(info); };
  await Promise.all([
    client(transport, { baseUrl: "https://one.example.test" }).info(),
    client(transport, { baseUrl: "https://two.example.test" }).info(),
  ]);
  assert.deepEqual(urls.sort(), ["https://one.example.test/api/v1/system/info", "https://two.example.test/api/v1/system/info"]);
});

test("invalid timeout values are rejected", () => {
  for (const timeoutMs of [0, -1, 0.5, Infinity, NaN, 120001]) {
    assert.throws(() => client(async () => json(info), { timeoutMs }), code("configuration"));
  }
});

test("native fetch interoperates with a local HTTP fixture, not a .NET server", async () => {
  const server = createServer((_request, response) => {
    response.writeHead(200, { "content-type": "application/json" });
    response.end(JSON.stringify(info));
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  try {
    const address = server.address();
    assert.ok(address && typeof address !== "string");
    const api = createIdentityAccessClient({ baseUrl: `http://127.0.0.1:${address.port}` });
    assert.deepEqual(await api.info(), info);
  } finally {
    await new Promise((resolve, reject) => server.close((error) => error ? reject(error) : resolve()));
  }
});
