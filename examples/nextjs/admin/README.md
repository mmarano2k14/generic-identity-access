# Next.js Administration Module

This directory is a copyable App Router example for the reusable Identity Access administration module.

The functional split is intentional:

- **Server Components** load protected data and construct permission-aware navigation.
- **Client Components** handle local filtering, modal state, pending state, and safe presentation feedback only.
- framework-required **Server Action functions** in `app/identity/actions.ts` are thin adapters; mutation logic lives in the `IdentityAccessAdminMutationService` class.
- `IdentityAccessAdminRequest` is a per-request **class** that resolves trusted server configuration and HTTP-only Bearer provenance.
- `IdentityAccessClient`, `IdentityAuthorizationContext`, and `IdentityAccessAdminUiBuilder` remain the reusable connector classes.
- no Client Component receives access tokens, database routes, connection information, RBAC internals, or secret references.

## Functional pages

```text
/identity/users
/identity/tenants
/identity/memberships
/identity/groups
/identity/policies
/identity/resource-scopes
/identity/sessions
/identity/authority
```

The pages include server-confirmed creation flows, local filtering, loading/error/empty states, and security-sensitive session revocation with explicit confirmation. Mutation paths are revalidated only after the server confirms success; the UI does not assume optimistic authorization or mutation success.

## CSS ownership rule

All custom CSS for this module lives in exactly one file:

```text
styles/identity-access-admin.css
```

Do not add CSS Modules, per-component stylesheets, `<style>` blocks, or inline style objects. The premium design increment must evolve this same file rather than fragmenting style ownership.

## Server configuration

Required server environment:

```text
IDENTITY_ACCESS_API_BASE_URL=http://127.0.0.1:5080
IDENTITY_ACCESS_IDENTITY_SCOPE_ID=<uuid>
IDENTITY_ACCESS_APPLICATION_KEY=<registered-application-key>
IDENTITY_ACCESS_BEARER_COOKIE_NAME=<http-only-cookie-name>
IDENTITY_ACCESS_TENANT_ID=<uuid> # required by tenant-scoped pages
```

Do not use `NEXT_PUBLIC_` for any of these values.

The visual system in this increment is structural and responsive. Final premium typography, spacing, motion, visual hierarchy, table treatment, permission editing, and theme polish remain a separate design increment.
