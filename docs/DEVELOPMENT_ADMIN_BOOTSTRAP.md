# Local Development Administrator Bootstrap

**Source version: 0.42.14. Date: September 23, 2026.**

This workflow exists only to establish the first local development administrator so the runnable Next.js administration host can be exercised against the real authentication, OIDC, Bearer, and RBAC paths.

It does not create a production default account. No password is committed, generated into source, written to logs, or stored in the Next.js environment file.

## Fixed local identities

The default development profile intentionally matches `examples/nextjs/admin/.env.local.example`:

```text
Identity scope  00000000-0000-0000-0000-000000000001
Tenant          00000000-0000-0000-0000-000000000002
User            00000000-0000-0000-0000-000000000003
Membership      00000000-0000-0000-0000-000000000004
Application     admin-web
OIDC client     admin-web
Login           admin
```

The bootstrap is idempotent for these identities. Re-running it resets the local administrator password, clears credential lockout state, and reactivates the local bootstrap records.

## Prerequisites

- .NET 10 SDK
- PostgreSQL command-line tools on `PATH`
- the default schema already applied
- `IDENTITY_ACCESS_POSTGRES_DEFAULT` configured for the API
- `PGPASSWORD` configured for local `psql` commands when password authentication is required
- a built compatible external RBAC reference directory for the API runtime

## Create the local administrator

From the repository root:

```powershell
$env:PGPASSWORD = "<local-postgres-password>"

.\scripts\authentication\bootstrap-dev-admin.ps1
```

The script securely prompts for the administrator password. Passwords must contain 12 to 256 characters.

The script creates or repairs:

- the active user;
- the local administration tenant and membership;
- the `admin-web` security model and concrete Identity Access capabilities;
- the password credential using the same ASP.NET Core Identity hash format as the runtime;
- identity-scope administration authority;
- tenant administration group/policy/binding;
- `examples/nextjs/admin/.env.local` containing only non-secret runtime identifiers.

The resulting login identifier is:

```text
admin
```

## Start the API

Set the PostgreSQL connection string and external RBAC reference directory, then run:

```powershell
$env:IDENTITY_ACCESS_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_identity_access_default;Username=postgres;Password=<password>"
$env:IDENTITY_ACCESS_RBAC_REFERENCE_DIRECTORY = "D:\path\to\multiplexed-rbac\bin\Release\net10.0"

.\scripts\authentication\run-dev-admin-api.ps1
```

The runner configures the trusted local `admin-web` client, OIDC Authorization Code + PKCE, Bearer validation, routing, PostgreSQL, and external RBAC for the current process only. It creates a development OIDC signing key under `secrets/` if none exists.

## Start the Next.js host

In another terminal:

```powershell
cd .\examples\nextjs\admin
npm run dev
```

Open:

```text
http://127.0.0.1:3000/login
```

Authenticate with login `admin` and the password supplied during bootstrap.

## Security boundary

This bootstrap is an explicit local root-of-trust operation. It must not be used as a production account provisioning mechanism.

Production onboarding must use controlled account lifecycle and delegated administration paths. A repository checkout never contains a usable administrator password.
