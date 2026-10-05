# Shared Identity Authentication and Authorization SDK — Authentication and Authorization SDK Manifest

## Scope

Activate `@generic-identity/auth` as a framework-neutral facade over the proven authentication and server-backed authorization behavior.

## ADDED

```text
packages/auth/package.json
packages/auth/tsconfig.json
packages/auth/src/authentication.ts
packages/auth/src/authorization.ts
packages/auth/src/authorization-context.ts
packages/auth/src/client.ts
packages/auth/src/errors.ts
packages/auth/src/index.ts
packages/auth/test/consumer-auth.ts
scripts/shared-identity/verify-delivery-03-auth.ps1
docs/shared-identity/AUTHORIZATION_SDK.md
docs/shared-identity/validation-manifests/AUTHORIZATION_SDK.md
```

## MODIFIED

```text
clients/typescript/src/index.ts
packages/README.md
packages/auth/README.md
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

No cleanup script is required for Authentication and Authorization SDK because nothing is moved or deleted.

## DATABASE MIGRATIONS

```text
NONE
```

## RUNTIME / SERVER CHANGES

```text
NONE
```

No ASP.NET Core, PostgreSQL, Redis, RBAC or MFA server implementation changes are included.

## EXISTING PUBLIC SURFACES REPLACED

```text
NONE
```

The existing package `@identity-access/client` remains authoritative for runtime behavior and existing consumers remain on it.

## NEW PUBLIC BOUNDARY

```text
@generic-identity/auth
```

Authentication and Authorization SDK keeps this package `private: true`. Publication/versioning is deferred to the release delivery.

## TEMPORARY EXTRACTION DEPENDENCY

```text
@generic-identity/auth
    -> @generic-identity/contracts
    -> @identity-access/client   temporary runtime bridge
```

The temporary bridge may be removed only by a later explicit migration that lists every `MOVED` and `DELETED` file and verifies the replacement before cleanup.

## EXISTING SOURCE EXPANSION

`clients/typescript/src/index.ts` adds type exports for declarations that already existed in `clients/typescript/src/contracts.ts`:

```text
IdentityRecoveryPasswordResetRequest
IdentitySelfServicePasswordChangeRequest
```

No implementation is changed.

## POST-APPLY VALIDATION

From the repository root:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Authentication and Authorization SDK markers include:

```text
Shared Identity Baseline and Structure structure validation: GREEN
Shared Identity public contracts source validation: GREEN
Shared Identity authentication and authorization SDK source validation: GREEN
Shared Identity integration validation: GREEN
Shared Identity public contracts typecheck: GREEN
Shared Identity authentication and authorization SDK typecheck: GREEN
```

The complete pre-existing verification must also remain GREEN.
