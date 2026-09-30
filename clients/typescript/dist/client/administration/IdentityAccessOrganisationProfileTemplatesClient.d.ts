import type { IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateOrganisationProfileTemplateRequest, IdentityOrganisationProfileTemplateRecord, IdentityUpdateOrganisationProfileTemplateRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Reusable OrganisationProfile template-definition client. */
export declare class IdentityAccessOrganisationProfileTemplatesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganisationProfileTemplateRecord[]>;
    get(context: IdentityAdministrationContext, templateKeyValue: string, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateRecord | null>;
    create(context: IdentityAdministrationContext, request: IdentityCreateOrganisationProfileTemplateRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateRecord>;
    update(context: IdentityAdministrationContext, templateKeyValue: string, request: IdentityUpdateOrganisationProfileTemplateRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateRecord>;
    enable(context: IdentityAdministrationContext, templateKeyValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateRecord>;
    disable(context: IdentityAdministrationContext, templateKeyValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateRecord>;
}
