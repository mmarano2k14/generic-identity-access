Version 0.25.0 adds the Organization Directory connector to the existing administration composition. `administration.organizations`, `administration.organizationMemberships`, and `administration.organizationResourceScopeLinks` all use the common Identity Access API host and trusted tenant context; no second application or client root is introduced.

Version 0.24.0 unifies reusable group-template operations under `administration.groups`. `IdentityGroupRecord` now exposes `isTemplate`; `groups.listTemplates()` lists reusable real groups, `groups.createFromTemplate()` creates a normal target-tenant group from one reusable group, and `groups.updateReusable()` is the identity-scope mutation path. The separate `administration.groupTemplates` client and origin/template provenance contracts are retired.

Version 0.23.0 added `administration.membershipCandidates` for exact-login tenant membership resolution and `administration.tenantGroupAssignments` for tenant-scoped aggregate group assignment reads. These focused clients support safe Add member and member-group administration without exposing a global user directory to tenant-scoped administrators.

# Identity & Access TypeScript Client

Version 0.21.0 closes the public legacy tenant-policy compatibility surface. `administration.managedPolicies` remains the identity-scope/application catalog administration surface, `administration.managedPolicyBindings` remains tenant-scoped for grants of published shared policy versions, and `administration.policies` is no longer composed by the public client. `scopeAuthority` remains a separate identity-scope administration model. The root client remains a lightweight facade; transport, system diagnostics, local authentication, OIDC/PKCE, authorization, and each administration domain live in focused classes under `src/client/`.

Relationship lookup lists remain typed, authorized, and bounded. Search terms are trimmed, must contain between 3 and 128 characters, and are sent explicitly to the protected administration list routes. The client never performs browser-side authorization or substitutes local collection filtering for the server-side search boundary.

All durable TypeScript runtime implementations remain class-based. The decorator surface remains the only deliberate function-shaped public API because TypeScript decorators are callable metadata declarations.

## Runtime architecture

```text
IdentityAccessClient
│
├── system
│   └── IdentityAccessSystemClient
│
├── authentication
│   └── IdentityAccessAuthenticationClient
│
├── oidc
│   └── IdentityAccessOidcClient
│
├── authorization
│   └── IdentityAccessAuthorizationClient
│
└── administration
    └── IdentityAccessAdministrationClient
        ├── users
        │   └── IdentityAccessUsersClient
        ├── tenants
        │   └── IdentityAccessTenantsClient
        ├── memberships
        │   └── IdentityAccessMembershipsClient
        ├── tenantUsers
        │   └── IdentityAccessTenantUsersClient
        ├── organizations
        │   └── IdentityAccessOrganizationsClient
        ├── organizationMemberships
        │   └── IdentityAccessOrganizationMembershipsClient
        ├── organizationResourceScopeLinks
        │   └── IdentityAccessOrganizationResourceScopeLinksClient
        ├── groups
        │   └── IdentityAccessGroupsClient
        │       ├── list / get / create / update
        │       ├── listTemplates / createFromTemplate
        │       └── updateReusable
        ├── managedPolicies
        │   └── IdentityAccessManagedPoliciesClient
        ├── managedPolicyBindings
        │   └── IdentityAccessManagedPolicyBindingsClient
        ├── resourceScopes
        │   └── IdentityAccessResourceScopesClient
        ├── securityModels
        │   └── IdentityAccessSecurityModelsClient
        ├── sessions
        │   └── IdentityAccessSessionsClient
        ├── scopeAuthority
        │   └── IdentityAccessScopeAuthorityClient
        └── securityAudit
            └── IdentityAccessSecurityAuditClient
```

Shared implementation responsibilities are also separated:

```text
IdentityAccessHttpTransport
IdentityAccessValueCodec
IdentityAccessProtocolCodec
IdentityAccessAdministrationCodec
IdentityAccessPathBuilder
IdentityAccessCrypto
IdentityAccessAdministrationTransport
```

`src/client.ts` is only a compatibility re-export. Runtime implementation belongs under `src/client/`.

## Root client

```typescript
import { IdentityAccessClient } from "@identity-access/client";

const client = new IdentityAccessClient({
  baseUrl: "http://127.0.0.1:5080",
  timeoutMs: 10_000,
});
```

The root client owns no global current-user, current-session, current-token, tenant, or database state. It only owns immutable transport configuration and composed responsibility classes.

## Diagnostics

```typescript
const info = await client.system.info();
const live = await client.system.liveness();
const readiness = await client.system.readiness();
```

## Password login and local session

```typescript
const session = await client.authentication.passwordLogin({
  clientId: "admin-web",
  loginIdentifier: "user@example.test",
  password,
  redirectUri: "https://app.example.test/callback",
});

await client.authentication.validateSession(session);

await client.authentication.logout(
  session,
  "https://app.example.test/signed-out",
);
```

`IdentityLocalSession` is directly usable as an `IdentitySessionCredential`. Session state remains caller-owned; the SDK does not retain a mutable current session.

## OIDC Authorization Code + PKCE

```typescript
const authorization = await client.oidc.authorize(session, {
  clientId: "admin-web",
  redirectUri: session.redirectUri,
});

const tokens = await client.oidc.exchangeAuthorizationCode(authorization);
```

PKCE uses cryptographically random 32-byte material and S256. Authorization redirects are handled manually, validated against the exact registered redirect target, and never followed automatically.

No client secret exists or is accepted by the public-client implementation.

## Refresh-token rotation

```typescript
const refreshed = await client.oidc.refreshTokens(
  "admin-web",
  tokens.refreshToken,
);
```

A successful refresh returns a replacement refresh token. The caller must discard the consumed token immediately. The client never retries a refresh token automatically and never retains token state globally.

