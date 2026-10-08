# Generic Identity — Application Security

**Introduced:** `1.4.0`  
**Current package family:** `1.5.0`

## Purpose

Application Security defines the registered authorization vocabulary for a consumer application without turning manifest metadata into authority.

Public surfaces cover:

```text
application security model list/get
manifest registration
scope type list/add
capability catalog
trusted effective administration context
structured permission references
```

## Manifest registration

The reusable Next.js server workflow accepts a project-owned JSON manifest and validates:

```text
file presence and JSON shape
application key against trusted administration context
model version
RBAC namespace declarations
capability structure
duplicate capabilities
size and file type constraints
```

Only the Identity API persists the registered model. Browser input cannot redefine the trusted application context.

## Scope types

Scope types are registered against an application security model version and may define parent relationships according to backend rules.

ResourceScope administration consumes this catalog instead of accepting arbitrary scope-type text.

## Capability catalog

Registered capabilities are metadata used by administration surfaces such as Managed Policies. Registration by itself grants no permission.

## Permission references

Structured permission references validate declared resource/feature/action coordinates. They are not credentials, grants, authorization decisions, or a public TRN string factory.

## Security invariants

- model metadata does not grant authority;
- effective administration context comes from the server;
- unknown capability coordinates fail closed;
- consumer UI is never an authorization boundary;
- RBAC evaluation remains server-side;
- consumer-specific business semantics remain outside Generic Identity.

No database migration or RBAC semantic change is introduced by the shared SDK workflows.
