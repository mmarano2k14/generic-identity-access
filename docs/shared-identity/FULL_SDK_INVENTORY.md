# Generic Identity — Full SDK Inventory

**Current package family:** `1.5.0`  
**Scope:** categorized Generic Identity SDK and reusable administration integration

## Purpose

This inventory records the backend-grounded public SDK surface. It distinguishes implemented backend capabilities from genuine backend gaps so the SDK does not manufacture unsupported semantics.

## Public categories

```text
Account & Authentication
Directory
Organizations
Access Control
Application Security
Security Operations
Protocol & Diagnostics
```

## Current findings

1. Backend-supported administration capabilities across Directory, Organizations, Access Control, Application Security, and Security Operations are exposed through categorized Generic Identity clients.
2. Reusable React and Next.js administration workflows now cover the main backend-supported administration lifecycle, including bounded relation lookup and server-authorized mutations.
3. Tenant authorization and identity-scope Delegated Authority remain separate authorization catalogs.
4. Security Audit is exposed as bounded, read-only evidence.
5. Session administration exposes containment by user/client plus bounded evidence; no administrative active-session list exists and none is inferred.
6. MFA provider discovery, policy administration, effective user state, and authenticator lifecycle are exposed; dedicated live functional acceptance of the reusable MFA workflow remains pending.
7. TOTP enrollment, WebAuthn registration, and recovery-code generation have internal services but no discovered public administration endpoint; they remain `BACKEND_API_MISSING`.
8. Invitations, effective-permission listing, and permission-explanation/grant-provenance remain backend gaps.
9. OIDC protocol and service diagnostics remain optional categorized public-SDK expansion areas rather than core administration UI requirements.
10. `OrganisationProfile` remains outside the Generic Identity categorized SDK boundary.

## Cross-cutting administration contract

Relational administration uses bounded server-backed entity references. Consumer applications own routes and branding but do not duplicate security semantics.

The detailed feature-by-feature status is maintained in:

- [`FULL_SDK_FEATURE_MATRIX.md`](FULL_SDK_FEATURE_MATRIX.md)
- [`full-sdk-feature-matrix.json`](full-sdk-feature-matrix.json)
