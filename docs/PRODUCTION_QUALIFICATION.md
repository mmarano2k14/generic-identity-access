# Production Qualification and Operational Hardening

Version 0.41.0 consolidates the executable qualification gates required before a deployment
candidate is treated as production-ready. It does not claim that a deployment is safe merely
because the repository compiles or a nominal login succeeds.

## Scope

The qualification surface covers:

- repository build, architecture, unit, and integration tests;
- external Multiplexed RBAC compatibility;
- PostgreSQL schema, isolation, concurrency, mutation, audit, session, and OIDC invariants;
- migration checksum integrity;
- backup and restore through a disposable PostgreSQL database;
- restored-schema structural validation;
- group-as-template migration/schema validation;
- operational key/secret handling and restart boundaries.

The generic MFA provider foundation, concrete TOTP/recovery/WebAuthn providers, 0.48 provider-policy/administration hardening, and 0.49 local-session assurance/OIDC integration are included in repository qualification gates. Provider-specific and session-assurance live PostgreSQL scripts remain separate dedicated gates and are not yet invoked by this consolidated production-qualification command. End-to-end browser interaction policy for arbitrary non-OIDC sensitive operations remains a consuming-application concern rather than a repository-wide declarative step-up feature.

## Primary qualification command

Run the qualification against a dedicated non-production database with the current schema applied:

```powershell
$env:PGPASSWORD = "<postgres-password>"
$env:IDENTITY_ACCESS_POSTGRES_DATABASE = "generic_identity_access_default"
$env:IDENTITY_ACCESS_POSTGRES_USER = "postgres"

.\scripts\verify-production-qualification.ps1 `
  -RbacReferenceDirectory "D:\Dev\Personal\multiplexed-rbac\implementations\dotnet\src\Multiplexed.Rbac.Core\bin\Release\net10.0"

Remove-Item Env:PGPASSWORD
```

The database gates exercise security-sensitive fixtures. They are intended for a qualification
or CI database, not a live production database.

## Qualification gate order

The orchestrator executes:

```text
repository source-consistency gates
        |
        v
restore / build / tests
        |
        v
external RBAC compatibility
        |
        v
migration integrity
        |
        v
PostgreSQL isolation / concurrency / persistence / group-as-template gates
        |
        v
authentication / session / audit / authority gates
        |
        v
OIDC authorization-code / refresh-token gates
        |
        v
backup -> isolated restore -> restored schema validation
```

A failure stops qualification. A technical failure is not converted into a successful or denied
security result.

## Backup and restore qualification

`verify-backup-restore.ps1` requires PostgreSQL client tools:

```text
psql
pg_dump
pg_restore
createdb
dropdb
```

The script:

1. verifies source migration metadata against repository migration checksums;
2. creates a custom-format backup;
3. creates a randomly named scratch database using the prefix
   `generic_identity_access_restore_`;
4. restores the backup with owner/privilege replay disabled;
5. reruns migration-integrity validation against the restored database;
6. validates required tables, migration checksums, validated constraints, valid indexes, and
   absence of forbidden raw-secret columns;
7. drops the scratch database in `finally` unless `-KeepRestoredDatabase` is explicitly supplied;
8. deletes the temporary dump file.

A backup that has never been restored is not treated as proof of recoverability.

The source database is read by `pg_dump`; the scratch database is disposable. Qualification
should still be executed against a controlled non-production source so the test environment and
backup contents are known and repeatable.

## Complete administration qualification

Production qualification does not by itself prove the browser administration workflows. For complete administration qualification, use:

```powershell
.\scripts\verify-administration-qualification.ps1 `
  -RbacReferenceDirectory "<multiplexed-rbac-release-directory>" `
  -Configuration Release
```

The wrapper runs production qualification first, then pauses before real-browser evidence. Keep these processes running in separate terminals before continuing:

```text
Identity & Access API   http://127.0.0.1:5080
Next.js Admin           http://127.0.0.1:3000
```

Use `127.0.0.1` consistently with the registered OIDC callback. The browser gate validates OIDC login, tenant/member administration, one-table `Template Yes/No` groups, `Create from template`, managed-policy binding behavior, membership non-copying, tenant isolation, Sessions, MFA, and logout.

A run with `-SkipBrowserQualification` is partial.

## Partial qualification

The main script accepts:

```powershell
-SkipBackupRestore
```

This exists for fast local iterations. When used, the script emits a warning that qualification is
partial. A release candidate should not be described as restore-qualified when this switch was used.

## PostgreSQL data-source initialization recovery

The PostgreSQL connection factory pins a successfully initialized data source per registered
destination for the process lifetime. Version 0.41.0 hardens initialization failure behavior:

```text
resolve trusted secret reference
        |
        v
