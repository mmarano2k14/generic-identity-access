# Administration UI end-to-end qualification

## Purpose

This runbook validates the administration experience against the real local stack. Type checking and production builds are necessary, but they are not substitutes for browser-level evidence.

The expected topology is:

```text
Browser / Next.js Admin
http://127.0.0.1:3000
        |
        v
Identity & Access API
http://127.0.0.1:5080
        |
        +-- OIDC Authorization Code + PKCE
        +-- PostgreSQL
        +-- external Multiplexed RBAC
```

Use `127.0.0.1` consistently. Do not mix `localhost` with the registered callback host.

## Preconditions

1. Apply the current PostgreSQL schema.
2. Confirm historical `0027_group_template_flag_foundation.sql` is intact and migration `0028_simplify_group_templates.sql` is applied; no separate development template seed is used.
3. Start the administration API with the real external RBAC reference.
4. Start the Next.js administration host.
5. Use an existing local administrator or bootstrap one through the supported development script.

## Required browser evidence

### 1. OIDC sign-in

- Open `http://127.0.0.1:3000/login`.
- Sign in with the development administrator.
- The callback completes without a redirect loop.
- A protected administration page loads.

### 2. Tenant list and member counts

As identity-scope administration:

- Memberships shows the authorized tenant list.
- Each tenant exposes its member count.
- Normal administration does not require typing TenantId/UserId pairs.

### 3. Create empty tenant

- Create a new tenant without adding a user.
- The tenant appears with `0 members`.
- Open the tenant detail successfully.

### 4. Group-as-Template and tenant groups

Inside the selected tenant:

- the Groups workspace contains one real-group table;
- every group shows `Template Yes/No`;
- the separate `Available group templates` catalogue is absent;
- reusable groups are available through the explicit `Create from template` action;
- tenant context cannot mark/unmark a group as reusable or mutate the definition of a reusable group.

### 5. Create tenant group and Create from template

With a subject holding the required tenant-group capability:

- create a custom tenant group and confirm it is scoped to the selected tenant with `Template No`;
- use `Create from template` with an authorized reusable source group;
- confirm the new target group is a normal group (`Template No`);
- confirm compatible managed-policy bindings are copied;
- confirm no source memberships are copied.

When adding a managed-policy binding, verify that capability selection comes from the published managed policy itself. The optional `Resource scope` selector only narrows that policy to a concrete resource-scope record; it is not where capabilities such as `user / read` are selected.

### 6. Safe Add member

For identity-scope administration, the broader authorized user selector may be used.

For tenant-scoped delegated administration:

- Add member uses exact login/email resolution;
- no browsable global user directory is exposed;
- the new membership is created only in the current authorized tenant;
- `membership.add` does not imply permission to create a new global user identity.

### 7. Manage groups

For a tenant member:

- open `Manage groups`;
- assign an existing normal group;
- assign an existing reusable group when that real group belongs to the selected tenant;
- confirm `Manage groups` never creates or clones a group implicitly;
- the member row shows the assigned real groups and their current Template/GROUP presentation;
- remove an assignment and confirm the row updates.

### 8. Tenant isolation and reusable-definition authority

Using a tenant-scoped subject:

- another tenant's directory is not enumerable;
- another tenant's groups are not assignable;
- `Make available as template` is not available;
- a reusable group's name/status/managed-policy definition cannot be modified through tenant authority;
- `Create from template` is denied when the copied grants exceed the caller's authority in the target tenant.

### 9. Managed authorization ALLOW / DENY

Exercise one protected operation whose authority is provided by the tested group/binding:

```text
assignment/binding present  -> ALLOW
assignment/binding removed  -> DENY
assignment/binding restored -> ALLOW
```

Do not use UI visibility as the proof. The backend decision is authoritative.

### 10. Sessions

- Sessions loads under the authenticated administrator.
- Session revocation remains protected and server-confirmed.

### 11. MFA

- MFA loads under the authenticated administrator.
- Sensitive factor administration remains protected by the configured assurance/capability rules.

### 12. Logout

- Logout clears the administration session.
- Protected administration routes require authentication again.

## Interactive evidence helper

After the API and UI are running, execute:

```powershell
.\scripts\verify-admin-ui-browser-qualification.ps1
```

The script checks local reachability, asks for explicit confirmation of every browser gate, and writes a JSON evidence record under `artifacts/qualification/`.

## Complete final qualification

Run:

```powershell
.\scripts\verify-administration-qualification.ps1 `
  -RbacReferenceDirectory 'D:\Dev\Personal\multiplexed-rbac\implementations\dotnet\src\Multiplexed.Rbac.Core\bin\Release\net10.0' `
  -Configuration Release
```

The final wrapper completes production qualification first, then pauses so the API and UI can be started in separate terminals before browser evidence is collected.

A full GREEN run requires both:

```text
production qualification GREEN
+
real-browser administration qualification GREEN
```

Skipping backup/restore or browser evidence makes the result partial and must not be reported as complete administration qualification.
