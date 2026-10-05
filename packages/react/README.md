# @generic-identity/react

Reusable React presentation boundary for Generic Identity.

## Public surface

The package provides providers, authorization-aware UX helpers, semantic UI primitives, theme hooks and categorized shared administration views.

Current functional categories include:

```text
account
directory
organizations
access-control
application-security
security-operations
```

Application Security exports reusable views for registered security models, model details, capability catalogs, scope types and effective administration context, plus presentation-only forms for manifest and scope-type registration.

Security Operations exports reusable MFA, session-revocation and security-audit presentation components. Privileged server actions remain consumer-owned.

## Dependency direction

```text
@generic-identity/contracts
          ↑
@generic-identity/auth
          ↑
@generic-identity/react
```

React and React DOM are peer dependencies. The package does not depend on Next.js.

## Theme contract

Consumers may import the default theme and override documented `--gi-*` custom properties, stable `gi-*` classes and `data-gi-*` hooks. Optional visual overrides are available for Button, Input, Panel and Table rendering.

Visual overrides may replace presentation only. They never own authentication, authorization, sessions, MFA, policy evaluation, TRN semantics or access-context behavior.

## Page ownership

Shared pages receive already-authorized data and consumer-supplied actions. They do not own application routes, middleware, credential persistence, backend mutations, database access, RBAC decisions, navigation or product branding.

`RequireCapability` remains an UX gate only. Protected operations must always be authorized again on the server.
