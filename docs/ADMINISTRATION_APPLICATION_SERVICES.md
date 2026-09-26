# Administration Application Services

**Source version: 0.62.6. Date: September 27, 2026.**

Administration orchestration remains in application-layer services rather than ASP.NET Core controllers. Repository version `0.62.6` closes the tenant-owned permission-policy compatibility surface from active composition.

## Active policy administration services

The active policy-related administration contracts are:

- `IManagedPolicyAdministrationService` for the shared identity-scope/application managed-policy catalog;
- `IManagedPolicyBindingAdministrationService` for tenant-scoped group bindings to concrete managed-policy versions;
- `IIdentityScopeAuthorityAdministrationService` for the separate identity-scope administration authority model.

`IPolicyAdministrationService` and its tenant-owned permission-policy persistence types remain as historical source/schema compatibility artifacts but are no longer registered into the running API.

## Managed policy administration

`ManagedPolicyAdministrationService` owns shared catalog operations:

- bounded managed-policy discovery;
- policy metadata create/update;
- draft version creation pinned to a registered security-model version;
- draft statement add/remove;
- publication and published-default selection.

Managed policy definitions contain no tenant ownership.

## Tenant binding administration

`ManagedPolicyBindingAdministrationService` owns tenant-scoped attachment and removal of managed-policy versions. Binding creation resolves an explicit version or the published default and persists a concrete `ManagedPolicyVersionReference` together with the tenant group and optional tenant resource scope.

## Legacy compatibility closure

The active host no longer:

- registers `IPermissionPolicyStore`, `IPolicyStatementStore`, `IGroupPolicyBindingStore`, or `IGroupPolicyBindingMutationStore`;
- registers `IPolicyAdministrationService`;
- registers an optional API feature for that service;
- discovers `PoliciesController` or `PolicyBindingsController` as MVC controllers.

The two historical controller classes are retained as `[NonController]` source tombstones so source-overlay delivery does not require deleting files. Historical PostgreSQL migrations/tables are also preserved. Neither source retention nor table retention makes legacy rows an authorization source.

## Directory administration

`DirectoryAdministrationService` continues to centralize user, tenant, membership, group and group-member operations. Each operation resolves exactly one database route and reuses that immutable route for all persistence calls in that operation.

## Concurrency and cancellation

Optimistic concurrency remains implemented by the persistence layer and exposed to the HTTP layer as `409 Conflict` for stale mutable records. Active administration contracts require explicit `CancellationToken` parameters and hold no per-request mutable state.

## Security boundary

Controllers remain responsible for route/body binding, OpenAPI metadata, HTTP status mapping and fail-closed handling. Authorization remains server-side. The separate `scopeAuthority` model is unaffected by tenant permission-policy retirement.

## Database

No PostgreSQL migration is introduced in `0.62.6`. The latest migration remains `0025_managed_policy_publication.sql`.
