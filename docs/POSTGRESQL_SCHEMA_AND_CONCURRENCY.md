# PostgreSQL Schema, Migration Integrity, and Concurrency

**Repository version:** 0.64.0  
**Database scope:** Generic Identity & Access  
**Owned PostgreSQL schema:** `identity_access`

This document describes the PostgreSQL model implemented by the repository migrations and the storage contracts that depend on it. It is intentionally derived from the migration history rather than from an idealized domain model.

## 1. Database boundary

The default local development database is:

```text
generic_identity_access_default
```

The owned schema inside a routed PostgreSQL destination is:

```text
identity_access
```

A database name is a deployment detail, not an identity or authorization boundary. The API resolves a trusted database route before storage access. Clients do not provide connection strings, destination keys, secret references, or database names.

The current migrations do **not** create a central routing-catalog database. Routing configuration is external to the routed identity schema. A future routing provider can be introduced behind the existing resolver contract without changing the identity model described here.

## 2. Schema at a glance

The final schema is organized into the following logical areas.

```text
identity_access
|
+-- Directory and tenancy
|   +-- users
|   +-- tenants
|   +-- tenant_memberships
|   +-- user_groups
|   +-- group_memberships
|
+-- Application security model
|   +-- application_security_models
|   +-- application_capabilities
|   +-- application_security_model_registrations
|   +-- application_security_namespaces
|
+-- Resource scopes
|   +-- application_scope_types
|   +-- resource_scopes
|
+-- Active managed-policy authorization
|   +-- managed_policies
|   +-- managed_policy_versions
|   +-- managed_policy_statements
|   +-- managed_group_policy_bindings
|
+-- Historical tenant-policy schema
|   +-- permission_policies
|   +-- policy_statements
|   +-- group_policy_bindings
|
+-- Identity-scope administration authority
|   +-- identity_scope_administration_groups
|   +-- identity_scope_administration_group_memberships
|   +-- identity_scope_administration_policies
|   +-- identity_scope_administration_policy_statements
|   +-- identity_scope_administration_group_policy_bindings
|
+-- Local authentication and OIDC
|   +-- password_credentials
|   +-- user_sessions
|   +-- oidc_authorization_codes
|   +-- oidc_refresh_tokens
|
+-- MFA
|   +-- mfa_policies
|   +-- mfa_policy_providers
|   +-- user_authenticators
|   +-- totp_authenticators
|   +-- recovery_code_sets
|   +-- recovery_codes
|   +-- webauthn_registration_challenges
|   +-- webauthn_credentials
|   +-- webauthn_authentication_challenges
|
+-- Audit and mutation evidence
|   +-- security_events
|   +-- security_mutation_events
|
+-- Migration metadata
    +-- schema_migrations
```

The retired `group_templates` table is **not** part of the final schema. Reusable templates are normal `user_groups` rows with `is_template = true`.

## 3. Core directory and tenant containment

### `users`

Stable user identity within an identity scope.

Primary key:

```text
(identity_scope_id, user_id)
```

The table stores display name, status, row version, and timestamps. Authentication credentials are deliberately stored separately.

### `tenants`

Tenant identity within an identity scope.

Primary key:

```text
(identity_scope_id, tenant_id)
```

### `tenant_memberships`

Connects users to tenants.

Primary key:

```text
(identity_scope_id, membership_id)
```

Important constraints:

```text
UNIQUE (identity_scope_id, tenant_id, user_id)
UNIQUE (identity_scope_id, tenant_id, membership_id)
```

The second unique key exists so tenant-scoped group membership can reference a membership while preserving tenant containment in the foreign key.

### `user_groups`

Tenant/application-scoped security groups.

Primary key:

```text
(identity_scope_id, tenant_id, application_key, group_id)
```

Final reusable-template representation:

```text
is_template boolean NOT NULL DEFAULT FALSE
```

A reusable template is therefore a real group with its normal managed-policy bindings. There is no parallel template identity model in the final schema.

`origin` and `template_id`, introduced by the historical separate-template design, are retired by the final simplification migration.

