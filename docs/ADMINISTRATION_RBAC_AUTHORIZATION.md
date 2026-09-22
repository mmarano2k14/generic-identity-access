# Administration RBAC Authorization

Administrative HTTP capability checks can be connected to the identity authorization
orchestration and external RBAC engine.

## Configuration

Authorization is disabled by default.

```json
{
  "IdentityAccess": {
    "Authorization": {
      "Enabled": "true",
      "Provider": "multiplexed",
      "RbacProject": "admin-project",
      "RbacNamespace": "admin-namespace",
      "ReferenceDirectory": "path/to/external/rbac/binaries"
    }
  }
}
```

All values are trusted server configuration.

The HTTP caller cannot select:

```text
RBAC project
RBAC namespace
external adapter
external binary directory
```

When enabled, startup requires:

```text
IDatabaseRouteResolver
IAssignedCapabilityReader
Multiplexed.Rbac.Core.dll
Multiplexed.Abstractions.dll
```

Missing requirements fail startup rather than silently disabling authorization.

## Authorization Flow

```text
HTTP request
    |
validated local session
    |
AdministrationRequestContext
    |
route boundary check
    |
tenant/resource target extraction
    |
IdentityAuthorizationRequest
    |
IdentityAuthorizationService
    |
current tenant/group/policy grants
    |
TRN materialization
    |
IRbacAuthorizationAdapter
    |
external RBAC engine
    |
Allowed / Denied / TechnicalFailure
```

## Trusted Identity

The subject and application used by authorization come from the validated session:

```text
AdministrationRequestContext.Subject
AdministrationRequestContext.Application
```

They are not accepted as authentication claims from route values, headers, query strings,
or request bodies.

## Authorization Target

The current grant model is tenant based.

A route containing:

```text
tenantId
```

produces a `TenantReference`.

A route also containing:

```text
resourceScopeId
```

produces a `ResourceScopeReference`, allowing existing exact-scope and descendant binding
rules to participate in grant projection.

Routes without `resourceScopeId` are evaluated at tenant level.

## Identity-Scope Operations

Administration operations without a tenant target use the dedicated identity-scope
administration authority model.

Examples include:

```text
user directory administration
tenant creation
credential administration
scope-type administration
bulk session administration
```

They are evaluated by `IdentityScopeAuthorizationService` using scope-specific
administration groups and policies.

Tenant grants are never promoted into identity-scope authority.

See `docs/IDENTITY_SCOPE_ADMINISTRATION_AUTHORITY.md`.

## Capability Mapping

MVC operation metadata provides the requested concrete capability:

```text
resource / feature / action
```

For example:

```text
identity-access / group / read
identity-access / policy / write
identity-access / resource-scope / read
```

The concrete capability is passed to `IdentityAuthorizationService`.

The HTTP layer does not evaluate wildcard semantics.

## Decision Mapping

```text
IdentityAuthorization Allowed
    -> administration Allowed

IdentityAuthorization Denied
    -> administration Denied
    -> HTTP 403

IdentityAuthorization TechnicalFailure
    -> administration Unavailable
    -> HTTP 503

missing/invalid authenticated session
    -> HTTP 401
```

Authentication context mismatch is denied before grant projection.

## External RBAC Authority

Wildcard matching remains exclusively inside the configured external RBAC engine.

The administration bridge does not duplicate:

```text
r:f:*
r:*:a
r:*:*
*:*:a
*:*:*
```

or any other wildcard evaluation logic.


## External Runtime Capability Evaluation

Class-based external connectors may evaluate a concrete capability through the same trusted administration authorizer used by MVC administration operations.

```text
POST /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/authorization/evaluate
POST /api/v1/identity-scopes/{identityScopeId}/tenants/{tenantId}/applications/{applicationKey}/authorization/evaluate
POST /api/v1/identity-scopes/{identityScopeId}/tenants/{tenantId}/applications/{applicationKey}/resource-scopes/{resourceScopeId}/authorization/evaluate
```

Request body:

```json
{
  "resource": "billing",
  "feature": "invoice",
  "action": "refund"
}
```

A normal RBAC denial returns `200 { "allowed": false }` so an external runtime can implement an `isAllowed(...)` contract without converting denial into a transport failure. Authentication failure remains `401`, trusted-boundary mismatch remains `403`, and technical authorization unavailability remains `503`.

The endpoint does not evaluate wildcard rules locally and does not accept caller-selected RBAC project/namespace values.
