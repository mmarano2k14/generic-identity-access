# Administration UI and authentication UX

Version `0.51.0` starts the reusable UI/UX refinement phase after the backend/security sequence closed at `0.50.0`.

The administration host remains a Next.js App Router application consuming the class-based TypeScript client. UI refinement does not change the authorization boundary: protected reads and mutations continue to be enforced by the .NET API and external RBAC integration.

## Design ownership

The administration module owns one custom stylesheet only:

```text
examples/nextjs/admin/styles/identity-access-admin.css
```

CSS Modules, component-local `<style>` blocks, and inline React style objects remain prohibited. Light/dark tokens, responsive behavior, authentication surfaces, focus treatment, dialogs, tables, and workspace chrome stay centralized.

## Navigation behavior

The desktop administration workspace keeps a persistent left navigation rail. Route visibility is still produced by `IdentityAccessAdminUiBuilder` from the trusted administration context.

`AdminNavigationActiveLink` adds current-route presentation only. It does not make an authorization decision and it does not expose hidden routes. The active route uses `aria-current="page"`.

At narrow viewport widths the permanent rail is removed from layout and the top bar exposes a compact native-details navigation surface. The mobile surface receives the same already-filtered navigation entries as the desktop rail. No second route catalogue is introduced.

A skip-to-content link provides direct keyboard access to the main administration region.

## Authentication surface

The sign-in page remains server coordinated:

```text
browser form
    -> Next.js Server Action
    -> IdentityAccessHostSessionService
    -> TypeScript authentication client
    -> password-login
    -> OIDC Authorization Code + PKCE
    -> HTTP-only host cookies
```

The password visibility toggle changes presentation only. Password values remain form-local and are submitted only to the server action.

## Account recovery surface

`/recovery` is a public host route over the recovery-code password replacement contract added in `0.50.0`.

The route asks for:

```text
login identifier
recovery code
new password
new password confirmation
```

The browser never supplies a recovery authenticator identifier. The host calls `IdentityAccessAuthenticationClient.recoverPasswordWithCode(...)`, and the backend resolves the one active recovery authenticator for the account.

The UI preserves the backend anti-enumeration boundary. Invalid account, authenticator, and code conditions are presented as the same generic recovery rejection. Successful recovery redirects to sign-in with a non-secret success marker.

Recovery proofs and passwords are not placed in URLs, client-readable cookies, or browser configuration.

## Responsive and accessibility invariants

- visible keyboard focus remains available through the shared design system;
- active navigation exposes `aria-current`;
- the main workspace exposes a skip target;
- password visibility controls have explicit accessible labels and pressed state;
- native form semantics and Server Actions remain intact;
- reduced-motion preferences remain respected;
- dark mode remains driven by the user-agent color-scheme preference;
- mobile navigation is a presentation concern only and receives server-filtered entries.

## Validation

The primary repository verification chain continues to run `scripts/verify-typescript-source-consistency.ps1`, TypeScript tests/typecheck, and the production Next.js build.

The source-consistency gate pins the new navigation/recovery surfaces and rejects reintroduction of additional custom CSS files, inline styles, browser-exposed Identity Access configuration, or direct client-side security calls.

## 0.52.0 administration CRUD behavior

The administration workspace now exposes mutation controls where the typed backend already defines them. Server Components still load trusted data, and Client Components remain limited to presentation, filtering, dialogs, and form interaction.

Lifecycle-managed records support create and edit:

```text
users
tenants
tenant memberships
groups
policies
resource scopes
scope-authority groups
scope-authority policies
MFA policy
```

These stable records are not given a synthetic hard-delete operation. When the API models lifecycle state, the UI exposes `Active` / `Inactive` editing instead of inventing a destructive endpoint.

Relationship records that have explicit backend removal contracts expose add/remove controls:

```text
group members
group policy bindings
policy statements
scope-authority members
scope-authority policy statements
scope-authority policy bindings
MFA authenticators (revocation lifecycle)
```

Destructive relationship mutations require an explicit confirmation phrase in the server-side mutation service. The UI never treats hiding a row or closing a dialog as proof that a mutation succeeded.

All mutable versioned records carry the currently observed `expectedVersion` back to the API. A stale browser view therefore remains subject to optimistic-concurrency rejection instead of overwriting a newer mutation.

The MFA administration page can create or edit provider-neutral policy from the installed provider registry and can invoke either normal authenticator revocation or the separate lost-factor recovery revocation path. Provider-owned secret material is never rendered into forms.

