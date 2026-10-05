# Shared Identity Next.js Integration - Next.js integration package

Next.js Integration activates `@generic-identity/next` as the reusable Next.js-specific integration layer.

## Scope

- explicit Next.js client boundary over the shared React provider;
- server-only opaque-session cookie coordination;
- server-side session validation and authenticated-page helper;
- server-backed capability checks with DENY kept separate from technical failure;
- application-owned route-map helpers;
- shared Identity page re-exports;
- strict React/Next peer dependency boundaries.

## Non-goals

Next.js Integration does not migrate the existing administration host. It does not remove the legacy TypeScript client, copy application routes, introduce a second RBAC, expose session credentials to browser code, or add database migrations.

The existing host remains the proven integration harness until the consumer-integration milestone.
