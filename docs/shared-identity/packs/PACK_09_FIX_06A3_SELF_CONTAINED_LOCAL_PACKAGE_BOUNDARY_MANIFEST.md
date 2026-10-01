# Shared Identity Pack 9 Fix 06A.3 — Self-contained local package boundary

## Destination

`identity-access` repository only.

## Purpose

Make the generated local npm artifacts consumable from a separate repository without relying on source-relative paths that escape the installed package directory. The fix also advances the local Generic Identity artifact versions to `0.2.1` so npm performs a real reinstall instead of reusing stale `0.2.0` tarballs at the same file path.

## Added

- `docs/shared-identity/packs/PACK_09_FIX_06A3_SELF_CONTAINED_LOCAL_PACKAGE_BOUNDARY_MANIFEST.md`

## Modified

- `packages/contracts/src/administration.ts`
- `packages/contracts/src/authorization.ts`
- `packages/contracts/src/errors.ts`
- `packages/contracts/src/identity.ts`
- `packages/contracts/src/mfa.ts`
- `packages/contracts/src/policies.ts`
- `packages/contracts/src/security-manifest.ts`
- `packages/contracts/src/session.ts`
- `packages/contracts/tsconfig.json`
- `packages/contracts/package.json`
- `packages/auth/package.json`
- `packages/react/package.json`
- `packages/next/package.json`
- `scripts/shared-identity/build-local-consumer-packages.mjs`
- `scripts/shared-identity/verify-pack-02-contracts.ps1`
- `scripts/shared-identity/verify-pack-09-local-package-artifacts.ps1`
- `CHANGELOG.md`

## Moved

NONE.

## Deleted

NONE.

## Database migrations

NONE.

## Runtime behavior changes

NONE. This is a package-boundary and local-artifact packaging fix only.

## Expected local artifacts

- `identity-access-client-0.26.0.tgz`
- `generic-identity-contracts-0.2.1.tgz`
- `generic-identity-auth-0.2.1.tgz`
- `generic-identity-react-0.2.1.tgz`
- `generic-identity-next-0.2.1.tgz`
