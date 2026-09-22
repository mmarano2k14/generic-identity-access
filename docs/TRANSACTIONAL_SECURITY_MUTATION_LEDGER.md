# Transactional Security Mutation Ledger

Identity Access maintains a database-level mutation ledger for security-sensitive persisted
state.

The ledger is separate from the higher-level semantic security event stream.

## Guarantee

Security-sensitive table mutations are captured by PostgreSQL `AFTER` triggers.

The ledger insert executes inside the same PostgreSQL transaction as the source mutation:

```text
source mutation COMMIT
    -> mutation ledger event COMMIT

source mutation ROLLBACK
    -> mutation ledger event ROLLBACK
```

This removes the failure window that exists when an application writes an audit event only
after the primary mutation has already committed.

## Storage

Migration `0011_transactional_security_mutation_ledger.sql` adds:

```text
identity_access.security_mutation_events
```

Each event stores:

```text
transaction id
timestamp
identity scope
application key when present
table name
INSERT / UPDATE / DELETE
safe record-key identifiers
actor identity scope
actor user
actor session
actor client
actor application
authentication context
correlation id
database role
```

The table is append-only. PostgreSQL rejects `UPDATE` and `DELETE` against ledger rows.

## Secret Exclusion

The generic trigger receives an explicit safe key-column list for each table.

It never stores complete rows.

In particular, mutation records do not include:

```text
password
password_hash
session token
token_hash
login identifier
connection string
secret reference
request body
arbitrary payload
```

For example, a password credential mutation records only:

```json
{
  "identity_scope_id": "...",
  "user_id": "..."
}
```

A session mutation records only:

```json
{
  "identity_scope_id": "...",
  "session_id": "..."
}
```

## Actor Provenance

Trusted administration identity is projected into the current ASP.NET Core `Activity`.

Before each PostgreSQL connection is returned to application code, the connection factory
overwrites a fixed set of PostgreSQL session settings:

```text
identity_access.correlation_id
identity_access.actor_identity_scope_id
identity_access.actor_user_id
identity_access.actor_session_id
identity_access.actor_client_id
identity_access.actor_application_key
identity_access.authentication_context_key
```

The mutation trigger copies these safe identifiers into the ledger.

No raw session credential is propagated.

Connections without an authenticated administration context still produce transactional
mutation records. Actor fields are nullable and `database_role` plus the PostgreSQL
transaction identifier remain available.

## Coverage

The transactional ledger covers persisted mutation tables for:

```text
users
tenants
tenant memberships
tenant groups
group memberships
application security models
application capabilities
tenant policies
tenant policy statements
tenant group-policy bindings
password credentials
local sessions
application scope types
resource scopes
identity-scope authority groups
identity-scope authority memberships
identity-scope authority policies
identity-scope authority statements
identity-scope authority bindings
```

The semantic `security_events` table is intentionally not mutation-triggered to avoid
recursive audit noise.

## Semantic Audit Versus Transactional Ledger

The two audit surfaces serve different purposes.

### Transactional mutation ledger

Provides the durable persistence guarantee:

```text
what persisted state changed?
which row identity changed?
which transaction committed it?
which trusted actor/correlation context was attached?
```

### Semantic security events

Provide higher-level application meaning:

```text
PasswordChanged
PolicyBindingAdded
PasswordLoginFailed
SessionRevoked
```

Semantic events remain best-effort enrichment because they may describe denied or
non-persistent events that do not belong inside a database mutation transaction.

For persisted state changes, the transactional mutation ledger is the authoritative
durability record.

## Transaction Rollback Proof

The validation fixture uses a PostgreSQL savepoint:

```text
create source row
    -> ledger row exists

ROLLBACK TO SAVEPOINT
    -> source row disappears
    -> ledger row disappears
```

This demonstrates transaction co-commit rather than eventual or post-commit logging.

## Validation

After applying migration 0011:

```powershell
.\scripts\postgresql\verify-transactional-security-audit.ps1
```

The validation checks:

- actor and correlation propagation;
- INSERT and UPDATE capture;
- secret-safe credential record keys;
- rollback co-commit behavior;
- append-only enforcement.

All fixture data is rolled back.