The group and scope-authority management surfaces deliberately distinguish durable object identity from removable relationship edges. Removing a membership or policy-binding edge does not delete the referenced user, group, policy, tenant membership, or resource scope.
## 0.53.0 administration visual and interaction polish

The administration data model and security boundaries are unchanged. This increment concentrates on dense collection usability and mutation clarity.

`AdminEntityTable` now provides presentation-only search, lifecycle-status filtering, optional sorting, a one-action reset, and an `aria-live` visible-result count. These controls operate only on rows already returned by the server. They do not expand the server query, discover hidden records, or make an authorization decision.

Table semantics are strengthened with a caption, explicit column scopes, complete identifier tooltips, and per-cell data labels. At narrow widths the same semantic table rows are presented as compact record cards so identifiers, lifecycle state, versions, and actions remain readable without horizontal navigation.

`AdminMutationDialog` now binds its native dialog to explicit accessible title/description identifiers and exposes `aria-busy` while a Server Action is pending. Security-sensitive dialogs render a dedicated warning panel that explains the mutation is still re-authorized server-side. The existing server-side confirmation phrase remains authoritative for destructive relationship/session mutations.

The polish remains dependency-free and continues to use the single shared stylesheet. No backend endpoint, database migration, RBAC behavior, OIDC behavior, MFA-provider contract, or public TypeScript client contract changes are part of this increment.

## 0.54.0 administration detail and management context

Collection pages now distinguish browsing from managing one selected durable record. `AdminEntityTable` can receive a server-selected identifier and renders that row as selected presentation state only; the client-side table still receives no extra authority and cannot load hidden records.

`AdminRecordContext` is a reusable Server Component for stable identifier, lifecycle state, optimistic-concurrency version, bounded descriptive facts, related navigation, and mutation actions. It does not fetch data itself and does not decide authorization. Pages continue loading typed administration records through `IdentityAccessAdminRequest` and the class-based TypeScript client.

Users can move directly from a selected identity into the corresponding tenant-membership lookup or MFA security state without copying the stable user identifier. Membership results link back to the same user and MFA context. These links are navigation conveniences only; every destination reloads and re-authorizes its own server-side data.

Tenant and resource-scope details surface the lifecycle and hierarchy metadata already returned by existing contracts. Group and policy management workspaces now keep the selected durable object visible above relationship maintenance, including counts that are derived only from server-loaded relationship collections.

No hard-delete semantics, browser authorization, backend endpoint, PostgreSQL migration, RBAC rule, OIDC behavior, MFA-provider contract, or public TypeScript client contract is added by this increment.
## 0.55.0 assigned access provenance and security insight

Selected user administration can now compose an access-provenance view from the existing typed administration APIs. The host resolves the current tenant membership, scans tenant authorization groups within a bounded diagnostic window, identifies group-membership edges, loads policy bindings, resource-scope metadata, and policy statements, and renders the resulting assignment paths server-side.

The path is intentionally descriptive:

```text
User
  -> Tenant membership
  -> Group membership
  -> Group policy binding
  -> Permission policy
  -> Capability statement
  -> Optional resource-scope target
```

This view does not impersonate the selected user and does not call authorization evaluation using the administrator's current session as a substitute. It does not evaluate wildcard matching, exact resource-target applicability, external RBAC state, deny semantics, or a final allow/deny decision. Those remain authoritative in the .NET authorization boundary and external RBAC engine.

Lifecycle readiness is shown only as structural diagnostic context. Inactive user, membership, group, policy, or referenced resource-scope state is surfaced as a blocker, but a lifecycle-ready assignment is still not labeled effective or allowed.

The diagnostic group scan is intentionally bounded. If the bound is reached, the UI says the view may be incomplete rather than presenting it as exhaustive. No new backend endpoint is introduced to work around the missing arbitrary-subject authorization-explanation contract.

All provenance rendering remains server-side and uses the existing class-based TypeScript administration clients. The browser receives only the already-loaded descriptive records needed to render the page; no bearer credential, database route, RBAC store, provider secret, or authorization engine is exposed.

## 0.56.0 security administration and audit experience

The administration host now exposes `/identity/security-audit` as a read-only server-first view over the existing durable `security_events` records. The browser does not query PostgreSQL, receive database-routing information, or evaluate RBAC. The .NET API resolves the trusted route, applies the dedicated `security-audit / read` administration capability, and returns only secret-safe audit metadata.

Responsibilities remain deliberately split:

