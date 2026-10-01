# Shared Identity Integration - Pack 7 Fix 02 - Next declaration typecheck

## Purpose

Keep the standalone `@generic-identity/next` source typecheck focused on this package while consuming Next.js public types. Next.js declaration files under `node_modules` are third-party inputs and are not re-typechecked by this repository.

## ADDED

- `docs/shared-identity/packs/PACK_07_FIX_02_NEXT_DECLARATION_TYPECHECK_MANIFEST.md`

## MODIFIED

- `packages/next/tsconfig.json`
- `scripts/shared-identity/verify-pack-07-next.ps1`
- `CHANGELOG.md`

## MOVED

- NONE

## DELETED

- NONE

## DELETE AFTER VALIDATION

- NONE

## DATABASE MIGRATIONS

- NONE

## RUNTIME / BACKEND CHANGES

- NONE

## Compatibility decision

- `skipLibCheck: true` is used only to skip semantic checking of dependency declaration files.
- Generic Identity `.ts` / `.tsx` source remains covered by strict TypeScript checking.
- Existing ESNext + Bundler resolution remains unchanged.

## Validation

Run from the repository root:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Pack 7 gates:

```text
Shared Identity Pack 7 Next.js integration source validation: GREEN
Shared Identity Pack 7 Next.js integration typecheck: GREEN
```
