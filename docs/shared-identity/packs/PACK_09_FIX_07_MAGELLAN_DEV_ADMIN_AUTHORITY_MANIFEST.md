# Shared Identity Pack 9 Fix 07 - MAGELLAN development administrator authority

## Destination

`identity-access` only.

## Purpose

The MAGELLAN local authentication client uses application key `magellan`. Authentication is intentionally separate from authorization, so the local `admin` user does not inherit the `admin-web` administration grants automatically. This fix adds an explicit, idempotent local-development bootstrap that composes the shared Generic Identity administration capability set into the MAGELLAN application model and grants the local administrator identity-scope administration authority for that application context.

## Added

- `scripts/authentication/grant-magellan-dev-admin.ps1`
- `scripts/shared-identity/verify-pack-09-magellan-dev-admin-authority.ps1`
- this manifest

## Modified

- `scripts/shared-identity/verify.ps1`
- `CHANGELOG.md`

## Moved

NONE.

## Deleted

NONE.

## Database migrations

NONE.

## Runtime behavior

No authorization bypass is introduced. The bootstrap writes normal Generic Identity administration groups, memberships, policies, statements and bindings for the `magellan` application context. Final decisions continue through the existing authorization pipeline and external RBAC adapter.

The script is explicitly for local development. Production authority must be provisioned through the normal administration lifecycle.
