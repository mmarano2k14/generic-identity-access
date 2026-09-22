# Atomic Mutations and Migration Integrity

Identity Access treats validation and mutation as one database operation whenever a write
depends on current persisted state from multiple records.

This avoids time-of-check/time-of-use races caused by reading several records on separate
connections and writing after those reads have become stale.

## Atomic Group Membership Creation

Group membership creation now uses `IGroupMembershipMutationStore`.

The PostgreSQL implementation performs one `INSERT ... SELECT` statement that requires:

- the target group to exist;
- the group to be active;
- the tenant membership to exist in the same tenant;
- the tenant membership to be active.

The edge is inserted only from the eligible row set. A concurrent state change cannot occur
between a separate validation read and the insert because there is no separate validation
round trip.

## Atomic Policy Binding Creation

Policy binding creation uses `IGroupPolicyBindingMutationStore`.

One PostgreSQL statement verifies:

- the group exists and is active;
- the policy exists and is active;
- the group and policy share the same identity scope, tenant, and application;
- an optional target resource scope exists in the same boundary and is active.

The binding is inserted only when all required rows satisfy the current-state predicates.

## Atomic Credential Administration

Credential creation and password update use `ICredentialMutationStore`.

Subject existence is checked inside the credential mutation statement instead of by a
separate user read.

Password update also preserves optimistic concurrency. A missing subject remains distinct
from a stale credential version.

## Operation Scope

Each atomic mutation still receives one immutable `ResolvedDatabaseRoute`.

No global transaction, cross-database transaction, or mutable process-wide unit of work is
introduced.

Atomicity applies only within one PostgreSQL destination and one logical operation.

## Migration Integrity

`identity_access.schema_migrations` records:

```text
version
name
checksum
applied_at
```

The checksum is SHA-256 over migration SQL after normalizing CRLF and CR line endings to LF.

On the first checksum-aware run, existing rows with a null checksum adopt the checksum of
the currently embedded migration with the same version and name.

After adoption:

- a changed migration name fails migration integrity validation;
- a changed migration body fails checksum validation;
- an applied migration version that no longer exists in the embedded set fails validation;
- newly applied migrations record their checksum immediately.

The checksum column is finalized as `NOT NULL`.

Applied migration files are therefore treated as immutable history.

## Verification

The repository includes:

```text
scripts/postgresql/verify-migration-integrity.ps1
scripts/postgresql/verify-atomic-mutations.ps1
```

Both scripts are read/validation-oriented and use the configured local PostgreSQL
destination. Atomic-mutation SQL fixtures run inside a transaction that is rolled back.
