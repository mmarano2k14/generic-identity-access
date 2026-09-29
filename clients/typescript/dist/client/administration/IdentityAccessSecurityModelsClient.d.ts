import type { IdentityAddScopeTypeRequest, IdentityAdministrationContext, IdentityApplicationSecurityManifestRequest, IdentityApplicationSecurityModelRecord, IdentityApplicationSecurityModelSummaryRecord, IdentityScopeTypeRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessSecurityModelsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityApplicationSecurityModelSummaryRecord[]>;
    get(context: IdentityAdministrationContext, modelVersionValue: number, signal?: AbortSignal): Promise<IdentityApplicationSecurityModelRecord | null>;
    registerManifest(context: IdentityAdministrationContext, request: IdentityApplicationSecurityManifestRequest, signal?: AbortSignal): Promise<IdentityApplicationSecurityModelRecord>;
    listScopeTypes(context: IdentityAdministrationContext, modelVersionValue: number, signal?: AbortSignal): Promise<readonly IdentityScopeTypeRecord[]>;
    addScopeType(context: IdentityAdministrationContext, modelVersionValue: number, request: IdentityAddScopeTypeRequest, signal?: AbortSignal): Promise<IdentityScopeTypeRecord>;
}
