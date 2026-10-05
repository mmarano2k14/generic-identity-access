# Shared Identity SDK Inventory and Category Contract — Full SDK Inventory & Category Contract Freeze

## Goal

Freeze the complete consumer-neutral Generic Identity SDK taxonomy against the code that actually exists before expanding public contracts, mutations or UI.

SDK Inventory and Category Contract is intentionally non-functional. It does not add endpoints, database migrations, authorization rules or new UI behavior.

## Deliverables

- `FULL_SDK_CATEGORY_MODEL.md` — category boundaries and dependency rules.
- `FULL_SDK_FEATURE_MATRIX.md` — human-readable feature-by-feature inventory.
- `full-sdk-feature-matrix.json` — machine-readable source for later validation gates.
- SDK Inventory and Category Contract source validation that anchors the inventory to representative current controllers, legacy SDK clients, public packages and shared pages.
- Consumer-neutral naming gate for public/runtime Identity source.

## Frozen categories

```text
Account & Authentication
Directory
Organizations
Access Control
Application Security
Security Operations
Protocol & Diagnostics
```

## Key findings

1. The legacy TypeScript client already covers substantially more administration behavior than the current categorized public `@generic-identity/auth` facade.
2. The current public administration facade is intentionally read-focused and must be expanded category by category rather than replaced wholesale.
3. Shared React pages exist for several core areas, but many are read-focused composition surfaces rather than complete CRUD experiences.
4. Organization Directory already has a rich backend and legacy TypeScript client but is not yet promoted into the public categorized SDK/UI.
5. Application security models, resource scopes, security audit, delegated scope authority and policy versioning are backend-real but not yet fully surfaced through the public SDK.
6. TOTP/WebAuthn/recovery providers contain internal enrollment services, but no public enrollment controller endpoint was discovered in the SDK Inventory and Category Contract baseline. These are backend API gaps, not SDK-only gaps.
7. No dedicated effective-permission listing or permission-explanation endpoint was discovered; authorization evaluation itself is implemented.
8. Invitations were not found as a backend/controller/client feature in the current baseline.
9. `OrganisationProfile` is intentionally excluded from the Generic Identity categorized SDK boundary.

## No behavior change

```text
Authentication behavior     unchanged
Authorization behavior      unchanged
RBAC                         unchanged
Database schema              unchanged
Package versions             unchanged
Runtime routes               unchanged
```

Account and Directory may begin only from this matrix and must not silently add a feature whose backend status is `BACKEND_MISSING` or `BACKEND_API_MISSING`.
