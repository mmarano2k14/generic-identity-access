# Assigned Capability Projection

**Source version: 0.7.0. Date: September 21, 2026.**

## Purpose

This version adds a provenance-preserving read model for capabilities structurally assigned to one subject inside an exact identity-scope, tenant and application boundary.

It deliberately stops before RBAC evaluation.

```text
Subject
  -> active TenantMembership
  -> active UserGroup
  -> GroupPolicyBinding
  -> active PermissionPolicy
  -> PolicyStatement
  -> versioned ApplicationCapability
  -> AssignedCapabilityGrant
```

An `AssignedCapabilityGrant` is evidence of the current stored assignment path. It is **not** an Allow/Deny decision, a credential, a TRN, or proof that a resource instance belongs to the requested scope.

## Contract

`IAssignedCapabilityReader.ListAsync` receives:

- the immutable `ResolvedDatabaseRoute` for the operation;
- one `TenantReference`;
- one `SubjectReference`;
- one `ApplicationKey`;
- an explicit `CancellationToken`.

The result keeps provenance for each assignment:

- subject;
- tenant;
- application;
- source group;
- source policy;
- statement id;
- security-model version;
- structured capability key.

Duplicate capability keys are intentionally not collapsed when their provenance differs. A later authorization bridge may need the complete origin chain for explanation, revocation and policy evaluation.

## PostgreSQL Projection

`PostgreSqlAssignedCapabilityReader` performs one operation-scoped query through the already resolved destination. It does not resolve another route and cannot switch databases during the read.

The query requires active state for:

- user;
- tenant;
- tenant membership;
- user group;
- permission policy.

Bindings and statements are immutable edges in the current model.

Suspending any mutable node in the assignment path removes that path from this projection without deleting its historical relational structure.

## Concurrency Independence

The reader has no process-global mutable state.

Every call:

1. receives its own immutable route snapshot;
2. opens its own logical database connection;
3. uses only parameters from that operation;
4. returns a new immutable result list.

Npgsql may pool physical connections, but no subject, tenant, application or route state is stored on a shared connection object by this component.

Concurrent reads for different scopes therefore share infrastructure only at the bounded pool level, not application-operation state.

## Indexes

Migration `0004_assignment_projection_indexes.sql` adds query-oriented indexes for:

- active tenant-membership lookup;
- group membership traversal;
- group-to-policy traversal;
- policy-statement traversal.

The migration changes performance support only. It does not change identity or authorization semantics.

## RBAC Boundary

This version intentionally does **not** implement:

- TRN compilation;
- wildcard behavior;
- deny semantics;
- inheritance;
- resource-instance authorization;
- `RequireCapability` integration;
- an Allow/Deny endpoint;
- access-context rotation.

Those behaviors must come from the actual RBAC contract rather than being inferred from the persisted capability model.

The future RBAC adapter can consume assigned capability provenance, but must still perform the real authorization decision under the existing engine guarantees.

## Validation

The live PostgreSQL script `scripts/postgresql/validate-assigned-capability-projection.sql` creates active and suspended assignment paths inside a transaction and verifies that only the active path is projected. The transaction is rolled back afterward.

Run it with:

```powershell
.\scripts\postgresql\verify-assigned-capability-projection.ps1
```