### `group_memberships`

Connects a tenant membership to a group.

Primary key:

```text
(identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
```

Composite foreign keys enforce that both the group and the member belong to the same persisted tenant boundary.

## 4. Application security model and capability coordinates

### `application_security_models`

Pins a versioned security model for an application.

Primary key:

```text
(identity_scope_id, application_key, model_version)
```

### `application_capabilities`

Stores the concrete capabilities declared by a security model.

After migration `0005_rbac_capability_alignment.sql`, the persisted capability tuple is:

```text
capability_resource
capability_feature
capability_action
```

Primary key:

```text
(identity_scope_id,
 application_key,
 model_version,
 capability_resource,
 capability_feature,
 capability_action)
```

The RBAC project and allowed RBAC namespaces are not stored in this table. They are registered separately by the manifest catalog introduced in migration `0021`.

### `application_security_model_registrations`

Records application-manifest provenance for each security-model version, including:

```text
manifest_schema_version
rbac_project
manifest_sha256
```

### `application_security_namespaces`

Stores the allowed RBAC namespaces for a registered model version.

This separation means that a managed-policy statement selects an application capability, while RBAC project/namespace context is supplied by the registered application security model.

## 5. Active authorization path: managed policies

The current runtime/administration grant path uses published managed policies.

```text
User
  -> TenantMembership
  -> GroupMembership
  -> UserGroup
  -> ManagedGroupPolicyBinding
  -> Published ManagedPolicyVersion
  -> ManagedPolicyStatement
  -> ApplicationCapability
  -> external RBAC evaluation
```

### `managed_policies`

Tenant-independent policy identity scoped by identity scope and application.

Primary key:

```text
(identity_scope_id, application_key, policy_id)
```

A stable `policy_key` is unique per identity scope/application. `default_version`, when present, must reference a published version.

### `managed_policy_versions`

Versioned policy content pinned to an application security-model version.

Primary key:

```text
(identity_scope_id, application_key, policy_id, policy_version)
```

Migration `0025_managed_policy_publication.sql` adds `published_at` and database guards that make a published version immutable.

### `managed_policy_statements`

Statements belong to one concrete managed-policy version and reference a concrete application capability.

The current managed-policy schema uses exact capability foreign keys. The wildcard persistence rules introduced for the older tenant-policy schema do not replace this exact managed-policy capability reference.

### `managed_group_policy_bindings`

Tenant-scoped assignment of one **published** managed-policy version to one group.

Important columns:

```text
identity_scope_id
tenant_id
application_key
group_id
policy_id
policy_version
resource_scope_id      NULL = tenant-wide
include_descendants
binding_target_key     generated
```

The generated `binding_target_key` allows tenant-wide and resource-scoped grants to coexist in the primary key.

A database trigger rejects a binding to an unpublished managed-policy version.

## 6. Historical tenant-policy schema

The following tables remain in the database for migration continuity and retained historical data:

```text
permission_policies
policy_statements
group_policy_bindings
```

They are **not** the active runtime grant source in version 0.64.0.

Migration `0006_persistent_wildcard_policy_patterns.sql` changed the historical `policy_statements` model from a strict concrete-capability foreign key to a model-pinned wildcard pattern validated by a trigger. Supported wildcard statements must still match at least one declared capability in the pinned security model.

These historical tables must not be removed casually: existing databases have checksum-protected migration history and may retain audit-relevant rows.

## 7. Resource-scope hierarchy

### `application_scope_types`

Declares application-defined resource-scope types for a security-model version and their parent type relationship.

### `resource_scopes`

Tenant-owned resource instances with:

```text
scope_model_version
scope_type_key
external_resource_id
display_name
parent_resource_scope_id
status
row_version
```

A validation trigger enforces the declared hierarchy, including parent type, model-version compatibility, and active-parent requirements.

Resource scope is a **grant narrowing mechanism**. It is not where capabilities such as `user / read` are selected. Capabilities belong to policy statements; an optional resource scope limits where the resulting policy binding applies.

## 8. Reusable groups and template lifecycle

