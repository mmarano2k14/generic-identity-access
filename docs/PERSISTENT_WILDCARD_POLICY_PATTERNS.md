# Persistent Wildcard Policy Patterns

**Version: 0.10.0. Date: September 21, 2026.**

## Purpose

This version allows policy statements to persist the exact whole-segment wildcard forms
validated against the external RBAC engine while keeping authorization evaluation outside
this repository.

Supported patterns are:

```text
r:f:a
r:f:*
r:*:a
r:*:*
*:*:a
*:*:*
```

Partial wildcards and unsupported shapes are rejected before persistence.

## Separation of responsibilities

`CapabilityPattern` validates only the representation accepted for persistence.
`RbacTrnCompiler` materializes that pattern into the external TRN wire format.
Neither component evaluates whether a request is allowed.

The final wildcard decision remains exclusively owned by the configured external RBAC
engine through `IRbacAuthorizationAdapter`.

## Persistence

Migration `0006_persistent_wildcard_policy_patterns.sql` removes the exact-capability
foreign key from policy statements because wildcard rows cannot reference one concrete
capability tuple.

The statement remains pinned to an `application_security_models` version. A database
trigger requires:

- an exact pattern to reference an actually declared concrete capability; or
- a wildcard pattern to match at least one declared concrete capability in the pinned model.

This prevents wildcard storage from bypassing the versioned application capability model.

## Concurrency and isolation

No mutable authorization state is introduced. Assignment projection remains operation
scoped, uses the already resolved immutable database route, and preserves identity scope,
tenant, application, group, policy, statement, and model-version provenance.

Policy mutation concurrency behavior is unchanged from the existing optimistic-concurrency
contract.

## Non-goals

This version does not add Deny semantics, wildcard evaluation, inheritance, or a second
RBAC implementation. It does not alter external access-context rotation or execution
context behavior.
