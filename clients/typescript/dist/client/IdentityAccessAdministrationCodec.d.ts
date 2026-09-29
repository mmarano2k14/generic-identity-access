import type { IdentityAddPolicyStatementRequest, IdentityApplicationSecurityCapabilityRecord, IdentityApplicationSecurityManifestRequest, IdentityApplicationSecurityModelRecord, IdentityApplicationSecurityModelSummaryRecord, IdentityCreateResourceScopeRequest, IdentityEffectiveAdministrationContext, IdentityGroupMemberRecord, IdentityGroupPolicyBindingRecord, IdentityGroupTemplateResourceScopeRequirement, IdentityManagedGroupPolicyBindingRecord, IdentityManagedPolicyRecord, IdentityManagedPolicyStatementRecord, IdentityManagedPolicyVersionRecord, IdentityOrganizationMembershipRecord, IdentityOrganizationRecord, IdentityOrganizationResourceScopeLinkRecord, IdentityOrganizationTreeNodeRecord, IdentityGroupRecord, IdentityPolicyRecord, IdentityPolicyStatementRecord, IdentityResourceScopeRecord, IdentityScopeAuthorityGroupRecord, IdentityScopeAuthorityMemberRecord, IdentityScopeAuthorityPolicyBindingRecord, IdentityScopeTypeRecord, IdentitySessionRevocationResult, IdentityTenantMembershipCandidateRecord, IdentityTenantGroupAssignmentRecord, IdentityTenantMembershipRecord, IdentityTenantRecord, IdentityTenantUserRecord, IdentityUpdateResourceScopeRequest, IdentityPasswordCredentialMetadataRecord, IdentityUserRecord } from "../admin-contracts.js";
import { type IdentityJsonObject } from "./IdentityAccessValueCodec.js";
/** Encodes and decodes administration DTOs without owning transport or routing. */
export declare class IdentityAccessAdministrationCodec {
    static userRecord(value: unknown): IdentityUserRecord;
    static tenantRecord(value: unknown): IdentityTenantRecord;
    static effectiveAdministrationContext(value: unknown): IdentityEffectiveAdministrationContext;
    static tenantMembershipRecord(value: unknown): IdentityTenantMembershipRecord;
    static tenantUserRecord(value: unknown): IdentityTenantUserRecord;
    static groupRecord(value: unknown): IdentityGroupRecord;
    static groupTemplateResourceScopeRequirement(value: unknown): IdentityGroupTemplateResourceScopeRequirement;
    static scopeAuthorityGroupRecord(value: unknown): IdentityScopeAuthorityGroupRecord;
    static groupMemberRecord(value: unknown): IdentityGroupMemberRecord;
    static tenantGroupAssignmentRecord(value: unknown): IdentityTenantGroupAssignmentRecord;
    static tenantMembershipCandidateRecord(value: unknown): IdentityTenantMembershipCandidateRecord;
    static policyRecord(value: unknown): IdentityPolicyRecord;
    static policyStatementRecord(value: unknown): IdentityPolicyStatementRecord;
    static managedPolicyRecord(value: unknown): IdentityManagedPolicyRecord;
    static managedPolicyVersionRecord(value: unknown): IdentityManagedPolicyVersionRecord;
    static managedPolicyStatementRecord(value: unknown): IdentityManagedPolicyStatementRecord;
    static managedGroupPolicyBindingRecord(value: unknown): IdentityManagedGroupPolicyBindingRecord;
    static groupPolicyBindingRecord(value: unknown): IdentityGroupPolicyBindingRecord;
    static passwordCredentialMetadataRecord(value: unknown): IdentityPasswordCredentialMetadataRecord;
    static resourceScopeRecord(value: unknown): IdentityResourceScopeRecord;
    static organizationRecord(value: unknown): IdentityOrganizationRecord;
    static organizationTreeNodeRecord(value: unknown): IdentityOrganizationTreeNodeRecord;
    static organizationMembershipRecord(value: unknown): IdentityOrganizationMembershipRecord;
    static organizationResourceScopeLinkRecord(value: unknown): IdentityOrganizationResourceScopeLinkRecord;
    static scopeTypeRecord(value: unknown): IdentityScopeTypeRecord;
    static applicationSecurityCapabilityRecord(value: unknown): IdentityApplicationSecurityCapabilityRecord;
    static applicationSecurityModelSummaryRecord(value: unknown): IdentityApplicationSecurityModelSummaryRecord;
    static applicationSecurityModelRecord(value: unknown): IdentityApplicationSecurityModelRecord;
    static applicationSecurityManifestBody(request: IdentityApplicationSecurityManifestRequest): IdentityJsonObject;
    static sessionRevocationResult(value: unknown): IdentitySessionRevocationResult;
    static scopeAuthorityMemberRecord(value: unknown): IdentityScopeAuthorityMemberRecord;
    static scopeAuthorityPolicyBindingRecord(value: unknown): IdentityScopeAuthorityPolicyBindingRecord;
    static policyStatementBody(request: IdentityAddPolicyStatementRequest): IdentityJsonObject;
    static resourceScopeBody(request: IdentityCreateResourceScopeRequest | IdentityUpdateResourceScopeRequest, update: boolean): IdentityJsonObject;
}
