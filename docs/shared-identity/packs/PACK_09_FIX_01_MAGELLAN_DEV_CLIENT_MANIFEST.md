# Shared Identity Pack 9 Fix 01 — MAGELLAN local development client

## Purpose

Allow the existing local Generic Identity API development host to authenticate the MAGELLAN Next.js consumer independently from the existing `admin-web` administration host.

## Modified

- `config/routing.admin-local.example.json`
- `scripts/authentication/run-dev-admin-api.ps1`
- `scripts/shared-identity/verify.ps1`
- `CHANGELOG.md`

## Added

- `scripts/shared-identity/verify-magellan-dev-client.ps1`
- this manifest

## Moved

NONE.

## Deleted

NONE.

## Database migrations

NONE.

## Runtime scope

Development configuration only. The existing `admin-web` client and route remain unchanged. A second client `magellan-ux` is registered with application key `magellan`, authentication context `magellan-primary`, and the same local identity-scope-backed PostgreSQL destination.
