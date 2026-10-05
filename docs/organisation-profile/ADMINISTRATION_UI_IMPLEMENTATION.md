# OrganisationProfile Administration UI — Administration UI

## Scope

Administration UI adds the application-facing OrganisationProfile workspace on top of the HTTP API and TypeScript SDK HTTP API and TypeScript SDK.

It adds no database migration and no new authorization engine.

The reference route is deliberately outside the Identity Access administration navigation:

```text
/organisations/{organizationId}/profile?tenantId={tenantId}
```

The consuming application supplies the Organization context. There is no project selector.

## Ownership boundary

The workspace reads an existing Organization through Organization Directory and treats it as a foreign identity.

It does not create, edit, enable, disable, or delete Organization identity.

It does not manage:

```text
OrganizationMembership
ResourceScope
RBAC grants
provider credentials
provider connections
```

Those authorities remain external.

## Focused UI composition

```text
OrganisationProfilePanel
├── ProfileTemplateSelector
├── DomainCompositionPanel
├── ProfileVersionPanel
└── ProfileLifecycleActions
```

Profile creation is kept in the separate:

```text
OrganisationProfileCreatePanel
```

No single component owns Organization administration, template catalog authoring, domain composition, version history, provider configuration, and authorization together.

## Template selection

Template choices come from protected HTTP API and TypeScript SDK catalog reads.

Only:

```text
active template definition
+
published immutable template version
```

is offered as a new selectable pin.

A historical current pin can be displayed but is not silently treated as newly selectable.

## Domain override editor

The editor replaces the complete override set atomically using the profile `RowVersion`.

Enable choices are derived from domain references observed in published immutable template versions. The form does not accept a free-form domain/version pair as a normal UI path.

The server action reloads the protected catalog and rejects forged selections that are not currently published.

If an existing Enable override points to a historical domain version that is no longer selectable, the complete-set editor becomes read-only rather than silently dropping or repinning that state.

## Effective versions

The version panel exposes immutable effective semantic history and can request deterministic resolution through the HTTP API and TypeScript SDK effective-version endpoint.

Existing semantic versions are displayed as:

```text
version
pinned template
explicit domain versions
content hash
resolved timestamp
```

The panel does not edit immutable versions.

## Authorization

Presentation visibility is derived from the existing capability families:

```text
identity-access / organization / read
identity-access / organisation-profile / read|write
identity-access / organisation-profile-template / read
identity-access / organisation-profile-domain-override / read|write
identity-access / organisation-profile-effective-version / read|write
```

UI visibility never replaces API authorization. Every mutation is submitted server-side and the API re-evaluates the capability.

## Tenant boundary

The route may receive `tenantId`, but it is accepted only through the already trusted effective administration context.

The workspace never scans databases or guesses which tenant owns an Organization.

## Verification

Administration UI adds:

```powershell
.\scripts\organisation-profile\verify-administration-ui.ps1
```

The complete validation gate remains:

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

The UI gate checks:

```text
workspace outside /identity navigation
focused component split
read-only Organization ownership boundary
protected template/domain reference selection
no Organization mutation calls
Next.js typecheck
Next.js production build
```

## Browser qualification

Run the manual browser evidence gate with:

```powershell
.\scripts\organisation-profile\verify-administration-ui-browser.ps1
```

The required browser scenarios are:

```text
1. open an existing Organization profile workspace
2. create an unpinned profile when no profile exists
3. create/select an active published template pin
4. reject forged/unavailable template references
5. replace domain overrides using catalog-derived references
6. reject forged/unpublished Enable references
7. resolve an immutable effective version
8. verify stale RowVersion conflict is surfaced without overwriting current state
9. enable/disable the profile without changing Organization identity
10. verify no OrganisationProfile section appears in Identity Access navigation
```

## Deferred

Administration UI does not add:

```text
template-authoring UI
Domain Registry administration UI
Provider Registry integration
provider credentials
qualification/hardening closure
```

Full adversarial qualification remains Qualification and Hardening.