The current server contract remains:

```text
authorization_code grant
    -> access token
    -> ID token
    -> refresh token

refresh_token grant
    -> access token
    -> rotated refresh token
    -> no new ID token
```

## Authorization context

The TypeScript equivalent of:

```csharp
if (!_auth.IsAllowed("billing", "invoice", "refund"))
{
    return;
}
```

is:

```typescript
const allowed = await auth.isAllowed("billing", "invoice", "refund");
if (!allowed) {
  return;
}
```

`IdentityAuthorizationContext` delegates through `IdentityAccessAuthorizationClient`, which always returns the decision to the server-side .NET authorization boundary and configured external RBAC engine.

Direct evaluation remains available through the composed class:

```typescript
const allowed = await client.authorization.evaluate(
  boundary,
  { resource: "billing", feature: "invoice", action: "refund" },
  credential,
);
```

## Declarative capability metadata

```typescript
export class ReplayExecutionHandler {
  @RequireCapability("replay", "execution", "run")
  public async run(): Promise<void> {
  }
}
```

The decorator stores metadata only. It never performs local authorization.

## Typed administration

Administration is grouped under `client.administration` and then separated by responsibility.

Scope-level directory example:

```typescript
const context = {
  identityScopeId,
  applicationKey: "admin-app",
  credential: { kind: "bearer", accessToken },
};

const user = await client.administration.users.create(context, {
  displayName: "Alice",
});

const updated = await client.administration.users.update(
  context,
  user.userId,
  {
    displayName: "Alice Updated",
    status: 1,
    expectedVersion: user.version,
  },
);
```

Tenant-scoped example:

```typescript
const tenantContext = {
  ...context,
  tenantId,
};

const group = await client.administration.groups.create(tenantContext, {
  displayName: "Billing Team",
});

const [managedPolicy] = await client.administration.managedPolicyBindings.listAvailablePolicies(
  tenantContext,
  { search: "billing", limit: 20 },
);

if (managedPolicy) {
  await client.administration.managedPolicyBindings.add(tenantContext, group.groupId, {
    policyId: managedPolicy.policyId,
  });
}
```

Bounded collection reads:

```typescript
const users = await client.administration.users.list(context, { offset: 0, limit: 50 });
const tenants = await client.administration.tenants.list(context, { limit: 50 });
const groups = await client.administration.groups.list(tenantContext, { limit: 50 });
const policies = await client.administration.managedPolicies.list(context, { limit: 50 });
```

The server accepts a maximum list size of 200 records per request.

The administration classes cover:

```text
users
 tenants
 tenant memberships
 groups + group memberships
 managed policies + published versions + tenant-scoped managed bindings
 resource scopes
 security models + capability catalogs + scope types
 bulk session revocation
 identity-scope authority groups/members/policies/statements/bindings
```

Whole-segment wildcard patterns remain server-authoritative. TypeScript validates the supported shape but never evaluates wildcard authority locally.

Application security-model discovery and registration are exposed through the focused `securityModels` client. A project-owned manifest registers one immutable model version with its RBAC project, allowed namespaces, and concrete `resource / feature / action` capabilities. Reusing a model version with different normalized semantics is a server conflict. The client transports and validates these contracts but does not evaluate RBAC decisions.

## Administration UI builder

```typescript
const definition = await new IdentityAccessAdminUiBuilder(auth)
  .withUsers()
  .withGroups()
  .withPolicies()
  .buildVisible();
```

Visibility filtering is presentation behavior only. Every protected read/mutation is authorized again by the backend.

## Next.js administration module

The runnable `examples/nextjs/admin` host consumes the composed API directly:

```text
IdentityAccessHostSessionService
    -> client.authentication
    -> client.oidc

IdentityAccessAdminRequest / pages
    -> client.administration.<responsibility>

IdentityAuthorizationContext
    -> client.authorization
```

Server Components own protected reads. Client Components only own interaction/presentation. Server Actions remain thin adapters over class-based server services.

The module owns exactly one custom stylesheet:

```text
examples/nextjs/admin/styles/identity-access-admin.css
```

CSS Modules, component-local style files, `<style>` blocks, and React inline style objects remain prohibited.

## Validation

```sh
npm test
npm run typecheck
npm pack
```

The repository-level `scripts/verify.ps1` also verifies the class-composed source layout, the runnable Next.js host, the single-CSS invariant, and the .NET suite.

Runtime code has no third-party npm runtime dependency. HTTPS is required except exact loopback development hosts. Requests are non-cacheable and bounded by timeout/cancellation. Raw passwords, session tokens, authorization codes, PKCE verifiers, access tokens, ID tokens, and refresh tokens are never retained in public errors.

The provider-neutral MFA administration foundation is implemented. Concrete TOTP, recovery-code, and passkey/WebAuthn provider flows remain later optional work.

## Generic MFA administration

MFA administration remains provider-neutral and class-based:

```typescript
const providers = await client.administration.mfa.listProviders(context);
const policy = await client.administration.mfa.getPolicy(context);

await client.administration.mfa.createPolicy(context, {
  mode: 2,
  allowedProviders: providers.map((provider) => provider.key),
});
```

The generic client does not calculate TOTP codes, verify WebAuthn assertions, or handle recovery-code material. Those behaviors are delivered by dedicated providers while the generic core owns policy and authenticator lifecycle metadata.

## Security audit administration

Security audit access is exposed through its own responsibility class:

```typescript
const events = await client.administration.securityAudit.list(
  administrationContext,
  { userId, outcome: "Denied", limit: 50 },
);
```

The client only validates/encodes the bounded query and decodes the secret-safe API response. It does not evaluate authorization, infer permissions from events, retain audit state globally, or expose raw PostgreSQL access.
