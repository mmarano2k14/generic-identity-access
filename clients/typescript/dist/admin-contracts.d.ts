import type { IdentityAccessCredential } from "./contracts.js";
/** Optional bounded list window for administration collection reads. */
export interface IdentityAdministrationListOptions {
    readonly offset?: number;
    readonly limit?: number;
    readonly search?: string;
}
/** Shared trusted administration boundary supplied explicitly per operation. */
export interface IdentityAdministrationContext {
    readonly identityScopeId: string;
    readonly applicationKey: string;
    readonly credential: IdentityAccessCredential;
}
/** Tenant-scoped administration boundary supplied explicitly per operation. */
export interface IdentityTenantAdministrationContext extends IdentityAdministrationContext {
    readonly tenantId: string;
}
export type IdentityAdministrationTenantVisibility = "membership-limited" | "scope-wide";
export interface IdentityEffectiveAdministrationTenantMembership {
    readonly membershipId: string;
    readonly tenantId: string;
}
export interface IdentityEffectiveAdministrationContext {
    readonly identityScopeId: string;
    readonly userId: string;
    readonly applicationKey: string;
    readonly tenantVisibility: IdentityAdministrationTenantVisibility;
    readonly activeTenantMemberships: readonly IdentityEffectiveAdministrationTenantMembership[];
}
export type IdentityUserStatus = 1 | 2;
export type IdentityTenantStatus = 1 | 2;
export type IdentityMembershipStatus = 1 | 2;
export type IdentityGroupStatus = 1 | 2;
export type IdentityPolicyStatus = 1 | 2;
export type IdentityResourceScopeStatus = 1 | 2;
export type IdentityOrganizationStatus = 1 | 2;
export type IdentityOrganizationMembershipStatus = 1 | 2;
export type IdentityOrganizationResourceScopeLinkStatus = 1 | 2;
export interface IdentityUserRecord {
    readonly userId: string;
    readonly displayName: string;
    readonly status: IdentityUserStatus;
    readonly version: number;
}
export interface IdentityCreateUserRequest {
    readonly userId?: string;
    readonly displayName: string;
    readonly status?: IdentityUserStatus;
}
export interface IdentityUpdateUserRequest {
    readonly displayName: string;
    readonly status: IdentityUserStatus;
    readonly expectedVersion: number;
}
export interface IdentityPasswordCredentialMetadataRecord {
    readonly userId: string;
    readonly loginIdentifier: string;
    readonly failedAccessCount: number;
    readonly lockoutUntil?: string;
    readonly version: number;
}
export interface IdentityCreatePasswordCredentialRequest {
    readonly loginIdentifier: string;
    readonly password: string;
}
export interface IdentityChangePasswordCredentialRequest extends IdentityCreatePasswordCredentialRequest {
    readonly expectedVersion: number;
}
export interface IdentityTenantRecord {
    readonly tenantId: string;
    readonly displayName: string;
    readonly status: IdentityTenantStatus;
    readonly version: number;
}
export interface IdentityCreateTenantRequest {
    readonly tenantId?: string;
    readonly displayName: string;
    readonly status?: IdentityTenantStatus;
}
export interface IdentityUpdateTenantRequest {
    readonly displayName: string;
    readonly status: IdentityTenantStatus;
    readonly expectedVersion: number;
}
export interface IdentityTenantMembershipRecord {
    readonly membershipId: string;
    readonly tenantId: string;
    readonly userId: string;
    readonly status: IdentityMembershipStatus;
    readonly version: number;
}
export interface IdentityTenantUserListOptions extends IdentityAdministrationListOptions {
    readonly activeMembershipsOnly?: boolean;
}
export interface IdentityTenantUserRecord {
    readonly membershipId: string;
    readonly tenantId: string;
    readonly userId: string;
    readonly displayName: string;
    readonly userStatus: IdentityUserStatus;
    readonly membershipStatus: IdentityMembershipStatus;
    readonly userVersion: number;
    readonly membershipVersion: number;
}
export interface IdentityCreateTenantMembershipRequest {
    readonly membershipId?: string;
    readonly userId: string;
    readonly status?: IdentityMembershipStatus;
}
export interface IdentityUpdateTenantMembershipRequest {
    readonly status: IdentityMembershipStatus;
    readonly expectedVersion: number;
}
export interface IdentityGroupRecord {
    readonly tenantId: string;
    readonly groupId: string;
    readonly displayName: string;
    readonly status: IdentityGroupStatus;
    readonly isTemplate: boolean;
    readonly version: number;
}
export interface IdentityGroupTemplateResourceScopeRequirement {
    readonly sourceResourceScopeId: string;
    readonly modelVersion: number;
    readonly scopeType: string;
    readonly displayName: string;
}
export interface IdentityGroupTemplateResourceScopeMapping {
    readonly sourceResourceScopeId: string;
    readonly targetResourceScopeId: string;
}
export interface IdentityCreateGroupFromTemplateRequest {
    readonly sourceTenantId: string;
    readonly sourceGroupId: string;
    readonly groupId?: string;
    readonly resourceScopeMappings?: readonly IdentityGroupTemplateResourceScopeMapping[];
}
export interface IdentityUpdateReusableGroupRequest {
    readonly displayName: string;
    readonly status: IdentityGroupStatus;
    readonly isTemplate: boolean;
    readonly expectedVersion: number;
}
export interface IdentityCreateGroupRequest {
    readonly groupId?: string;
    readonly displayName: string;
    readonly status?: IdentityGroupStatus;
}
export interface IdentityUpdateGroupRequest {
    readonly displayName: string;
    readonly status: IdentityGroupStatus;
    readonly expectedVersion: number;
}
export interface IdentityGroupMemberRecord {
    readonly tenantMembershipId: string;
    readonly userId: string;
}
export interface IdentityTenantGroupAssignmentRecord {
    readonly groupId: string;
    readonly tenantMembershipId: string;
    readonly userId: string;
}
export interface IdentityTenantMembershipCandidateRecord {
    readonly userId: string;
    readonly displayName: string;
    readonly userStatus: IdentityUserStatus;
    readonly existingMembershipId?: string;
    readonly existingMembershipStatus?: IdentityMembershipStatus;
}
export interface IdentityPolicyRecord {
    readonly policyId: string;
    readonly displayName: string;
    readonly status: IdentityPolicyStatus;
    readonly version: number;
}
export interface IdentityCreatePolicyRequest {
    readonly policyId?: string;
    readonly displayName: string;
    readonly status?: IdentityPolicyStatus;
}
export interface IdentityUpdatePolicyRequest {
    readonly displayName: string;
    readonly status: IdentityPolicyStatus;
    readonly expectedVersion: number;
}
export interface IdentityPolicyStatementRecord {
    readonly statementId: string;
    readonly modelVersion: number;
    readonly resource: string;
    readonly feature: string;
    readonly action: string;
}
export interface IdentityAddPolicyStatementRequest {
    readonly statementId?: string;
    readonly modelVersion: number;
    readonly resource: string;
    readonly feature: string;
    readonly action: string;
}
export interface IdentityManagedPolicyRecord {
    readonly policyId: string;
    readonly policyKey: string;
    readonly displayName: string;
    readonly status: IdentityPolicyStatus;
    readonly defaultVersion?: number;
    readonly version: number;
}
export interface IdentityCreateManagedPolicyRequest {
    readonly policyId?: string;
    readonly policyKey: string;
    readonly displayName: string;
    readonly status?: IdentityPolicyStatus;
}
export interface IdentityUpdateManagedPolicyRequest {
    readonly policyKey: string;
    readonly displayName: string;
    readonly status: IdentityPolicyStatus;
    readonly expectedVersion: number;
}
export interface IdentityManagedPolicyVersionRecord {
    readonly policyId: string;
    readonly policyVersion: number;
    readonly modelVersion: number;
    readonly publishedAt?: string;
}
export interface IdentityCreateManagedPolicyVersionRequest {
    readonly policyVersion: number;
    readonly modelVersion: number;
}
export interface IdentityPublishManagedPolicyVersionRequest {
    readonly makeDefault?: boolean;
}
export interface IdentityManagedPolicyStatementRecord {
    readonly statementId: string;
    readonly policyVersion: number;
    readonly modelVersion: number;
    readonly resource: string;
    readonly feature: string;
    readonly action: string;
}
export interface IdentityAddManagedPolicyStatementRequest {
    readonly statementId?: string;
    readonly resource: string;
    readonly feature: string;
    readonly action: string;
}
export interface IdentityManagedGroupPolicyBindingRecord {
    readonly groupId: string;
    readonly policyId: string;
    readonly policyVersion: number;
    readonly resourceScopeId?: string;
    readonly includeDescendants: boolean;
}
export interface IdentityAddManagedGroupPolicyBindingRequest {
    readonly policyId: string;
    readonly policyVersion?: number;
    readonly resourceScopeId?: string;
    readonly includeDescendants?: boolean;
}
export interface IdentityGroupPolicyBindingRecord {
    readonly groupId: string;
    readonly policyId: string;
    readonly resourceScopeId?: string;
    readonly includeDescendants: boolean;
}
export interface IdentityAddGroupPolicyBindingRequest {
    readonly policyId: string;
    readonly resourceScopeId?: string;
    readonly includeDescendants?: boolean;
}
export interface IdentityResourceScopeRecord {
    readonly resourceScopeId: string;
    readonly modelVersion: number;
    readonly scopeType: string;
    readonly externalResourceId: string;
    readonly displayName: string;
    readonly parentResourceScopeId?: string;
    readonly status: IdentityResourceScopeStatus;
    readonly version: number;
}
export interface IdentityCreateResourceScopeRequest {
    readonly resourceScopeId?: string;
    readonly modelVersion: number;
    readonly scopeType: string;
    readonly externalResourceId: string;
    readonly displayName: string;
    readonly parentResourceScopeId?: string;
    readonly status?: IdentityResourceScopeStatus;
}
export interface IdentityUpdateResourceScopeRequest {
    readonly modelVersion: number;
    readonly scopeType: string;
    readonly externalResourceId: string;
    readonly displayName: string;
    readonly parentResourceScopeId?: string;
    readonly status: IdentityResourceScopeStatus;
    readonly expectedVersion: number;
}
export interface IdentityOrganizationRecord {
    readonly identityScopeId: string;
    readonly tenantId: string;
    readonly organizationId: string;
    readonly organizationKey: string;
    readonly displayName: string;
    readonly organizationType: string;
    readonly parentOrganizationId?: string;
    readonly status: IdentityOrganizationStatus;
    readonly rowVersion: number;
    readonly createdAt: string;
    readonly updatedAt: string;
}
export interface IdentityOrganizationTreeNodeRecord {
    readonly organization: IdentityOrganizationRecord;
    readonly children: readonly IdentityOrganizationTreeNodeRecord[];
}
export interface IdentityCreateOrganizationRequest {
    readonly organizationId?: string;
    readonly organizationKey: string;
    readonly displayName: string;
    readonly organizationType: string;
    readonly parentOrganizationId?: string;
}
export interface IdentityUpdateOrganizationRequest {
    readonly displayName: string;
    readonly organizationType: string;
    readonly parentOrganizationId?: string;
    readonly expectedRowVersion: number;
}
export interface IdentityOrganizationLifecycleRequest {
    readonly expectedRowVersion: number;
}
export interface IdentityOrganizationMembershipRecord {
    readonly identityScopeId: string;
    readonly tenantId: string;
    readonly organizationId: string;
    readonly tenantMembershipId: string;
    readonly status: IdentityOrganizationMembershipStatus;
    readonly rowVersion: number;
    readonly createdAt: string;
    readonly updatedAt: string;
}
export interface IdentityCreateOrganizationMembershipRequest {
    readonly tenantMembershipId: string;
}
export interface IdentityOrganizationMembershipLifecycleRequest {
    readonly expectedRowVersion: number;
}
export interface IdentityOrganizationResourceScopeLinkRecord {
    readonly organizationId: string;
    readonly applicationKey: string;
    readonly resourceScopeId: string;
    readonly scopeType: string;
    readonly modelVersion: number;
    readonly status: IdentityOrganizationResourceScopeLinkStatus;
    readonly rowVersion: number;
    readonly createdAt: string;
    readonly updatedAt: string;
}
export interface IdentityCreateOrganizationResourceScopeLinkRequest {
    readonly resourceScopeId: string;
}
export interface IdentityUpdateOrganizationResourceScopeLinkRequest {
    readonly resourceScopeId: string;
    readonly expectedRowVersion: number;
}
export interface IdentityScopeTypeRecord {
    readonly key: string;
    readonly displayName: string;
    readonly parentKey?: string;
    readonly canAttachToTenant: boolean;
}
export interface IdentityAddScopeTypeRequest {
    readonly key: string;
    readonly displayName: string;
    readonly parentKey?: string;
    readonly canAttachToTenant: boolean;
}
export interface IdentityApplicationSecurityCapabilityRecord {
    readonly resource: string;
    readonly feature: string;
    readonly action: string;
    readonly displayName: string;
}
export interface IdentityApplicationSecurityModelSummaryRecord {
    readonly schemaVersion: number;
    readonly applicationKey: string;
    readonly modelVersion: number;
    readonly rbacProject: string;
    readonly rbacNamespaces: readonly string[];
    readonly manifestSha256: string;
    readonly capabilityCount: number;
}
export interface IdentityApplicationSecurityModelRecord extends IdentityApplicationSecurityModelSummaryRecord {
    readonly capabilities: readonly IdentityApplicationSecurityCapabilityRecord[];
}
export interface IdentityApplicationSecurityManifestAction {
    readonly name: string;
    readonly displayName: string;
}
export interface IdentityApplicationSecurityManifestFeature {
    readonly name: string;
    readonly actions: readonly IdentityApplicationSecurityManifestAction[];
}
export interface IdentityApplicationSecurityManifestResource {
    readonly name: string;
    readonly features: readonly IdentityApplicationSecurityManifestFeature[];
}
export interface IdentityApplicationSecurityManifestRequest {
    readonly schemaVersion: 1;
    readonly applicationKey: string;
    readonly modelVersion: number;
    readonly rbac: {
        readonly project: string;
        readonly namespaces: readonly string[];
    };
    readonly resources: readonly IdentityApplicationSecurityManifestResource[];
}
export interface IdentitySessionRevocationResult {
    readonly revokedCount: number;
}
export type IdentitySecurityAuditOutcome = "Succeeded" | "Denied" | "Failed";
export interface IdentitySecurityAuditQuery {
    readonly tenantId?: string;
    readonly userId?: string;
    readonly eventType?: string;
    readonly outcome?: IdentitySecurityAuditOutcome;
    readonly correlationId?: string;
    readonly offset?: number;
    readonly limit?: number;
}
export interface IdentitySecurityAuditRecord {
    readonly eventId: string;
    readonly occurredAt: string;
    readonly eventType: string;
    readonly outcome: IdentitySecurityAuditOutcome;
    readonly identityScopeId: string;
    readonly tenantId?: string;
    readonly userId?: string;
    readonly applicationKey?: string;
    readonly clientId?: string;
    readonly targetId?: string;
    readonly reasonCode?: string;
    readonly correlationId?: string;
}
export interface IdentityScopeAuthorityGroupRecord {
    readonly groupId: string;
    readonly displayName: string;
    readonly status: IdentityGroupStatus;
    readonly version: number;
}
export interface IdentityScopeAuthorityMemberRecord {
    readonly groupId: string;
    readonly userId: string;
}
export interface IdentityScopeAuthorityPolicyRecord extends IdentityPolicyRecord {
}
export interface IdentityScopeAuthorityPolicyStatementRecord extends IdentityPolicyStatementRecord {
}
export interface IdentityScopeAuthorityPolicyBindingRecord {
    readonly groupId: string;
    readonly policyId: string;
}
export type IdentityMfaPolicyMode = 1 | 2 | 3;
export type IdentityAuthenticatorStatus = 1 | 2 | 3;
export type IdentityMfaProviderCapability = "enrollment" | "verification" | "recovery";
export interface IdentityMfaProviderRecord {
    readonly key: string;
    readonly displayName: string;
    readonly capabilities: readonly IdentityMfaProviderCapability[];
}
export interface IdentityMfaPolicyRecord {
    readonly mode: IdentityMfaPolicyMode;
    readonly allowedProviders: readonly string[];
    readonly version: number;
}
export interface IdentityCreateMfaPolicyRequest {
    readonly mode: IdentityMfaPolicyMode;
    readonly allowedProviders: readonly string[];
}
export interface IdentityUpdateMfaPolicyRequest {
    readonly mode: IdentityMfaPolicyMode;
    readonly allowedProviders: readonly string[];
    readonly expectedVersion: number;
}
export interface IdentityMfaUserSecurityState {
    readonly policyConfigured: boolean;
    readonly policyMode?: IdentityMfaPolicyMode;
    readonly mfaRequired: boolean;
    readonly hasActiveVerificationFactor: boolean;
    readonly hasActivePrimaryFactor: boolean;
    readonly hasActiveRecoveryFactor: boolean;
    readonly satisfiesCurrentPolicy: boolean;
    readonly activeVerificationProviders: readonly string[];
    readonly activePrimaryProviders: readonly string[];
    readonly activeRecoveryProviders: readonly string[];
}
export interface IdentityUserAuthenticatorRecord {
    readonly authenticatorId: string;
    readonly userId: string;
    readonly providerKey: string;
    readonly displayName: string;
    readonly status: IdentityAuthenticatorStatus;
    readonly createdAt: string;
    readonly confirmedAt?: string;
    readonly lastUsedAt?: string;
    readonly revokedAt?: string;
    readonly version: number;
}
