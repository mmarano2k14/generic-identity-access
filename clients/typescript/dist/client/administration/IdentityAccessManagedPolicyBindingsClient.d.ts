import type { IdentityAddManagedGroupPolicyBindingRequest, IdentityAdministrationListOptions, IdentityManagedGroupPolicyBindingRecord, IdentityManagedPolicyRecord, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Tenant-scoped client for attaching shared managed-policy versions to groups. */
export declare class IdentityAccessManagedPolicyBindingsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    listAvailablePolicies(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyRecord[]>;
    list(context: IdentityTenantAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityManagedGroupPolicyBindingRecord[]>;
    add(context: IdentityTenantAdministrationContext, groupIdValue: string, request: IdentityAddManagedGroupPolicyBindingRequest, signal?: AbortSignal): Promise<IdentityManagedGroupPolicyBindingRecord>;
    remove(context: IdentityTenantAdministrationContext, groupIdValue: string, policyIdValue: string, policyVersion: number, resourceScopeIdValue?: string, signal?: AbortSignal): Promise<boolean>;
    private basePath;
}
