# Consumer Application Onboarding

**Generic Identity version:** 1.5.0  
**Audience:** application developers, platform engineers, local-development operators  
**Scope:** register a new consumer application, establish initial local administration authority, integrate the public SDK, and validate the security boundary

---

## 1. Purpose

Generic Identity is designed to serve multiple consumer applications without embedding consumer-specific logic in the Identity repository.

A new application must establish four separate things:

```text
APPLICATION IDENTITY
    ApplicationKey
    ClientId / authentication context

APPLICATION SECURITY MODEL
    project-owned security manifest
    immutable model version
    RBAC project / namespaces
    concrete capabilities

INITIAL ADMINISTRATION AUTHORITY
    trusted administrator subject
    administration group
    administration policy
    group/policy binding

CONSUMER INTEGRATION
    Generic Identity SDK
    server-side session handling
    server-side RBAC enforcement
    optional shared React / Next.js UI
```

These concerns are related, but they are not the same thing. Registering an application manifest does not authenticate a user. Registering an authentication client does not grant administration authority. Rendering an administration page does not authorize the operation behind it.

The server remains the authority for authentication, session validation, administration context and RBAC evaluation.

---

## 2. Key identifiers

Before onboarding an application, choose its stable identifiers.

| Value | Purpose | Example |
|---|---|---|
| `IdentityScopeId` | Identity authority boundary | `00000000-0000-0000-0000-000000000001` |
| `ApplicationKey` | Stable Identity-side application identifier | `sample-app` |
| `ClientId` | Registered authentication client | `sample-web` |
| `AuthenticationContextKey` | Trusted pre-authentication routing context | `sample-primary` |
| `ModelVersion` | Immutable application security-model version | `1` |
| RBAC project | RBAC execution project | `sample-project` |
| RBAC namespace | RBAC execution namespace | `operations` |

`ApplicationKey` is not a database name, tenant id, credential or permission. It identifies the consumer application inside the Generic Identity contracts.

`ClientId` and `ApplicationKey` may be the same value, but they are separate concepts and should not be treated as interchangeable by design.

---

## 3. Author the application security manifest

The consumer application owns its security vocabulary in source control.

Start from:

```text
config/application-security-manifest.example.json
```

Example:

```json
{
  "schemaVersion": 1,
  "applicationKey": "sample-app",
  "modelVersion": 1,
  "rbac": {
    "project": "sample-project",
    "namespaces": ["operations"]
  },
  "resources": [
    {
      "name": "billing",
      "features": [
        {
          "name": "invoice",
          "actions": [
            { "name": "read", "displayName": "Read invoices" },
            { "name": "refund", "displayName": "Refund invoices" }
          ]
        }
      ]
    }
  ]
}
```

The manifest declares what the application software understands. It is not an authorization grant.

The registered projection is immutable by `(IdentityScopeId, ApplicationKey, ModelVersion)` semantics:

```text
same version + same normalized semantic fingerprint
    -> idempotent

same version + different normalized semantic fingerprint
    -> conflict
```

When semantics change, publish a new model version. Do not silently mutate an already registered version.

See `docs/APPLICATION_SECURITY_MANIFESTS.md` for the complete registration contract.

---

## 4. Configure the trusted authentication client

Authentication clients are trusted server configuration. Generic Identity does not expose dynamic browser-driven client registration.

A client configuration follows the model shown in:

```text
config/authentication.local.example.json
```

Example:

```json
{
  "ClientId": "sample-web",
  "ApplicationKey": "sample-app",
  "AuthenticationContextKey": "sample-primary",
  "RedirectUris": [
    "http://127.0.0.1:3000/auth/callback"
  ],
  "PostLogoutRedirectUris": [
    "http://127.0.0.1:3000/"
  ]
}
```

For OIDC-enabled applications, configure the exact redirect URI, allowed scopes, issuer and other OIDC settings through trusted server configuration.

This step establishes an authentication client. It does **not** create a user and does **not** grant administration authority.

---

## 5. Establish local development administration authority

There are two distinct local-development steps.

### 5.1 Establish the first Generic Identity local root administrator

