# OrganisationProfile HTTP API and TypeScript SDK — HTTP API + RBAC + Audit + TypeScript SDK

## Scope

HTTP API and TypeScript SDK exposes the OrganisationProfile application model through the existing Identity Access API host.

It adds no second host and no database migration.

## HTTP routes

Tenant-owned profiles:

```text
GET  /api/v1/identity-scopes/{scope}/applications/{app}/tenants/{tenant}/organisation-profiles
GET  /api/v1/identity-scopes/{scope}/applications/{app}/tenants/{tenant}/organisation-profiles/{profileId}
GET  /api/v1/identity-scopes/{scope}/applications/{app}/tenants/{tenant}/organisation-profiles/by-organization/{organizationId}
POST /api/v1/identity-scopes/{scope}/applications/{app}/tenants/{tenant}/organisation-profiles

PUT  /.../organisation-profiles/{profileId}/template
POST /.../organisation-profiles/{profileId}/enable
POST /.../organisation-profiles/{profileId}/disable
```

Domain overrides:

```text
GET /.../organisation-profiles/{profileId}/domain-overrides
PUT /.../organisation-profiles/{profileId}/domain-overrides
```

Immutable effective versions:

```text
GET  /.../organisation-profiles/{profileId}/effective-versions
GET  /.../organisation-profiles/{profileId}/effective-versions/{version}
POST /.../organisation-profiles/{profileId}/effective-versions/resolve
```

Reusable template catalog:

```text
GET  /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates
GET  /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates/{templateKey}
POST /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates
PUT  /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates/{templateKey}
POST /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates/{templateKey}/enable
POST /api/v1/identity-scopes/{scope}/applications/{app}/organisation-profile-templates/{templateKey}/disable
```

Template versions:

```text
GET  /.../organisation-profile-templates/{templateKey}/versions
GET  /.../organisation-profile-templates/{templateKey}/versions/{version}
POST /.../organisation-profile-templates/{templateKey}/versions
PUT  /.../organisation-profile-templates/{templateKey}/versions/{version}/domains
POST /.../organisation-profile-templates/{templateKey}/versions/{version}/publish
POST /.../organisation-profile-templates/{templateKey}/versions/{version}/retire
```

## Controller separation

```text
OrganisationProfilesController
OrganisationProfileDomainOverridesController
OrganisationProfileEffectiveVersionsController
OrganisationProfileTemplatesController
OrganisationProfileTemplateVersionsController
```

No single controller owns profile lifecycle, template catalog, domain overrides, and semantic-version history together.

## Tenant boundary

Any route accepting a raw `organisationProfileId` re-loads the profile and validates:

```text
IdentityScopeId
TenantId
```

against the route before returning or mutating state.

This prevents a valid profile identifier from crossing a tenant route boundary.

## Authorization

Controller actions enforce the Security Integration capability model:

```text
identity-access / organisation-profile / read|write
identity-access / organisation-profile-template / read|write
identity-access / organisation-profile-domain-override / read|write
identity-access / organisation-profile-effective-version / read|write
```

Authorization remains enforced by the existing `RequireAdministrationCapability` filter and trusted administration context.

## Audit

Successful mutations emit the semantic Security Integration audit events through:

```text
IOrganisationProfileSecurityAuditWriter
```

Read operations are not audited as mutations.

## Stable problem mapping

Expected conflicts map through `ApiExceptionHandler`.

Examples:

```text
optimistic concurrency             -> 409
duplicate profile/template/version -> 409
immutable published content        -> 409
disabled profile/template          -> 409
unavailable domain version         -> 409
missing Organization/template      -> 404
storage/routing unavailable        -> 503
```

Internal exception details are not copied into HTTP problem responses.

## TypeScript SDK

Existing package:

```text
@identity-access/client
```

advances to:

```text
0.26.0
```

Focused clients:

```text
administration.organisationProfiles
administration.organisationProfileDomainOverrides
administration.organisationProfileEffectiveVersions
administration.organisationProfileTemplates
administration.organisationProfileTemplateVersions
```

The TypeScript SDK keeps mutable profile configuration separate from immutable semantic-version history.

## Verification

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

HTTP API and TypeScript SDK adds:

```text
OrganisationProfile API + TypeScript SDK validation: GREEN
```

The TypeScript gate executes:

```text
npm run typecheck
npm test
```
