# Shared Identity Pack 9 Fix 07.1 - MAGELLAN dev admin security-model registration

## Destination

`identity-access` only.

## Purpose

The local MAGELLAN administrator grant must compose authority through the normal Generic Identity application-security catalog. `application_security_namespaces` is foreign-keyed to `application_security_model_registrations`; the original Fix 07 created the security model and attempted to insert the namespace without first creating its registration.

This fix registers the derived local `magellan` security model before namespaces and capabilities are inserted, using a deterministic fingerprint built from the shared administration capability source, the MAGELLAN application key, the requested model version, RBAC project, namespaces and concrete capability set.

## Added

- this manifest

## Modified

- `scripts/authentication/grant-magellan-dev-admin.ps1`
- `scripts/shared-identity/verify-pack-09-magellan-dev-admin-authority.ps1`
- `CHANGELOG.md`

## Moved

NONE.

## Deleted

NONE.

## Database migrations

NONE.

## Runtime behavior

No authorization bypass is introduced. The local development bootstrap now satisfies the existing application-security catalog foreign-key order and then grants authority through the existing identity-scope administration group/policy/binding pipeline.

The bootstrap remains additive and idempotent. A conflicting security-model registration fingerprint fails closed rather than replacing a previously registered model silently.