If the development database does not yet contain a trusted Generic Identity administrator, use:

```text
scripts/authentication/bootstrap-dev-admin.ps1
```

The normal first bootstrap uses the repository-owned Generic Identity administration application and manifest:

```text
ApplicationKey: admin-web
Manifest:       config/identity-access-admin-security-manifest.json
```

Example:

```powershell
$env:PGPASSWORD = "<local-postgres-password>"

.\scripts\authentication\bootstrap-dev-admin.ps1
```

The script securely prompts for the password unless `-Password` is supplied.

This larger bootstrap creates or repairs:

- an active user;
- the local administration tenant;
- tenant membership;
- the manifest-backed Generic Identity administration security model;
- the password credential using the repository's development password hasher;
- identity-scope administration authority;
- tenant administration group and managed policy;
- the managed policy version and binding;
- optionally `examples/nextjs/admin/.env.local` with non-secret runtime identifiers.

This step establishes the local development root of trust. It is normally performed once for a local environment, not once per consumer application.

### 5.2 Grant that existing administrator authority for a new consumer application

Once the administrator user exists, use:

```text
scripts/authentication/grant-consumer-dev-admin.ps1
```

This focused script grants the existing active user Generic Identity administration authority under another consumer `ApplicationKey`.

Example:

```powershell
$env:PGPASSWORD = "<local-postgres-password>"

.\scripts\authentication\grant-consumer-dev-admin.ps1 `
  -IdentityScopeId "00000000-0000-0000-0000-000000000001" `
  -UserId "00000000-0000-0000-0000-000000000003" `
  -ApplicationKey "sample-app" `
  -ModelVersion 4 `
  -SecurityManifestPath ".\config\identity-access-admin-security-manifest.json"
```

The selected manifest for this operation is the shared Generic Identity administration manifest, not the consumer application's business capability manifest.

`grant-consumer-dev-admin.ps1` intentionally allows the shared administration manifest to be projected under another `ApplicationKey`; it does not require the manifest's own authoring `applicationKey` to equal the consumer key.

Pass `-ModelVersion` explicitly. The current script defaults to `1` and does not derive the version from the manifest.

Model versions are immutable application coordinates. Reserve a version for this local administration projection that does not collide with a different consumer-owned manifest version.

After this grant exists, the trusted administrator can register the consumer application's own project-owned security manifest through the protected Application Security API/SDK.

Both scripts are development-only root-of-trust utilities. They are not production account or application provisioning APIs.

---

## 6. What `grant-consumer-dev-admin.ps1` actually does

This section describes the script's behavior step by step.

### 6.1 Preconditions

The script requires:

- `psql` on `PATH`;
- an already migrated Generic Identity PostgreSQL database;
- an existing **active** user identified by `IdentityScopeId + UserId`;
- a valid Generic Identity administration security manifest;
- PostgreSQL credentials supplied through normal PostgreSQL mechanisms such as `PGPASSWORD` when needed.

Database selection is taken from:

```text
IDENTITY_ACCESS_POSTGRES_DATABASE
```

or defaults to:

```text
generic_identity_access_default
```

The PostgreSQL user is taken from:

```text
IDENTITY_ACCESS_POSTGRES_USER
```

or defaults to `postgres`.

### 6.2 Validates the requested application boundary

The script validates:

```text
ApplicationKey
    lowercase application identifier
    starts with a letter
    letters / numbers / hyphens only
    maximum 64 characters

ModelVersion
    positive integer
