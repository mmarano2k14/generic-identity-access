import type { IdentityAdministrationContext, IdentityCreateOrganisationProfileTemplateVersionRequest, IdentityOrganisationProfileTemplateVersionRecord, IdentityReplaceOrganisationProfileTemplateDomainsRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Draft/publication lifecycle client for reusable OrganisationProfile template versions. */
export declare class IdentityAccessOrganisationProfileTemplateVersionsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, templateKeyValue: string, signal?: AbortSignal): Promise<readonly IdentityOrganisationProfileTemplateVersionRecord[]>;
    get(context: IdentityAdministrationContext, templateKeyValue: string, version: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateVersionRecord | null>;
    createDraft(context: IdentityAdministrationContext, templateKeyValue: string, request: IdentityCreateOrganisationProfileTemplateVersionRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateVersionRecord>;
    replaceDomains(context: IdentityAdministrationContext, templateKeyValue: string, version: number, request: IdentityReplaceOrganisationProfileTemplateDomainsRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateVersionRecord>;
    publish(context: IdentityAdministrationContext, templateKeyValue: string, version: number, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateVersionRecord>;
    retire(context: IdentityAdministrationContext, templateKeyValue: string, version: number, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileTemplateVersionRecord>;
    private static domainSelectionBody;
}
