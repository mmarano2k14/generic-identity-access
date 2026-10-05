# Shared Identity Packages

This directory contains the public TypeScript package boundaries for the shared Identity integration.

Current activation state:

```text
contracts   ACTIVE — type-only public contract bridge
auth        ACTIVE — framework-neutral authentication/authorization facade
react       ACTIVE — React foundation + shared pages + theming/visual overrides
next        RESERVED
```

Dependency direction is strictly one-way:

```text
contracts
   ↑
 auth
   ↑
 react
   ↑
 next
```

Rules:

- `contracts` contains passive public contracts and depends on no internal package.
- `auth` depends on `contracts` and, temporarily during extraction, on the proven `@identity-access/client` runtime implementation; it never depends on React or Next.js.
- `react` depends on `contracts` and `auth`; React/React DOM are peer dependencies and Next.js is forbidden.
- shared React pages are presentation-only and do not own routing, session persistence or protected backend mutations.
- Theme tokens and component overrides change presentation only; security semantics remain owned by Generic Identity.
- `next` may depend on `contracts`, `auth` and `react` once activated by its owning milestone.
- server storage, PostgreSQL, Redis/Lua internals and CLR implementation details must not leak into these packages.
- secret-bearing credentials and tokens do not belong in `contracts`; they are owned by the authentication boundary.
- Generic Organization Directory and OrganisationProfile remain separate ownership boundaries.
- existing code is exposed progressively; it is not rewritten merely to fit this directory structure.
- a package is activated only by its owning milestone and only after the preceding verification remains GREEN.
- any future source relocation must list every `MOVED` and `DELETED` file explicitly before cleanup.

## Next.js Integration status

`@generic-identity/next` is now active as the Next.js-specific integration boundary. The existing administration host is not migrated yet; consumer switching remains a later milestone.

