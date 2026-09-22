# Authorization Orchestration

**Source version: 0.9.0. Date: September 21, 2026.**

## Purpose

This version connects the persistent assignment projection to the neutral RBAC adapter boundary without moving wildcard evaluation into this repository.

The orchestration path is:

```text
IdentityAuthorizationRequest
        |
        v
IDatabaseRouteResolver
        |
        v
immutable ResolvedDatabaseRoute
        |
        v
IAssignedCapabilityReader
        |
        v
AssignedCapabilityGrant[]
        |
        v
RbacTrnCompiler
        |
        v
candidate TRNs
        |
        v
IRbacAuthorizationAdapter
        |
        v
external RBAC engine
        |
        +-- Allowed
        +-- Denied
        +-- TechnicalFailure
```

## Authority Boundary

`IdentityAuthorizationService` is orchestration, not a second authorization engine.

It does not evaluate wildcard semantics, precedence, inheritance, or matching rules. It materializes the current candidate grants and delegates the final decision to `IRbacAuthorizationAdapter`.

The external RBAC integration remains the authority for wildcard evaluation.

## Route Stability

One database route is resolved at the beginning of the operation and the same immutable `ResolvedDatabaseRoute` instance is passed to the assignment reader.

The service does not resolve again between grant loading and authorization evaluation.

A routing failure is a technical failure, never a business denial.

## Grant Provenance

Every projected grant is checked against the request subject, tenant, and application before any TRN is sent to the adapter.

A mismatched grant produces `GrantProvenanceMismatch` and the RBAC adapter is not called.

This guard treats cross-context projection as an integrity failure rather than silently importing or dropping the grant.

## Decision Semantics

The application-level result keeps three states:

```text
Allowed
Denied
TechnicalFailure
```

`Denied` is reserved for an explicit RBAC denial.

Failures in route resolution, grant projection, TRN materialization, or adapter invocation are technical failures and are not converted into denial.

Cancellation propagates as `OperationCanceledException` and is never converted to either a denial or a technical result object.

## Concurrency Independence

The service contains no mutable current-user, current-tenant, current-route, or current-permission state.

Each authorization call owns:

- its request;
- its resolved route snapshot;
- its projected grant collection;
- its materialized TRN collection;
- its adapter invocation.

Concurrent authorization calls may therefore use different identity scopes and database destinations without sharing operation state.

## Current Wildcard Limitation

The external adapter has been validated against the real external wildcard engine.

The persistent policy model in this version still materializes concrete `CapabilityKey` assignments. Persisted wildcard policy statements are not introduced by this version.

Adding wildcard-capable policy statements requires an explicit policy-pattern model and persistence contract. It must not be implemented by moving wildcard matching into `IdentityAuthorizationService`.

## No Public HTTP Endpoint

This version does not add a public authorization endpoint.

The service is an internal application boundary intended to be called from authenticated and trusted server-side execution paths once authentication and application-context verification are connected.
