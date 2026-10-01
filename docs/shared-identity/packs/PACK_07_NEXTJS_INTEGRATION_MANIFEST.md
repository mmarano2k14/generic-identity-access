# Pack 7 manifest - Next.js integration package

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
- `docs/shared-identity/PACK_07_NEXTJS_INTEGRATION.md`
- `docs/shared-identity/packs/PACK_07_NEXTJS_INTEGRATION_MANIFEST.md`
- `scripts/shared-identity/verify-pack-07-next.ps1`

## MODIFIED

- `packages/next/README.md`
- `packages/README.md`
- `scripts/shared-identity/verify-pack-01-structure.ps1`
- `scripts/shared-identity/verify.ps1`
- `scripts/verify.ps1`
- `CHANGELOG.md`

## MOVED

NONE

## DELETED

NONE

## DELETE AFTER VALIDATION

NONE IN PACK 7.

The existing `examples/nextjs/admin` implementation is intentionally retained. Its route adapters and local integration helpers become cleanup candidates only after the dedicated consumer-integration pack proves the shared Next package in the real host.

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

Pack 7 must keep Packs 1-6 GREEN and additionally report the Pack 7 source and typecheck gates GREEN.
