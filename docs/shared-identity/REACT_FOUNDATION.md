# Shared Identity React Foundation — React foundation

## Purpose

Activate `@generic-identity/react` as a reusable React-only boundary over the public `contracts` and `auth` packages without moving the proven Next.js administration UI.

## Scope

React Foundation adds:

- `IdentityProvider`;
- `useIdentityClient`;
- `useAuthorization`;
- `useCapability`;
- `RequireCapability`;
- React peer-dependency and TypeScript boundaries;
- a consumer compile probe;
- source/architecture verification.

## Non-goals

This delivery does not:

- move or delete any existing administration component;
- extract full Users, Groups, Policies, Sessions or MFA pages;
- add a Next.js dependency;
- add session/cookie persistence;
- implement a second permission engine;
- parse TRNs locally;
- alter server authorization semantics;
- modify PostgreSQL or any backend schema;
- integrate the existing Next.js host with the new React package yet.

## Authorization behavior

React authorization is intentionally presentation-only:

```text
RequireCapability
      ↓
useCapability
      ↓
GenericIdentityAuthorizationContext
      ↓
existing TypeScript client
      ↓
.NET / RBAC authority
```

A technical failure is represented as an error and is not silently collapsed into a business denial.

## Exit gate

React Foundation is closed only when:

- all pre-existing verification remains GREEN;
- `@generic-identity/contracts` typecheck remains GREEN;
- `@generic-identity/auth` typecheck remains GREEN;
- `@generic-identity/react` typecheck is GREEN;
- React/React DOM remain peer dependencies;
- no Next.js import exists in the React package;
- the existing administration host still consumes the proven legacy client and has not been redirected early;
- no existing file is moved or deleted.
