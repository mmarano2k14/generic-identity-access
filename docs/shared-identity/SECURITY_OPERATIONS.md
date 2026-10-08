# Generic Identity — Security Operations

**Release family:** `1.5.0`  
**Status:** public SDK and reusable administration workflows available for backend-supported operations

## 1. Purpose

Security Operations provides server-authoritative administration for MFA, authenticator lifecycle, session containment, and security-audit evidence.

Public categorized clients:

```ts
identity.security.sessions
identity.security.mfa
identity.security.audit
```

The category delegates to the proven backend and transport implementation. It does not introduce a second session store, MFA engine, audit store, or authorization model.

## 2. MFA administration

Supported public operations:

```text
provider discovery
MFA policy get/create/update
user effective MFA security state
authenticator list
authenticator revoke
recovery-oriented authenticator revoke
```

Reusable workflow/presentation:

```text
loadNextMfaWorkspace
createNextMfaPolicyFromForm
updateNextMfaPolicyFromForm
revokeNextMfaAuthenticatorFromForm
recoveryRevokeNextMfaAuthenticatorFromForm
MfaPage
MfaPolicyForm
AuthenticatorRevocationForm
```

Authenticator responses expose safe lifecycle metadata only. Provider secrets are not part of shared administration contracts.

The MFA administration implementation is source-qualified. Dedicated live functional acceptance remains pending and must be executed with disposable factors in a safe development environment.

## 3. Session security and containment

The backend exposes administrative containment operations:

```ts
await identity.security.sessions.revokeUser(context, userId);
await identity.security.sessions.revokeClient(context, clientId);
```

It does **not** expose an administrative active-session list.

Accordingly, the reusable Sessions administration workflow combines bounded security-audit evidence with server-confirmed containment operations:

```text
loadNextSessionSecurityWorkspace
revokeNextUserSessionsFromForm
revokeNextClientSessionsFromForm
SessionSecurityPage
SessionSecurityFilterForm
SessionRevocationForm
```

The workspace never infers active, expired, or revoked state from the current browser session or from missing audit evidence.

Session containment requires literal destructive-action confirmation and returns the backend-confirmed revoked count.

## 4. Security audit

Security Audit is read-only, application-scoped evidence.

Reusable workflow/presentation:

```text
loadNextSecurityAuditWorkspace
normalizeNextSecurityAuditQuery
summarizeSecurityAudit
SecurityAuditPage
SecurityAuditFilterForm
```

Supported bounded filters:

```text
User
Tenant
Outcome
Correlation ID
Window: 25 / 50 / 100 / 200
```

User and Tenant selection use the shared server-backed entity autocomplete where the effective administration context permits scope-wide lookup.

The presentation exposes categorical event metadata such as outcome, event type, user, tenant, client, target, reason, correlation, and timestamp. It does not expose credentials, tokens, provider secrets, or arbitrary raw payloads.

## 5. Deliberately unavailable public operations

No public administration endpoint is currently available for:

- TOTP enrollment and confirmation;
- WebAuthn/passkey registration;
- recovery-code generation or replacement;
- administrative active-session listing.

These remain classified as backend API or backend contract gaps. The SDK does not invent equivalent routes or contracts.

Session-bound TOTP, recovery-code, and WebAuthn step-up authentication belong to Account & Authentication and are not duplicated into Security Operations.

## 6. Authorization model

Security Operations preserves independent capabilities for independent concerns. For example, the ability to read Security Audit evidence is not treated as equivalent to the ability to revoke sessions.

UI visibility is not authority. Every protected read or mutation remains server-authorized.

## 7. Compatibility

Compatibility administration surfaces remain available during the transition, but new consumer integrations should prefer `identity.security` and the reusable Next.js server workflows.

No database migration or RBAC semantic change is required by the shared administration workflows.
