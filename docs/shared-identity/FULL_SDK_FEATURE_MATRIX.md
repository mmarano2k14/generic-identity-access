# Generic Identity — Full SDK Feature Matrix

**Status:** Application Security public SDK/UI complete  
**Source:** current controllers, TypeScript legacy client, public Generic Identity packages and shared React pages

Legend:

```text
COMPLETE                 public backend + public SDK path is usable
COMPLETE_READ            read path is usable; no mutation requirement applies to this row
SDK_PARTIAL              proven backend/legacy client is richer than the current public Generic SDK
SDK_MISSING              backend/legacy client exists but categorized public SDK is missing
SDK_UI_MISSING           backend/legacy client exists; public SDK and/or shared UI still missing
UI_MISSING               public SDK exists; shared UI is missing
NEXT_FLOW_MISSING        shared React surface exists but complete Next/server action flow is not frozen
BACKEND_MISSING          requested capability has no public backend endpoint discovered
BACKEND_API_MISSING      internal service exists but no public API endpoint was discovered
BACKEND_CONTRACT_MISSING no appropriate public backend contract exists for the intended semantics
SDK_CONTRACT_FREEZE_REQUIRED public semantics exist in pieces but categorized contract is not frozen
SDK_OPTIONAL             protocol/diagnostic surface is optional for the main administration SDK
```

## Account & Authentication

**Implementation milestone:** Account & Authentication

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| Password login | AuthenticationController.PasswordLogin | IdentityAccessAuthenticationClient.passwordLogin | GenericIdentityAuthenticationClient.passwordLogin / signIn | SignInPage | `COMPLETE` |
| Logout | AuthenticationController.Logout | IdentityAccessAuthenticationClient.logout | GenericIdentityAuthenticationClient.logout / signOut | consumer route action + shared SignIn flow | `COMPLETE` |
| Current session validation | AuthenticationController.ValidateSession | IdentityAccessAuthenticationClient.validateSession | GenericIdentityAuthenticationClient.validateSession | AccountPage can display supplied session metadata | `COMPLETE` |
| Self-service password change | SelfServiceCredentialsController.ChangePassword | IdentityAccessAuthenticationClient.changePassword | GenericIdentityAccountClient.changePassword | PasswordPage | `COMPLETE` |
| Recovery-code password reset | RecoveryPasswordResetController.ResetPassword | IdentityAccessAuthenticationClient.recoverPasswordWithCode | GenericIdentityAccountClient.recoverPasswordWithCode | RecoveryPage | `COMPLETE` |
| TOTP session step-up | MfaSessionController.VerifyTotp | IdentityAccessAuthenticationClient.verifyTotp | GenericIdentityAccountClient.verifyTotp | AuthenticationStepUpPage shell | `SDK_PARTIAL` |
| Recovery-code session step-up | MfaSessionController.VerifyRecovery | IdentityAccessAuthenticationClient.verifyRecoveryCode | GenericIdentityAccountClient.verifyRecoveryCode | AuthenticationStepUpPage shell | `SDK_PARTIAL` |
| WebAuthn session step-up | MfaSessionController Begin/Complete WebAuthn | IdentityAccessAuthenticationClient begin/complete WebAuthn step-up | GenericIdentityAccountClient begin/complete WebAuthn step-up | AuthenticationStepUpPage shell | `SDK_PARTIAL` |
| Administrative password credential lifecycle | CredentialsController Get/Create/ChangePassword | IdentityAccessCredentialsClient get/create/changePassword | GenericIdentityAccountClient.passwordCredentials | PasswordCredentialForm | `COMPLETE` |
| Self-service profile update | No dedicated self-service profile controller discovered; UsersController is administrative | No dedicated self-service client | No dedicated self-service SDK | ProfilePage is read-only | `BACKEND_CONTRACT_MISSING` |

## Directory

**Implementation milestone:** Directory

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| Users CRUD | UsersController List/Get/Create/Update | IdentityAccessUsersClient full CRUD | GenericIdentityDirectoryClient.users full CRUD | UsersPage + UserDetailsPage + UserForm | `COMPLETE` |
| Tenants CRUD | TenantsController List/Get/Create/Update | IdentityAccessTenantsClient full CRUD | GenericIdentityDirectoryClient.tenants full CRUD | TenantsPage + TenantDetailsPage + TenantForm | `COMPLETE` |
| Tenant user projection | TenantUsersController.List | IdentityAccessTenantUsersClient.list | GenericIdentityTenantUsersClient.list | Used by consumer Users route | `COMPLETE_READ` |
| Tenant memberships CRUD | TenantMembershipsController List/Get/FindByUser/Create/Update | IdentityAccessMembershipsClient full CRUD | GenericIdentityDirectoryClient.memberships full CRUD | MembershipsPage + TenantDetailsPage/UserDetailsPage + MembershipForm | `COMPLETE` |
| Membership candidate lookup / add by login | TenantMembershipCandidatesController FindByLogin/CreateMembershipByLogin | IdentityAccessMembershipCandidatesClient | GenericIdentityDirectoryClient.membershipCandidates | MembershipCandidateForm + MembershipCandidatePanel | `COMPLETE` |
| Tenant group assignments read model | TenantGroupAssignmentsController.List | IdentityAccessTenantGroupAssignmentsClient.list | GenericIdentityDirectoryClient.tenantGroupAssignments | Available for directory composition | `COMPLETE_READ` |
| Invitations | No controller/client implementation discovered | No legacy client implementation discovered | Missing | Missing | `BACKEND_MISSING` |
| User status / lifecycle administration | UsersController.Update | IdentityAccessUsersClient.update | GenericIdentityDirectoryClient.users.update | UserForm | `COMPLETE` |