The final schema deliberately has no independent `GroupTemplate` security entity.

```text
user_groups.is_template = false   ordinary group
user_groups.is_template = true    reusable group definition
```

The historical evolution is:

```text
0026_group_templates.sql
    introduced the temporary separate group_templates catalogue and
    origin/template_id provenance on user_groups

0027_group_template_flag_foundation.sql
    historical transition migration; retained unchanged for already-migrated databases

0028_simplify_group_templates.sql
    completes the transition to user_groups.is_template and retires
    group_templates, origin, and template_id
```

Migration `0028` performs explicit cleanup of only the known artificial development template records and their known relationship edges. It does not use a broad `CASCADE`. If unexpected non-development rows remain in the retired global catalogue, the migration fails instead of silently discarding them.

The database stores the reusable marker and normal group relationships. Higher-level rules such as "only identity-scope administration may promote a group to reusable" and "Create from template copies policy bindings but never memberships" are server authorization/application-service invariants. They are not inferred from `is_template` alone.

## 9. Identity-scope administration authority

Identity-scope administration is intentionally separate from tenant group authorization.

Tables:

```text
identity_scope_administration_groups
identity_scope_administration_group_memberships
identity_scope_administration_policies
identity_scope_administration_policy_statements
identity_scope_administration_group_policy_bindings
```

These records have no `tenant_id` because they represent authority over the identity-scope administration surface. Tenant-level permissions are not promoted into this authority path.

Identity-scope administration statements support validated whole-segment wildcard patterns and are pinned to an application security-model version.

## 10. Local authentication and sessions

### `password_credentials`

Stores login identity and password-verification state separately from `users`.

Important constraints include:

```text
UNIQUE (identity_scope_id, normalized_login_identifier)
password_hash is non-empty
failed_access_count >= 0
row_version > 0
```

The `users` table therefore does not need to treat an e-mail address or login string as the immutable user identity.

### `user_sessions`

Stores opaque local sessions using a unique SHA-256-sized token hash rather than the raw token.

Migration `0019_session_authentication_assurance.sql` adds durable assurance state:

```text
assurance_level
assurance_methods
assurance_verified_at
```

The constraints distinguish password-only sessions from sessions that have been raised to multi-factor assurance.

## 11. OAuth 2.0 / OpenID Connect persistence

### `oidc_authorization_codes`

Persists one-time hashed Authorization Code + PKCE state. The schema constrains:

```text
scope = 'openid'
code_challenge_method = 'S256'
32-byte code hash
registered session/user linkage
single consumed_at timestamp
```

### `oidc_refresh_tokens`

Persists rotating refresh-token families using hashed tokens and explicit parent/sequence lineage.

Important state includes:

```text
family_id
token_id
parent_token_id
sequence_number
consumed_at
revoked_at
revocation_reason
```

Migration `0020_credential_security_hardening.sql` expands valid revocation reasons to include credential change and account recovery in addition to reuse detection.

The current PostgreSQL migrations do **not** create an OIDC client-registration table or signing-key table. Those concerns are configured and managed outside this schema in the current implementation.

## 12. MFA persistence

### Policy and provider selection

```text
mfa_policies
mfa_policy_providers
```

### Generic authenticator identity

```text
user_authenticators
```

Provider-specific records use the generic authenticator identity rather than adding provider columns to the user table.

### TOTP

```text
totp_authenticators
```

The table stores a protected secret and replay state (`last_accepted_time_step`). The current migration constrains the persisted TOTP profile to SHA-1, six digits, and a 30-second period.

### Recovery codes

```text
recovery_code_sets
recovery_codes
```

Recovery codes are persisted as SHA-256 hashes and consumed atomically through `consumed_at`. The schema allows only one active recovery authenticator per user.

### WebAuthn

```text
webauthn_registration_challenges
webauthn_credentials
webauthn_authentication_challenges
```

Credential storage includes credential ID, COSE public key, ES256 algorithm constraint, AAGUID, signature counter, backup eligibility/state, and a fixed-size user handle.

