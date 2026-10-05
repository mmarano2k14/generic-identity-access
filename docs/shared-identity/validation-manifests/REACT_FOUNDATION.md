# delivery 04 — React Foundation Manifest

## Scope

Activate the reusable React-only Generic Identity foundation over the Public Contracts and Authentication SDK milestones while preserving the current administration host unchanged.

## ADDED

```text
packages/react/package.json
packages/react/tsconfig.json
packages/react/src/internal/IdentityContext.ts
packages/react/src/providers/IdentityProvider.tsx
packages/react/src/providers/index.ts
packages/react/src/hooks/useIdentityContextValue.ts
packages/react/src/hooks/useIdentityClient.ts
packages/react/src/hooks/useAuthorization.ts
packages/react/src/hooks/useCapability.ts
packages/react/src/hooks/index.ts
packages/react/src/authorization/RequireCapability.tsx
packages/react/src/authorization/index.ts
packages/react/src/index.ts
packages/react/test/consumer-react.tsx
docs/shared-identity/REACT_FOUNDATION.md
docs/shared-identity/validation-manifests/REACT_FOUNDATION.md
scripts/shared-identity/verify-delivery-04-react.ps1
```

## MODIFIED

```text
packages/README.md
packages/react/README.md
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

The existing Next.js administration components remain authoritative. A future extraction delivery must list every source relocation explicitly before any cleanup.

## DATABASE MIGRATIONS

```text
NONE
```

## BACKEND / RUNTIME CHANGES

```text
NONE
```

## EXISTING HOST INTEGRATION

```text
NONE
```

The existing host is deliberately not redirected to `@generic-identity/react` in this delivery.

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected new markers include:

```text
Shared Identity React Foundation React foundation source validation: GREEN
Shared Identity React Foundation React foundation typecheck: GREEN
```
