# Identity & Access TypeScript client

Version 0.1.0 is an initial diagnostic client, not an authentication or authorization SDK.
No PostgreSQL, Redis or .NET engine internals are exposed. The package name is local and
unpublished; install from the generated local archive, not from an assumed registry entry.

```sh
npm install
npm test
npm pack
```

Runtime code has no third-party dependencies. The compiler is a development dependency.

```typescript
import { createIdentityAccessClient } from "@identity-access/client";

const api = createIdentityAccessClient({
  baseUrl: "http://127.0.0.1:5080", // Trusted server configuration, not request input.
  timeoutMs: 10000,
});

const info = await api.info();
const live = await api.liveness();
const readiness = await api.readiness(); // Expected ready: false / HTTP 503 in this increment.
```

`info`, `liveness` and `readiness` accept an optional AbortSignal. Each operation owns its
AbortController and timeout; there is no global mutable current-user/session state.
Errors distinguish configuration, HTTP, protocol, timeout, cancellation and transport.
HTTP failures are not turned into business authorization denials. Raw error bodies and
transport error messages are not copied into public errors.

HTTPS is required except for exact loopback hosts over HTTP. Base URL credentials, query
parameters and fragments are rejected. Redirects are rejected, cookies omitted and cache
storage disabled. There is no automatic retry. Injected transports must honor the provided
AbortSignal and redirect policy. The generic client does not itself authenticate an API.

The unit suite uses injected fixtures and one native HTTP fixture served by Node.
After starting the .NET API, run the separate live-contract smoke check:

```sh
npm run smoke -- http://127.0.0.1:5080
```

The Next.js server integration is an example in `examples/nextjs` at repository root.
It does not include login, session storage, Server Actions guards, MFA or access-context rotation.