```text
PostgreSqlSecurityAuditReader
    -> persistence query only

SecurityAuditAdministrationService
    -> trusted route resolution + bounded read orchestration

SecurityAuditEventsController
    -> HTTP parsing + administration authorization metadata

IdentityAccessSecurityAuditClient
    -> typed transport + protocol decoding

IdentityAccessAdminSecurityAuditQuery
    -> browser query normalization only

IdentityAccessAdminSecurityAuditService
    -> server-side loading only

IdentityAccessAdminSecurityAuditPresentation
    -> labels/categories/classes only

IdentityAccessAdminSecurityAuditSummary
    -> visible-window aggregation only
```

No single audit service owns persistence, authorization, transport, presentation, and aggregation. Framework-required React page/components may remain function-shaped presentation adapters; durable runtime/service responsibilities remain class-based.

The audit query is bounded and newest-first. Exact filters are available for tenant, user, outcome, and correlation in the reusable host, while the public client also supports the backend event-type filter. Empty UI filters never widen authorization and never change database placement.

The visible summary counts only the records returned in the current bounded window. It is not a global security metric. Audit records can link to existing user and tenant administration contexts or to the same correlation window, but every destination re-runs its own server-side authorization.

The response intentionally excludes passwords, password hashes, session/access/refresh tokens, connection strings, secret references, provider secrets, and arbitrary payloads. No new PostgreSQL migration is required because the read path uses the existing `identity_access.security_events` schema.

## 0.57.0 sessions and security operations administration

`/identity/sessions` is now a server-first investigation and containment workspace built only from administration contracts that already exist. The Identity Access API currently exposes user-wide and registered-client-wide session revocation, but it does not expose an administration contract that enumerates active sessions. The UI therefore does not synthesize an active-session table or infer active, expired, or revoked state from missing records.

The host composes secret-safe `security_events` evidence for session-related investigation. Password-login session issuance, session revocation, user/client containment, authentication-assurance changes, refresh-token reuse detection, and refresh-token-family revocation are loaded through the existing authorized audit API in bounded windows. User and outcome filters are sent to the audit API; an optional client filter is applied only to the already-authorized records returned to the server-side host.

Responsibilities remain split deliberately:

```text
IdentityAccessAdminSessionQuery
    -> query validation and normalization only

IdentityAccessAdminSessionService
    -> server-side bounded audit loading only

IdentityAccessAdminSessionPresentation
    -> labels, categories, and presentation classes only

IdentityAccessAdminSessionSummary
    -> visible-window aggregation only

IdentityAccessAdminSessionMutationService
    -> existing session containment mutations only

AdminSessionSecurityTimeline
    -> server-rendered evidence presentation only
```

The session mutation service uses only the existing user-wide and registered-client-wide revocation client methods. Both actions require explicit `REVOKE` confirmation and are treated as successful only after the API confirms them. No exact-session delete/revoke endpoint, refresh-token-family administration endpoint, or other backend operation is invented for UI convenience.

Security-audit reading and session containment remain separately authorized. If the current administrator cannot read security-audit evidence, the page states that limitation and keeps only independently authorized containment controls available. Technical transport, timeout, availability, and protocol failures are not converted into successful revocation or a normal authorization denial.

Investigation links connect session evidence to user, MFA, security-audit, and correlation contexts. These are navigation conveniences only. Every destination resolves its own request context and server-side authorization.

Only secret-safe audit metadata is rendered. Raw session, access, and refresh tokens; token hashes; password material; MFA secrets; database credentials; routing secret references; and RBAC internal state remain outside the administration surface. The public TypeScript client contract is unchanged by this increment.

## 0.58.0 production hardening and failure UX

Administration failures are now projected through a dedicated server-only presentation class instead of being formatted inside mutation services. The classifier accepts only typed client failures or the host's own bounded validation errors and returns a serializable safe descriptor: failure category, title, message, and presentation-only recovery guidance. Raw exception causes, response bodies, URLs, tokens, credentials, and database details remain outside Client Components.

The protected mutation path distinguishes at least:

```text
validation              -> correct the submitted values
unauthenticated         -> sign in again
forbidden               -> no retry implication; operation is not authorized
HTTP 409 conflict       -> reload current state before retry
unavailable             -> dependency failure; mutation is not treated as confirmed
timeout / transport     -> outcome is not inferred; reload current state before retry
protocol failure        -> response cannot be safely accepted
configuration failure   -> deployment/operator action required
```