```

It then loads the selected JSON security manifest and requires:

- `schemaVersion = 1`;
- a concrete RBAC project;
- at least one concrete RBAC namespace;
- no wildcard, colon or oversized RBAC context segments;
- exactly one `identity-access` resource;
- at least one concrete administration capability under that resource.

### 6.3 Builds the administration capability projection

The script reads the `identity-access` resource from the manifest and flattens its feature/action declarations into concrete capability rows:

```text
identity-access / <feature> / <action>
```

Display names are retained as presentation metadata.

The script does not invent capabilities independently of the manifest.

### 6.4 Computes a deterministic semantic fingerprint

A SHA-256 fingerprint is calculated over normalized semantic values including:

- manifest schema version;
- requested application key;
- requested model version;
- RBAC project;
- sorted RBAC namespaces;
- sorted concrete administration capabilities and display names.

This fingerprint is stored in the application security registration projection.

The purpose is to make semantic reuse detectable. Reusing the same application/model coordinate for incompatible content is not treated as a silent update.

### 6.5 Verifies that the administrator user already exists

Before granting anything, the SQL transaction checks:

```text
identity_access.users
```

for an active record matching the requested `IdentityScopeId` and `UserId`.

If that active user does not exist, the operation fails. The script does not create the user.

### 6.6 Creates the application's local administration security projection

Inside one PostgreSQL transaction, the script creates or repairs the local administration projection for the requested application:

```text
application_security_models
application_security_model_registrations
application_security_namespaces
application_capabilities
```

The capability rows are copied from the shared Generic Identity administration manifest under the requested consumer `ApplicationKey` and `ModelVersion`.

### 6.7 Creates the identity-scope administration grant

The script then creates or repairs:

```text
identity_scope_administration_groups
identity_scope_administration_group_memberships
identity_scope_administration_policies
identity_scope_administration_policy_statements
identity_scope_administration_group_policy_bindings
```

The development administrator user is placed in the application-scoped administration group.

The administration policy contains the statement:

```text
identity-access / * / *
```

pinned to the requested model version.

That wildcard is an administration policy statement interpreted by the existing authorization/RBAC path. The script does not become an authorization evaluator.

### 6.8 Executes atomically

The generated SQL is executed through:

```text
psql --single-transaction
```

A failure causes the bootstrap operation to fail instead of leaving a partially applied local grant.

### 6.9 Verifies the resulting authority

After the transaction, the script runs a verification query joining:

```text
administration group membership
    -> group/policy binding
    -> administration policy statement
    -> application security model registration
```

It verifies that the target user has at least one matching administration grant for:

```text
IdentityScopeId
ApplicationKey
RBAC project
identity-access / * / *
```

Only then does it print:

```text
Consumer application local Generic Identity administration authority: GREEN
```

### 6.10 Idempotency

The script uses conflict-safe insert/update behavior so the same local development grant can be applied again to repair or confirm the expected state.

It is intended to be repeatable for the same development coordinates.

### 6.11 What the script deliberately does **not** do

`grant-consumer-dev-admin.ps1` does **not**:

- create a user;
- create or reset a password;
- create a tenant or tenant membership;
- register a trusted authentication `ClientId`;
- configure OIDC redirect URIs;
- create signing keys;
- start the Generic Identity API;
- register the consumer application's business security manifest through the public API;
- grant application business permissions merely because a capability exists;
- provision a production administrator;
- bypass normal server-side RBAC evaluation.

Those boundaries are intentional.

---

## 7. Register the consumer application's own security model

Once trusted administration authority exists and the application can authenticate, register the application-owned security manifest through the protected Application Security API/SDK.

The underlying HTTP contract is:

```text
PUT /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/security-models/{modelVersion}
```

Using the categorized SDK:

```ts
await identity.applicationSecurity.manifests.register(
  administrationContext,
  manifest,
);
```

The route application key and model version must match the manifest being registered.

Manifest registration records supported capabilities. It does not grant those capabilities to any user or group.

---

## 8. SDK package model

Generic Identity exposes four aligned public packages:

```text
@generic-identity/contracts
@generic-identity/auth
@generic-identity/react
@generic-identity/next
```

### `@generic-identity/contracts`

Passive public types only.

Use it for:

- authorization boundaries;
- capability requirements;
- directory records;
- organization records;
- access-control contracts;
- Application Security contracts;
- Security Operations contracts.

It has no authentication or authorization runtime behavior.

### `@generic-identity/auth`

Framework-neutral runtime SDK.

Main categorized client surface:

```ts
identity.authentication
identity.authorization
identity.administration
identity.account
identity.directory
identity.organizations
identity.accessControl
identity.applicationSecurity
identity.security
```

Create a client:

```ts
import { createIdentityClient } from "@generic-identity/auth";

