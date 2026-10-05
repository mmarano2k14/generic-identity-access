# Next.js Integration manifest - Next.js integration package

## ADDED

- `packages/next/package.json`
- `packages/next/tsconfig.json`
- `packages/next/src/index.ts`
- `packages/next/src/client/NextIdentityProvider.tsx`
- `packages/next/src/client/index.ts`
- `packages/next/src/pages/index.ts`
- `packages/next/src/routing/routes.ts`
- `packages/next/src/routing/index.ts`
- `packages/next/src/server/config.ts`
- `packages/next/src/server/session.ts`
- `packages/next/src/server/authorization.ts`
- `packages/next/src/server/protected-page.ts`
- `packages/next/src/server/index.ts`
- `packages/next/test/consumer-next.tsx`
- `packages/next/test/consumer-server.ts`
- `docs/shared-identity/NEXTJS_INTEGRATION.md`
- `docs/shared-identity/validation-manifests/NEXTJS_INTEGRATION.md`
- `scripts/shared-identity/verify-delivery-07-next.ps1`

## MODIFIED

- `packages/next/README.md`
- `packages/README.md`
- `scripts/shared-identity/verify-delivery-01-structure.ps1`
- `scripts/shared-identity/verify.ps1`
- `scripts/verify.ps1`
- `CHANGELOG.md`

## MOVED

NONE

## DELETED

NONE

## DELETE AFTER VALIDATION

NONE IN Next.js Integration.

The existing `examples/nextjs/admin` implementation is intentionally retained. Its route adapters and local integration helpers become cleanup candidates only after the dedicated consumer-integration delivery proves the shared Next package in the real host.

## DATABASE MIGRATIONS

NONE

## BACKEND / RUNTIME CHANGES

NONE

## BREAKING CHANGES

NONE. `@generic-identity/next` is additive and remains private during extraction.

## VALIDATION

Run:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Next.js Integration must keep the previously qualified Shared Identity milestones GREEN and additionally report the Next.js Integration source and typecheck gates GREEN.
