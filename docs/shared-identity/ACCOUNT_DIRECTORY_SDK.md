# Generic Identity — Account and Directory

**Current package family:** `1.5.0`

## Purpose

Account & Authentication and Directory expose proven Identity backend capabilities through categorized, consumer-neutral SDK surfaces.

## Public category surfaces

```text
@generic-identity/contracts/account
@generic-identity/contracts/directory
@generic-identity/auth/account
@generic-identity/auth/directory
```

The root client exposes:

```text
client.account
client.directory
```

Compatibility surfaces remain available during the transition.

## Account & Authentication

Supported categorized operations include:

```text
password login / logout / session validation
self-service password change
recovery-code password reset
TOTP / recovery / WebAuthn session step-up
administrative password credential lifecycle
```

No self-service profile mutation is invented where no dedicated backend contract exists.

## Directory

Supported directory operations include:

```text
users CRUD
tenants CRUD
tenant user projection
tenant memberships lifecycle
membership candidate lookup / add by login
tenant group assignment reads
```

Invitations remain unsupported until a public backend contract exists.

## Administration workflows

The reusable Next.js integration provides complete server workflows for Users, Tenants, and Memberships, including optimistic concurrency, safe credential administration, scope-aware visibility, and assignment reconciliation.

Relational selectors use the shared bounded entity autocomplete rather than loading unrestricted catalogs into the browser.

See:

- [`USERS_ADMINISTRATION.md`](USERS_ADMINISTRATION.md)
- [`MEMBERSHIPS_ADMINISTRATION.md`](MEMBERSHIPS_ADMINISTRATION.md)
- [`ADMINISTRATION_INTEGRATION.md`](ADMINISTRATION_INTEGRATION.md)
