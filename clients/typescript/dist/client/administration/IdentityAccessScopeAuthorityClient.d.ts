import type { IdentityAddPolicyStatementRequest, IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateGroupRequest, IdentityCreatePolicyRequest, IdentityScopeAuthorityGroupRecord, IdentityScopeAuthorityMemberRecord, IdentityScopeAuthorityPolicyBindingRecord, IdentityScopeAuthorityPolicyRecord, IdentityScopeAuthorityPolicyStatementRecord, IdentityUpdateGroupRequest, IdentityUpdatePolicyRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessScopeAuthorityClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    listGroups(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityGroupRecord[]>;
    getGroup(context: IdentityAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord | null>;
    createGroup(context: IdentityAdministrationContext, request: IdentityCreateGroupRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord>;
    updateGroup(context: IdentityAdministrationContext, groupIdValue: string, request: IdentityUpdateGroupRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityGroupRecord>;
    listMembers(context: IdentityAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityMemberRecord[]>;
    addMember(context: IdentityAdministrationContext, groupIdValue: string, userIdValue: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityMemberRecord>;
    removeMember(context: IdentityAdministrationContext, groupIdValue: string, userIdValue: string, signal?: AbortSignal): Promise<boolean>;
    listPolicies(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyRecord[]>;
    getPolicy(context: IdentityAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord | null>;
    createPolicy(context: IdentityAdministrationContext, request: IdentityCreatePolicyRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord>;
    updatePolicy(context: IdentityAdministrationContext, policyIdValue: string, request: IdentityUpdatePolicyRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyRecord>;
    listPolicyStatements(context: IdentityAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyStatementRecord[]>;
    addPolicyStatement(context: IdentityAdministrationContext, policyIdValue: string, request: IdentityAddPolicyStatementRequest, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyStatementRecord>;
    removePolicyStatement(context: IdentityAdministrationContext, policyIdValue: string, statementIdValue: string, signal?: AbortSignal): Promise<boolean>;
    listPolicyBindings(context: IdentityAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityScopeAuthorityPolicyBindingRecord[]>;
    addPolicyBinding(context: IdentityAdministrationContext, groupIdValue: string, policyIdValue: string, signal?: AbortSignal): Promise<IdentityScopeAuthorityPolicyBindingRecord>;
    removePolicyBinding(context: IdentityAdministrationContext, groupIdValue: string, policyIdValue: string, signal?: AbortSignal): Promise<boolean>;
}
