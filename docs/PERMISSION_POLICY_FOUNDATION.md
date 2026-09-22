# Permission and Policy Foundation

**Source version: 0.6.0. Date: September 21, 2026.**

## Scope

This version adds the persistent structural model required before RBAC/TRN integration:

- versioned application security models;
- declared application capabilities;
- tenant/application-scoped permission policies;
- immutable policy statements referencing declared capabilities;
- exact group-to-policy bindings;
- PostgreSQL persistence for the complete structure.

This version does **not** define the TRN grammar, wildcard behavior, deny semantics, inheritance rules, or the final RBAC decision algorithm. Those behaviors must come from the actual authorization engine contract rather than being invented by this repository.

## Capability Model

At version 0.6.0, the three capability segments were provisionally named `namespace / resource / action`.
Version 0.8.0 aligned the model with the audited RBAC contract and the current names are:

```text
resource / feature / action
```

Example:

```text
billing / invoice / read
```

`CapabilityKey` is not a TRN. In the current model it contains the concrete `resource / feature / action` tuple; RBAC project and authorization namespace are supplied separately by the compatibility context.

Capabilities belong to an immutable `ApplicationSecurityModelReference`:

```text
IdentityScopeId
ApplicationKey
ModelVersion
```

A new model version is created instead of silently rewriting the meaning of a capability already referenced by durable policy state.

## Policies and Statements

A `PermissionPolicy` is scoped by:

```text
IdentityScopeId
TenantId
ApplicationKey
PolicyId
```

Policy metadata is mutable under optimistic `row_version` concurrency.

A `PolicyStatement` references:

- one policy;
- one immutable application-security-model version;
- one declared capability.

A policy statement is structural state. Persisting it does not grant access by itself.

## Group Policy Bindings

A `GroupPolicyBinding` connects a group to a policy only when both belong to the same:

- identity scope;
- tenant;
- application.

New domain bindings require an active group and an active policy. Persisted bindings may be structurally rehydrated without treating that rehydration as an authorization decision.

The future authorization path must still evaluate current user, tenant, membership, group, policy, context, resource scope, and RBAC-engine state.

## PostgreSQL Schema

Migration `0003_permission_policy_foundation.sql` adds:

```text
application_security_models
application_capabilities
permission_policies
policy_statements
group_policy_bindings
```

All durable identities include `identity_scope_id` in their keys and relevant foreign keys.

Policy updates use compare-and-increment `row_version` semantics. A stale writer receives `IdentityConcurrencyException` through the store contract rather than silently overwriting a committed update.

## Concurrency Independence

Every persistence method:

1. receives one immutable `ResolvedDatabaseRoute`;
2. validates that the model belongs to the route identity scope;
3. opens its own logical connection;
4. passes an explicit `CancellationToken` through asynchronous database operations;
5. does not use mutable process-global current-user, current-tenant, current-policy, or current-database state.

Pooled physical connections may be reused by Npgsql, but no logical operation state is stored on those pooled connections.

Equal local policy, group, capability, or statement identifiers in different identity scopes remain distinct durable identities.

## Cancellation Contract

The new permission/policy persistence contracts require a `CancellationToken` argument explicitly. Tests and internal callers pass the token explicitly rather than relying on omitted optional arguments.

Cancellation remains a technical operation outcome and must not be converted into an authorization denial.

## Live PostgreSQL Validation

After applying the migrations:

```powershell
.\scripts\postgresql\verify-permission-persistence.ps1
```

The validation runs inside a transaction and rolls back its fixtures. It checks:

- equal local policy identifiers in two independent identity scopes;
- equal group-policy bindings in two independent identity scopes;
- scoped updates affecting only the selected scope;
- stale policy updates matching zero rows after another writer advances `row_version`.

## Security Boundary

The new stores are server-side infrastructure only.

No public permission-management endpoint, authentication endpoint, OIDC flow, MFA operation, TRN compiler, or RBAC evaluator is exposed by this version.

A stored capability, policy statement, or binding is never proof that a request is currently authorized.
