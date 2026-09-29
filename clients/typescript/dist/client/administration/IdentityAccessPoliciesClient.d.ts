import type { IdentityAddGroupPolicyBindingRequest, IdentityAddPolicyStatementRequest, IdentityAdministrationListOptions, IdentityCreatePolicyRequest, IdentityGroupPolicyBindingRecord, IdentityPolicyRecord, IdentityPolicyStatementRecord, IdentityTenantAdministrationContext, IdentityUpdatePolicyRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessPoliciesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityPolicyRecord[]>;
    get(context: IdentityTenantAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<IdentityPolicyRecord | null>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreatePolicyRequest, signal?: AbortSignal): Promise<IdentityPolicyRecord>;
    update(context: IdentityTenantAdministrationContext, policyIdValue: string, request: IdentityUpdatePolicyRequest, signal?: AbortSignal): Promise<IdentityPolicyRecord>;
    listStatements(context: IdentityTenantAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<readonly IdentityPolicyStatementRecord[]>;
    addStatement(context: IdentityTenantAdministrationContext, policyIdValue: string, request: IdentityAddPolicyStatementRequest, signal?: AbortSignal): Promise<IdentityPolicyStatementRecord>;
    removeStatement(context: IdentityTenantAdministrationContext, policyIdValue: string, statementIdValue: string, signal?: AbortSignal): Promise<boolean>;
    listBindings(context: IdentityTenantAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityGroupPolicyBindingRecord[]>;
    addBinding(context: IdentityTenantAdministrationContext, groupIdValue: string, request: IdentityAddGroupPolicyBindingRequest, signal?: AbortSignal): Promise<IdentityGroupPolicyBindingRecord>;
    removeBinding(context: IdentityTenantAdministrationContext, groupIdValue: string, policyIdValue: string, resourceScopeIdValue?: string, signal?: AbortSignal): Promise<boolean>;
}
