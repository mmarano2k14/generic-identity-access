# Pack 1 — Baseline and Structure Gates

## Purpose

Freeze the existing GREEN repository layout before public package extraction starts.

This pack is intentionally additive except for one verification-entry-point modification.
It introduces no runtime behavior, no database change and no public API replacement.

## Added

```text
docs/shared-identity/SHARED_IDENTITY_EXTRACTION_BASELINE.md
docs/shared-identity/packs/PACK_01_BASELINE_STRUCTURE_MANIFEST.md

packages/README.md
packages/contracts/README.md
packages/auth/README.md
packages/react/README.md
packages/next/README.md

scripts/shared-identity/verify.ps1
scripts/shared-identity/verify-pack-01-structure.ps1
```

## Modified

```text
scripts/verify.ps1
```

Modification purpose:

- invoke the new shared-identity structure gate as part of the existing repository verification;
- preserve every existing verification step and its ordering otherwise.

## Moved

```text
NONE
```

## Deleted

```text
NONE
```

## Cleanup required after ZIP overlay

```text
NONE
```

No cleanup script is required because this pack moves and deletes no files.

## Database migrations

```text
NONE
```

## Runtime changes

```text
NONE
```

The new `packages/*` directories contain documentation-only boundary reservations. They intentionally contain no `package.json`, `src/`, compiled output or runtime implementation.

## Protected baseline

This pack deliberately leaves the following existing implementation locations authoritative:

```text
clients/typescript
examples/nextjs/admin
src/IdentityAccess.*
src/OrganizationDirectory.*
src/OrganisationProfile.*
```

## Validation

Run the complete existing gate:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected additional Pack 1 output:

```text
Shared Identity Pack 1 structure validation: GREEN
Shared Identity integration validation: GREEN
```

The complete repository verification must remain GREEN.

## Rollback

Because there are no moved or deleted files, rollback consists only of removing the added Pack 1 files/directories and reverting the small `scripts/verify.ps1` integration call.
