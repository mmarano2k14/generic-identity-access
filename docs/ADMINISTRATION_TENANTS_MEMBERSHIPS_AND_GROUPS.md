# Tenant, Membership, Group, and Policy Administration

## Purpose

This document defines the stable administration model for tenants, users, memberships, groups, reusable group templates, managed policies, and user assignment.

The model is application-agnostic. Consuming applications publish their own capability catalog through the application security manifest; Identity & Access owns the administration and authorization plumbing around those capabilities.

## Core model

```text
User
  -> TenantMembership
  -> GroupMembership
  -> UserGroup
  -> Managed Policy Binding
  -> Published Managed Policy Version
  -> Capability statements
  -> optional Resource Scope restriction
  -> external RBAC evaluation
  -> ALLOW / DENY
```

The backend is authoritative. UI visibility, hidden actions, route metadata, resource identifiers, or TRNs are never treated as proof of authorization.

## Tenants and memberships

A `Tenant` is the administration boundary for tenant-scoped users, groups, and grants. A `TenantMembership` records that a user belongs to a tenant and has its own lifecycle state.

The administration UI is tenant-centric:

- identity-scope administration can list authorized tenants and member counts;
- an empty tenant can be created without creating a user at the same time;
- tenant-scoped administration can add an existing user by exact login when authorized;
- tenant-scoped administration does not expose a browsable global user directory;
- adding a tenant membership does not grant group permissions automatically.

## Groups

A `UserGroup` is a real tenant-scoped group. Group membership and policy assignment are separate relationships.

A normal group contains:

```text
UserGroup
  - identity scope
  - tenant
  - application
  - group identifier
  - display name
  - status
  - is_template
  - memberships
  - managed-policy bindings
```

A user receives a group's effective permissions only after the user is a member of that group and the group has applicable published managed-policy bindings.

## Reusable group templates

A reusable template is not a separate security entity. It is a real `UserGroup` with:

```text
is_template = true
```

Only identity-scope administration may mark or unmark a group as reusable or mutate the definition of a reusable group.

`Create from template` creates a new normal group in the target tenant. The operation:

- copies compatible managed-policy bindings;
- re-evaluates delegation against the target tenant;
- validates resource-scope portability when a binding is scoped;
- creates the target group with `is_template = false`;
- never copies memberships from the source group.

The source group remains a real group in its original tenant. Reusability is a property of that group, not a second catalog entity.

## Managed policies

Capabilities are selected when a managed policy version is authored.

Example capability statements:

```text
user / read
user / write
group / read
group / write
tenant-membership / read
tenant-membership / write
```

The exact coordinates are validated against the registered application security manifest. A managed policy version becomes grantable only after publication.

A group receives permissions by binding a published managed-policy version to the group.

## Resource scope

`Resource scope` is an optional restriction on a managed-policy binding. It narrows where the selected policy applies; it does not select permissions.

For example:

```text
Managed policy
  Application User Manager

Capabilities
  user / read
  user / write

Resource scope
  No resource scope
```

means the published policy carries the permissions. Choosing a resource scope would restrict those permissions to the selected resource hierarchy.

A field labelled `Resource scope` must therefore never be interpreted as the place to choose `user / read`, `group / write`, or any other capability.

## Assigning users

The supported assignment flow is:

```text
1. Create or select a tenant.
2. Ensure the user has a TenantMembership in that tenant.
3. Create a group or create one from an authorized reusable template.
4. Bind one or more published managed policies to the group.
5. Add the tenant member to the group through Manage groups.
6. Verify the protected backend operation produces the expected ALLOW or DENY decision.
```

`Manage groups` changes group membership only. It does not create groups, clone templates, publish policies, or widen policy scopes.

## Authority boundaries

Identity-scope administration and tenant administration are distinct authorities.

Identity-scope administration may perform operations such as:

- manage reusable group definitions;
- mark or unmark a group as reusable;
- administer scope-level authority;
- use broader directory lookup where explicitly authorized.

Tenant-scoped administration may perform only the operations granted for the target tenant. It must not:

- enumerate another tenant's directory;
- assign another tenant's groups;
- promote a group into a reusable template;
- mutate a reusable group definition;
- copy grants that exceed its authority in the target tenant.

## Administration UI expectations

The Groups workspace uses one group table. Each row exposes the reusable state as `Template: Yes/No`.

The UI must not expose a second global `Available group templates` catalog. Reusable source groups are surfaced through the explicit `Create from template` workflow.

The Memberships workspace presents group assignment separately from permission definition. A tenant member can be assigned to existing groups through `Manage groups` without exposing a global user directory.

Managed-policy binding UI should make the distinction between permission content and resource restriction clear:

```text
Managed policy
  <published policy>

Permissions included
  <read-only summary of policy statements>

Resource restriction
  No resource scope
  or
  <authorized resource scope>
```

## Persistence and migration compatibility

Historical migrations are immutable once recorded in `identity_access.schema_migrations` because migration integrity is checksum-protected.

The current schema transition preserves:

```text
0026_group_templates.sql
0027_group_template_flag_foundation.sql
```

and applies the append-only cleanup migration:

```text
0028_simplify_group_templates.sql
```

The current schema uses `user_groups.is_template` and no longer uses `identity_access.group_templates`, `user_groups.origin`, or `user_groups.template_id` as active model elements.

## Validation

Repository validation:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

PostgreSQL group-model validation:

```powershell
$env:PGPASSFILE = (Resolve-Path .\scripts\postgresql\.pgpass.local).Path
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-group-as-template.ps1
```

Real-browser administration validation:

```powershell
.\scripts\verify-admin-ui-browser-qualification.ps1
```

Complete administration qualification:

```powershell
.\scripts\verify-administration-qualification.ps1 `
  -RbacReferenceDirectory "<multiplexed-rbac-release-directory>" `
  -Configuration Release
```

A complete result requires repository verification, live PostgreSQL validation, backup/restore validation, and real-browser evidence.
