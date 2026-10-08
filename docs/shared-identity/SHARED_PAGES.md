# Generic Identity — Shared React Administration

`@generic-identity/react` provides reusable, consumer-neutral presentation for Generic Identity administration.

## Scope

The package includes shared views and forms for:

```text
sign-in and recovery
account / profile presentation
users and password credentials
tenants and memberships
organizations
groups and managed policies
resource scopes
delegated authority
application security
MFA administration
security audit
session security and containment
```

Shared React components do not fetch privileged data or own backend authority. Data and mutations are supplied by server integration code.

## Stable presentation boundary

Reusable presentation uses stable `gi-*` class names and `data-gi-*` hooks. `IdentityThemeRoot` and component overrides allow consumers to adapt visual presentation without modifying security behavior.

## Server-action compatibility

Forms support Next.js/React function actions without forcing conflicting HTML methods. Consumer applications retain ownership of Server Actions, routes, redirects, revalidation, and navigation.

## Security boundary

Shared pages never treat hidden controls as authorization. Privileged operations remain server-authorized, and secret-bearing values are excluded from browser-visible contracts.
