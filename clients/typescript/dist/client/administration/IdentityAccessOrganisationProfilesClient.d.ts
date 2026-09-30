import type { IdentityAdministrationListOptions, IdentityCreateOrganisationProfileRequest, IdentityOrganisationProfileRecord, IdentitySetOrganisationProfileTemplateRequest, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Mutable tenant-local OrganisationProfile definition client. */
export declare class IdentityAccessOrganisationProfilesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganisationProfileRecord[]>;
    get(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord | null>;
    getByOrganization(context: IdentityTenantAdministrationContext, organizationIdValue: string, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord | null>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreateOrganisationProfileRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord>;
    setTemplate(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, request: IdentitySetOrganisationProfileTemplateRequest, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord>;
    enable(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord>;
    disable(context: IdentityTenantAdministrationContext, organisationProfileIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganisationProfileRecord>;
    private static templatePinBody;
}