Timeout, transport, cancellation, and invalid-protocol responses intentionally use stronger language than a normal rejection. When no usable response proves the final server outcome, the UI does not say that the mutation definitely failed or succeeded. It instructs the administrator to reload current state before deciding whether another mutation is required.

Optimistic concurrency is surfaced explicitly. The TypeScript transport already exposes the HTTP status on its secret-safe client error, so the host can identify `409` without adding a new public client contract. A stale form is never encouraged to overwrite the newer server version.

The protected administration layout now treats a server-side `unauthenticated` result during navigation authorization as an invalid administration context and redirects to sign-in with a non-secret notice. This does not convert `403` into `401`, does not clear or rewrite RBAC decisions, and does not expose bearer-token details.

The sessions workspace has an additional degraded evidence state. If the authorized security-audit read path is temporarily unavailable or returns an unusable technical response, the page can still render its separately authorized containment controls. The missing evidence is labeled unavailable and is never interpreted as proof that a session is active, expired, or revoked. Authentication loss and request cancellation remain exceptional rather than being hidden as degraded evidence.

The architecture remains deliberately separated:

```text
Mutation Service
    -> form validation + typed mutation orchestration only

Failure Presentation
    -> typed failure classification + safe recovery guidance only

AdminActionState
    -> serializable safe descriptor only

AdminFailureFeedback
    -> browser presentation/recovery affordances only
```

No backend, database, OIDC, MFA, routing, or RBAC semantic change is part of this increment.
## 0.59.0 accessibility, responsive, and final consistency

The administration host closes the remaining presentation-level accessibility and narrow-screen consistency gaps without changing any security or data contract. Authorization, routing, authentication, session, MFA, and mutation semantics remain server-owned.

The compact mobile navigation now has one focused Client Component for disclosure state only. The component receives the already server-filtered navigation tree, exposes the native disclosure state through `aria-expanded`/`aria-controls`, and closes when the current route changes. It does not discover routes or decide visibility.

The skip link now targets a programmatically focusable `main` region so keyboard navigation lands on the administration workspace rather than only scrolling it. Mutation dialogs explicitly return focus to their trigger after controlled close or successful completion. Native dialog semantics, accessible title/description relationships, and server-confirmed mutation handling remain unchanged.

Shared administration fields now associate hint text through stable `aria-describedby` identifiers while preserving any description identifier supplied by the caller. Login and recovery forms expose pending state through `aria-busy`; the recovery-code field explicitly references its usage hint and disables browser spell correction without exposing recovery material elsewhere.

The single stylesheet adds:

```text
forced-colors support
increased-contrast support
dynamic viewport-height handling
visible focus for skip targets and generic interactive elements
very-narrow-screen dialog/action composition
```

Reduced-motion and automatic dark-mode behavior remain intact. No second stylesheet, CSS Module, inline style, browser authorization evaluator, or security configuration is introduced.


## 0.60.0 application security catalog and policy builder

The administration host adds a read-only `/identity/security-models` workspace over registered project-owned manifests. It displays model version, manifest fingerprint, RBAC project, allowed namespaces, concrete `resource / feature / action` capabilities, and descriptive TRN previews. The page does not author manifests and does not evaluate RBAC decisions.

Policy statement creation now composes through focused responsibilities:

```text
IdentityAccessAdminPolicyBuilderService
    -> registered-model/capability loading only

IdentityAccessAdminPolicyMutationService
    -> policy and policy-statement mutation orchestration only

Policy page
    -> exact registered capability selection only
```

The selected capability value carries the pinned model version plus `resource / feature / action`. Browser selection remains untrusted input; backend policy persistence continues to validate the statement against the pinned security model. Existing wildcard contracts remain available at the API boundary and wildcard evaluation remains the responsibility of the external RBAC engine.

## Administration entity references

Version `0.60.5` removes manual copy/paste of administrable relationship identifiers from the reusable host. Foreign references to users, memberships, policies, resource scopes, authority groups/policies, and similar Identity Access records use the shared `AdminEntityAutocomplete` interaction. Search is descriptive only; the hidden submitted value remains the stable identifier and the backend remains authoritative for existence, scope, concurrency, and authorization.

Reference-option construction is owned by the focused `IdentityAccessAdminEntityReferencePresentation` class. React owns filtering, keyboard interaction, selection state, and accessible presentation only. Technical identifiers that are not references to an administrable catalog (for example OIDC client ID, correlation ID, and external provider/resource ID) are intentionally not converted into entity selectors.

