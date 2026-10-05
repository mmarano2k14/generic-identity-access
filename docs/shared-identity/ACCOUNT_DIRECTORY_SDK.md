# Shared Identity Account and Directory — Account & Directory SDK/UI

## Purpose

Promote the real Account & Authentication and Directory backend/client capabilities into categorized public SDK surfaces without replacing the compatibility `administration.*` facade.

## Public category surfaces

```text
@generic-identity/contracts/account
@generic-identity/contracts/directory

@generic-identity/auth/account
@generic-identity/auth/directory
```

The root `GenericIdentityClient` now exposes:

```text
client.account
client.directory
```

Existing surfaces remain available:

```text
client.authentication
client.authorization
client.administration
```

## Account category

The categorized account facade exposes only proven operations:

```text
self-service password change
recovery-code password reset
session validation
TOTP step-up
recovery-code step-up
WebAuthn step-up
administrative password credential lifecycle
```

No self-service profile mutation is invented because no dedicated backend contract is currently available.

## Directory category

```text
users CRUD
tenants CRUD
tenant user projection
tenant memberships CRUD
membership candidate lookup/add-by-login
tenant group assignment listing
```

Invitations remain explicitly unsupported until a real backend contract exists.

## Shared UI

Added reusable consumer-neutral views:

```text
PasswordPage
AuthenticationStepUpPage
TenantsPage
TenantDetailsPage
MembershipsPage
MembershipCandidatePanel
```

Mutation authorization remains server-authoritative. Shared pages expose presentation and action slots; they do not implement a second permission model.

## Versioning

The four Generic Identity public packages advance additively to `1.1.0`.
The release qualifier now derives the shared release version from the package manifests and verifies that all four shared packages remain version-aligned.
