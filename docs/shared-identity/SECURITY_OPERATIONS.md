# Generic Identity — Security Operations

**Release:** 1.5.0  
**Status:** Public SDK and shared UI available for backend-supported operations  
**Scope:** Administrative session revocation, MFA administration, authenticator lifecycle metadata and security audit

## 1. Purpose

Security Operations is the Generic Identity category for operational account-security administration. It promotes the existing server-backed session, MFA and security-audit capabilities into the categorized public SDK without moving security decisions into the frontend.

The category is backed by existing server capabilities:

- `SessionsController` for administrative revocation of sessions by user or registered client;
- `MfaAdministrationController` for provider discovery, MFA policy lifecycle, authenticator inspection and revocation;
- `SecurityAuditEventsController` for filtered read-only security-audit queries.

The release does not create endpoints for functionality that is only implemented as an internal provider service.

## 2. Public SDK

The categorized client is available as:

```ts
identity.security.sessions
identity.security.mfa
identity.security.audit
```

Representative operations:

```ts
const providers = await identity.security.mfa.listProviders(context);
const policy = await identity.security.mfa.getPolicy(context);

await identity.security.mfa.createPolicy(context, request);
await identity.security.mfa.updatePolicy(context, request);

const authenticators = await identity.security.mfa.listAuthenticators(context, userId);
const state = await identity.security.mfa.getUserSecurityState(context, userId);

await identity.security.mfa.revokeAuthenticator(
  context,
  userId,
  authenticatorId,
  expectedVersion,
);

await identity.security.mfa.revokeAuthenticatorForRecovery(
  context,
  userId,
  authenticatorId,
  expectedVersion,
);

await identity.security.sessions.revokeUser(context, userId);
await identity.security.sessions.revokeClient(context, clientId);

const events = await identity.security.audit.list(context, {
  userId,
  outcome: "Denied",
  limit: 50,
});
```

The facade delegates to the proven `IdentityAccessMfaClient`, `IdentityAccessSessionsClient` and `IdentityAccessSecurityAuditClient`. HTTP transport, validation, authorization and persistence semantics remain owned by the existing implementation.

## 3. Shared presentation surface

The reusable React package exports the Security Operations surface under `@generic-identity/react/security-operations`:

- `MfaPage`, including provider, policy, user security state and authenticator metadata;
- `MfaPolicyForm`;
- `AuthenticatorRevocationForm`;
- `SessionsPage`;
- `SessionRevocationForm`;
- `SecurityAuditPage`;
- `SecurityPage` composition shell.

The Next.js package re-exports the same presentation surface under `@generic-identity/next/security-operations`.

Routes, server actions, navigation, branding and product-specific labels remain consumer-owned. Shared forms submit ordinary form data to consumer-owned actions; they do not call privileged endpoints directly from the browser.

## 4. Deliberately unavailable public operations

The backend contains internal provider services for additional factor lifecycle operations, but no public controller endpoint was discovered for:

- TOTP enrollment and confirmation;
- WebAuthn/passkey registration;
- recovery-code generation or replacement;
- administrative active-session listing.

These features remain explicitly classified as `BACKEND_API_MISSING` or `BACKEND_MISSING`. No SDK method, mock route or speculative contract is added for them.

Session-bound TOTP, recovery-code and WebAuthn **step-up authentication** already belongs to the Account & Authentication category and remains available through the existing authentication/account facade. Security Operations does not duplicate those methods.

## 5. Security invariants

The Security Operations surface preserves the following rules:

- administrative mutations remain server-authorized;
- UI visibility is never authority;
- session revocation uses the existing server-side session authority;
- MFA provider metadata never exposes provider secrets;
- authenticator responses contain lifecycle metadata only;
- recovery-oriented authenticator revocation remains a distinct server operation;
- security audit is read-only through this public category;
- technical failure is not converted into an authorization grant;
- no second MFA engine, session store or audit implementation is introduced.

## 6. Compatibility

Existing `administration.mfa`, `administration.sessions` and `administration.securityAudit` compatibility surfaces remain available. The categorized `identity.security` surface is additive and is the preferred API for new consumers.

No backend route, database migration, MFA semantic, session semantic or RBAC behavior change is required by this release.

## 7. Package version

The four public Generic Identity packages advance together to `1.5.0`:

```text
@generic-identity/contracts
@generic-identity/auth
@generic-identity/react
@generic-identity/next
```

The package dependency direction remains:

```text
contracts
   ↑
auth
   ↑
react
   ↑
next
```
