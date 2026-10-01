# Shared Identity Integration — Pack 10 — Release packaging and versioning

## Scope

Freeze the first stable Shared Identity package release boundary after Packs 1–9 are GREEN.

## ADDED

- `scripts/shared-identity/qualify-release-packages.mjs`
- `scripts/shared-identity/verify-pack-10-release.ps1`
- `docs/shared-identity/PACK_10_RELEASE_QUALIFICATION.md`
- `docs/shared-identity/packs/PACK_10_RELEASE_QUALIFICATION_MANIFEST.md`

## MODIFIED

- `packages/contracts/package.json`
- `packages/auth/package.json`
- `packages/react/package.json`
- `packages/next/package.json`
- `scripts/shared-identity/build-local-consumer-packages.mjs`
- `scripts/shared-identity/verify.ps1`
- `CHANGELOG.md`

## MOVED

NONE.

## DELETED

NONE.

## DELETE AFTER VALIDATION

NONE.

## DATABASE MIGRATIONS

NONE.

## Release contract

- `@generic-identity/contracts` — `1.0.0`
- `@generic-identity/auth` — `1.0.0`
- `@generic-identity/react` — `1.0.0`
- `@generic-identity/next` — `1.0.0`
- `@identity-access/client` remains the transitional transport dependency at `0.26.0`.

Repository package manifests remain `private: true` to prevent accidental direct publication. The artifact builder stages publishable copies with `private: false`, `publishConfig.access=public`, exact version dependencies, checksums, and no `file:`/`link:`/`workspace:` dependency leakage.

Pack 10 qualifies publication artifacts but does not publish to any registry.
