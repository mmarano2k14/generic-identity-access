import type { IdentityAddManagedPolicyStatementRequest, IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateManagedPolicyRequest, IdentityCreateManagedPolicyVersionRequest, IdentityManagedPolicyRecord, IdentityManagedPolicyStatementRecord, IdentityManagedPolicyVersionRecord, IdentityPublishManagedPolicyVersionRequest, IdentityUpdateManagedPolicyRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Administration client for the tenant-independent managed-policy catalog. */
export declare class IdentityAccessManagedPoliciesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyRecord[]>;
    get(context: IdentityAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord | null>;
    create(context: IdentityAdministrationContext, request: IdentityCreateManagedPolicyRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord>;
    update(context: IdentityAdministrationContext, policyIdValue: string, request: IdentityUpdateManagedPolicyRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyRecord>;
    listVersions(context: IdentityAdministrationContext, policyIdValue: string, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyVersionRecord[]>;
    getVersion(context: IdentityAdministrationContext, policyIdValue: string, policyVersion: number, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord | null>;
    createVersion(context: IdentityAdministrationContext, policyIdValue: string, request: IdentityCreateManagedPolicyVersionRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord>;
    publishVersion(context: IdentityAdministrationContext, policyIdValue: string, policyVersion: number, request?: IdentityPublishManagedPolicyVersionRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyVersionRecord>;
    listStatements(context: IdentityAdministrationContext, policyIdValue: string, policyVersion: number, signal?: AbortSignal): Promise<readonly IdentityManagedPolicyStatementRecord[]>;
    addStatement(context: IdentityAdministrationContext, policyIdValue: string, policyVersion: number, request: IdentityAddManagedPolicyStatementRequest, signal?: AbortSignal): Promise<IdentityManagedPolicyStatementRecord>;
    removeStatement(context: IdentityAdministrationContext, policyIdValue: string, policyVersion: number, statementIdValue: string, signal?: AbortSignal): Promise<boolean>;
    private basePath;
    private versionPath;
}