## Organizations

**Implementation milestone:** Organizations

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| Organizations CRUD + tree + enable/disable | OrganizationsController | IdentityAccessOrganizationsClient | GenericIdentityOrganizationsClient.organizations full CRUD + tree + lifecycle | OrganizationsPage + OrganizationDetailsPage + OrganizationTreePage + OrganizationForm | `COMPLETE` |
| Organization memberships lifecycle | OrganizationMembershipsController | IdentityAccessOrganizationMembershipsClient | GenericIdentityOrganizationsClient.memberships full lifecycle | OrganizationMembershipsPage + OrganizationMembershipForm + OrganizationDetailsPage | `COMPLETE` |
| Organizations for tenant membership | TenantMembershipOrganizationsController.List | IdentityAccessOrganizationMembershipsClient.listForTenantMembership | GenericIdentityOrganizationsClient.memberships.listForTenantMembership | OrganizationMembershipsPage supports tenant-membership view | `COMPLETE` |
| Organization ↔ resource-scope link | OrganizationResourceScopeLinksController | IdentityAccessOrganizationResourceScopeLinksClient | GenericIdentityOrganizationsClient.resourceScopeLinks full lifecycle | OrganizationDetailsPage + OrganizationResourceScopeLinkForm | `COMPLETE` |

## Access Control

**Implementation milestone:** Access Control

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| Tenant groups + templates CRUD | GroupsController | IdentityAccessGroupsClient full surface | GenericIdentityAccessControlClient.groups full surface | GroupsPage + GroupDetailsPage + GroupAccessPage + GroupForm | `COMPLETE` |
| Group membership add/remove/list | GroupMembersController | IdentityAccessGroupsClient listMembers/addMember/removeMember | GenericIdentityAccessControlClient.groups membership lifecycle | GroupAccessPage + GroupMemberForm | `COMPLETE` |
| Tenant policies + statements + bindings | Retired legacy PoliciesController + PolicyBindingsController (`[NonController]`) | IdentityAccessPoliciesClient retained as non-composed compatibility source | Not exposed; use managedPolicies + managedPolicyBindings | No active shared UI; compatibility tombstones only | `RETIRED_COMPATIBILITY` |
| Versioned managed policies + publication | ManagedPoliciesController | IdentityAccessManagedPoliciesClient full lifecycle | GenericIdentityAccessControlClient.managedPolicies full lifecycle | PoliciesPage + PolicyDetailsPage + managed-policy forms | `COMPLETE` |
| Managed policy bindings | ManagedPolicyBindingsController | IdentityAccessManagedPolicyBindingsClient | GenericIdentityAccessControlClient.managedPolicyBindings full lifecycle | ManagedPolicyBindingsPage + ManagedPolicyBindingForm | `COMPLETE` |
| Resource scopes CRUD | ResourceScopesController | IdentityAccessResourceScopesClient | GenericIdentityAccessControlClient.resourceScopes full CRUD | ResourceScopesPage + ResourceScopeForm | `COMPLETE` |
| Identity-scope delegated authority groups/policies/bindings | IdentityScopeAdministration* controllers | IdentityAccessScopeAuthorityClient full lifecycle | GenericIdentityAccessControlClient.delegatedAuthority full lifecycle | DelegatedAuthorityPage + authority forms | `COMPLETE` |
| Authorization evaluation | AuthorizationEvaluationController | IdentityAccessAuthorizationClient.evaluate | GenericIdentityAuthorizationClient / authorization context | RequireCapability + server helpers | `COMPLETE` |
| Effective permission listing | No dedicated public endpoint discovered | No dedicated legacy method | Missing | Missing | `BACKEND_MISSING` |
| Permission explanation / grant provenance | No dedicated public endpoint discovered | No dedicated legacy method | Missing | Missing | `BACKEND_MISSING` |

## Application Security

