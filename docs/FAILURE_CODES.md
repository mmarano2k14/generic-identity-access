# Typed Failure Codes

Identity Access uses typed failure categories for internal application and integration
results.

Free-form strings are not used as stable internal failure identifiers.

## Authorization

`IdentityAuthorizationFailureCode` identifies failures in authorization orchestration,
including:

- route resolution;
- grant projection;
- grant provenance;
- TRN materialization;
- RBAC adapter invocation;
- external RBAC technical failure;
- unknown RBAC decisions.

When the external RBAC adapter returns a technical failure,
`IdentityAuthorizationResult.RbacFailureCode` preserves the typed RBAC failure category.

## RBAC Adapter

`RbacAuthorizationFailureCode` identifies failures at the external RBAC boundary.

Stable categories include:

```text
ExternalBinariesMissing
ExternalLoadFailed
ExternalContractMismatch
ExternalInvocationFailed
```

A missing distribution, an unloadable distribution, a reflection-contract mismatch, and a
runtime invocation failure remain distinct technical categories.

The stable categories are separate from diagnostic detail. An adapter may attach safe
internal diagnostic detail without turning that detail into a public or stable failure
code.

## Authentication

`AuthenticationFailureCode` identifies local authentication failures such as unknown
clients, rejected redirect URIs, unavailable directories, invalid credentials, invalid
sessions, and rejected post-logout redirects.

HTTP mapping uses these typed categories rather than string comparisons.

## OAuth / OpenID Connect

`OidcAuthorizationFailureCode` identifies authorization-endpoint failures such as malformed
requests, unknown clients, rejected redirect URIs, unsupported response types/scopes,
missing PKCE S256, login requirements, and directory unavailability.

`OidcTokenFailureCode` identifies token-endpoint failures such as invalid requests, invalid
clients, invalid grants, unsupported grant types, directory unavailability, and token
issuance failures.

Protocol wire errors are mapped from these typed categories. Refresh-token unknown, expired,
revoked, session-ineligible, and consumed-token replay outcomes all remain public
`invalid_grant` responses. Replay detection and family revocation are internal security
semantics and do not create an information-leaking OAuth error. Free-form strings are not used
as internal decision identifiers.

OIDC signing-key ring failures are provider bootstrap/configuration failures rather than token-
endpoint protocol decisions. Missing active-key membership, duplicate `kid` values, invalid or
unavailable PEM material, and a public-only active key prevent OIDC service registration. The
provider does not fall back to another configured key or expose key-file diagnostics through an
OAuth error response.

`OidcAccessTokenValidationFailureCode` identifies internal Bearer validation failures including
malformed JWTs, unsupported headers, unknown signing keys, invalid signatures, issuer/audience
mismatch, expiry/lifetime violations, claim inconsistency, and registered-client binding failure.
These categories are not exposed as a token-validation oracle to callers; invalid Bearer credentials
map to the same administration HTTP 401 boundary.

`AdministrationAuthenticationFailureCode` additionally distinguishes an invalid Bearer credential
from technical Bearer/session-continuity validation unavailability. Technical failures map to HTTP
503 and must not be collapsed into an authorization denial. A cryptographically valid token that
requests a different identity-scope/application route reaches the existing authenticated boundary
mismatch and maps to HTTP 403.

## Administration Authorization

`AdministrationAccessFailureCode` identifies technical unavailability of administration
authorization.

An explicit denial remains a decision, not a technical failure code.

## Design Rule

Failure codes describe stable machine-readable categories.

Diagnostic details are separate and must not be treated as stable API contracts.
Unexpected exception messages, stack traces, connection details, tokens, passwords, and
other secrets must never be used as client-facing failure codes.
