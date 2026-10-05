# Organizations - Organizations SDK/UI - Manifest

## ADDED

```text
packages/contracts/src/organizations/index.ts
packages/auth/src/organizations.ts
packages/auth/test/consumer-organizations.ts
packages/react/src/organizations/OrganizationForm.tsx
packages/react/src/organizations/OrganizationMembershipForm.tsx
packages/react/src/organizations/OrganizationResourceScopeLinkForm.tsx
packages/react/src/organizations/index.ts
packages/react/src/pages/OrganizationsPage.tsx
packages/react/src/pages/OrganizationDetailsPage.tsx
packages/react/src/pages/OrganizationMembershipsPage.tsx
packages/react/src/pages/OrganizationTreePage.tsx
packages/react/test/consumer-organizations.tsx
packages/next/src/organizations/index.ts
packages/next/test/consumer-organizations.tsx
scripts/shared-identity/verify-organizations-organizations.ps1
docs/shared-identity/ORGANIZATIONS.md
docs/shared-identity/validation-manifests/ORGANIZATIONS.md
```

## MODIFIED

```text
packages/contracts/src/index.ts
packages/contracts/package.json
packages/auth/src/index.ts
packages/auth/src/client.ts
packages/auth/package.json
packages/auth/tsconfig.json
packages/react/src/index.ts
packages/react/src/pages/index.ts
packages/react/package.json
packages/react/tsconfig.json
packages/next/src/index.ts
packages/next/src/pages/index.ts
packages/next/package.json
packages/next/tsconfig.json
docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md
docs/shared-identity/full-sdk-feature-matrix.json
scripts/shared-identity/verify-account-and-directory-account-directory.ps1
scripts/shared-identity/verify.ps1
CHANGELOG.md
```

## MOVED

NONE

## DELETED

NONE

## DELETE AFTER VALIDATION

NONE

## DATABASE MIGRATIONS

NONE

## RUNTIME CHANGES

Only public TypeScript SDK/UI composition changes. Existing backend Organization Directory runtime behavior is reused unchanged.

## BREAKING CHANGES

NONE. Existing public surfaces remain available.

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```