Provider-owned dependent rows use `ON DELETE CASCADE` where the generic authenticator is the owner. Core identity, tenant, group, policy, and session relationships primarily use `ON DELETE RESTRICT`.

## 13. Security audit versus mutation ledger

Two distinct audit concepts are persisted.

### `security_events`

Semantic application-level security events such as authentication or administration outcomes. These records include optional user, tenant, client, target, reason, and correlation fields.

### `security_mutation_events`

Database-level append-only evidence of security-sensitive row mutations.

Each row records:

```text
transaction_id
identity_scope_id
application_key
table_name
operation
record_key
actor identity/session/client context
correlation_id
database_role
```

A database trigger rejects `UPDATE` and `DELETE` against the mutation ledger itself.

Security-sensitive tables added after migration `0011` install their own mutation trigger when introduced.

The semantic audit stream and the mutation ledger serve different purposes and should not be collapsed into one model.

## 14. Migration metadata and integrity

`identity_access.schema_migrations` is created by the migration runner rather than by a numbered migration.

```text
version      integer primary key
name         varchar(200) not null
checksum     char(64) not null after migration initialization/finalization
applied_at   timestamptz not null
```

Migration filenames must use:

```text
NNNN_name.sql
```

The migration version is the four-digit prefix. Reusing an already-applied version with another filename is an integrity violation.

Checksums use SHA-256 over UTF-8 SQL text after normalizing line endings to LF. Already-applied migrations are therefore append-only history: changing their SQL content causes checksum validation to fail.

The migration runner also rejects an applied migration version that is no longer present in the embedded/current migration set.

### Historical migration rule

Once a migration is recorded, do not:

- rename it;
- reuse its numeric version;
- edit its SQL;
- delete it from the repository;
- manually rewrite its checksum metadata.

Corrections are made through a new migration.

## 15. Migration history

| Version | Migration | Durable effect |
|---:|---|---|
| 0001 | `identity_directory` | Users, tenants, memberships, tenant groups, group memberships. |
| 0002 | `directory_query_indexes` | Directory read indexes. |
| 0003 | `permission_policy_foundation` | Versioned application capabilities and the historical tenant-policy schema. |
| 0004 | `assignment_projection_indexes` | Assignment/projection indexes. |
| 0005 | `rbac_capability_alignment` | Aligns persisted capability coordinates to resource / feature / action. |
| 0006 | `persistent_wildcard_policy_patterns` | Historical tenant-policy wildcard pattern persistence and validation. |
| 0007 | `local_authentication_foundation` | Password credentials and opaque sessions. |
| 0008 | `resource_scope_hierarchy` | Scope-type hierarchy, resource scopes, scoped historical bindings. |
| 0009 | `security_audit` | Semantic security-event table and indexes. |
| 0010 | `identity_scope_administration_authority` | Dedicated scope-wide administration groups/policies/bindings. |
| 0011 | `transactional_security_mutation_ledger` | Append-only mutation ledger and security-table triggers. |
| 0012 | `oidc_authorization_code_pkce` | Hashed one-time OIDC authorization codes with PKCE state. |
| 0013 | `oidc_refresh_token_rotation` | Rotating hashed refresh-token families and replay lineage. |
| 0014 | `mfa_provider_foundation` | MFA policy, provider allow-list, generic authenticators. |
| 0015 | `totp_provider` | Provider-specific TOTP persistence. |
| 0016 | `recovery_provider` | Hash-only recovery-code sets and codes. |
| 0017 | `webauthn_registration` | WebAuthn registration challenges and credentials. |
| 0018 | `webauthn_authentication` | WebAuthn authentication challenges. |
| 0019 | `session_authentication_assurance` | Durable session/OIDC authentication-assurance state. |
| 0020 | `credential_security_hardening` | Refresh-token invalidation reasons for credential change/recovery. |
| 0021 | `application_security_manifest_catalog` | Manifest fingerprint, RBAC project, allowed namespaces. |
| 0022 | `administration_search_indexes` | Case-normalized administration search indexes. |
| 0023 | `managed_policy_catalog` | Tenant-independent managed policies, versions, exact statements. |
| 0024 | `managed_policy_bindings` | Tenant group bindings to concrete managed-policy versions. |
| 0025 | `managed_policy_publication` | Publication state, immutability, published-default/binding guards. |
| 0026 | `group_templates` | Historical temporary separate group-template model. |
| 0027 | `group_template_flag_foundation` | Historical transition migration retained unchanged. |
| 0028 | `simplify_group_templates` | Final `user_groups.is_template` model; retires separate template table/provenance. |

