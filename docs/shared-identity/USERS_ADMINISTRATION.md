# Generic Identity — Users Administration

**Current package family:** `1.5.0`  
**Status:** reusable administration workflow qualified

## Scope

The Users administration surface provides a reusable, server-authoritative workflow for scope-wide and membership-limited directory administration.

Supported behavior includes:

```text
scope-wide user list and detail
membership-limited tenant user projection
bounded all-authorized-tenants composition
user create and update
status / lifecycle update
optimistic concurrency
secret-free password credential metadata
password credential create/change
assigned-access provenance view
cross-navigation to memberships, MFA, sessions and audit
```

## Visibility model

Scope-wide administrators may use the unrestricted user directory when authorized.

Membership-limited administrators never fall back to scope-wide user APIs. Their view is composed from authorized tenant memberships and tenant-user projections.

A tenant identifier received from a browser request is requested context only. The server validates it against the effective administration context before any tenant-scoped operation.

## Credential security

Password administration uses server-only mutations. Shared presentation never renders:

```text
plaintext passwords
password hashes
session tokens
refresh tokens
MFA secrets
```

Credential metadata is lifecycle metadata only. Backend password/session revocation semantics remain authoritative.

## Access insight

User Access Insight reports assignment provenance and lifecycle blockers. It is diagnostic composition, not an authorization verdict and not a replacement for RBAC evaluation.

## Concurrency

User mutations forward `expectedVersion` to the backend. Stale writes remain conflicts and must be resolved by reloading authoritative state.

## Related reusable components

```text
loadNextUsersDirectory
loadNextUserCredentialMetadata
loadNextUserAccessInsight
createNextUserFromForm
updateNextUserFromForm
createNextPasswordCredentialFromForm
changeNextPasswordCredentialFromForm
UserForm
UserDetailsPage
TenantLinkedUsersPage
UserAccessInsightPanel
UserPasswordCredentialPanel
PasswordCredentialForm
```
