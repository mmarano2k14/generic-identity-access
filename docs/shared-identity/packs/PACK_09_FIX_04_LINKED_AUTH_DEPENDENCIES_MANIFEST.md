# Shared Identity Pack 9 Fix 04 — Linked auth development dependencies

**DESTINATION:** `identity-access` repository root only.

## Purpose

Prepare the source-linked `@generic-identity/auth` package for external Next.js consumers. During Packs 8-9 the package is consumed through a local `file:` link and still exports TypeScript source. TypeScript/Turbopack therefore resolve imports from the real `packages/auth` source path, where the package's declared local dependencies must be available.

## ADDED

- `docs/shared-identity/packs/PACK_09_FIX_04_LINKED_AUTH_DEPENDENCIES_MANIFEST.md`

## MODIFIED

- `scripts/verify.ps1`
- `CHANGELOG.md`

## MOVED

NONE

## DELETED

NONE

## CLEANUP

NONE

## DATABASE MIGRATIONS

NONE

## Runtime/security behavior

NONE. The patch only restores local development dependencies for the existing source-linked auth facade after the legacy TypeScript client has been built. It does not change authentication, authorization, RBAC, sessions, MFA, routes, or database behavior.

## Validation

Run from the `identity-access` repository root:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

The verification now ensures these linked-development dependencies exist under `packages/auth/node_modules`:

- `@identity-access/client`
- `@generic-identity/contracts`

Then rerun the MAGELLAN verification from its repository root.
