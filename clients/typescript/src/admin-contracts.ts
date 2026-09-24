import type { IdentityAccessCredential } from "./contracts.js";

/** Optional bounded list window for administration collection reads. */
export interface IdentityAdministrationListOptions {
  readonly offset?: number;
  readonly limit?: number;
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

export type IdentityUserStatus = 1 | 2;
export type IdentityTenantStatus = 1 | 2;
export type IdentityMembershipStatus = 1 | 2;
export type IdentityGroupStatus = 1 | 2;
export type IdentityPolicyStatus = 1 | 2;
export type IdentityResourceScopeStatus = 1 | 2;

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
  readonly groupId: string;
  readonly displayName: string;
  readonly status: IdentityGroupStatus;
  readonly version: number;
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

export interface IdentitySessionRevocationResult {
  readonly revokedCount: number;
}

export interface IdentityScopeAuthorityGroupRecord extends IdentityGroupRecord {}

export interface IdentityScopeAuthorityMemberRecord {
  readonly groupId: string;
  readonly userId: string;
}

export interface IdentityScopeAuthorityPolicyRecord extends IdentityPolicyRecord {}

export interface IdentityScopeAuthorityPolicyStatementRecord extends IdentityPolicyStatementRecord {}

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
