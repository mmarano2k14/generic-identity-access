# Shared Identity Public Contracts — Public Contracts Manifest

## Scope

Activate the first shared package boundary, `@generic-identity/contracts`, as a type-only facade over proven existing contract declarations.

This delivery performs no runtime behavior change.

## ADDED

```text
packages/contracts/package.json
packages/contracts/tsconfig.json
packages/contracts/src/administration.ts
packages/contracts/src/authorization.ts
packages/contracts/src/errors.ts
packages/contracts/src/identity.ts
packages/contracts/src/index.ts
packages/contracts/src/mfa.ts
packages/contracts/src/policies.ts
packages/contracts/src/security-manifest.ts
packages/contracts/src/session.ts
packages/contracts/test/consumer-contracts.ts
scripts/shared-identity/verify-delivery-02-contracts.ps1
docs/shared-identity/PUBLIC_CONTRACTS.md
docs/shared-identity/validation-manifests/PUBLIC_CONTRACTS.md
```

## MODIFIED

```text
packages/README.md
packages/contracts/README.md
scripts/shared-identity/verify-delivery-01-structure.ps1
scripts/shared-identity/verify.ps1
scripts/verify.ps1
```

## MOVED

```text
NONE
```

## DELETED

```text
NONE
```

## DELETE AFTER VALIDATION

```text
NONE
```

No cleanup script is required for Public Contracts because nothing is moved or deleted.

## DATABASE MIGRATIONS

```text
NONE
```

## RUNTIME / SERVER CHANGES

```text
NONE
```

## EXISTING PUBLIC SURFACES REPLACED

```text
NONE
```

The existing package `@identity-access/client` remains authoritative for runtime behavior and keeps its current source location.

## NEW PUBLIC BOUNDARY

```text
@generic-identity/contracts
```

Public Contracts keeps this package `private: true`. Publication/versioning is deferred to the release delivery.

## SECURITY EXCLUSIONS

The package must not expose access/session credentials, password-bearing requests, token sets, server storage internals, Generic Organization Directory contracts or OrganisationProfile contracts.

## POST-APPLY VALIDATION

From the repository root:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Public Contracts markers include:

```text
Shared Identity Baseline and Structure structure validation: GREEN
Shared Identity public contracts source validation: GREEN
Shared Identity integration validation: GREEN
Shared Identity public contracts typecheck: GREEN
```

The complete pre-existing verification must also remain GREEN.
