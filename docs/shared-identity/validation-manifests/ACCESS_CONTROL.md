# Access Control — ACCESS CONTROL SDK/UI — MANIFEST

## ADDED

Access Control adds categorized Access Control contracts, SDK types, managed-policy/resource-scope/delegated-authority UI and verification.

Historical tenant-policy UI files introduced during the first Access Control overlay remain physically present as inert compatibility tombstones. They are not exported by the active categorized SDK/UI.

## MODIFIED

- `clients/typescript/src/index.ts`
- `clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts`
- `packages/contracts/src/access-control/index.ts`
- `packages/auth/src/access-control.ts`
- `packages/auth/src/client.ts`
- `packages/react/src/access-control/index.ts`
- `packages/react/src/pages/index.ts`
- `packages/react/src/pages/GroupAccessPage.tsx`
- `packages/next/src/pages/index.ts`
- `docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md`
- `docs/shared-identity/full-sdk-feature-matrix.json`
- `docs/shared-identity/ACCESS_CONTROL.md`
- `scripts/shared-identity/verify-sdk-inventory-and-category-contract-full-sdk-inventory.ps1`
- `scripts/shared-identity/verify-access-control-access-control.ps1`
- `CHANGELOG.md`

## MOVED

NONE

## DELETED

NONE

## DELETE AFTER VALIDATION

NONE

## DATABASE MIGRATIONS

NONE

## RUNTIME CHANGES

Additive categorized SDK/UI surface only. The active authorization model remains managed-policy based. Retired legacy tenant-policy administration is not reactivated.

## BREAKING CHANGES

NONE to active public surfaces.

## PACKAGE VERSION

```text
@generic-identity/contracts  1.3.0
@generic-identity/auth       1.3.0
@generic-identity/react      1.3.0
@generic-identity/next       1.3.0
```

## POST-APPLY VALIDATION

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Expected:

```text
Managed policy legacy compatibility closure source consistency validation passed.
Shared Identity SDK Inventory and Category Contract full SDK inventory/category validation: GREEN
Shared Identity Access Control SDK/UI source validation: GREEN
```
