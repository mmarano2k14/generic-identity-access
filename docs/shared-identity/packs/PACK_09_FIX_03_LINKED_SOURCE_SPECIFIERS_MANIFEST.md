# Shared Identity Integration - Pack 9 Fix 03 - Linked raw-source module specifiers

**DESTINATION:** `D:\Dev\Personal\identity-access`

## Purpose

Fix runtime Turbopack resolution of the pre-publication linked TypeScript packages. The packages are currently consumed from `src/` directly. Relative specifiers such as `./administration.js` refer to emitted files that do not exist yet, so the consumer bundler cannot resolve them.

## MODIFIED

- `packages/contracts/tsconfig.json`
- `packages/contracts/src/index.ts`
- `packages/auth/tsconfig.json`
- shared source files under `packages/auth/src/` containing relative `.js` specifiers
- `packages/react/tsconfig.json`
- shared source files under `packages/react/src/` containing relative `.js` specifiers
- shared source files under `packages/next/src/` containing relative `.js` specifiers
- `scripts/shared-identity/verify-pack-02-contracts.ps1`
- `scripts/shared-identity/verify-pack-03-auth.ps1`
- `scripts/shared-identity/verify-pack-04-react.ps1`
- `scripts/shared-identity/verify-pack-05-pages.ps1`
- `scripts/shared-identity/verify-pack-07-closure.ps1`
- `CHANGELOG.md`

## ADDED

- this manifest

## MOVED

NONE

## DELETED

NONE

## CLEANUP

NONE

## DATABASE MIGRATIONS

NONE

## Runtime semantics

No authentication, authorization, RBAC, session, administration or API semantics change. This patch changes only source-module resolution for linked pre-publication packages.

## Validation

Run from `identity-access`:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Then restart MAGELLAN `npm run dev` and retry the anonymous Pack 9 authorization request.