construct Npgsql data source
        |
        +-- success -> process-pinned registration
        |
        +-- failure -> remove failed Lazy registration
                       + return stable InvalidConnectionString failure
                       + do not retain parser exception details
```

A malformed connection-string value therefore cannot permanently poison the destination entry
until process restart. A later request can resolve the trusted secret again and retry initialization.

The public storage exception deliberately does not retain the connection-string parser exception
for `InvalidConnectionString`, preventing raw secret values from being exposed through exception
serialization or broad exception logging.

A successfully initialized destination remains pinned to its original secret reference for the
process lifetime. Changing a secret reference or signing-key configuration is an operational
restart-controlled action; callers cannot mutate those authorities through an API request.

## Restart and secret/key operations

### PostgreSQL connection secrets

- connection strings remain outside repository configuration;
- routing stores only opaque `env:` secret references;
- a successfully constructed data source is process-pinned;
- rotate the backing secret through the trusted secret store/environment and perform a controlled
  restart of API instances;
- drain/restart instances progressively when multiple replicas are deployed;
- validate readiness and a known database operation before returning each instance to service.

### OIDC signing keys

Use the controlled rollover already documented in `OIDC_AUTHORIZATION_CODE_PKCE.md`:

```text
pre-publish retained/new public key
        -> restart and verify JWKS
activate new private signing key
        -> restart and verify new kid issuance
retain old public key until issued JWT lifetime is exhausted
        -> remove retired key on a later controlled restart
```

Do not remove a validation key while unexpired tokens bearing its `kid` are expected to remain
valid.

## Failure and attack-path expectations

Production qualification preserves these fail-closed properties:

- unknown/disabled/ambiguous database routes never fall back to another destination;
- invalid connection strings do not expose raw connection-string values;
- a failed data-source initialization is retryable and does not create permanent in-process poison;
- stale mutation row versions fail rather than overwrite current state;
- technical RBAC failure remains distinct from explicit denial;
- Bearer signature/issuer/audience/session-continuity failures do not become authorization success;
- refresh-token reuse revokes the family while the public client receives only `invalid_grant`;
- migration metadata that differs from repository files blocks qualification;
- restored schemas with invalid indexes, unvalidated constraints, missing security tables, or raw
  secret columns block restore qualification.

## What this release does not prove by itself

Repository qualification is necessary but not sufficient for every deployment environment. The
following remain deployment-specific evidence:

- infrastructure capacity under the target traffic profile;
- PostgreSQL server sizing, replication, failover, and backup retention;
- network and TLS termination configuration;
- secret-store permissions and rotation automation;
- production observability, alert routing, and incident response;
- restore time objective and recovery point objective under real backup volume;
- operating-system/container hardening;
- penetration testing or independent security review.

Those results should be recorded with the exact environment, version, configuration, and date.

## Administration-host failure semantics

Version `0.58.0` hardens the runnable Next.js administration host without changing the backend security protocols. Protected mutation failures are projected into secret-safe categories before reaching Client Components. In particular, optimistic-concurrency `409`, authentication `401`, authorization `403`, dependency `503`, timeout, transport, protocol, cancellation, and configuration failures remain distinguishable in the host UX.

For mutations, lack of a usable response is not interpreted as a confirmed failure or success. Timeout, transport, cancellation, and invalid-protocol cases require reloading current server state before deciding whether to retry. This avoids blindly replaying a mutation whose server-side outcome is unknown to the browser.

A protected administration context that becomes unauthenticated during server-side navigation authorization is returned to sign-in. Authorization refusal remains separate, and technical dependency failures are not converted into RBAC denial. Session-security audit evidence may degrade independently from separately authorized containment controls; missing evidence never becomes inferred session state.


Version `0.59.0` closes the administration presentation-consistency pass without changing backend qualification semantics. The reusable host must preserve a focusable skip target, route-aware mobile disclosure behavior, dialog focus restoration, explicit field descriptions, pending-state semantics, forced-colors support, increased-contrast support, reduced motion, automatic dark mode, and dynamic viewport-height behavior. These checks are presentation gates only; they do not substitute for the repository security, PostgreSQL, restore, concurrency, or failure qualification described here.