const identity = createIdentityClient({
  baseUrl: process.env.IDENTITY_API_BASE_URL!,
});
```

The public facade composes the proven Identity transport. It does not create a second authentication or authorization engine.

### `@generic-identity/react`

Reusable presentation layer.

It provides:

- `IdentityProvider`;
- authorization-aware UX helpers;
- `RequireCapability` for presentation gating;
- shared account, directory, organizations, access-control, Application Security and Security Operations views;
- theme and visual override contracts.

`RequireCapability` is **not** the final security boundary. Every protected operation must be authorized again on the server.

### `@generic-identity/next`

Next.js integration layer.

It provides:

- `NextIdentityProvider`;
- server-only opaque-session cookie handling;
- server-side session validation;
- server-side capability enforcement;
- trusted administration-context helpers;
- shared page exports.

It does not own application routes, navigation, branding or business semantics.

---

## 9. Framework-neutral SDK example

```ts
import {
  createIdentityClient,
  createAuthorizationContext,
} from "@generic-identity/auth";

const identity = createIdentityClient({
  baseUrl: "http://127.0.0.1:5080",
});

const authorization = createAuthorizationContext(identity, {
  identityScopeId: "00000000-0000-0000-0000-000000000001",
  applicationKey: "sample-app",
  credential: serverOwnedCredential,
});

const allowed = await authorization.isAllowed(
  "billing",
  "invoice",
  "refund",
);

if (!allowed) {
  // Return/throw the application's authorization refusal.
}
```

The credential must remain in a trusted runtime boundary. Do not expose session/access credentials merely to perform UI checks.

---

## 10. Next.js server integration

Create a request-bound session only in server code:

```ts
import {
  NextIdentityServerSession,
  createNextAdministrationContext,
  requireAuthenticatedIdentitySession,
  requireServerCapability,
} from "@generic-identity/next/server";

const session = await NextIdentityServerSession.fromCurrentRequest({
  baseUrl: process.env.IDENTITY_API_BASE_URL!,
  clientId: process.env.IDENTITY_CLIENT_ID!,
  cookiePrefix: "sample_identity",
});

const current = await requireAuthenticatedIdentitySession(session);

const administrationContext = createNextAdministrationContext(
  session,
  process.env.IDENTITY_SCOPE_ID!,
  "sample-app",
);
```

For a protected business operation:

```ts
await requireServerCapability(
  session,
  {
    identityScopeId: process.env.IDENTITY_SCOPE_ID!,
    applicationKey: "sample-app",
  },
  {
    resource: "billing",
    feature: "invoice",
    action: "refund",
  },
);
```

An explicit authorization denial throws `NextIdentityPermissionDeniedError`. Technical authorization failures propagate as technical failures; they are not converted into business denial or allow.

---

## 11. Sign-in and session behavior

`NextIdentityServerSession` keeps the opaque session id/token in HTTP-only cookies.

Typical server-side sign-in:

```ts
const session = await NextIdentityServerSession.fromCurrentRequest(options);

