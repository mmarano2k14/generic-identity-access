# delivery 05 — Shared React Pages Manifest

## ADDED

```text
docs/shared-identity/SHARED_PAGES.md
docs/shared-identity/validation-manifests/SHARED_PAGES.md
packages/react/src/components/IdentityEmptyState.tsx
packages/react/src/components/IdentityPageFrame.tsx
packages/react/src/components/IdentityPanel.tsx
packages/react/src/components/IdentityStatus.tsx
packages/react/src/components/IdentityTable.tsx
packages/react/src/components/index.ts
packages/react/src/pages/AccountPage.tsx
packages/react/src/pages/GroupDetailsPage.tsx
packages/react/src/pages/GroupsPage.tsx
packages/react/src/pages/MfaPage.tsx
packages/react/src/pages/PoliciesPage.tsx
packages/react/src/pages/PolicyDetailsPage.tsx
packages/react/src/pages/ProfilePage.tsx
packages/react/src/pages/RecoveryPage.tsx
packages/react/src/pages/SecurityPage.tsx
packages/react/src/pages/SessionsPage.tsx
packages/react/src/pages/SignInPage.tsx
packages/react/src/pages/UserDetailsPage.tsx
packages/react/src/pages/UsersPage.tsx
packages/react/src/pages/index.ts
packages/react/src/pages/internal.tsx
packages/react/test/consumer-pages.tsx
scripts/shared-identity/verify-delivery-05-pages.ps1
```

## MODIFIED

```text
packages/README.md
packages/react/README.md
packages/react/package.json
packages/react/src/index.ts
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
NONE IN Shared React Pages
```

The following existing host areas are **future cleanup candidates only** and MUST NOT be deleted by this delivery:

```text
examples/nextjs/admin/app/login/**
examples/nextjs/admin/app/recovery/**
examples/nextjs/admin/app/identity/users/**
examples/nextjs/admin/app/identity/groups/**
examples/nextjs/admin/app/identity/policies/**
examples/nextjs/admin/app/identity/sessions/**
examples/nextjs/admin/app/identity/mfa/**
related identity-only presentation components under examples/nextjs/admin/components/**
```

Cleanup becomes legal only after the future Next.js adapter and consumer integration own those routes and full verification is GREEN. A later delivery must list each exact `OLD -> NEW` relocation and deletion and provide a guarded cleanup script.

## DATABASE MIGRATIONS

```text
NONE
```

## BACKEND / RUNTIME CHANGES

```text
NONE
```

## HOST INTEGRATION CHANGES

```text
NONE
```

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected Shared React Pages markers:

```text
Shared Identity shared React pages source validation: GREEN
Shared Identity shared React pages typecheck: GREEN
```