**Implementation milestone:** Application Security

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| Application security model list/get/register | ApplicationSecurityModelsController | IdentityAccessSecurityModelsClient list/get/registerManifest | GenericIdentityApplicationSecurityClient.models list/get + manifests.register | ApplicationSecurityModelsPage + ApplicationSecurityModelDetailsPage + ApplicationSecurityManifestForm | `COMPLETE` |
| Security-model scope types list/add | ScopeTypesController | IdentityAccessSecurityModelsClient listScopeTypes/addScopeType | GenericIdentityApplicationSecurityClient.scopeTypes list/add | ApplicationScopeTypesPage + ApplicationScopeTypeForm | `COMPLETE` |
| Effective administration context | AdministrationContextController | IdentityAccessAdministrationContextClient.get | GenericIdentityApplicationSecurityClient.context.get; compatibility administration.context remains available | ApplicationSecurityContextPage | `COMPLETE` |
| Capabilities declared through security model manifest | ApplicationSecurityModelsController registration payload | IdentityAccessSecurityModelsClient.registerManifest | GenericIdentityApplicationSecurityClient.capabilities.listForModel + manifests.register | ApplicationCapabilitiesPage + ApplicationSecurityManifestForm | `COMPLETE` |
| TRN-safe permission representation | Policy/security-model contracts + server RBAC mapping | Legacy contracts/helpers | IdentityApplicationSecurityPermissionReference + createApplicationSecurityPermissionReference; no public TRN string construction | No direct TRN editing UI by design | `COMPLETE` |

## Security Operations

**Implementation milestone:** Security Operations

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| MFA provider discovery | MfaAdministrationController.ListProviders | IdentityAccessMfaClient.listProviders | GenericIdentitySecurityOperationsClient.mfa.listProviders | MfaPage provider inventory | `COMPLETE` |
| MFA policy get/create/update | MfaAdministrationController | IdentityAccessMfaClient full policy lifecycle | GenericIdentitySecurityOperationsClient.mfa full policy lifecycle | MfaPage + MfaPolicyForm | `COMPLETE` |
| Authenticator list/state/revoke/recovery-revoke | MfaAdministrationController | IdentityAccessMfaClient full surface | GenericIdentitySecurityOperationsClient.mfa full authenticator administration surface | MfaPage + AuthenticatorRevocationForm | `COMPLETE` |
| TOTP enrollment/confirmation | Provider service exists internally, but no public controller endpoint discovered | No legacy public client enrollment method | Missing | Missing | `BACKEND_API_MISSING` |
| WebAuthn registration/enrollment | Registration service exists internally, but no public controller endpoint discovered | No legacy public client registration method | Missing | Missing | `BACKEND_API_MISSING` |
| Recovery code generation/enrollment | Provider service exists internally, but no public controller endpoint discovered | No legacy public enrollment method | Missing | Missing | `BACKEND_API_MISSING` |
| Administrative session revocation by user/client | SessionsController | IdentityAccessSessionsClient revokeUser/revokeClient | GenericIdentitySecurityOperationsClient.sessions revokeUser/revokeClient | SessionRevocationForm + SessionsPage composition | `COMPLETE` |
| Administrative active session listing | No list endpoint discovered; current session validation exists separately | No list client | Missing | SessionsPage accepts supplied list but source is missing | `BACKEND_MISSING` |
| Security audit listing | SecurityAuditEventsController.List | IdentityAccessSecurityAuditClient.list | GenericIdentitySecurityOperationsClient.audit.list | SecurityAuditPage | `COMPLETE` |

## Protocol & Diagnostics

**Implementation milestone:** Protocol & Diagnostics

| Feature | Backend | Legacy TypeScript client | Current public Generic SDK | Shared UI | Status |
|---|---|---|---|---|---|
| OIDC authorization flow | OidcAuthorizationController | IdentityAccessOidcClient.authorize | Not exposed by GenericIdentityClient | No administration UI required for flow | `SDK_MISSING` |
| OIDC code exchange / refresh | OidcTokenController | IdentityAccessOidcClient exchangeAuthorizationCode/refreshTokens | Not exposed by GenericIdentityClient | No shared UI required for token endpoint | `SDK_MISSING` |
| OIDC discovery | OidcDiscoveryController | Transport/consumer HTTP surface | Not exposed by GenericIdentityClient | N/A | `SDK_OPTIONAL` |
| Liveness/readiness | HealthController | IdentityAccessSystemClient liveness/readiness | Not exposed by GenericIdentityClient | N/A | `SDK_OPTIONAL` |
| Service info | SystemController.Info | IdentityAccessSystemClient.info | Not exposed by GenericIdentityClient | N/A | `SDK_OPTIONAL` |

## Out of categorized Generic Identity SDK scope

- `OrganisationProfile` and its templates/domain composition are not promoted into the Generic Identity categories.
- Consumer-specific route names, shell design, branding and business capability declarations remain consumer responsibilities.
- A backend feature marked missing is not synthesized by the SDK.
