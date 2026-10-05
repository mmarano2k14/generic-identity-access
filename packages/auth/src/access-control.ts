import type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@identity-access/client";
import type {
  IdentityAddManagedGroupPolicyBindingRequest,
  IdentityAddManagedPolicyStatementRequest,
  IdentityAddPolicyStatementRequest,
  IdentityAdministrationListOptions,
  IdentityCreateGroupFromTemplateRequest,
  IdentityCreateGroupRequest,
  IdentityCreateManagedPolicyRequest,
  IdentityCreateManagedPolicyVersionRequest,
  IdentityCreatePolicyRequest,
  IdentityCreateResourceScopeRequest,
  IdentityGroupMemberRecord,
  IdentityGroupRecord,
  IdentityGroupTemplateResourceScopeRequirement,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
  IdentityPublishManagedPolicyVersionRequest,
  IdentityResourceScopeRecord,
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityScopeAuthorityPolicyStatementRecord,
  IdentityUpdateGroupRequest,
  IdentityUpdateManagedPolicyRequest,
  IdentityUpdatePolicyRequest,
  IdentityUpdateResourceScopeRequest,
  IdentityUpdateReusableGroupRequest,
} from "@generic-identity/contracts/access-control";
import type { GenericIdentityAuthorizationClient } from "./authorization";

export interface GenericIdentityAccessGroupsClient {
  list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupRecord[]>;
  listTemplates(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupRecord[]>;
  listTemplateScopeRequirements(context: IdentityTenantAdministrationContext, sourceTenantId: string, sourceGroupId: string, signal?: AbortSignal): Promise<readonly IdentityGroupTemplateResourceScopeRequirement[]>;
  get(context: IdentityTenantAdministrationContext, groupId: string, signal?: AbortSignal): Promise<IdentityGroupRecord | null>;
  create(context: IdentityTenantAdministrationContext, request: IdentityCreateGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
  createFromTemplate(context: IdentityTenantAdministrationContext, request: IdentityCreateGroupFromTemplateRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
  updateReusable(context: IdentityAdministrationContext, sourceTenantId: string, groupId: string, request: IdentityUpdateReusableGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
  update(context: IdentityTenantAdministrationContext, groupId: string, request: IdentityUpdateGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
  listMembers(context: IdentityTenantAdministrationContext, groupId: string, signal?: AbortSignal): Promise<readonly IdentityGroupMemberRecord[]>;
  addMember(context: IdentityTenantAdministrationContext, groupId: string, tenantMembershipId: string, signal?: AbortSignal): Promise<IdentityGroupMemberRecord>;
  removeMember(context: IdentityTenantAdministrationContext, groupId: string, tenantMembershipId: string, signal?: AbortSignal): Promise<boolean>;
}


export interface GenericIdentityManagedPoliciesCatalogClient {
  list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyRecord[]>;
  get(context: IdentityAdministrationContext, policyId: string, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord | null>;
  create(context: IdentityAdministrationContext, request: IdentityCreateManagedPolicyRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord>;
  update(context: IdentityAdministrationContext, policyId: string, request: IdentityUpdateManagedPolicyRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord>;
  listVersions(context: IdentityAdministrationContext, policyId: string, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyVersionRecord[]>;
  getVersion(context: IdentityAdministrationContext, policyId: string, policyVersion: number, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord | null>;
  createVersion(context: IdentityAdministrationContext, policyId: string, request: IdentityCreateManagedPolicyVersionRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord>;
  publishVersion(context: IdentityAdministrationContext, policyId: string, policyVersion: number, request?: IdentityPublishManagedPolicyVersionRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord>;
  listStatements(context: IdentityAdministrationContext, policyId: string, policyVersion: number, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyStatementRecord[]>;
  addStatement(context: IdentityAdministrationContext, policyId: string, policyVersion: number, request: IdentityAddManagedPolicyStatementRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyStatementRecord>;
  removeStatement(context: IdentityAdministrationContext, policyId: string, policyVersion: number, statementId: string, signal?: AbortSignal): Promise<boolean>;
}

export interface GenericIdentityManagedPolicyBindingsClient {
  listAvailablePolicies(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyRecord[]>;
  list(context: IdentityTenantAdministrationContext, groupId: string, signal?: AbortSignal): Promise<readonly IdentityManagedGroupPolicyBindingRecord[]>;
  add(context: IdentityTenantAdministrationContext, groupId: string, request: IdentityAddManagedGroupPolicyBindingRequest, signal?: AbortSignal): Promise<IdentityManagedGroupPolicyBindingRecord>;
  remove(context: IdentityTenantAdministrationContext, groupId: string, policyId: string, policyVersion: number, resourceScopeId?: string, signal?: AbortSignal): Promise<boolean>;
}

export interface GenericIdentityResourceScopesClient {
  list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityResourceScopeRecord[]>;
  get(context: IdentityTenantAdministrationContext, resourceScopeId: string, signal?: AbortSignal): Promise<IdentityResourceScopeRecord | null>;
  create(context: IdentityTenantAdministrationContext, request: IdentityCreateResourceScopeRequest, signal?: AbortSignal): Promise<IdentityResourceScopeRecord>;
  update(context: IdentityTenantAdministrationContext, resourceScopeId: string, request: IdentityUpdateResourceScopeRequest, signal?: AbortSignal): Promise<IdentityResourceScopeRecord>;
}

export interface GenericIdentityDelegatedAuthorityClient {
  listGroups(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityGroupRecord[]>;
  getGroup(context: IdentityAdministrationContext, groupId: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord | null>;
  createGroup(context: IdentityAdministrationContext, request: IdentityCreateGroupRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord>;
  updateGroup(context: IdentityAdministrationContext, groupId: string, request: IdentityUpdateGroupRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord>;
  listMembers(context: IdentityAdministrationContext, groupId: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityMemberRecord[]>;
  addMember(context: IdentityAdministrationContext, groupId: string, userId: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityMemberRecord>;
  removeMember(context: IdentityAdministrationContext, groupId: string, userId: string, signal?: AbortSignal): Promise<boolean>;
  listPolicies(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyRecord[]>;
  getPolicy(context: IdentityAdministrationContext, policyId: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord | null>;
  createPolicy(context: IdentityAdministrationContext, request: IdentityCreatePolicyRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord>;
  updatePolicy(context: IdentityAdministrationContext, policyId: string, request: IdentityUpdatePolicyRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord>;
  listPolicyStatements(context: IdentityAdministrationContext, policyId: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyStatementRecord[]>;
  addPolicyStatement(context: IdentityAdministrationContext, policyId: string, request: IdentityAddPolicyStatementRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyStatementRecord>;
  removePolicyStatement(context: IdentityAdministrationContext, policyId: string, statementId: string, signal?: AbortSignal): Promise<boolean>;
  listPolicyBindings(context: IdentityAdministrationContext, groupId: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyBindingRecord[]>;
  addPolicyBinding(context: IdentityAdministrationContext, groupId: string, policyId: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyBindingRecord>;
  removePolicyBinding(context: IdentityAdministrationContext, groupId: string, policyId: string, signal?: AbortSignal): Promise<boolean>;
}

export interface GenericIdentityAccessControlClient {
  readonly groups: GenericIdentityAccessGroupsClient;
  readonly managedPolicies: GenericIdentityManagedPoliciesCatalogClient;
  readonly managedPolicyBindings: GenericIdentityManagedPolicyBindingsClient;
  readonly resourceScopes: GenericIdentityResourceScopesClient;
  readonly delegatedAuthority: GenericIdentityDelegatedAuthorityClient;
  readonly authorization: GenericIdentityAuthorizationClient;
}