## 0.60.6 server-backed relationship lookup

Relationship autocompletes do not preload entity collections into the browser. `AdminEntityAutocomplete` starts searching after three characters, debounces input, cancels superseded requests, and renders at most 20 authorized results. A thin host route delegates to `IdentityAccessAdminEntityReferenceSearchService`, which uses focused typed administration clients. The .NET API normalizes the search term and PostgreSQL filters before paging.

Tenant membership creation explicitly selects both Tenant and User through this same lookup path. Existing selected IDs remain the submitted authority-bearing values; display names are descriptive only. Opaque technical identifiers that do not reference selectable Identity Access records remain plain inputs.
## 0.61.1 tenant administration context UX

Tenant-scoped workspaces now present the administration authority mode explicitly. A trusted effective context with scope-wide authority is shown as `Identity Scope Administrator`; membership-derived visibility is shown as tenant-scoped. These labels are descriptive projections only and do not replace per-operation RBAC evaluation.

Creating a tenant-scoped authorization group no longer relies on a previously selected hidden tenant. The mutation dialog always carries an explicit tenant ownership target:

```text
Identity Scope Administrator
    -> server-backed tenant lookup

Membership-limited subject, one tenant
    -> tenant resolved read-only

Membership-limited subject, multiple tenants
    -> explicit choice restricted to active membership tenants
```

The selected tenant is still submitted as requested context and is revalidated by the server-side mutation coordinator before the protected API operation. Group detail context surfaces the owning tenant identifier so an administrator cannot mistake a Local Administration Tenant group for a group owned by another tenant merely because display names are similar.

The aggregate `All authorized tenants` collection mode is intentionally implemented in the following increment; `0.61.1` first establishes the ownership-selection and authority-presentation primitives without duplicating tenant-scoped mutation semantics.


## 0.61.2 authorized tenant aggregate collections

`All authorized tenants` is a read-only collection context, not a tenant identity. The host uses a separate `tenantView=all` query dimension so `tenantId` always denotes one concrete tenant.

For an Identity Scope Administrator, aggregate candidates come from the identity-scope tenant directory. For a membership-limited subject, candidates come only from active tenant memberships. Each tenant-scoped API call is still independently authorized. Explicit `403` results are omitted from the aggregate; authentication failures and technical authorization/service failures are not converted into empty tenant results.

Aggregate rows carry their concrete tenant identity. Opening a record enters that tenant through the normal `tenantId` route, and every mutation continues to submit and revalidate one concrete tenant. The aggregate view therefore avoids duplicated workspaces without creating global groups, policies, resource scopes, memberships, or tenant-owned users.

## 0.61.3 concrete tenant mutation targeting

`All authorized tenants` remains a collection context rather than a synthetic tenant, but tenant-owned creation does not require duplicating the workspace or leaving aggregate mode. At this historical stage, Group, legacy permission-policy, and Resource Scope creation each resolved one concrete tenant ownership target inside the mutation dialog. Repository version `0.62.5` superseded the legacy policy part of this behavior, and `0.62.6` closes the remaining compatibility UI: managed policy definitions are shared and tenant-free, Groups expose only managed policy bindings, while Groups and Resource Scopes remain tenant-owned.

Identity Scope Administrators select the target through the server-backed tenant directory. Membership-limited subjects with one active membership resolve that tenant automatically; subjects with multiple active memberships must explicitly choose one and no membership is silently preselected. Every submitted tenant identifier is reconstructed through the trusted request context and re-authorized by the protected API operation.

Resource-scope creation couples optional parent lookup to the concrete tenant selected in the same dialog. Changing the target tenant resets the parent selector, and parent search is disabled until a tenant target exists. This preserves the invariant that a hierarchy edge never crosses tenant boundaries while keeping the aggregate administration surface reusable.

Aggregate collection and mutation semantics are therefore separate:

```text
tenantView=all
    = collection context only

tenantId=<concrete tenant>
    = requested ownership / operation context
    -> trusted server validation
    -> protected API authorization
```

## Shared managed-policy workspace

The Policies workspace is identity-scope/application scoped. It does not participate in tenant selection because managed-policy definitions are reusable application security definitions rather than tenant-owned records.

The workspace exposes policy metadata, draft version creation, capability selection from the registered application security model, explicit publication, and default-version selection. Published versions are rendered as immutable. Tenant-specific grant administration stays in Groups through managed policy bindings and optional tenant resource scopes.
