# Generic Identity — Administration Integration

**Current package family:** `1.5.0`  
**Qualification snapshot:** 2026-10-07  
**Scope:** reusable administration workflows, shared presentation, and consumer integration boundaries

## Purpose

Generic Identity administration is designed so applications can consume a complete identity-management surface without copying privileged logic into their own codebase.

The reference architecture is:

```text
ASP.NET Core Identity API
        │
        v
@identity-access/client
        │
        v
@generic-identity/auth
        │
        ├── categorized clients
        └── authorization context
        │
        v
@generic-identity/next/server
        │
        ├── trusted context composition
        ├── authorization checks
        ├── bounded query/workspace loaders
        └── mutation workflows
        │
        v
@generic-identity/react
        │
        └── reusable administration presentation
        │
        v
Consumer-owned routes / Server Actions / navigation / branding
```

The consumer application is deliberately thin. It owns URLs and presentation composition, but it does not own Identity authorization rules, TRN construction, database routing, session authority, or duplicated administration business logic.

## Qualified administration areas

| Area | Reusable SDK workflow | Qualification state |
|---|---|---|
| Authentication | server session establishment and validation | Qualified |
| Authorization / RBAC | server-backed capability evaluation | Qualified |
| Tenants | list/detail/create/update with concurrency | Qualified |
| Application Security | model registration, manifest upload, scope types, capability catalog | Qualified |
| Users | scope-aware directory, CRUD, credential administration, access insight | Qualified |
| Memberships | candidate lookup, create/update, group and Organization reconciliation | Qualified |
| Entity references | bounded server-backed autocomplete | Qualified |
| Organizations | hierarchy, lifecycle, membership, ResourceScope linkage | Qualified |
| Groups | lifecycle, templates, members, managed-policy bindings | Qualified |
| Managed Policies | lifecycle, versions, statements, publication, bindings | Qualified |
| Resource Scopes | lifecycle, hierarchy and registered scope-type selection | Qualified |
| Delegated Authority | scope-authority groups, policies, statements, members and bindings | Qualified |
| Security Audit | bounded filters, metrics, timeline and correlation navigation | Qualified |
| Sessions | bounded security evidence and server-confirmed containment | Qualified |
| MFA administration | policy, providers, user state and authenticator lifecycle | Implemented; dedicated live acceptance pending |

`Qualified` means the reusable source boundary, consumer build/type integration, and the corresponding functional administration workflow have been exercised. It does not imply that backend capabilities classified as missing elsewhere in the SDK inventory have been created.

## Cross-cutting entity reference selection

Relational administration controls use a single reusable server-backed autocomplete contract rather than loading unrestricted catalogs into the browser.

Supported reference kinds:

```text
user
tenant
tenant-membership
managed-policy
resource-scope
authority-group
authority-policy
```

Behavioral rules:

```text
minimum search length  3 characters
debounce               approximately 250 ms
previous request        aborted when superseded
maximum results         20
browser submits         stable selected identifier only
server                  re-authorizes every lookup
```

Tenant-scoped reference kinds require an authorized tenant context. Scope-wide reference kinds are unavailable to membership-limited subjects.

## Authorization boundaries

### Tenant authorization

```text
Tenant Group
  -> Group Member
  -> Managed Policy Binding
  -> Managed Policy Version
  -> Statement
  -> Resource Scope
```

### Identity-scope administration

```text
Scope Authority Group
  -> Member
  -> Scope Authority Policy Binding
  -> Scope Authority Policy
  -> Statement
```

The two catalogs are intentionally separate. A scope administrator, including a Super Administrator, is discovered through Delegated Authority and is not implicitly copied into tenant Groups or Managed Policies.

## Managed policy versus delegated-authority statements

Managed Policies are constrained to the registered application-security capability catalog and published model versions.

Delegated Authority statements remain typed administrative coordinates that may use supported wildcard patterns. They are not converted into the Managed Policy capability picker because the two models serve different authorization boundaries.

## Session administration

The public administration backend exposes revocation by user and by authentication client, but it does not expose an administrative active-session list.

The shared Sessions workspace therefore uses:

```text
bounded security-audit evidence
+
server-confirmed revokeUser / revokeClient operations
```

It never interprets the current browser session or missing audit events as an authoritative active-session inventory.

## MFA administration

The reusable MFA administration surface covers:

```text
installed provider metadata
application MFA policy
effective user MFA state
authenticator lifecycle metadata
normal authenticator revocation
recovery-oriented authenticator revocation
```

It deliberately does not expose provider secrets or invent public enrollment APIs for TOTP, WebAuthn, or recovery-code generation where the backend has no public endpoint.

The implementation and source qualification are complete. Dedicated live functional acceptance remains pending and must be performed with disposable factors and a safe development account before operational acceptance.

## Consumer integration rules

A conforming consumer integration must:

- import reusable functionality from `@generic-identity/*` packages rather than `@identity-access/client` directly;
- keep privileged mutations in Server Actions or other server-only integration code;
- construct administration context from trusted server configuration and authenticated session state;
- treat browser tenant/user identifiers as requested context only, never as authority;
- revalidate authoritative state after mutations;
- preserve backend concurrency errors rather than silently overwrite;
- present technical failure separately from authorization denial;
- avoid duplicating RBAC, TRN, policy, routing, MFA, or session semantics.

## Known backend boundaries

The current public backend does not provide:

```text
administrative active-session listing
TOTP enrollment/confirmation administration API
WebAuthn registration/enrollment administration API
recovery-code generation/replacement administration API
invitations
effective permission listing
permission explanation / grant provenance
```

These remain explicitly classified in the feature matrix. The SDK does not synthesize them.