await session.signIn(
  loginIdentifier,
  password,
  redirectUri,
);
```

Session validation:

```ts
const current = await session.current();
```

Sign-out:

```ts
await session.signOut();
```

Client Components should not receive raw session tokens, access tokens, refresh tokens, database routes, connection strings or RBAC internal state.

---

## 12. Administration contexts

Identity administration calls require an explicit trusted context.

Identity-scope context:

```ts
const context = createNextAdministrationContext(
  session,
  identityScopeId,
  applicationKey,
);
```

Tenant-scoped administration uses:

```ts
createNextTenantAdministrationContext(
  session,
  identityScopeId,
  applicationKey,
  tenantId,
);
```

The browser may request a tenant or resource scope, but the server must reconstruct and authorize the effective context. Client input is never authority by itself.

---

## 13. Common categorized SDK operations

Directory:

```ts
await identity.directory.users.list(context);
await identity.directory.tenants.list(context);
await identity.directory.memberships.list(tenantContext);
```

Organizations:

```ts
await identity.organizations.organizations.list(context);
await identity.organizations.memberships.list(tenantContext);
```

Access Control:

```ts
await identity.accessControl.groups.list(tenantContext);
await identity.accessControl.managedPolicies.list(context);
await identity.accessControl.resourceScopes.list(tenantContext);
```

Application Security:

```ts
const models = await identity.applicationSecurity.models.list(context);
const capabilities = await identity.applicationSecurity.capabilities.listForModel(
  context,
  modelVersion,
);
```

Security Operations:

```ts
await identity.security.sessions.revokeUser(context, userId);
const providers = await identity.security.mfa.listProviders(context);
const events = await identity.security.audit.list(context, { limit: 50 });
```

Only operations backed by real server endpoints are exposed. The SDK does not invent missing backend functionality.

---

## 14. Recommended local onboarding sequence

For a new consumer application in local development:

```text
1. Choose ApplicationKey / ClientId / authentication context.
2. Author the consumer security manifest.
3. Configure the trusted authentication client server-side.
4. Ensure the database schema is current.
5. Ensure an administrator user exists.
6. If no local administrator exists, run bootstrap-dev-admin.ps1.
7. If the user already exists, run grant-consumer-dev-admin.ps1 for the new ApplicationKey.
8. Start the Generic Identity API with the required routing/auth/RBAC configuration.
9. Integrate @generic-identity/auth or @generic-identity/next in the consumer.
10. Authenticate and obtain a real server-owned session.
11. Register the consumer-owned security manifest through the protected Application Security surface.
12. Create application groups/policies/bindings as required.
13. Enforce every sensitive operation server-side through the real authorization path.
14. Use React capability checks only as UX filtering.
15. Run negative and positive authorization validation.
```

---

## 15. Validation checklist

A consumer application is not considered integrated merely because login succeeds.

Minimum validation should cover:

```text
Unauthenticated protected request           -> rejected
Invalid/expired session                     -> rejected
Revoked session                             -> rejected
Wrong ApplicationKey                        -> rejected
Wrong tenant                                -> rejected
Unknown capability                          -> rejected
Known capability / wrong resource scope     -> denied
Explicit RBAC deny                          -> denied
Authorization dependency unavailable        -> technical failure, never allow
Valid capability + valid scope              -> real server evaluation
Same manifest/version/fingerprint            -> idempotent
Different semantics under same model version -> conflict
```

For the repository itself, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

---

## 16. Production boundary

The development bootstrap scripts are deliberately privileged local root-of-trust tools.

Do **not** use them as production onboarding APIs.

Production onboarding must use controlled deployment/provisioning procedures and normal delegated administration after initial trusted authority has been established.

Production rules include:

- no default administrator password;
- no committed credentials;
- no browser-controlled authentication client registration;
- no browser-provided database route or connection string;
- no client-side RBAC evaluator;
- no permission granted merely because a capability exists in a manifest;
- no reuse of an immutable model version for changed semantics;
- no conversion of technical authorization failure into `ALLOW`.

---

## 17. Related documentation

Use these documents for deeper detail:

```text
docs/APPLICATION_SECURITY_MANIFESTS.md
docs/DEVELOPMENT_ADMIN_BOOTSTRAP.md
docs/OIDC_AUTHORIZATION_CODE_PKCE.md
docs/ADMINISTRATION_RBAC_AUTHORIZATION.md
docs/SESSION_LIFECYCLE.md
docs/shared-identity/FULL_SDK_CATEGORY_MODEL.md
docs/shared-identity/APPLICATION_SECURITY.md
docs/shared-identity/SECURITY_OPERATIONS.md
packages/contracts/README.md
packages/auth/README.md
packages/react/README.md
packages/next/README.md
examples/nextjs/admin/README.md
```

---

## 18. Summary

The onboarding rule is:

> **The consumer owns its application identity, routes, business capability manifest and product experience. Generic Identity owns authentication, sessions, identity administration, authorization semantics and the shared SDK. Initial local administration authority is explicitly bootstrapped; normal application behavior remains server-authorized.**
