# Directory Administration API

**Source version: 0.12.0. Date: September 21, 2026.**

Version 0.12.0 extends the MVC controller surface with users, tenants, tenant memberships, groups, and group-membership administration.

All actions remain behind fail-closed administration authorization. Each request resolves one immutable server-side database route and explicitly propagates the request cancellation token through routing and persistence. Mutable records use optimistic concurrency and return HTTP `409 Conflict` for stale writes. Group-membership edges use exact add/remove operations.

Swagger UI remains available at `http://127.0.0.1:5080/swagger`.

This version adds no login, OIDC, session, MFA, redirect URI, account-recovery, or PostgreSQL schema behavior.
