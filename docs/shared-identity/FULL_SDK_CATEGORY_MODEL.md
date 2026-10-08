# Generic Identity — Full SDK Category Model

**Status:** SDK category contract freeze  
**Scope:** Generic Identity only  
**Rule:** Public Identity architecture is consumer-neutral. Consumer product names do not belong in Identity runtime, packages or public contracts.

## 1. Purpose

The complete public SDK is organized by responsibility rather than by controller or UI route. The public package family remains small; categories live inside the existing packages.

```text
@generic-identity/contracts
@generic-identity/auth
@generic-identity/react
@generic-identity/next
```

The categories are:

1. **Account & Authentication** — sign-in/out, current session, password lifecycle, recovery, step-up authentication.
2. **Directory** — users, tenants, tenant memberships, membership candidates and account lifecycle administration.
3. **Organizations** — generic organization identity, hierarchy, membership and resource-scope linkage.
4. **Access Control** — groups, policies, bindings, resource scopes, delegated authority and authorization evaluation.
5. **Application Security** — security models, scope types, application capabilities, manifests and trusted administration context.
6. **Security Operations** — sessions, MFA administration, authenticator lifecycle and security audit.
7. **Protocol & Diagnostics** — OIDC protocol clients and service diagnostics. This category is public-SDK optional and is not automatically a user-facing administration area.

## 2. Dependency direction

```text
contracts
   ↑
 auth
   ↑
 react
   ↑
 next
```

No category may invert this package dependency direction.

## 3. Consumer-neutral naming

Public/runtime Identity source must use generic names such as:

```text
ApplicationKey
ClientId
ConsumerApplication
ApplicationSecurityModel
AdministrationContext
DevelopmentConsumer
```

It must not encode a specific consuming product name into a class, interface, package, route contract or reusable SDK API.

## 4. Explicit boundary — OrganisationProfile

`OrganisationProfile` is **not** promoted into the Generic Identity categorized SDK. It is a separate application-semantic module that is currently co-located in the repository. The full Identity SDK inventory covers Identity, Authentication, Authorization, Organization Directory and shared security administration only.

## 5. Backend-grounded rule

A public SDK feature may be classified as complete only when the underlying backend contract actually exists. Internal provider services do not count as a public API. For example, an internal TOTP or WebAuthn enrollment service without a controller endpoint is recorded as `BACKEND_API_MISSING`; The SDK inventory does not invent an endpoint.


## 6. Cross-cutting administration integration

The category model is consumed through reusable Next.js server workflows and React presentation. Cross-category relations use the bounded Generic Identity entity-reference autocomplete rather than introducing category-specific browser catalogs.

Consumer applications remain route and branding owners. The reusable SDK remains consumer-neutral and server-authoritative.

Current aligned public package family: `1.5.0`.
