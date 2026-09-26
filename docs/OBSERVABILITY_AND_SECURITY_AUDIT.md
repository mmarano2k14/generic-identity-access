# Observability, Correlation, and Security Audit

Identity Access uses structured request logging, server-generated correlation identifiers,
and a durable PostgreSQL security audit sink.

## Correlation

Every HTTP response includes:

```text
X-Correlation-ID
```

The value is derived from the current ASP.NET Core activity trace identifier when
available. The server does not treat a client-supplied correlation value as security
context.

Request logs use a structured `CorrelationId` scope and record only:

- HTTP method;
- matched endpoint display name;
- response status;
- elapsed milliseconds.

Request bodies, authorization headers, cookies, query strings, passwords, session tokens,
and connection details are not logged by the correlation middleware.

## Security Audit

Typed contracts:

```text
SecurityAuditEvent
SecurityAuditEventType
SecurityAuditOutcome
SecurityAuditReasonCode
ISecurityAuditWriter
```

The PostgreSQL sink stores events in:

```text
identity_access.security_events
```

Stored data is intentionally limited to categorical event information, logical identifiers,
safe target identifiers, and correlation information. The audit contract contains no
password, password hash, session token, connection string, secret reference, or arbitrary
payload field.

## Audited Operations

Successful security-relevant administration mutations include users, tenants, tenant
memberships, groups, group memberships, policies, policy statements, policy bindings,
scope types, resource scopes, credential creation, and password changes.

Local authentication records password-login success/failure after directory resolution and
session-revocation success/failure after directory resolution.

Requests rejected before a routed identity directory exists are not persisted into a
tenant-routed audit store.

## Administration Read Path

Version `0.56.0` adds a bounded, read-only administration surface over the existing `identity_access.security_events` table. The path does not change event production or persistence semantics.

The read pipeline is intentionally separated by responsibility:

```text
PostgreSQL reader
    -> application administration service
    -> authorized MVC controller
    -> focused TypeScript administration client
    -> server-only Next.js query/load/presentation classes
```

The database reader always scopes records by `identity_scope_id` and `application_key`, orders by newest event first, and applies a bounded offset/limit. Optional exact filters cover tenant, user, event type, outcome, and correlation identifier. The public response contains only the existing categorical and identifier metadata; there is no raw event payload or secret field.

The controller requires the dedicated `identity-access / security-audit / read` administration capability. Query filtering is not authorization, and the audit UI never evaluates or reimplements RBAC.

## Failure Semantics

Audit persistence is non-authoritative for a completed primary operation.

`ISecurityAuditWriter.TryWriteAsync` returns `false` and logs the sink failure when audit
persistence fails. It does not retroactively convert a committed mutation or issued
authentication result into a failed primary operation.

Audit events are durable when the sink succeeds, but are not transactionally co-committed
with every existing application mutation.

## Verification

After applying migrations:

```powershell
.\scripts\postgresql\verify-security-audit.ps1
```


## Transactional Mutation Ledger

Persisted security-sensitive state changes are additionally captured by the PostgreSQL
transactional mutation ledger introduced in migration 0011.

Unlike semantic security-event writes, the mutation-ledger trigger executes in the same
database transaction as the source state change. A rollback therefore removes both the
mutation and its ledger record.

The ledger captures identifiers and provenance only; it never stores password hashes,
session-token hashes, connection information, or complete row payloads.

See `docs/TRANSACTIONAL_SECURITY_MUTATION_LEDGER.md`.