The authoritative SQL is always the migration file itself. This table describes durable intent and must not be used as a substitute for migration integrity validation.

## 16. Concurrency model

### Operation isolation

Each persistence operation receives one resolved database-route snapshot and opens storage against that destination. Mutable process-global "current user", "current tenant", or "current database" state is not part of the storage contract.

Npgsql may pool physical connections, but request/operation state remains isolated.

### Optimistic concurrency

Mutable administrative records use positive `row_version` values. Mutation stores condition writes on the expected version and increment it on success. A stale version produces a concurrency failure rather than silently overwriting a concurrent change.

Representative tables with row-version state include users, tenants, memberships, groups, policies, resource scopes, credentials, MFA policy/authenticator records, and provider-specific records that require replay-safe mutation.

### Migration concurrency

The embedded .NET migrator takes a PostgreSQL advisory transaction lock before migration work. Migration metadata and applied SQL are committed transactionally.

The PowerShell provisioning path applies each numbered migration with `psql --single-transaction` and records its metadata in the same invocation.

## 17. Foreign-key and deletion policy

Core ownership relationships prefer `ON DELETE RESTRICT` so identity, tenant, group, policy, session, and authorization data cannot disappear through an unrelated parent deletion.

`ON DELETE CASCADE` is intentionally limited to tightly owned dependent records, principally:

- MFA provider selections owned by an MFA policy;
- TOTP/recovery/WebAuthn provider records owned by a generic authenticator;
- recovery codes owned by a recovery-code set;
- transient WebAuthn authentication challenges owned by a user.

The group-template retirement migration performs explicit relationship cleanup instead of broad cascade deletion.

## 18. Multi-database implications

The same schema can exist in multiple routed PostgreSQL destinations. Composite identity keys keep logical identity independent from the physical database name.

The schema does not provide cross-database foreign keys or distributed transactions. If related data is placed in separate destinations, consistency must be handled by the application-level routing and operation contract.

A route change is not a data migration. Moving identity data between destinations requires an explicit operational migration procedure.

## 19. What is intentionally outside this schema

The current PostgreSQL migration set does not define:

- a central database-routing catalog;
- product/business-domain tables from consuming applications;
- external RBAC engine storage;
- OIDC signing-key persistence;
- an OIDC client-registration table;
- browser session cookies or browser-side authorization state;
- application-specific profile extension tables.

Those boundaries are deliberate. This repository owns generic identity, authentication, authorization metadata, and security evidence rather than consumer-application business data.

## 20. Operational validation

Create the local database when required:

```powershell
psql -U postgres -d postgres -f .\scripts\postgresql\create-default-database.sql
```

Apply and verify migrations:

```powershell
$env:PGPASSFILE = (Resolve-Path .\scripts\postgresql\.pgpass.local).Path
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-migration-integrity.ps1
```

Validate the current group-as-template schema:

```powershell
.\scripts\postgresql\verify-group-as-template.ps1
```

Run the repository verification:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Production qualification additionally includes live PostgreSQL security/concurrency checks and a disposable backup/restore validation. See `docs/PRODUCTION_QUALIFICATION.md` and `docs/VALIDATION.md`.

## 21. Documentation authority

For schema questions, use the following order of authority:

1. numbered migration SQL in `src/IdentityAccess.Infrastructure.PostgreSql/Migrations`;
2. migration-integrity metadata and validation scripts;
3. PostgreSQL persistence implementation and tests;
4. this document as the human-readable schema reference.

When documentation and a migration differ, the migration is authoritative and the documentation must be corrected.
