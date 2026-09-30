import type { IdentityOrganisationProfileDomainOverrideRecord, IdentityReplaceOrganisationProfileDomainOverridesRequest, IdentityOrganisationProfileRecord, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Organization-specific OrganisationProfile domain override client. */
export declare class IdentityAccessOrganisationProfileDomainOverridesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, signal?: AbortSignal): Promise<readonly IdentityOrganisationProfileDomainOverrideRecord[]>;
    replace(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, request: IdentityReplaceOrganisationProfileDomainOverridesRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord>;
    private static overrideBody;
}
